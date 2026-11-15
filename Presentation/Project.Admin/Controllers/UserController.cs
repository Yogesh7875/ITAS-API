using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Mvc;
using Project.Admin.Factories;
using Project.Admin.Models;
using Project.Admin.Models.Users;
using Project.Core.Domain.Users;
using Project.Core.Security;
using Project.Services.Catalog;
using Project.Services.Users;
using Project.Web.Framework;
using Project.Web.Framework.Infrastructure.Mapper.Extensions;
using Project.Web.Framework.Mvc.Filters;

namespace Project.Admin.Controllers;

public class UserController : BaseProtectedController
{
    #region Fields

    private readonly IUserService _userService;
    private readonly IUserRoleService _userRoleService;

    #endregion

    #region Constructor

    public UserController(IUserService userService,
        IUserRoleService userRoleService
        )
    {
        _userService = userService;
        _userRoleService = userRoleService;
    }

    #endregion

    #region Methods

    [HttpGet]
    //[PermissionAuthorization("User.Read")]
    public async Task<IActionResult> Get(long id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null || user.Deleted)
            return Error(string.Empty, "User not found.");

        user.Password = EncryptionHelper.DecryptPassword(user.Password);
        return Success(user);
    }




    [HttpPost]
    //   [PermissionAuthorization("User.Write")]
    public async Task<IActionResult> Insert(UserModel model)
    {
        var userdetails = await _userService.GetUserByUserNameAsync(model.UserName, model.Id);
        if (userdetails != null && userdetails.Deleted == false)
            return Error(string.Empty, "This username is already in use.");

        var userRole = await _userRoleService.GetUserRoleByIdAsync(model.RoleId);
        if (userRole == null || userRole.Deleted)
            return Error(string.Empty, "User role not found.");

        var user = model.ToEntity<User>();
        user.CreatedOnUtc = DateTime.UtcNow;
        user.UpdatedOnUtc = DateTime.UtcNow;
        user.Active = true;
        user.Password = EncryptionHelper.EncryptPassword(user.Password);
        await _userService.InsertUserAsync(user);

        return Success(model.Id, "User inserted successfully.");
    }

    [HttpPost]
    //[PermissionAuthorization("User.Write")]
    public async Task<IActionResult> Update(UserModel model)
    {
        var user = await _userService.GetUserByIdAsync(model.Id);
        if (user == null || user.Deleted)
            return Error(string.Empty, "User not found.");

        var userdetails = await _userService.GetUserByUserNameAsync(model.UserName, model.Id);
        if (userdetails != null && userdetails.Deleted == false)
            return Error(string.Empty, "This username is already in use.");


        
        user = model.ToEntity(user);
        user.UpdatedOnUtc = DateTime.UtcNow;
        user.Active = true;
        user.Password = EncryptionHelper.EncryptPassword(user.Password);
        //update user
        await _userService.UpdateUserAsync(user);

        return Success(model.Id, "User updated successfully.");
    }

    [HttpDelete]
    //[PermissionAuthorization("User.Delete")]
    public async Task<IActionResult> Delete(long id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null || user.Deleted)
            return Error("User not found.");

        //delete user
        await _userService.DeleteUserAsync(user);
        //var sqsData = new UserManagmentSQSModel();
        //sqsData.User = user;
        //await _sqsSender.SendAsync(SQSGroup.CENTRALTOPOSDELETEUSER, sqsData, SQSSendTo.POS);

        return Success(id, "User deleted successfully.");
    }



    #region Permissions
    [HttpGet]
    [PermissionAuthorization("User.Read")]
    public async Task<IActionResult> GetAllUsers(DataLoadOptionModel loadOptionModel, [FromQuery] UserSearchModel searchModel)
    {
        //prepare department list model
        var query = await _userService.GetAllUsersAsync(searchModel.SearchText);
        var result = await DataSourceLoader.LoadAsync(query, loadOptionModel);
        return Json(result);
    }
    #endregion

    #endregion
}
