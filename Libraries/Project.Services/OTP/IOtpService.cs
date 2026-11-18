using Project.Services.OTP.DataModel;

namespace Project.Services.OTP;

public interface IOtpService
{
    /// <summary>
    /// Generates an OTP for (purpose, mobile) and sends it by SMS
    /// </summary>
    Task<OtpSendResult> SendAsync(string purpose, string mobile);

    /// <summary>
    /// Verifies the OTP. A correct OTP is consumed (one time use).
    /// </summary>
    Task<bool> VerifyAsync(string purpose, string mobile, string otp);
}
