namespace Project.Services.OTP.DataModel;


public enum OtpSendStatus { Sent, TooSoon, LimitReached, Failed }

public record OtpSendResult(OtpSendStatus Status, int RetryAfterSeconds = 0, string Otp = null);
public class OtpRecord
{
    public string Hash { get; set; }
    public int Attempts { get; set; }
    public DateTime SentAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

