using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Project.Core.Configuration;
using Project.Core.Domain.Users;
using Project.Services.Users;

namespace Project.Services.Authentication;

/// <summary>
/// Claim names used by the app (put in its own file if you prefer)
/// </summary>
public static class ProjectClaimTypes
{
    public const string SESSION_ID = "sid";
}

/// <summary>
/// Represents service using jwt middleware for the authentication
/// </summary>
public partial class JwtAuthenticationService : IAuthenticationService
{
    #region Fields

    private readonly AppSettings _appSettings;
    protected readonly IUserService _userService;
    protected readonly IHttpContextAccessor _httpContextAccessor;

    protected User _cachedUser;

    #endregion

    #region Constructor

    public JwtAuthenticationService(AppSettings appSettings,
        IUserService userService,
        IHttpContextAccessor httpContextAccessor)
    {
        _appSettings = appSettings;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Generate access token bound to a device session
    /// </summary>
    /// <param name="user">User</param>
    /// <param name="sessionId">Session id from UserSession (single device login)</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task<string> GenerateTokenAsync(User user, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var jwt = _appSettings.Get<JwtConfig>();

        //create claims for user's id, username, email and session
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ProjectClaimTypes.SESSION_ID, sessionId.ToString())
        };

        if (!string.IsNullOrEmpty(user.FirstName))
            claims.Add(new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}", ClaimValueTypes.String, jwt.Issuer));

        if (!string.IsNullOrEmpty(user.EmailAddress))
            claims.Add(new Claim(ClaimTypes.Email, user.EmailAddress, ClaimValueTypes.Email, jwt.Issuer));

        //create jwt
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var securityToken = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            signingCredentials: credentials,
            expires: DateTime.UtcNow.AddMinutes(jwt.TokenExpirationInMinitues)
        );

        var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

        return Task.FromResult(token);
    }

    /// <summary>
    /// Sign out
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SignOutAsync()
    {
        //reset cached customer
        _cachedUser = null;

        //and sign out from the current authentication scheme
        await _httpContextAccessor.HttpContext.SignOutAsync(ProjectAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Get authenticated user
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    public async Task<User> GetAuthenticatedUserAsync()
    {
        //whether there is a cached user
        if (_cachedUser != null)
            return _cachedUser;

        //try to get authenticated user identity
        //(a replaced/revoked session already fails here because of JwtBearerEvents.OnTokenValidated)
        var authenticateResult = await _httpContextAccessor.HttpContext.AuthenticateAsync(ProjectAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
            return null;

        User user = null;
        if (authenticateResult.Principal.Claims.Any(claim => claim.Type == ClaimTypes.NameIdentifier))
        {
            var usernameClaim = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.NameIdentifier
                && claim.Issuer.Equals(_appSettings.Get<JwtConfig>().Issuer, StringComparison.InvariantCultureIgnoreCase));
            if (usernameClaim != null && long.TryParse(usernameClaim.Value, out long id))
                user = await _userService.GetUserByIdAsync(id);
        }

        //whether the found user is available
        if (user == null || !user.Active || user.Deleted)
            return null;

        //cache authenticated user
        _cachedUser = user;

        return _cachedUser;
    }

    #endregion
}