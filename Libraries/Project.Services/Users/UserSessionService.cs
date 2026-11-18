using System.Security.Cryptography;
using System.Text;
using LinqToDB;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Project.Core.Caching;
using Project.Core.Configuration;
using Project.Core.Domain.Users;
using Project.Data;
using Project.Services.Users.DataModels;

namespace Project.Services.Users;

public class UserSessionService : IUserSessionService
{
    #region Fields

    private const string DEFAULT_APP_TYPE = "default";
    private static readonly TimeSpan _rotationGrace = TimeSpan.FromSeconds(10); // a retried/parallel call is not "theft"

    private readonly IRepository<UserSession> _sessionRepository;
    private readonly IStaticCacheManager _staticCacheManager;
    private readonly ILocker _locker;
    private readonly AppSettings _appSettings;
    private readonly ILogger<UserSessionService> _logger;

    #endregion

    #region Ctor

    public UserSessionService(IRepository<UserSession> sessionRepository,
        IStaticCacheManager staticCacheManager,
        ILocker locker,
        AppSettings appSettings,
        ILogger<UserSessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _staticCacheManager = staticCacheManager;
        _locker = locker;
        _appSettings = appSettings;
        _logger = logger;
    }

    #endregion

    #region Utilities

    private TimeSpan RefreshLifetime =>
        TimeSpan.FromDays(_appSettings.Get<JwtConfig>().RefreshTokenExpirationInDays);

    //URL-safe (no + / =) so the token survives query strings and form posts unchanged
    private static string GenerateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    private static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    //swagger sends the literal "string" as default; never store placeholders
    private static string Clean(string value, string fallback, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("string", StringComparison.OrdinalIgnoreCase))
            return fallback;

