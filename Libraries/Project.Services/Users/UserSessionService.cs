using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Project.Core.Configuration;
using Project.Core.Domain.Users;
using Project.Data;
using Project.Services.Users.DataModels;

namespace Project.Services.Users;

public class UserSessionService : IUserSessionService
{
    #region Fields

    private const string DEFAULT_APP_TYPE = "default";
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);      // short TTL: a missed invalidation heals itself
    private static readonly TimeSpan _rotationGrace = TimeSpan.FromSeconds(10); // parallel refresh calls are not "theft"

    // bounded set of locks (serialises login/refresh per user/session inside this process)
    private static readonly SemaphoreSlim[] _gates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    private readonly IRepository<UserSession> _userSessionRepository;
    private readonly IDistributedCache _cache;
    private readonly AppSettings _appSettings;
    private readonly ILogger<UserSessionService> _logger;

    #endregion

    #region Ctor

    public UserSessionService(IRepository<UserSession> userSessionRepository,
        IDistributedCache cache,
        AppSettings appSettings,
        ILogger<UserSessionService> logger)
    {
        _userSessionRepository = userSessionRepository;
        _cache = cache;
        _appSettings = appSettings;
        _logger = logger;
    }

    #endregion

    #region Utilities

    // cache is keyed by SESSION, so revoking one session can never evict another one
    private static string CacheKey(long userId, Guid sessionId) => $"user-session:{userId}:{sessionId}";

    private TimeSpan RefreshLifetime => TimeSpan.FromDays(_appSettings.Get<JwtConfig>().RefreshTokenExpirationInDays);

    private static SemaphoreSlim GateFor(string key) => _gates[(uint)key.GetHashCode() % (uint)_gates.Length];

    private Task SetCacheAsync(UserSession session)
    {
        var ttl = session.ExpiresAtUtc - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
            return Task.CompletedTask;
        if (ttl > _cacheTtl)
            ttl = _cacheTtl;

        return _cache.SetStringAsync(CacheKey(session.UserId, session.SessionId), "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
    }

    private Task RemoveCacheAsync(UserSession session) =>
        _cache.RemoveAsync(CacheKey(session.UserId, session.SessionId));

    // URL-safe (no + / =) so the token survives query strings / form posts unchanged
    private static string GenerateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    private static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    // swagger sends the literal "string" as a default value; never store placeholders
    private static string Clean(string value, string fallback, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("string", StringComparison.OrdinalIgnoreCase))
            return fallback;

        value = value.Trim();
        return value.Length > maxLength ? value[..maxLength] : value;
    }

    #endregion

    #region Methods

    public async Task<NewSession> CreateSessionAsync(long userId, SessionInfo info)
    {
        var appType = Clean(info.AppType, DEFAULT_APP_TYPE, 50).ToLowerInvariant();
        var refreshToken = GenerateRefreshToken();
        var now = DateTime.UtcNow;

        var session = new UserSession
        {
            UserId = userId,
            AppType = appType,
            SessionId = Guid.NewGuid(),
            RefreshTokenHash = Hash(refreshToken),
            CreatedOnUtc = now,
            ExpiresAtUtc = now.Add(RefreshLifetime),
            DeviceId = Clean(info.DeviceId, "unknown", 200),
            DeviceName = Clean(info.DeviceName, "unknown", 200),
            IpAddress = Clean(info.IpAddress, null, 64),
            UserAgent = Clean(info.UserAgent, null, 500)
        };

        IList<UserSession> revoked;

        var gate = GateFor($"login:{userId}:{appType}");
        await gate.WaitAsync();
        try
        {
            // revoke old + insert new as ONE unit (if this fails nothing changes)
            // If you get "This platform does not support distributed transactions", remove the scope;
            // the unique filtered index (UserId, AppType) still guarantees a single active session.
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled);

            //1) single device login: revoke the user's active session(s) of THIS app type only
            revoked = await _userSessionRepository.GetAllAsync(query =>
                query.Where(s => s.UserId == userId && s.AppType == appType && s.RevokedOnUtc == null));

            if (revoked.Count > 0)
            {
                foreach (var s in revoked)
                {
                    s.RevokedOnUtc = now;
                    s.RevokedReason = "Logged in from another device";
                }
                await _userSessionRepository.UpdateAsync(revoked);
            }

            //2) create the new session
            await _userSessionRepository.InsertAsync(session);
            scope.Complete();
        }
        finally
        {
            gate.Release();
        }

        //3) cache: evict the replaced sessions (their access tokens now fail), prime the new one
        foreach (var old in revoked)
            await RemoveCacheAsync(old);
        await SetCacheAsync(session);

        _logger.LogInformation("Session created. UserId={UserId} AppType={AppType} SessionId={SessionId} DeviceId={DeviceId} Ip={Ip} Replaced={Replaced}",
            userId, appType, session.SessionId, session.DeviceId, session.IpAddress, revoked.Count);

        return new NewSession(session.SessionId, refreshToken, session.ExpiresAtUtc);
    }

    public async Task<(UserSession Session, string NewRefreshToken)?> RotateAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var hash = refreshToken;

        var matches = await _userSessionRepository.GetAllAsync(query =>
            query.Where(s => s.RefreshTokenHash == hash || s.PreviousRefreshTokenHash == hash));
        var found = matches.FirstOrDefault();
        if (found == null)
        {
            _logger.LogWarning("Refresh rejected: no session matches the supplied token (wrong token type or stale token).");
            return null;
        }

        // serialise refreshes of the same session, then re-read the row inside the lock
        var gate = GateFor($"refresh:{found.SessionId}");
        await gate.WaitAsync();
        try
        {
            matches = await _userSessionRepository.GetAllAsync(query =>
                query.Where(s => s.RefreshTokenHash == hash || s.PreviousRefreshTokenHash == hash));
            var session = matches.FirstOrDefault();

            if (session == null || session.RevokedOnUtc != null || session.ExpiresAtUtc <= DateTime.UtcNow)
                return null;

            if (session.RefreshTokenHash != hash && session.PreviousRefreshTokenHash == hash)
            {
                // old token presented again. Right after a rotation it is most likely a parallel/retried call: do not punish it.
                if (session.LastRefreshedOnUtc.HasValue && DateTime.UtcNow - session.LastRefreshedOnUtc.Value <= _rotationGrace)
                {
                    _logger.LogWarning("Refresh ignored (parallel call within grace window). SessionId={SessionId}", session.SessionId);
                    return null;
                }

                _logger.LogWarning("Refresh token reuse detected. UserId={UserId} SessionId={SessionId}", session.UserId, session.SessionId);
                await RevokeAsync(session.UserId, session.SessionId, "Refresh token reuse detected");
                return null;
            }

            var newToken = GenerateRefreshToken();
            session.PreviousRefreshTokenHash = session.RefreshTokenHash;
            session.RefreshTokenHash = Hash(newToken);
            session.LastRefreshedOnUtc = DateTime.UtcNow;
            session.ExpiresAtUtc = DateTime.UtcNow.Add(RefreshLifetime); // sliding expiry

            await _userSessionRepository.UpdateAsync(session);
            await SetCacheAsync(session);

            _logger.LogInformation("Refresh token rotated. UserId={UserId} SessionId={SessionId}", session.UserId, session.SessionId);
            return (session, newToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RevokeAsync(long userId, Guid sessionId, string reason)
    {
        // both conditions: a caller can only revoke their own session
        var sessions = await _userSessionRepository.GetAllAsync(query =>
            query.Where(s => s.UserId == userId && s.SessionId == sessionId && s.RevokedOnUtc == null));

        var session = sessions.FirstOrDefault();
        if (session == null)
            return;

        session.RevokedOnUtc = DateTime.UtcNow;
        session.RevokedReason = reason;
        await _userSessionRepository.UpdateAsync(session);

        await RemoveCacheAsync(session); // only THIS session's cache entry

        _logger.LogInformation("Session revoked. UserId={UserId} SessionId={SessionId} Reason={Reason}", userId, sessionId, reason);
    }

    public async Task<bool> IsSessionActiveAsync(long userId, Guid sessionId)
    {
        // cache only ever holds sessions that were active; revoke/replace removes the entry, TTL is 5 min at most
        if (await _cache.GetStringAsync(CacheKey(userId, sessionId)) != null)
            return true;

        var now = DateTime.UtcNow;
        var sessions = await _userSessionRepository.GetAllAsync(query =>
            query.Where(s => s.UserId == userId && s.SessionId == sessionId && s.RevokedOnUtc == null && s.ExpiresAtUtc > now));

        var active = sessions.FirstOrDefault();
        if (active == null)
            return false;

        await SetCacheAsync(active);
        return true;
    }

    /// <summary>
    /// Deletes sessions that expired / were revoked more than <paramref name="retentionDays"/> ago.
    /// </summary>
    public async Task CleanupAsync(int retentionDays = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        var old = await _userSessionRepository.GetAllAsync(query =>
            query.Where(s => (s.RevokedOnUtc != null && s.RevokedOnUtc < cutoff) || s.ExpiresAtUtc < cutoff)
                 .Take(1000)); // batch; the daily job catches up

        if (old.Count == 0)
            return;

        await _userSessionRepository.DeleteAsync(old);
        _logger.LogInformation("Session cleanup removed {Count} rows older than {Cutoff:u}", old.Count, cutoff);
    }

    #endregion
}