namespace Project.Admin.Models.Users;

public class SendOtpModel
{
    public string Mobile { get; set; }
}

public class VerifyOtpModel
{
    public string Mobile { get; set; }
    public string Otp { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    /// <summary>Firebase Cloud Messaging token of the device; saved on the user for push notifications</summary>
    public string FcmToken { get; set; }
    //add FullName / Email here when new customers register during verify
}

public class RefreshTokenModel
{
    public string RefreshToken { get; set; }
}

public class UpdateFcmTokenModel
{
    public string FcmToken { get; set; }
}
