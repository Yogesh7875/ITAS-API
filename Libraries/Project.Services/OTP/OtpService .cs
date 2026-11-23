using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Project.Core;
using Project.Core.Caching;
using Project.Core.Configuration;
using Project.Services.OTP.DataModel;

namespace Project.Services.OTP;

public class OtpService : IOtpService
{
    #region Fields

    private readonly IStaticCacheManager _staticCacheManager;
    private readonly ILocker _locker;
    private readonly AppSettings _appSettings;
    private readonly ILogger<OtpService> _logger;

    #endregion

    #region Ctor

    public OtpService(IStaticCacheManager staticCacheManager,
        ILocker locker,
        AppSettings appSettings,
        ILogger<OtpService> logger)
    {
        _staticCacheManager = staticCacheManager;
        _locker = locker;
        _appSettings = appSettings;
        _logger = logger;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Returns a 10 digit number, or null when the input is not a valid mobile number
    /// </summary>
    public static string NormalizeMobile(string mobile)
    {
        var digits = new string((mobile ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > 10)
            digits = digits[^10..];

        return digits.Length == 10 ? digits : null;
    }

    //HMAC with the server key: a leaked cache entry cannot be brute-forced offline without the key
    private string Hash(string purpose, string mobile, string otp)
    {
        var key = Encoding.UTF8.GetBytes(_appSettings.Get<JwtConfig>().Key);
        return Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{purpose}:{mobile}:{otp}")));
    }

    private static int MinutesLeft(DateTime expiresAtUtc) =>
        Math.Max(1, (int)Math.Ceiling((expiresAtUtc - CommonHelper.IndianTimeNow).TotalMinutes));

    #endregion

    #region Methods

    public async Task<OtpSendResult> SendAsync(string purpose, string mobile)
    {
        var config = _appSettings.Get<OtpConfig>();
        var result = new OtpSendResult(OtpSendStatus.Failed);

        var executed = await _locker.PerformActionWithLockAsync(string.Format(OtpDefaults.OtpLockKey, purpose, mobile),
            TimeSpan.FromSeconds(15), async () =>
            {
                var codeKey = _staticCacheManager.PrepareKeyForDefaultCache(OtpDefaults.OtpCodeCacheKey, purpose, mobile);
                var countKey = _staticCacheManager.PrepareKeyForDefaultCache(OtpDefaults.OtpSendCountCacheKey, purpose, mobile);

                var existing = await _staticCacheManager.GetAsync<OtpRecord>(codeKey);
                if (existing != null)
                {
                    var wait = config.ResendCooldownSeconds - (CommonHelper.IndianTimeNow - existing.SentAtUtc).TotalSeconds;
                    if (wait > 0)
                    {
                        result = new OtpSendResult(OtpSendStatus.TooSoon, (int)Math.Ceiling(wait));
                        return;
                    }
                }

                var sent = await _staticCacheManager.GetAsync<int>(countKey);
                if (sent >= config.MaxSendsPerHour)
                {
                    result = new OtpSendResult(OtpSendStatus.LimitReached, 3600);
                    return;
                }

                var min = (int)Math.Pow(10, config.Length - 1);
                var otp = RandomNumberGenerator.GetInt32(min, min * 10).ToString();
                var now = CommonHelper.IndianTimeNow;

                codeKey.CacheTime = config.ExpiryMinutes;
                await _staticCacheManager.SetAsync(codeKey, new OtpRecord
                {
                    Hash = Hash(purpose, mobile, otp),
                    Attempts = 0,
                    SentAtUtc = now,
                    ExpiresAtUtc = now.AddMinutes(config.ExpiryMinutes)
                });

                try
                {
                    //await _smsSender.SendAsync(mobile,
                    //    $"{otp} is your iTas verification code. It is valid for {config.ExpiryMinutes} minutes. Do not share it with anyone.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OTP SMS failed. Purpose={Purpose} Mobile=***{Last4}", purpose, mobile[^4..]);
                    await _staticCacheManager.RemoveAsync(OtpDefaults.OtpCodeCacheKey, purpose, mobile); //allow an immediate retry
                    result = new OtpSendResult(OtpSendStatus.Failed);
                    return;
                }

                //rolling one hour window (cache time is in minutes)
                countKey.CacheTime = 60;
                await _staticCacheManager.SetAsync(countKey, sent + 1);

                _logger.LogInformation("OTP sent. Purpose={Purpose} Mobile=***{Last4}", purpose, mobile[^4..]);
                result = new OtpSendResult(OtpSendStatus.Sent, 0, otp);
            });

        //somebody else is sending to this number right now
        return executed ? result : new OtpSendResult(OtpSendStatus.TooSoon, config.ResendCooldownSeconds);
    }

    public async Task<bool> VerifyAsync(string purpose, string mobile, string otp)
    {
        if (string.IsNullOrWhiteSpace(otp))
            return false;

        var config = _appSettings.Get<OtpConfig>();
        var verified = false;

        //locked so parallel guesses cannot get around the attempt counter
        await _locker.PerformActionWithLockAsync(string.Format(OtpDefaults.OtpLockKey, purpose, mobile),
            TimeSpan.FromSeconds(15), async () =>
            {
                var codeKey = _staticCacheManager.PrepareKeyForDefaultCache(OtpDefaults.OtpCodeCacheKey, purpose, mobile);
                var record = await _staticCacheManager.GetAsync<OtpRecord>(codeKey);

                if (record == null || record.ExpiresAtUtc <= CommonHelper.IndianTimeNow)
                    return; //expired or never sent

                if (record.Attempts >= config.MaxVerifyAttempts)
                {
                    await _staticCacheManager.RemoveAsync(OtpDefaults.OtpCodeCacheKey, purpose, mobile);
                    return;
                }

                var supplied = Hash(purpose, mobile, otp.Trim());
                if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(record.Hash), Encoding.UTF8.GetBytes(supplied)))
                {
                    await _staticCacheManager.RemoveAsync(OtpDefaults.OtpCodeCacheKey, purpose, mobile); //one time use
                    verified = true;
                    return;
                }

                record.Attempts++;
                codeKey.CacheTime = MinutesLeft(record.ExpiresAtUtc);
                await _staticCacheManager.SetAsync(codeKey, record);

                _logger.LogWarning("OTP verify failed. Purpose={Purpose} Mobile=***{Last4} Attempt={Attempt}", purpose, mobile[^4..], record.Attempts);
            });

        return verified;
    }

    #endregion
}

