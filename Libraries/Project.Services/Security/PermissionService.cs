using DocumentFormat.OpenXml.Spreadsheet;
using Project.Core;
using Project.Core.Caching;
using Project.Core.Domain.Security;
using Project.Core.Domain.Users;
using Project.Data;
using Project.Services.Common.DataModel;
using Project.Services.Security.DataModels;
using Project.Services.Users;

namespace Project.Services.Security;

/// <summary>
/// Permission service
/// </summary>
public partial class PermissionService : IPermissionService
{
    #region Fields

    private readonly IUserService _userService;
    private readonly IWorkContext _workContext;
    private readonly IStaticCacheManager _staticCacheManager;
    private readonly IRepository<PermissionRecord> _permissionRecordRepository;
    private readonly IRepository<UserPermissionRecordMapping> _userPermissionRecordRepository;
    private readonly IRepository<PermissionRecordUserRoleMapping> _permissionRecordUserRoleMappingRepository;

    #endregion

    #region Ctor

    public PermissionService(IUserService userService,
        IWorkContext workContext,
        IStaticCacheManager staticCacheManager,
        IRepository<PermissionRecord> permissionRecordRepository,
        IRepository<UserPermissionRecordMapping> userPermissionRecordRepository,
        IRepository<PermissionRecordUserRoleMapping> permissionRecordUserRoleMappingRepository)
    {
        _userService = userService;
        _workContext = workContext;
        _staticCacheManager = staticCacheManager;
        _permissionRecordRepository = permissionRecordRepository;
        _userPermissionRecordRepository = userPermissionRecordRepository;
        _permissionRecordUserRoleMappingRepository = permissionRecordUserRoleMappingRepository;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Get permission records by customer role identifier
    /// </summary>
    /// <param name="userRoleId">Customer role identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the permissions
    /// </returns>
    protected virtual async Task<IList<PermissionRecord>> GetPermissionRecordsByUserIdAsync(long userId)
    {
        var key = _staticCacheManager.PrepareKeyForDefaultCache(ProjectSecurityDefaults.PermissionRecordsAllCacheKey, userId);

        var query = from pr in _permissionRecordRepository.Table
                    join prcrm in _userPermissionRecordRepository.Table on pr.Id equals prcrm
                        .PermissionRecordId
                    where prcrm.UserId == userId
                    orderby pr.Id
                    select pr;

        return await _staticCacheManager.GetAsync(key, async () => await query.ToListAsync());
    }

    /// <summary>
    /// Delete a permission
    /// </summary>
    /// <param name="permission">Permission</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task DeletePermissionRecordAsync(PermissionRecord permission)
    {
        await _permissionRecordRepository.DeleteAsync(permission);
    }

    /// <summary>
    /// Gets a permission
    /// </summary>
    /// <param name="systemName">Permission system name</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the permission
    /// </returns>
    protected virtual async Task<PermissionRecord> GetPermissionRecordBySystemNameAsync(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return null;

        var query = from pr in _permissionRecordRepository.Table
                    where pr.SystemName == systemName
                    orderby pr.Id
                    select pr;

        var permissionRecord = await query.FirstOrDefaultAsync();
        return permissionRecord;
    }

    /// <summary>
    /// Inserts a permission
    /// </summary>
    /// <param name="permission">Permission</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task InsertPermissionRecordAsync(PermissionRecord permission)
    {
        await _permissionRecordRepository.InsertAsync(permission);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets all permissions
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the permissions
    /// </returns>
    public virtual async Task<IList<PermissionRecord>> GetAllPermissionRecordsAsync()
    {
        var permissions = await _permissionRecordRepository.GetAllAsync(query =>
        {
            return from pr in query
                   orderby pr.Name
                   select pr;
        });

        return permissions;
    }

    /// <summary>
    /// Get permissions select data model list
    /// </summary>
    /// <returns></returns>
    public async Task<IList<SelectListDataModel>> GetAllPermissionRecordsSelectDataModelListAsync()
    {
        var query = from permission in _permissionRecordRepository.Table
                          select new SelectListDataModel
                          {
                              Id = permission.Id,
                              Name = $"{permission.Category}-{permission.Name}"
                          };

        var permissions = await query.ToListAsync();

        return permissions;
    }
    public async Task<List<string>> GetUserPermissionListAsync(long userId)
    {
        var query = from permission in _permissionRecordRepository.Table
                    join userPermission in _userPermissionRecordRepository.Table
                    on permission.Id equals userPermission.PermissionRecordId
                    where userPermission.UserId == userId
                    select permission.SystemName;

        return await query.ToListAsync();
    }
    public async Task<List<UserGroupPermissionDataModel>> GetAllUserGroupPermissionListAsync(long userId)
    {
        var query = from permission in _permissionRecordRepository.Table
                    join userPermission in _userPermissionRecordRepository.Table
                    on new { PermissionId = permission.Id, UserId = userId }
                    equals new { PermissionId = userPermission.PermissionRecordId, UserId = userPermission.UserId }
                    into userPermissions // Perform LEFT JOIN
                    from userPermission in userPermissions.DefaultIfEmpty() // Handle NULL values for unmatched records
                    select new
                    {
                        Category = permission.Category,
                        Group = permission.Group,
                        Name = permission.Name,
                        Id = permission.Id,
                        IsChecked = userPermission != null // Set to true if user has permission, otherwise false
                    };

        var mainQuery = await query.ToListAsync().ConfigureAwait(false);

        var data = mainQuery
            .GroupBy(p => p.Group)
            .Select(g => new UserGroupPermissionDataModel
            {
                Group = g.Key,
                CategoryWisePermissions = g.GroupBy(p => p.Category)
                    .Select(c => new CategoryWisePermissionModel
                    {
                        Category = c.Key,
                        Permissions = c.Select(p => new SelectListDataModel
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Code = p.Name,
                            IsChecked = p.IsChecked // Assign IsChecked based on user permission check
                        }).ToList()
                    }).ToList()
            }).ToList();

        return data;
    }


    /// <summary>
    /// Updates the permission
    /// </summary>
    /// <param name="permission">Permission</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task UpdatePermissionRecordAsync(PermissionRecord permission)
    {
        await _permissionRecordRepository.UpdateAsync(permission);
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permission">Permission record</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    public virtual async Task<bool> AuthorizeAsync(PermissionRecord permission)
    {
        return await AuthorizeAsync(permission, await _workContext.GetCurrentUserAsync());
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permission">Permission record</param>
    /// <param name="user">User</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    public virtual async Task<bool> AuthorizeAsync(PermissionRecord permission, User user)
    {
        if (permission == null)
            return false;

        if (user == null)
            return false;

        return await AuthorizeAsync(permission.SystemName, user);
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permissionRecordSystemName">Permission record system name</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    public virtual async Task<bool> AuthorizeAsync(string permissionRecordSystemName)
    {
        return await AuthorizeAsync(permissionRecordSystemName, await _workContext.GetCurrentUserAsync());
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permissionRecordSystemName">Permission record system name</param>
    /// <param name="user">User</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    public virtual async Task<bool> AuthorizeAsync(string permissionRecordSystemName, User user)
    {
        if (string.IsNullOrEmpty(permissionRecordSystemName))
            return false;

      //  var role = await _userService.GetUserRoleAsync(user.Id);
        if (await AuthorizeAsync(permissionRecordSystemName, user.Id))
            //yes, we have such permission
            return true;

        //no permission found
        return false;
    }

    /// <summary>
    /// Authorize permission
    /// </summary>
    /// <param name="permissionRecordSystemName">Permission record system name</param>
    /// <param name="userRoleId">User role identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rue - authorized; otherwise, false
    /// </returns>
    public virtual async Task<bool> AuthorizeAsync(string permissionRecordSystemName, long userId)
    {
        if (string.IsNullOrEmpty(permissionRecordSystemName))
            return false;

        var key = _staticCacheManager.PrepareKeyForDefaultCache(ProjectSecurityDefaults.PermissionAllowedCacheKey, permissionRecordSystemName, userId);

        return await _staticCacheManager.GetAsync(key, async () =>
        {
            var permissions = await GetPermissionRecordsByUserIdAsync(userId);
            foreach (var permission in permissions)
                if (permission.SystemName.Equals(permissionRecordSystemName, StringComparison.InvariantCultureIgnoreCase))
                    return true;

            return false;
        });
    }

    /// <summary>
    /// Gets a permission record-user role mapping
    /// </summary>
    /// <param name="permissionId">Permission identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<IList<PermissionRecordUserRoleMapping>> GetMappingByPermissionRecordIdAsync(long permissionId)
    {
        var query = _permissionRecordUserRoleMappingRepository.Table;

        query = query.Where(x => x.PermissionRecordId == permissionId);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Delete a permission record-customer role mapping
    /// </summary>
    /// <param name="permissionId">Permission identifier</param>
    /// <param name="userRoleId">Customer role identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task DeletePermissionRecordCustomerRoleMappingAsync(long permissionId, long userRoleId)
    {
        var mapping = _permissionRecordUserRoleMappingRepository.Table
            .FirstOrDefault(prcm => prcm.UserRoleId == userRoleId && prcm.PermissionRecordId == permissionId);
        if (mapping is null)
            return;

        await _permissionRecordUserRoleMappingRepository.DeleteAsync(mapping);
    }

    /// <summary>
    /// Inserts a permission record-user role mapping
    /// </summary>
    /// <param name="permissionRecordUserRoleMapping">Permission record-user role mapping</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task InsertPermissionRecordCustomerRoleMappingAsync(PermissionRecordUserRoleMapping permissionRecordUserRoleMapping)
    {
        await _permissionRecordUserRoleMappingRepository.InsertAsync(permissionRecordUserRoleMapping);
    }

    #endregion
}