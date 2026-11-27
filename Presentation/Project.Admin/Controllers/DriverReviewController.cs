using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Common;
using Project.Admin.Models.Drivers;
using Project.Core;
using Project.Services.Admin;
using Project.Services.Drivers;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

/// <summary>Admin: review submitted drivers and their documents.</summary>
public class DriverReviewController : BaseProtectedController
{
    private readonly IWorkContext _workContext;
    private readonly IDriverOnboardingService _onboardingService;
    private readonly IAdminQueryService _adminQueryService;

    public DriverReviewController(IWorkContext workContext, IDriverOnboardingService onboardingService,
        IAdminQueryService adminQueryService)
    {
        _workContext = workContext;
        _onboardingService = onboardingService;
        _adminQueryService = adminQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSubmittedDrivers()
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        return Success(await _onboardingService.GetSubmittedDriversAsync());
    }

    /// <summary>All drivers, paged. Submitted (waiting for review) drivers come first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllDrivers([FromQuery] AdminDriverFilterModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var list = await _adminQueryService.GetDriversAsync(new AdminDriverSearch
        {
            SearchText = model.Search,
            OnboardingStatusId = model.OnboardingStatusId,
            VehicleCategoryId = model.VehicleCategoryId,
            Active = model.Active,
            PageIndex = model.PageIndex,
            PageSize = model.PageSize
        });
        return Success(PagedResultModel<AdminDriverListItem>.From(list));
    }

    /// <summary>One driver: personal details, onboarding status, vehicle and every document with its files.</summary>
    [HttpGet]
    public async Task<IActionResult> GetDriverDetail(long userId)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var detail = await _adminQueryService.GetDriverDetailAsync(userId);
        if (detail == null)
            return Error(string.Empty, "Driver not found");

        return Success(detail);
    }

    /// <summary>All uploaded driver documents, paged. Pending documents come first, oldest first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllDriverDocuments([FromQuery] AdminDocumentFilterModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var list = await _adminQueryService.GetDriverDocumentsAsync(new AdminDocumentSearch
        {
            SearchText = model.Search,
            UserId = model.UserId,
            DocumentTypeId = model.DocumentTypeId,
            StatusId = model.StatusId,
            PageIndex = model.PageIndex,
            PageSize = model.PageSize
        });
        return Success(PagedResultModel<AdminDocumentListItem>.From(list));
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
