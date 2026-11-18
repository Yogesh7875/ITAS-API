namespace Project.Services.Users.DataModels;


public record SessionInfo(string DeviceId, string DeviceName, string IpAddress, string UserAgent, string AppType = "default", bool MultiDeviceLoginEnabled = false);

public record NewSession(Guid SessionId, string RefreshToken, DateTime RefreshExpiresAtUtc);

