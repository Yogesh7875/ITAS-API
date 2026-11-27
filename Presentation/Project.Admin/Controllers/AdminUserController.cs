using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Common;
using Project.Admin.Models.Users;
using Project.Core;
using Project.Core.Domain.Users;
using Project.Services.Admin;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

/// <summary>Admin: list and view customers and corporate users, approve corporate users.</summary>
public class AdminUserController : BaseProtectedController
{
    #region Fields

    private readonly IWorkContext _workContext;
    private readonly IAdminQueryService _adminQueryService;

    #endregion

    #region Constructor

    public AdminUserController(IWorkContext workContext, IAdminQueryService adminQueryService)
    {
        _workContext = workContext;
        _adminQueryService = adminQueryService;
    }

    #endregion

    #region Methods

    /// <summary>All users. Optional roleName filter ("User", "Corporate", "Driver", ...).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers([FromQuery] AdminUserFilterModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var list = await _adminQueryService.GetUsersAsync(new AdminUserSearch
        {
            RoleName = model.RoleName,
            SearchText = model.Search,
            CompanyId = model.CompanyId,
            HasCompany = model.HasCompany,
            ApprovalStatusId = model.ApprovalStatusId,
            Active = model.Active,
            PageIndex = model.PageIndex,
            PageSize = model.PageSize
        });
        return Success(PagedResultModel<AdminUserListItem>.From(list));
    }

    /// <summary>Customers only (role "User").</summary>
    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] AdminUserFilterModel model)
    {
        model.RoleName = ProjectUserDefaults.UserRoleName;
        return await GetAllUsers(model);
    }

    /// <summary>Corporate users only. Filter by companyId and approvalStatusId (0 Pending, 1 Approved, 2 Rejected).</summary>
    [HttpGet]
    public async Task<IActionResult> GetCorporateUsers([FromQuery] AdminUserFilterModel model)
    {
        model.RoleName = ProjectUserDefaults.CorporateRoleName;
        return await GetAllUsers(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserDetail(long userId)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var user = await _adminQueryService.GetUserDetailAsync(userId);
        if (user == null)
            return Error(string.Empty, "User not found");

        return Success(user);
    }

    [HttpPost]
    public async Task<IActionResult> ReviewCorporateUser(ReviewCorporateUserModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        try
        {
            var admin = await _workContext.GetCurrentUserAsync();
            await _adminQueryService.SetCorporateApprovalAsync(model.UserId, model.Approve, model.Reason, admin.Id);
            return Success(string.Empty, model.Approve ? "Corporate user approved" : "Corporate user rejected");
        }
        catch (AdminException ex)
        {
            return Error(string.Empty, ex.Message);
        }
    }

    #endregion
}
