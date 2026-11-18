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
    //add FullName / Email here when new customers register during verify
}

public class RefreshTokenModel
{
    public string RefreshToken { get; set; }
}