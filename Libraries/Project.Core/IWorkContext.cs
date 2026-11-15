using Project.Core.Domain.Catalog;
using Project.Core.Domain.Localization;
using Project.Core.Domain.Users;

namespace Project.Core;

/// <summary>
/// Represents work context
/// </summary>
public partial interface IWorkContext
{
    /// <summary>
    /// Gets the current user
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<User> GetCurrentUserAsync();

    /// <summary>
    /// Sets the current user
    /// </summary>
    /// <param name="user">Current user</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task SetCurrentUserAsync(User user = null);

    /// <summary>
    /// Gets current user working language
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<Language> GetWorkingLanguageAsync();

    /// <summary>
    /// Sets current user working language
    /// </summary>
    /// <param name="language">Language</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task SetWorkingLanguageAsync(Language language);

    /// <summary>
    /// Gets the current user roles list
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
   Task<UserRole> GetCurrentUserRolesAsync();

    /// <summary>
    /// Validates whether current user is admin or not
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<bool> IsCurrentUserIsAdmin();
}