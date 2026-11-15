using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Users;
using Project.Core.Security;
using Project.Services.Authentication;
using Project.Services.Catalog;
using Project.Services.Users;
using Project.Services.Users.DataModels;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

public class AuthController : BaseController
{
    #region Fields

    private readonly IUserService _userService;
    private readonly IUserRoleService _userRoleService;
    private readonly IUserSessionService _sessionService;
    private readonly IAuthenticationService _authenticationService;
    #endregion

    #region Constructor

    public AuthController(IUserService userService,
        IUserRoleService userRoleService,
        IUserSessionService sessionService,
        IAuthenticationService authenticationService)
    {
        _userService = userService;
        _userRoleService = userRoleService;
        _sessionService = sessionService;
        _authenticationService = authenticationService;
    }

    #endregion

    #region Methods

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        model.Password = EncryptionHelper.EncryptPassword(model.Password);
        var user = await _userService.ValidateUserNameAndPasswordAsync(model.Email, model.Password);


        //validate user account status
        if (user == null)
            return Error(string.Empty, "User credentials are not valid");
        else if (user.Deleted)
            return Error(string.Empty, "The user account has been deleted. Please contact support or administrator your account to proceed.");
        else if (!user.Active)
            return Error(string.Empty, "The user account is not active. Please contact support or administrator your account to proceed.");

        var userName = (await _userRoleService.GetUserRoleByIdAsync(user.RoleId)).Name;
        if (!string.IsNullOrEmpty(userName) && userName.ToLower() != "super admin")
        {
            return Error(string.Empty, "Only super admin have access to log-in");
        }

        //prepare model
        //var authModel = await _userModelFactory.PrepareUserAuthModelAsync(user);

        return Success(model.Password, "Login successfull");
    }
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> UserLogin([FromBody] LoginModel model)
    {
        // use your existing credential validation here
        model.Password = EncryptionHelper.EncryptPassword(model.Password);

        var user = await _userService.ValidateUserNameAndPasswordAsync(model.Email, model.Password);
        if (user == null || !user.Active || user.Deleted)
            return Unauthorized(new { message = "Invalid credentials" });



        var info = new SessionInfo(model.DeviceId, model.DeviceName, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());

        var session = await _sessionService.CreateSessionAsync(user.Id, info);
        var accessToken = await _authenticationService.GenerateTokenAsync(user, session.SessionId);

        return Success(new { AccessToken = accessToken, SessionId = session.SessionId }, "Login successful");
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest model)
    {
        var result = await _sessionService.RotateAsync(model.RefreshToken);
        if (result == null)
            return Unauthorized(new { message = "Session expired. Please login again." });

        var (session, newRefreshToken) = result.Value;

        var user = await _userService.GetUserByIdAsync(session.UserId);
        if (user == null || !user.Active || user.Deleted)
        {
            await _sessionService.RevokeAsync(session.UserId, session.SessionId, "User inactive");
            return Unauthorized();
        }


        var accessToken = await _authenticationService.GenerateTokenAsync(user, session.SessionId);
        return Success(new { AccessToken = accessToken, SessionId = session.SessionId }, "Token refreshed successfully");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sessionId = Guid.Parse(User.FindFirstValue(ProjectClaimTypes.SESSION_ID)!);

        await _sessionService.RevokeAsync(userId, sessionId, "User logout");
        return Success(string.Empty, "Logout successful");
    }

    #endregion
}
