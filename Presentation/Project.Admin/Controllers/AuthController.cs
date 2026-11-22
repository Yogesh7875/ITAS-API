using System.Security.Claims;
using ExCSS;
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
        if (user == null)
            return false;

        var role = await _userRoleService.GetUserRoleByIdAsync(user.RoleId);
        if (role == null)
            return false;

        return roleNames.Any(r => r.Equals(role.Name, StringComparison.OrdinalIgnoreCase) ||
            (r.Equals(ProjectUserDefaults.UserRoleName, StringComparison.OrdinalIgnoreCase) && role.Name.Equals("Customer", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>One mobile number may exist under several roles (same table), so pick the account of the right role</summary>
    private async Task<User> FindUserByMobileAndRoleAsync(string mobile, params string[] roleNames)
    {
        var users = await _userService.GetAllUsersByMobileNumberAsync(mobile);

        foreach (var user in users)
        {
            if (await HasRoleAsync(user, roleNames))
                return user;
        }

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
            Id=user.Id,
            isPersonalDetailDone = string.IsNullOrEmpty(user.FirstName),
            AccessToken = accessToken,
            RefreshToken = session.RefreshToken,                  //the client must store this and replace it after every refresh
            RefreshTokenExpiresAtUtc = session.RefreshExpiresAtUtc,
            SessionId = session.SessionId
        }, message);
    }

    private async Task<IActionResult> SendOtpAsync(string purpose, string mobileInput, string roleName)
    {
        var mobile = OtpService.NormalizeMobile(mobileInput);
        if (mobile == null)
            return Error(string.Empty, "Please enter a valid 10 digit mobile number");

        var role = await _userRoleService.GetUserRoleByNameAsync(roleName);
        if (role == null)
            return Error(string.Empty, $"User role '{roleName}' not found");

        if (IsCustomerSide(roleName))
        {
            var otherRole = roleName == ProjectUserDefaults.UserRoleName
                ? ProjectUserDefaults.CorporateRoleName
                : ProjectUserDefaults.UserRoleName;

            var otherAccount = await FindUserByMobileAndRoleAsync(mobile, otherRole);
            if (otherAccount != null)
                return Error(string.Empty, roleName == ProjectUserDefaults.UserRoleName
                    ? "This number is registered as a corporate account. Please log in as Corporate."
                    : "This number is registered as a normal account. Please log in as Customer.");
        }

        var existingUser = await _userService.GetUserByMobileAndRoleIdAsync(mobile, role.Id);
        if (existingUser != null && (!existingUser.Active || existingUser.IsBanned))
            return Error(string.Empty, "Your account is not active. Please contact support");

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

        if (existingUser == null)
        {
            var newUser = new User
            {
                MobileNumber = mobile,
                RoleId = role.Id,
                OTP = result.Otp,
                Active = true,
                Deleted = false,
                FirstName = null,
                LastName = null,
                EmailAddress = null,
                Password = null,
                Gender = null,
                Address = null,
                Remark = null,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await _userService.InsertUserAsync(newUser);
        }
        else
        {
            existingUser.OTP = result.Otp;
            existingUser.UpdatedOnUtc = DateTime.UtcNow;
            if (existingUser.Deleted)
            {
                existingUser.Deleted = false;
                existingUser.Active = true;
            }
            await _userService.UpdateUserAsync(existingUser);
        }

        return Success(result.Otp, "OTP sent successfully");
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
    public async Task<IActionResult> SendCustomerOtp([FromBody] SendOtpModel model) =>
        await SendOtpAsync(CUSTOMER_OTP_PURPOSE, model?.Mobile, ProjectUserDefaults.UserRoleName);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCustomerOtp([FromBody] VerifyOtpModel model)
    {
        var mobile = OtpService.NormalizeMobile(model?.Mobile);
        if (mobile == null)
            return Error(string.Empty, "Please enter a valid 10 digit mobile number");

        var user = await FindUserByMobileAndRoleAsync(mobile, ProjectUserDefaults.UserRoleName);
        if (user == null)
            return Error(string.Empty, "Account not found. Please register first");

        if (!user.Active || user.IsBanned)
            return Error(string.Empty, "Your account is not active. Please contact support");

        var isVerified = await _otpService.VerifyAsync(CUSTOMER_OTP_PURPOSE, mobile, model.Otp);
        if (!isVerified && !string.IsNullOrEmpty(user.OTP) && user.OTP == model.Otp?.Trim())
        {
            isVerified = true;
        }

        if (!isVerified)
            return Error(string.Empty, "Invalid or expired OTP");



        await _userService.UpdateUserAsync(user);

        return await IssueTokensAsync(user, ProjectUserDefaults.UserRoleName, model.DeviceId, model.DeviceName, "Login successful");
    }

    #endregion

    #region Driver (OTP)

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SendDriverOtp([FromBody] SendOtpModel model) =>
        await SendOtpAsync(DRIVER_OTP_PURPOSE, model?.Mobile, ProjectUserDefaults.DriverRoleName);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyDriverOtp([FromBody] VerifyOtpModel model)
    {
        var mobile = OtpService.NormalizeMobile(model?.Mobile);
        if (mobile == null)
            return Error(string.Empty, "Please enter a valid 10 digit mobile number");

        var user = await FindUserByMobileAndRoleAsync(mobile, ProjectUserDefaults.DriverRoleName);
        if (user == null)
            return Error(string.Empty, "This number is not registered as a driver");

        if (!user.Active || user.IsBanned)
            return Error(string.Empty, "Your account is not active. Please contact support");

        var isVerified = await _otpService.VerifyAsync(DRIVER_OTP_PURPOSE, mobile, model.Otp);
        if (!isVerified && !string.IsNullOrEmpty(user.OTP) && user.OTP == model.Otp?.Trim())
        {
            isVerified = true;
        }

        if (!isVerified)
            return Error(string.Empty, "Invalid or expired OTP");

        // clear OTP after successful verification
        user.OTP = null;
        user.UpdatedOnUtc = DateTime.UtcNow;
        await _userService.UpdateUserAsync(user);

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

    #region Utility
    private static bool IsCustomerSide(string roleName) =>
     roleName == ProjectUserDefaults.UserRoleName ||
     roleName == ProjectUserDefaults.CorporateRoleName;
    #endregion
}