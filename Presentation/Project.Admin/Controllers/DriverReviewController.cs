using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Drivers;
using Project.Core;
using Project.Services.Drivers;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

/// <summary>Admin: review submitted drivers and their documents.</summary>
public class DriverReviewController : BaseProtectedController
{
    private readonly IWorkContext _workContext;
    private readonly IDriverOnboardingService _onboardingService;

    public DriverReviewController(IWorkContext workContext, IDriverOnboardingService onboardingService)
    {
        _workContext = workContext;
        _onboardingService = onboardingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSubmittedDrivers()
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        return Success(await _onboardingService.GetSubmittedDriversAsync());
    }

    [HttpGet]
    public async Task<IActionResult> GetDriverReview(long userId)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var vehicle = await _onboardingService.GetVehicleAsync(userId);
        var documents = await _onboardingService.GetRequirementsAsync(userId);
        var summary = await _onboardingService.GetSummaryAsync(userId);
        return Success(new { summary, vehicle, documents });
    }

    [HttpPost]
    public async Task<IActionResult> ReviewDocument(ReviewDocumentModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        try
        {
            var admin = await _workContext.GetCurrentUserAsync();
            await _onboardingService.ReviewDocumentAsync(model.UserDocumentId, model.Approve, model.Reason, admin.Id);
            return Success(string.Empty, model.Approve ? "Document approved" : "Document rejected");
        }
        catch (OnboardingException ex)
        {
            return Error(string.Empty, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> ReviewDriver(ReviewDriverModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        try
        {
            var admin = await _workContext.GetCurrentUserAsync();
            await _onboardingService.ReviewDriverAsync(model.UserId, model.Approve, model.Reason, admin.Id);
            return Success(string.Empty, model.Approve ? "Driver approved" : "Driver rejected");
        }
        catch (OnboardingException ex)
        {
            return Error(string.Empty, ex.Message);
        }
    }
}
