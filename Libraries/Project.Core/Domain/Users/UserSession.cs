namespace Project.Core.Domain.Users;

public class UserSession : BaseEntity
{
    public long UserId { get; set; }
    public Guid SessionId { get; set; }
    public string RefreshTokenHash { get; set; }
    public string PreviousRefreshTokenHash { get; set; } // used for reuse (theft) detection
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? LastRefreshedOnUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedOnUtc { get; set; }
    public string RevokedReason { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string AppType { get; set; }

    /// <summary>
    /// True when this session was created under the single-device rule (user.MultiDeviceLoginEnabled == false).
    /// The unique filtered index (UserId, AppType) only applies to these rows.
    /// </summary>
    public bool IsSingleDevice { get; set; } = true;

    public bool IsActive => RevokedOnUtc == null && ExpiresAtUtc > CommonHelper.IndianTimeNow;
}
