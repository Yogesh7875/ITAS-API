using Project.Admin.Models.Users;
using Project.Core.Domain.Users;
using Project.Services.Authentication;
using Project.Services.Catalog;
using Project.Services.Helpers;
using Project.Services.Security;
using Project.Services.Security.DataModels;
using Project.Services.Users;
using Project.Web.Framework.Infrastructure.Mapper.Extensions;
using Project.Web.Framework.Models;

namespace Project.Admin.Factories;

/// <summary>
/// Represents user model factory
/// </summary>
public partial record UserModelFactory : IUserModelFactory
{
    #region Fields

    private readonly IUserService _userService;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly IDepartmentService _departmentService;
    private readonly IPermissionService _permissionService;
    private readonly IUserRoleService _userRoleService;
    private readonly IAuthenticationService _authenticationService;

    #endregion

    #region Constructor

    public UserModelFactory(IUserService userService,
        IDateTimeHelper dateTimeHelper,
        IDepartmentService departmentService,
        IPermissionService permissionService,
        IUserRoleService userRoleService,
        IAuthenticationService authenticationService)
    {
        _userService = userService;
        _dateTimeHelper = dateTimeHelper;
        _departmentService = departmentService;
        _permissionService = permissionService;
        _userRoleService = userRoleService;
        _authenticationService = authenticationService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare user authentication model
    /// </summary>
    /// <param name="user">User</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user auth model
    /// </returns>
    public async Task<UserAuthModel> PrepareUserAuthModelAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var authModel = user.ToModel<UserAuthModel>();
        authModel.Token = await _authenticationService.GenerateTokenAsync(user, new Guid());
        authModel.RoleName = (await _userRoleService.GetUserRoleByIdAsync(user.RoleId)).Name;
        authModel.Permissions = await _permissionService.GetUserPermissionListAsync(user.Id);
        return authModel;
    }



    /// <summary>
    /// Prepare user model
    /// </summary>
    /// <param name="model">User model</param>
    /// <param name="user">User</param>
    /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the genre model
    /// </returns>
    public async Task<UserModel> PrepareUserModelAsync(UserModel model, User user, bool excludeProperties = false)
    {
        if (user != null)
        {
            model ??= user.ToModel<UserModel>();

        }
        return model;
    }

    #region Permissions

    /// <summary>
    /// Prepare user permission model
    /// </summary>
    /// <param name="user">User</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user permission model
    /// </returns>
    public async Task<UserPermissionModel> PrepareUserPermissionModelAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var model = new UserPermissionModel
        {
            Id = user.Id,
            //Permissions = (await _userService.GetPermissionSelectDataModelListByUserAsync(user)).ToModel<SelectListModel>(),
            AvailablePermissions = (await _permissionService.GetAllPermissionRecordsSelectDataModelListAsync()).ToModel<SelectListModel>()
        };

        return model;
    }

    public async Task<List<UserGroupPermissionDataModel>> PrepareUserGroupPermissionModelAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        var model = await _permissionService.GetAllUserGroupPermissionListAsync(user.Id);
        return model;
    }

    #endregion

    #endregion
}
