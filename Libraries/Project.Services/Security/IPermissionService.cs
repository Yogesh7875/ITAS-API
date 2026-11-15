using Project.Core.Domain.Catalog;
using Project.Core.Domain.Security;
using Project.Core.Domain.Users;
using Project.Services.Common.DataModel;
using Project.Services.Security.DataModels;

namespace Project.Services.Security;

/// <summary>
/// Permission service interface
/// </summary>
public partial interface IPermissionService
{
    /// <summary>
    /// Gets all permissions
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the permissions
    /// </returns>
    Task<IList<PermissionRecord>> GetAllPermissionRecordsAsync();

    /// <summary>
    /// Get permissions select data model list
    /// </summary>
    /// <returns></returns>
    Task<IList<SelectListDataModel>> GetAllPermissionRecordsSelectDataModelListAsync();

    /// <summary>
    /// Updates the permission
    /// </summary>
    /// <param name="permission">Permission</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdatePermissionRecordAsync(PermissionRecord permission);

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permission">Permission record</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    Task<bool> AuthorizeAsync(PermissionRecord permission);

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permission">Permission record</param>
    /// <param name="user">User</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    Task<bool> AuthorizeAsync(PermissionRecord permission, User user);

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permissionRecordSystemName">Permission record system name</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    Task<bool> AuthorizeAsync(string permissionRecordSystemName);

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permissionRecordSystemName">Permission record system name</param>
    /// <param name="user">User</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    Task<bool> AuthorizeAsync(string permissionRecordSystemName, User user);

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permissionRecordSystemName">Permission record system name</param>
    /// <param name="userRoleId">User role identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    Task<bool> AuthorizeAsync(string permissionRecordSystemName, long userRoleId);

    /// <summary>
    /// Gets a permission record-user role mapping
    /// </summary>
    /// <param name="permissionId">Permission identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<IList<PermissionRecordUserRoleMapping>> GetMappingByPermissionRecordIdAsync(long permissionId);

    /// <summary>
    /// Delete a permission record-user role mapping
    /// </summary>
    /// <param name="permissionId">Permission identifier</param>
    /// <param name="userRoleId">User role identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeletePermissionRecordCustomerRoleMappingAsync(long permissionId, long userRoleId);

    /// <summary>
    /// Inserts a permission record-user role mapping
    /// </summary>
    /// <param name="permissionRecordUserRoleMapping">Permission record-user role mapping</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertPermissionRecordCustomerRoleMappingAsync(PermissionRecordUserRoleMapping permissionRecordUserRoleMapping);

    Task<List<UserGroupPermissionDataModel>> GetAllUserGroupPermissionListAsync(long id);

    Task<List<string>> GetUserPermissionListAsync(long userId);
}