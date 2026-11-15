using Project.Core.Domain.Users;

namespace Project.Services.Authentication;

/// <summary>
/// Represents middleware that enables authentication
/// </summary>
public partial interface IAuthenticationService
{
    /// <summary>
    /// Generate token
    /// </summary>
    /// <param name="user">User</param>
    /// <param name="sessionId">Session id</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<string> GenerateTokenAsync(User user, Guid sessionId);

    /// <summary>
    /// Sign out
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task SignOutAsync();

    /// <summary>
    /// Get authenticated user
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    Task<User> GetAuthenticatedUserAsync();
}
