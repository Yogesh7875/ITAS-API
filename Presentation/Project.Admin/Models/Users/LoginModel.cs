namespace Project.Admin.Models.Users;

/// <summary>
/// Represents login model
/// </summary>
public class LoginModel
{
    #region Properties

    public string Email { get; set; }
    public string Password { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }

    #endregion
}
