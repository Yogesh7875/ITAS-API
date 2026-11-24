using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Drivers;
using Project.Core;
using Project.Core.Domain.Master;
using Project.Services.Common.DataModel;
using Project.Services.Master;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

public class VehicleCategoryController : BaseController
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

    [HttpPost, Authorize]
    public async Task<IActionResult> Save(VehicleCategorySaveModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");
        if (string.IsNullOrWhiteSpace(model.Name))
            return Error(string.Empty, "Name is required");

        var entity = model.Id > 0 ? await _categoryService.GetVehicleCategoryByIdAsync(model.Id) : new VehicleCategory();
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Category not found");

        entity.Name = model.Name.Trim();
        entity.Seats = model.Seats;
        entity.DisplayOrder = model.DisplayOrder;
        entity.Active = model.Active;

        if (entity.Id == 0)
            await _categoryService.InsertVehicleCategoryAsync(entity);
        else
            await _categoryService.UpdateVehicleCategoryAsync(entity);

        return Success(new { entity.Id }, "Saved");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Delete(long id)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var entity = await _categoryService.GetVehicleCategoryByIdAsync(id);
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Category not found");

        await _categoryService.DeleteVehicleCategoryAsync(entity);
        return Success(string.Empty, "Deleted");
    }
}
