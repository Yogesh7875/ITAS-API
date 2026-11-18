namespace Project.Core.Configuration;

/// <summary>
/// Represents OTP (one time password) configuration
/// </summary>
public class OtpConfig : IConfig
{
    /// <summary>
    /// Gets or sets the number of digits in the OTP
    /// </summary>
    public int Length { get; set; } = 6;

    /// <summary>
    /// Gets or sets the OTP lifetime in minutes
    /// </summary>
    public int ExpiryMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum seconds between two OTP requests for the same mobile number
    /// </summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the wrong guesses allowed per OTP
    /// </summary>
    public int MaxVerifyAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum OTPs sent to one mobile number per hour
    /// </summary>
    public int MaxSendsPerHour { get; set; } = 5;
}