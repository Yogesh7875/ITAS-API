using Microsoft.AspNetCore.Mvc;
using Project.Services.Users;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

public class CompanyController : BaseController
{
    #region Fields

    private readonly ICompanyService _companyService;

    #endregion

    #region Constructor

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    #endregion

    #region Methods

    [HttpGet]
    public async Task<IActionResult> GetCompanySelectList()
    {
        var companyList = await _companyService.GetCompanySelectListAsync();
        return Success(companyList);
    }

    [HttpGet]
    public async Task<IActionResult> GetSelectDataModelList()
    {
        var selectList = await _companyService.GetSelectDataModelListAsync();
        return Success(selectList);
    }

    #endregion
}
