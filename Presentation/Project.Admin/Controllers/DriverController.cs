using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Drivers;
using Project.Core;
using Project.Services.Drivers;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

/// <summary>Driver app: vehicle, dynamic document checklist, upload and submit.</summary>
public class DriverController : BaseProtectedController
{
    #region Fields

    private readonly IWorkContext _workContext;
    private readonly IDriverOnboardingService _onboardingService;

    #endregion

    #region Constructor

    public DriverController(IWorkContext workContext, IDriverOnboardingService onboardingService)
    {
        _workContext = workContext;
        _onboardingService = onboardingService;
    }

    #endregion

    #region Methods

    [HttpGet]
    public async Task<IActionResult> GetOnboardingStatus()
    {
        var driver = await _workContext.GetCurrentUserAsync();
        if (driver == null)
            return Error(string.Empty, "User not found.");

        return Success(await _onboardingService.GetSummaryAsync(driver.Id));
    }

    [HttpGet]
    public async Task<IActionResult> GetDocumentRequirements()
    {
        var driver = await _workContext.GetCurrentUserAsync();
        if (driver == null)
            return Error(string.Empty, "User not found.");

        return Success(await _onboardingService.GetRequirementsAsync(driver.Id));
    }

    [HttpGet]
    public async Task<IActionResult> GetVehicle()
    {
        var driver = await _workContext.GetCurrentUserAsync();
        if (driver == null)
            return Error(string.Empty, "User not found.");

        var v = await _onboardingService.GetVehicleAsync(driver.Id);
        return Success(new DriverVehicleModel
        {
            VehicleCategoryId = v?.VehicleCategoryId ?? 0,
            RegistrationNumber = v?.RegistrationNumber,
            Make = v?.Make,
            Model = v?.Model,
            Year = v?.Year,
            Color = v?.Color
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveVehicle(DriverVehicleModel model)
    {
        var driver = await _workContext.GetCurrentUserAsync();
        if (driver == null)
            return Error(string.Empty, "User not found.");

        try
        {
            await _onboardingService.SaveVehicleAsync(driver.Id, model.VehicleCategoryId,
                model.RegistrationNumber, model.Make, model.Model, model.Year, model.Color, driver.Id);
            return Success(string.Empty, "Vehicle saved");
        }
        catch (OnboardingException ex)
        {
            return Error(string.Empty, ex.Message);
        }
    }

    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentModel model)
    {
        var driver = await _workContext.GetCurrentUserAsync();
        if (driver == null)
            return Error(string.Empty, "User not found.");

        var files = new List<UploadedFile>();
        try
        {
            foreach (var f in model.Files ?? new())
            {
                // Copy to memory so the stream is seekable for validation and upload.
                if (f.Length > 10 * 1024 * 1024)
                    return Error(string.Empty, "File is too large");

                var ms = new MemoryStream();
                await f.CopyToAsync(ms);
                ms.Position = 0;
                files.Add(new UploadedFile
                {
                    FileName = Path.GetFileName(f.FileName),
                    ContentType = f.ContentType,
                    Length = ms.Length,
                    Content = ms
                });
            }

            await _onboardingService.SaveDocumentAsync(driver.Id, model.DocumentTypeId,
                model.DocumentNumber, model.ExpiryDate, files, driver.Id);
            return Success(string.Empty, "Document uploaded");
        }
        catch (OnboardingException ex)
        {
            return Error(string.Empty, ex.Message);
        }
        finally
        {
            foreach (var f in files)
                f.Content?.Dispose();
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitForReview()
    {
        var driver = await _workContext.GetCurrentUserAsync();
        if (driver == null)
            return Error(string.Empty, "User not found.");

        try
        {
            await _onboardingService.SubmitForReviewAsync(driver.Id, driver.Id);
            return Success(string.Empty, "Submitted for review");
        }
        catch (OnboardingException ex)
        {
            return Error(string.Empty, ex.Message);
        }
    }

    /// <summary>Owner or admin only. The app sends its Bearer token with the request.</summary>
    [HttpGet]
    public async Task<IActionResult> DownloadDocumentFile(long id)
    {
        var user = await _workContext.GetCurrentUserAsync();
        if (user == null)
            return Unauthorized();

        var isAdmin = await _workContext.IsCurrentUserIsAdmin();
        try
        {
            var (file, stream) = await _onboardingService.OpenFileAsync(id, user.Id, isAdmin);
            return File(stream, file.ContentType, file.FileName);
        }
        catch (OnboardingException)
        {
            return NotFound();
        }
    }

    #endregion
}
