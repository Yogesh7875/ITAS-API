using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Mvc;
using Project.Admin.Factories;
using Project.Admin.Models;
using Project.Admin.Models.Catalog;
using Project.Core.Domain.Catalog;
using Project.Services.Catalog;
using Project.Web.Framework;
using Project.Web.Framework.Infrastructure.Mapper.Extensions;
using Project.Web.Framework.Mvc.Filters;

namespace Project.Admin.Controllers;

public class UserRoleController : BaseProtectedController
{
    #region Fields

    private readonly IUserRoleService _userRoleService;
    private readonly IUserRoleModelFactory _userRoleModelFactory;

    #endregion

    #region Constructor

    public UserRoleController(IUserRoleService userRoleService,
        IUserRoleModelFactory userRoleModelFactory)
    {
        _userRoleService = userRoleService;
        _userRoleModelFactory = userRoleModelFactory;
    }

    #endregion

    #region Methods

    [HttpGet]
    public async Task<IActionResult> Get(long id)
    {
        var userRole = await _userRoleService.GetUserRoleByIdAsync(id);
        if (userRole == null || userRole.Deleted)
            return Error(string.Empty, "User role not found.");

        //prepare user role model
        var model = await _userRoleModelFactory.PrepareUserRoleModelAsync(null, userRole);

        return Success(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserRoleSelectList()
    {
        var userRole = await _userRoleService.GetUserRoleSelectListAsync();
        return Success(userRole);
    }

    [HttpGet]
    public async Task<IActionResult> GetPagedList(DataLoadOptionModel loadOptionModel, [FromQuery] UserRoleSearchModel searchModel)
    {
        //prepare user role list model
        var query = await _userRoleService.GetAllUserRoleAsync(searchModel.SearchName);
        var result = await DataSourceLoader.LoadAsync(query, loadOptionModel);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> Insert(UserRoleModel model)
    {
        var isExist = await _userRoleService.HasUserRoleByNameAsync(roleName: model.Name);
        if (isExist)
            return Error(string.Empty, "User role name is already exist.");

        var userRole = model.ToEntity<UserRole>();
        userRole.CreatedOnUtc = DateTime.UtcNow;
        userRole.UpdatedOnUtc = DateTime.UtcNow;
        userRole.Active = true;

        await _userRoleService.InsertUserRoleAsync(userRole);

        //var sqsData = new UserManagmentSQSModel();
        //sqsData.UserRole=userRole;
        //await _sqsSender.SendAsync(SQSGroup.CENTRALTOPOSINSERTUSERROLE, sqsData, SQSSendTo.POS);

        return Success(model.Id, "User role created successfully.");
    }

    [HttpPost]
    [PermissionAuthorization("UserRole.Write")]
    public async Task<IActionResult> Update(UserRoleModel model)
    {
        var userRole = await _userRoleService.GetUserRoleByIdAsync(model.Id);
        if (userRole == null || userRole.Deleted)
            return Error(string.Empty, "User role not found.");

        if (model.Name != userRole.Name)
        {
            var isExist = await _userRoleService.HasUserRoleByNameAsync(roleName: model.Name, roleId: model.Id);
            if (isExist)
                return Error(string.Empty, "User role name is already exist.");
        }

        userRole = model.ToEntity<UserRole>();
        userRole.UpdatedOnUtc = DateTime.UtcNow;
        userRole.Active = true;
        //update user role
        await _userRoleService.UpdateUserRoleAsync(userRole);

        //var sqsData = new UserManagmentSQSModel();
        //sqsData.UserRole = userRole;
        //await _sqsSender.SendAsync(SQSGroup.CENTRALTOPOSUPDATEUSERROLE, sqsData, SQSSendTo.POS);

        return Success(model.Id, "User role updated successfully.");
    }

    [HttpPost]
    [PermissionAuthorization("UserRole.Delete")]
    public async Task<IActionResult> Delete([FromForm] long id)
    {
        var userRole = await _userRoleService.GetUserRoleByIdAsync(id);
        if (userRole == null || userRole.Deleted)
            return Error(string.Empty, "User role not found.");

        //delete user role
        await _userRoleService.DeleteUserRoleAsync(userRole);

        return Success(id, "User role deleted successfully.");
    }
   #endregion
}