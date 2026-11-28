using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Drivers;
using Project.Core;
using Project.Core.Domain.Master;
using Project.Services.Common.DataModel;
using Project.Services.Master;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

public class VehicleCategoryController : BaseProtectedController
{
    private readonly IWorkContext _workContext;
    private readonly IVehicleCategoryService _categoryService;

    public VehicleCategoryController(IWorkContext workContext, IVehicleCategoryService categoryService)
    {
        _workContext = workContext;
        _categoryService = categoryService;
    }

    /// <summary>Public list for the driver's vehicle screen (and ride options later).</summary>
    [HttpGet]
    public async Task<IActionResult> GetSelectDataModelList()
    {
        var list = (await _categoryService.GetAllVehicleCategoriesAsync())
            .Select(c => new SelectListDataModel { Id = c.Id, Name = c.Name, ReferenseId = c.Seats })
            .ToList();
        return Success(list);
    }

    /// <summary>All categories (active and inactive) with how many vehicles use each.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var categories = await _categoryService.GetAllVehicleCategoriesAsync(showHidden: true);
        var counts = await _categoryService.GetVehicleCountsAsync();

        return Success(categories.Select(c => new
        {
            c.Id,
            c.Name,
            c.Seats,
            c.DisplayOrder,
            c.Active,
            c.CreatedOnUtc,
            c.UpdatedOnUtc,
            VehicleCount = counts.TryGetValue(c.Id, out var n) ? n : 0
        }).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Save(VehicleCategorySaveModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var name = model.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            return Error(string.Empty, "Name is required");
        if (name.Length > 100)
            return Error(string.Empty, "Name must be 100 characters or fewer");
        if (model.Seats < 1 || model.Seats > 50)
            return Error(string.Empty, "Seats must be between 1 and 50");

        var byName = await _categoryService.GetVehicleCategoryByNameAsync(name);
        if (byName != null && byName.Id != model.Id)
            return Error(string.Empty, "A category with this name already exists");

        var entity = model.Id > 0 ? await _categoryService.GetVehicleCategoryByIdAsync(model.Id) : new VehicleCategory();
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Category not found");

        entity.Name = name;
        entity.Seats = model.Seats;
        entity.DisplayOrder = model.DisplayOrder;
        entity.Active = model.Active;

        var currentAdmin = await _workContext.GetCurrentUserAsync();
        entity.UpdatedBy = currentAdmin?.Id;

        if (entity.Id == 0)
        {
            entity.CreatedBy = currentAdmin?.Id ?? 0;
            await _categoryService.InsertVehicleCategoryAsync(entity);
        }
        else
        {
            await _categoryService.UpdateVehicleCategoryAsync(entity);
        }

        return Success(new { entity.Id }, "Saved");
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(long id, bool active)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var entity = await _categoryService.GetVehicleCategoryByIdAsync(id);
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Category not found");

        entity.Active = active;
        entity.UpdatedBy = (await _workContext.GetCurrentUserAsync())?.Id;
        await _categoryService.UpdateVehicleCategoryAsync(entity);
        return Success(string.Empty, active ? "Category activated" : "Category deactivated");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var entity = await _categoryService.GetVehicleCategoryByIdAsync(id);
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Category not found");

        var counts = await _categoryService.GetVehicleCountsAsync();
        if (counts.TryGetValue(id, out var vehicles) && vehicles > 0)
            return Error(string.Empty, $"{vehicles} vehicle(s) are in this category. Deactivate it instead.");

        var currentAdmin = await _workContext.GetCurrentUserAsync();
        entity.UpdatedBy = currentAdmin?.Id;
        entity.UpdatedOnUtc = CommonHelper.IndianTimeNow;

        await _categoryService.DeleteVehicleCategoryAsync(entity);
        return Success(string.Empty, "Deleted");
    }
}
