using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Users;
using Project.Core;
using Project.Core.Domain.Users;
using Project.Services.Catalog;
using Project.Services.Users;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

public class UserController : BaseProtectedController
{
    #region Fields

    private readonly IUserService _userService;
    private readonly IWorkContext _workContext;
    private readonly ICompanyService _companyService;
    private readonly IUserRoleService _userRoleService;

    #endregion

    #region Constructor

    public UserController(IUserService userService, IWorkContext workContext, ICompanyService companyService, IUserRoleService userRoleService)
    {
        _userService = userService;
        _workContext = workContext;
        _companyService = companyService;
        _userRoleService = userRoleService;
    }

    #endregion

    #region Methods
    [HttpGet]
    public async Task<IActionResult> GetCustomerPersonalDetails()
    {
        var workUser = await _workContext.GetCurrentUserAsync();
        if (workUser == null)
        {
            return Error(string.Empty, "User not found");
        }
        var user = await _userService.GetUserByIdAsync(workUser.Id);

        if (user == null)
        {
            return Error(string.Empty, "User not found");
        }

        var record = new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.EmailAddress
        };

        return Success(record, "Success");
    }
    [HttpPost]
    public async Task<IActionResult> UpdateUserPersonalDetail(UserPersonalDetails model)
    {
        var user = await _userService.GetUserByIdAsync(model.Id);
        if (user == null)
        {
            return Error(string.Empty, "User not found");
        }
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.EmailAddress = model.EmailAddress;
        user.UpdatedOnUtc = CommonHelper.IndianTimeNow;
        await _userService.UpdateUserAsync(user);
        return Success(string.Empty, "User updated successfuly");
    }
    [HttpGet]
    public async Task<IActionResult> GetDriverUser(long id)
    {
        var workUser = await _workContext.GetCurrentUserAsync();
        if (workUser == null)
        {
            return Error(string.Empty, "User not found");
        }
        var user = await _userService.GetUserByIdAsync(workUser.Id);

        if (user == null)
        {
            return Error(string.Empty, "User not found");
        }

        var record = new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.EmailAddress,
            user.Dob,
            user.Address,
            user.Gender,
        };

        return Success(record, "Success");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateDriverPersonalDetails(UserModel model)
    {
        var user = await _userService.GetUserByIdAsync(model.Id);
        if (user == null)
        {
            return Error(string.Empty, "User not found");
        }
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.EmailAddress = model.EmailAddress;
        user.Dob = model.Dob;
        user.Gender = model.Gender;
        user.Address = model.Address;
        user.UpdatedOnUtc = CommonHelper.IndianTimeNow;
        await _userService.UpdateUserAsync(user);
        return Success(string.Empty, "Driver updated successfuly");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCorporateUserDetail(CorporateUserDetails model)
    {
        var workUser = await _workContext.GetCurrentUserAsync();
        if (workUser == null)
            return Error(string.Empty, "User not found");

        var user = await _userService.GetUserByIdAsync(workUser.Id);
        if (user == null)
            return Error(string.Empty, "User not found");

        // Only corporate accounts can use this endpoint
        var corporateRole = await _userRoleService.GetUserRoleByNameAsync(ProjectUserDefaults.CorporateRoleName);
        if (corporateRole == null || user.RoleId != corporateRole.Id)
            return Error(string.Empty, "Only corporate accounts can update company details");

        if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            return Error(string.Empty, "Please enter your first and last name");

        var email = model.EmailAddress?.Trim();
        try
        { _ = new MailAddress(email ?? string.Empty); }
        catch { return Error(string.Empty, "Please enter a valid work email"); }

        var company = await _companyService.GetCompanyByIdAsync(model.CompanyId);
        if (company == null || company.Deleted || !company.Active)
            return Error(string.Empty, "Company not found");

        var domain = company.EmailDomain?.Trim().TrimStart('@');
        if (string.IsNullOrEmpty(domain) ||
            !email.EndsWith("@" + domain, StringComparison.OrdinalIgnoreCase))
            return Error(string.Empty, $"Please use your {company.Name} work email (@{domain})");

        // First submission, a changed company, or a rejected account goes back to Pending.
        var needsApproval = user.CompanyId != company.Id || user.ApprovalStatusId == 2;

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.EmailAddress = email;
        user.CompanyId = company.Id;
        if (needsApproval)
            user.ApprovalStatusId = 0;
        user.UpdatedOnUtc = CommonHelper.IndianTimeNow;
        await _userService.UpdateUserAsync(user);

        return Success(new { user.CompanyId, ApprovalStatus = user.ApprovalStatusId },
            needsApproval ? "Details submitted for approval" : "Details updated");
    }
    #endregion
}
