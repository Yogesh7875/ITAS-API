using Project.Core.Caching;

namespace Project.Services.OTP;

/// <summary>
/// Represents default values related to OTP
/// </summary>
public static partial class OtpDefaults
{
    /// <remarks>
    /// {0} : purpose (customer / driver)
    /// {1} : mobile number
    /// </remarks>
    public static CacheKey OtpCodeCacheKey => new("Project.Otp.code.{0}.{1}");

    /// <remarks>
    /// {0} : purpose
    /// {1} : mobile number
    /// </remarks>
    public static CacheKey OtpSendCountCacheKey => new("Project.Otp.sendcount.{0}.{1}");

    /// <remarks>
    /// {0} : purpose
    /// {1} : mobile number
    /// </remarks>
    public static string OtpLockKey => "Project.Otp.lock.{0}.{1}";
}