        value = value.Trim();
        return value.Length > maxLength ? value[..maxLength] : value;
    }

    private async Task SetActiveCacheAsync(UserSession session)
    {
        var remaining = (session.ExpiresAtUtc - DateTime.UtcNow).TotalMinutes;
        var minutes = (int)Math.Ceiling(Math.Min(remaining, UserSessionDefaults.ActiveSessionCacheMinutes));
        if (minutes <= 0)
            return;

        var key = _staticCacheManager.PrepareKeyForDefaultCache(UserSessionDefaults.ActiveSessionCacheKey,
            session.UserId, session.SessionId);
        key.CacheTime = minutes;

        await _staticCacheManager.SetAsync(key, true);
    }

    private Task RemoveActiveCacheAsync(long userId, Guid sessionId) =>
        _staticCacheManager.RemoveAsync(UserSessionDefaults.ActiveSessionCacheKey, userId, sessionId);

    //ILocker does not wait: it returns false when somebody else holds the lock, so retry for a moment
    private async Task<bool> RunWithLockAsync(string resource, Func<Task> action)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (await _locker.PerformActionWithLockAsync(resource, TimeSpan.FromSeconds(30), action))
                return true;

            await Task.Delay(200);
        }

        return false;
    }

    #endregion

    #region Methods

    public async Task<NewSession> CreateSessionAsync(long userId, SessionInfo info)
    {
        var appType = Clean(info.AppType, DEFAULT_APP_TYPE, 50).ToLowerInvariant();
        var singleDevice = !info.MultiDeviceLoginEnabled;
        var refreshToken = GenerateRefreshToken();
        var now = DateTime.UtcNow;

        var session = new UserSession
        {
            UserId = userId,
            AppType = appType,
            IsSingleDevice = singleDevice,
            SessionId = Guid.NewGuid(),
            RefreshTokenHash = Hash(refreshToken),
            CreatedOnUtc = now,
            ExpiresAtUtc = now.Add(RefreshLifetime),
            DeviceId = Clean(info.DeviceId, "unknown", 250),
            DeviceName = Clean(info.DeviceName, "unknown", 250),
            IpAddress = Clean(info.IpAddress, null, 50),
            UserAgent = Clean(info.UserAgent, null, 500)
        };

        IList<UserSession> replaced = new List<UserSession>();

        //serialise concurrent logins of the same user/app (ILocker is distributed when Redis is configured);
        //the unique filtered index (UserId, AppType) is the database-level backstop
        var executed = await RunWithLockAsync(string.Format(UserSessionDefaults.LoginLockKey, userId, appType), async () =>
        {
            if (singleDevice)
            {
                replaced = await _sessionRepository.GetAllAsync(query => query.Where(s => s.UserId == userId && s.AppType == appType && s.RevokedOnUtc == null));

                foreach (var old in replaced)
                {
                    old.RevokedOnUtc = now;
                    old.RevokedReason = "Logged in from another device";
                }

                if (replaced.Count > 0)
                    await _sessionRepository.UpdateAsync(replaced);
            }

            await _sessionRepository.InsertAsync(session);
        });

        if (!executed)
            throw new InvalidOperationException("Could not start a session right now. Please try again.");

        //evict replaced sessions (their access tokens fail immediately), prime the new one
        foreach (var old in replaced)
            await RemoveActiveCacheAsync(old.UserId, old.SessionId);
        await SetActiveCacheAsync(session);

        _logger.LogInformation("Session created. UserId={UserId} AppType={AppType} SessionId={SessionId} DeviceId={DeviceId} Ip={Ip} SingleDevice={SingleDevice} Replaced={Replaced}", userId, appType, session.SessionId, session.DeviceId, session.IpAddress, singleDevice, replaced.Count);

        return new NewSession(session.SessionId, refreshToken, session.ExpiresAtUtc);
    }

    public async Task<(UserSession Session, string NewRefreshToken)?> RotateAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var hash = Hash(refreshToken.Trim());
        var now = DateTime.UtcNow;

        var matches = await _sessionRepository.GetAllAsync(query =>
            query.Where(s => s.RefreshTokenHash == hash || s.PreviousRefreshTokenHash == hash));
        var session = matches.FirstOrDefault();

        if (session == null)
        {
            _logger.LogWarning("Refresh rejected: no session matches the supplied token (wrong token type or stale token).");
            return null;
        }

        if (session.RevokedOnUtc != null || session.ExpiresAtUtc <= now)
            return null;

        //the PREVIOUS token was presented again
        if (session.RefreshTokenHash != hash)
        {
            //right after a rotation this is almost always a retried/parallel call: reject it, but keep the session
            if (session.LastRefreshedOnUtc.HasValue && now - session.LastRefreshedOnUtc.Value <= _rotationGrace)
            {
                _logger.LogWarning("Refresh ignored (parallel call within grace window). SessionId={SessionId}", session.SessionId);
                return null;
            }

            _logger.LogWarning("Refresh token reuse detected. UserId={UserId} SessionId={SessionId}", session.UserId, session.SessionId);
            await RevokeAsync(session.UserId, session.SessionId, "Refresh token reuse detected");
            return null;
        }

        var newToken = GenerateRefreshToken();
        var newHash = Hash(newToken);
        var newExpiry = now.Add(RefreshLifetime);

        //atomic compare-and-swap in SQL: only ONE of several parallel calls with the same token can match the WHERE
        var updated = await _sessionRepository.Table
            .Where(s => s.Id == session.Id && s.RefreshTokenHash == hash && s.RevokedOnUtc == null)
            .Set(s => s.PreviousRefreshTokenHash, s => s.RefreshTokenHash)
            .Set(s => s.RefreshTokenHash, newHash)
            .Set(s => s.LastRefreshedOnUtc, (DateTime?)now)
            .Set(s => s.ExpiresAtUtc, newExpiry)
            .UpdateAsync();

        if (updated != 1)
        {
            _logger.LogWarning("Refresh lost the race to a parallel call. SessionId={SessionId}", session.SessionId);
            return null;
        }

        session.PreviousRefreshTokenHash = hash;
        session.RefreshTokenHash = newHash;
        session.LastRefreshedOnUtc = now;
        session.ExpiresAtUtc = newExpiry;

        await SetActiveCacheAsync(session);

        _logger.LogInformation("Refresh token rotated. UserId={UserId} SessionId={SessionId}", session.UserId, session.SessionId);
        return (session, newToken);
    }

    public async Task RevokeAsync(long userId, Guid sessionId, string reason)
    {
        //both conditions: a caller can only revoke their own session
        var revoked = await _sessionRepository.Table
            .Where(s => s.UserId == userId && s.SessionId == sessionId && s.RevokedOnUtc == null)
            .Set(s => s.RevokedOnUtc, (DateTime?)DateTime.UtcNow)
            .Set(s => s.RevokedReason, reason)
            .UpdateAsync();

        await RemoveActiveCacheAsync(userId, sessionId); //only THIS session's entry

        if (revoked > 0)
            _logger.LogInformation("Session revoked. UserId={UserId} SessionId={SessionId} Reason={Reason}", userId, sessionId, reason);
    }

    public async Task<bool> IsSessionActiveAsync(long userId, Guid sessionId)
    {
        //the flag is only ever stored for active sessions; revoke/replace removes it, and it expires within minutes
        var key = _staticCacheManager.PrepareKeyForDefaultCache(UserSessionDefaults.ActiveSessionCacheKey, userId, sessionId);
        if (await _staticCacheManager.GetAsync<bool>(key))
            return true;

        var now = DateTime.UtcNow;
        var sessions = await _sessionRepository.GetAllAsync(query => query.Where(s => s.UserId == userId && s.SessionId == sessionId && s.RevokedOnUtc == null && s.ExpiresAtUtc > now));
        var session = sessions.FirstOrDefault();

        if (session == null)
            return false;

        await SetActiveCacheAsync(session);
        return true;
    }

    /// <summary>
    /// Deletes sessions that expired / were revoked more than <paramref name="retentionDays"/> ago
    /// </summary>
    public async Task CleanupAsync(int retentionDays = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        var deleted = await _sessionRepository.DeleteAsync(s => (s.RevokedOnUtc != null && s.RevokedOnUtc < cutoff) || s.ExpiresAtUtc < cutoff);

        _logger.LogInformation("Session cleanup removed {Count} rows older than {Cutoff:u}", deleted, cutoff);
    }

    #endregion
}