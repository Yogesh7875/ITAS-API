using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Companies;
using Project.Core;
using Project.Core.Domain.Users;
using Project.Services.Users;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

public class CompanyController : BaseProtectedController
{
    private readonly ICompanyService _companyService;
    private readonly IWorkContext _workContext;

    public CompanyController(ICompanyService companyService, IWorkContext workContext)
    {
        _companyService = companyService;
        _workContext = workContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompanySelectList() =>
        Success(await _companyService.GetCompanySelectListAsync());

    [HttpGet]
    public async Task<IActionResult> GetSelectDataModelList() =>
        Success(await _companyService.GetSelectDataModelListAsync());

    // ---------- admin ----------

    [HttpGet]
    public async Task<IActionResult> GetAll(string search = null, int pageIndex = 0, int pageSize = 20)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var list = await _companyService.GetAllCompaniesAsync(search, pageIndex, pageSize, showHidden: true);
        return Success(new
        {
            items = list.Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.EmailDomain,
                c.CreditLimit,
                c.BillingCycleDays,
                c.Remark,
                c.Active,
                c.CreatedOnUtc
            }).ToList(),
            totalCount = list.TotalCount,
            pageIndex = list.PageIndex,
            pageSize = list.PageSize
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var c = await _companyService.GetCompanyByIdAsync(id);
        if (c == null || c.Deleted)
            return Error(string.Empty, "Company not found");

        return Success(new
        {
            c.Id,
            c.Code,
            c.Name,
            c.EmailDomain,
            c.CreditLimit,
            c.BillingCycleDays,
            c.Remark,
            c.Active,
            linkedUsers = await _companyService.GetLinkedUserCountAsync(c.Id)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(CompanySaveModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var code = model.Code?.Trim().ToUpperInvariant();
        var name = model.Name?.Trim();
        var domain = model.EmailDomain?.Trim().TrimStart('@').ToLowerInvariant();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return Error(string.Empty, "Code and name are required");
        if (string.IsNullOrEmpty(domain) ||
            !Regex.IsMatch(domain, @"^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)+$"))
            return Error(string.Empty, "Enter a valid email domain, for example company.com");
        if (model.CreditLimit < 0)
            return Error(string.Empty, "Credit limit cannot be negative");
        if (model.BillingCycleDays < 1 || model.BillingCycleDays > 365)
            return Error(string.Empty, "Billing cycle must be between 1 and 365 days");

        var byCode = await _companyService.GetCompanyByCodeAsync(code);
        if (byCode != null && byCode.Id != model.Id)
            return Error(string.Empty, "A company with this code already exists");

        var byName = await _companyService.GetCompanyByNameAsync(name);
        if (byName != null && byName.Id != model.Id)
            return Error(string.Empty, "A company with this name already exists");

        // One domain per company, otherwise a work email could match the wrong company.
        var byDomain = await _companyService.GetCompanyByEmailDomainAsync(domain);
        if (byDomain != null && byDomain.Id != model.Id)
            return Error(string.Empty, "Another company already uses this email domain");

        var entity = model.Id > 0 ? await _companyService.GetCompanyByIdAsync(model.Id) : new Company();
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Company not found");

        entity.Code = code;
        entity.Name = name;
        entity.EmailDomain = domain;
        entity.CreditLimit = model.CreditLimit;
        entity.BillingCycleDays = model.BillingCycleDays;
        entity.Remark = model.Remark?.Trim();
        entity.Active = model.Active;

        if (entity.Id == 0)
            await _companyService.InsertCompanyAsync(entity);
        else
            await _companyService.UpdateCompanyAsync(entity);

        return Success(new { entity.Id }, "Saved");
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(long id, bool active)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var entity = await _companyService.GetCompanyByIdAsync(id);
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Company not found");

        entity.Active = active;
        await _companyService.UpdateCompanyAsync(entity);
        return Success(string.Empty, active ? "Company activated" : "Company deactivated");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var entity = await _companyService.GetCompanyByIdAsync(id);
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Company not found");

        var linked = await _companyService.GetLinkedUserCountAsync(id);
        if (linked > 0)
            return Error(string.Empty, $"{linked} user(s) are linked to this company. Deactivate it instead.");

        await _companyService.DeleteCompanyAsync(entity);
        return Success(string.Empty, "Deleted");
    }
}