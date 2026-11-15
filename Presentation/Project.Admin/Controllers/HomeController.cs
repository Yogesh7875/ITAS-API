using Microsoft.AspNetCore.Mvc;
using Project.Core;
using Project.Services.ExportImport;

namespace Project.Admin.Controllers;

public class HomeController : Controller
{
    private readonly IWorkContext _workContext;
    private readonly IImportManager _importManager;

    public HomeController(IWorkContext workContext, IImportManager importManager)
    {
        _workContext = workContext;
        _importManager = importManager;
    }

    public IActionResult Index()
    {
        string htmlContent = "<h1>Welcome to <em>API-</em></h1>";
        return Content(htmlContent, "text/html");
    }

    [HttpGet]
    public async Task<IActionResult> Test()
    {
        var user = await _workContext.GetCurrentUserAsync();
        return Ok(user);
    }

    [HttpPost]
    public virtual async Task<IActionResult> ImportFromXlsx(IFormFile importexcelfile)
    {
        try
        {
            if (importexcelfile != null && importexcelfile.Length > 0)
            {
                await _importManager.ImportUsersFromXlsxAsync(importexcelfile.OpenReadStream());
            }
            else
            {
                //_notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Common.UploadFile"));
                return RedirectToAction("List");
            }

            //_notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Catalog.Manufacturers.Imported"));
            return RedirectToAction("List");
        }
        catch (Exception)
        {
            return RedirectToAction("List");
        }
    }
}
