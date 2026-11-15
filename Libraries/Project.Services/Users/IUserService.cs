using Project.Core;
using Project.Core.Domain.Catalog;
using Project.Core.Domain.Users;
using Project.Services.Common.DataModel;
using Project.Services.Users.DataModels;
using Project.Services.Users.EntityModel;

namespace Project.Services.Users;

/// <summary>
/// Represents user service interface
/// </summary>
public partial interface IUserService
{
    
    /// <summary>
    ///  Gets a user by code
    /// </summary>
    /// <param name="code"></param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the User
    /// </returns>
    Task<User> GetUserByUserNameAsync(string code,long id);

    /// <summary>
    /// Gets a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    Task<User> GetUserByIdAsync(long userId);
    /// <summary>
    /// Gets a user user Ids by role Id
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    Task<List<long>> GetUserIdsByRoleIdAsync(long roleId);

    /// <summary>
    /// Gets user role list
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    Task<UserRole> GetUserRoleAsync(long userId);

    /// <summary>
    /// Insert user
    /// </summary>
    /// <param name="user">user</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    Task InsertUserAsync(User user);

    /// <summary>
    /// Update user
    /// </summary>
    /// <param name="user">user</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    Task UpdateUserAsync(User user);

    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task DeleteUserAsync(User user);

    /// <summary>
    /// Validate user name and password
    /// </summary>
    /// <param name="code">Username</param>
    /// <param name="password">Password</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    Task<User> ValidateUserNameAndPasswordAsync(string userName, string password);

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <param name="searchText">User search text; null to load all records</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">A value indicating whether to show hidden records</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the users list
    /// </returns>

    Task<IQueryable<UserDataModel>> GetAllUsersAsync(string searchText);
    /// <summary>
    /// Gets all users
    /// </summary>
    /// <param name="searchText">User search text; null to load all records</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">A value indicating whether to show hidden records</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the users list
    /// </returns>
}
