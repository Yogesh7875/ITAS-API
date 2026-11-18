using System.Security.Claims;
using Google.Apis.Auth.OAuth2.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Users;
using Project.Core.Domain.Users;
using Project.Core.Security;
using Project.Services.Authentication;
using Project.Services.Catalog;
using Project.Services.OTP;
using Project.Services.OTP.DataModel;
using Project.Services.Users;
using Project.Services.Users.DataModels;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

public class AuthController : BaseController
{
    #region Fields

    private const string CUSTOMER_OTP_PURPOSE = "customer";
    private const string DRIVER_OTP_PURPOSE = "driver";

    private readonly IUserService _userService;
    private readonly IUserRoleService _userRoleService;
    private readonly IUserSessionService _sessionService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IOtpService _otpService;

    #endregion

    #region Constructor

    public AuthController(IUserService userService,
        IUserRoleService userRoleService,
        IUserSessionService sessionService,
        IAuthenticationService authenticationService,
        IOtpService otpService)
    {
        _userService = userService;
        _userRoleService = userRoleService;
        _sessionService = sessionService;
        _authenticationService = authenticationService;
        _otpService = otpService;
    }

    #endregion

    #region Utilities

    private async Task<bool> HasRoleAsync(User user, params string[] roleNames)
    {
        var role = await _userRoleService.GetUserRoleByIdAsync(user.RoleId);
        return role != null && roleNames.Any(r => r.Equals(role.Name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>One mobile number may exist under several roles (same table), so pick the account of the right role</summary>
    private async Task<User> FindUserByMobileAndRoleAsync(string mobile, params string[] roleNames)
    {
        var users = await _userService.GetUsersByMobileNumberAsync(mobile);

        if (await HasRoleAsync(users, roleNames))
            return users;


        return null;
    }

    /// <summary>Creates the session (single device rule applies) and returns the token payload</summary>
    private async Task<IActionResult> IssueTokensAsync(User user, string appType, string deviceId, string deviceName, string message)
    {
        var info = new SessionInfo(deviceId, deviceName,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            appType,
            user.MultiDeviceLoginEnabled);

        var session = await _sessionService.CreateSessionAsync(user.Id, info);
        var accessToken = await _authenticationService.GenerateTokenAsync(user, session.SessionId);

        return Success(new
        {
            AccessToken = accessToken,
            RefreshToken = session.RefreshToken,                  //the client must store this and replace it after every refresh
            RefreshTokenExpiresAtUtc = session.RefreshExpiresAtUtc,
            SessionId = session.SessionId
        }, message);
    }

    private async Task<IActionResult> SendOtpAsync(string purpose, string mobileInput, Func<string, Task<bool>> shouldSend)
    {
        var mobile = OtpService.NormalizeMobile(mobileInput);
        if (mobile == null)
            return Error(string.Empty, "Please enter a valid 10 digit mobile number");

        //same answer whether or not the number is known: no account enumeration, and no SMS cost for unknown numbers
        if (await shouldSend(mobile))
        {
            var result = await _otpService.SendAsync(purpose, mobile);
            switch (result.Status)
            {
                case OtpSendStatus.TooSoon:
                    return Error(string.Empty, $"Please wait {result.RetryAfterSeconds} seconds before requesting another OTP");
                case OtpSendStatus.LimitReached:
                    return Error(string.Empty, "Too many OTP requests. Please try again after an hour");
                case OtpSendStatus.Failed:
                    return Error(string.Empty, "We could not send the OTP right now. Please try again");
            }
        }

        return Success(string.Empty, "If the number is eligible, an OTP has been sent");
    }

    #endregion

    #region Admin (password)

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginModel model)
    {
        model.Password = EncryptionHelper.EncryptPassword(model.Password);
        var user = await _userService.ValidateUserNameAndPasswordAsync(model.Email, model.Password);

        //validate user account status
        if (user == null)
            return Error(string.Empty, "User credentials are not valid");
        if (user.Deleted)
            return Error(string.Empty, "The user account has been deleted. Please contact support or administrator your account to proceed.");
        if (!user.Active || user.IsBanned)
            return Error(string.Empty, "The user account is not active. Please contact support or administrator your account to proceed.");

        if (!await HasRoleAsync(user, ProjectUserDefaults.AdministratorsRoleName))
            return Error(string.Empty, "Only super admin have access to log-in");

        //TODO: add the auth model (user details) to the response if the admin panel needs it
        return await IssueTokensAsync(user, ProjectUserDefaults.AdministratorsRoleName, model.DeviceId, model.DeviceName, "Login successfull");
    }

    #endregion

    #region Customer (OTP)

    [HttpPost]
    [AllowAnonymous]
    public Task<IActionResult> SendCustomerOtp([FromBody] SendOtpModel model) =>
        //customers can be new (the OTP is also the registration step), so always send
        SendOtpAsync(CUSTOMER_OTP_PURPOSE, model.Mobile, _ => Task.FromResult(true));

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCustomerOtp([FromBody] VerifyOtpModel model)
    {
        var mobile = OtpService.NormalizeMobile(model.Mobile);
        if (mobile == null || !await _otpService.VerifyAsync(CUSTOMER_OTP_PURPOSE, mobile, model.Otp))
            return Error(string.Empty, "Invalid or expired OTP");

        var user = await FindUserByMobileAndRoleAsync(mobile, ProjectUserDefaults.UserRoleName);

        //TODO new customer: create the account here (model.FullName / model.Email), then continue
        if (user == null)
            return Error(string.Empty, "Account not found. Please register first");

        if (!user.Active || user.IsBanned)
            return Error(string.Empty, "Your account is not active. Please contact support");

        //TODO corporate users: block here until an admin has approved the account

        return await IssueTokensAsync(user, ProjectUserDefaults.UserRoleName, model.DeviceId, model.DeviceName, "Login successful");
    }

    #endregion

    #region Driver (OTP)

    [HttpPost]
    [AllowAnonymous]
    public Task<IActionResult> SendDriverOtp([FromBody] SendOtpModel model) =>
        //only numbers that belong to a driver account get an SMS; the answer stays the same either way
        SendOtpAsync(DRIVER_OTP_PURPOSE, model.Mobile, async mobile =>
            await FindUserByMobileAndRoleAsync(mobile, ProjectUserDefaults.DriverRoleName) != null);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyDriverOtp([FromBody] VerifyOtpModel model)
    {
        var mobile = OtpService.NormalizeMobile(model.Mobile);
        if (mobile == null || !await _otpService.VerifyAsync(DRIVER_OTP_PURPOSE, mobile, model.Otp))
            return Error(string.Empty, "Invalid or expired OTP");

        var user = await FindUserByMobileAndRoleAsync(mobile, ProjectUserDefaults.DriverRoleName);
        if (user == null)
            return Error(string.Empty, "This number is not registered as a driver");

        if (!user.Active || user.IsBanned)
            return Error(string.Empty, "Your account is not active. Please contact support");

        //TODO block here until the driver's documents are approved

        return await IssueTokensAsync(user, ProjectUserDefaults.DriverRoleName, model.DeviceId, model.DeviceName, "Login successful");
    }

    #endregion


    #region Refresh / Logout (shared by all apps)
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest model)
    {
        var result = await _sessionService.RotateAsync(model.RefreshToken);
        if (result == null)
            return Unauthorized(new { message = "Session expired. Please login again." });

        var (session, newRefreshToken) = result.Value;

        var user = await _userService.GetUserByIdAsync(session.UserId);
        if (user == null || !user.Active || user.Deleted || user.IsBanned)
        {
            await _sessionService.RevokeAsync(session.UserId, session.SessionId, "User inactive");
            return Unauthorized(new { message = "User is not active." });
        }

        var accessToken = await _authenticationService.GenerateTokenAsync(user, session.SessionId);

        return Success(new
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresAtUtc = session.ExpiresAtUtc,
            SessionId = session.SessionId
        }, "Token refreshed successfully");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sessionId = Guid.Parse(User.FindFirstValue(ProjectClaimTypes.SESSION_ID)!);

        await _sessionService.RevokeAsync(userId, sessionId, "User logout");
        return Success(string.Empty, "Logout successful");
    }

    #endregion
}