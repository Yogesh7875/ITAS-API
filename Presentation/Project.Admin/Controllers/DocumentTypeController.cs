using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Drivers;
using Project.Core;
using Project.Core.Domain.Master;
using Project.Services.Master;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

/// <summary>Admin: manage the document master (what drivers must upload).</summary>
public class DocumentTypeController : BaseProtectedController
{
    private readonly IWorkContext _workContext;
    private readonly IDocumentTypeService _documentTypeService;

    public DocumentTypeController(IWorkContext workContext, IDocumentTypeService documentTypeService)
    {
        _workContext = workContext;
        _documentTypeService = documentTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        return Success(await _documentTypeService.GetAllDocumentTypesAsync(showHidden: true));
    }

    [HttpPost]
    public async Task<IActionResult> Save(DocumentTypeSaveModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
            return Error(string.Empty, "Code and name are required");
        if (model.AppliesToId != (int)DocumentAppliesTo.Driver && model.AppliesToId != (int)DocumentAppliesTo.Vehicle)
            return Error(string.Empty, "Applies to must be Driver (1) or Vehicle (2)");
        if (model.FilesRequired < 1 || model.FilesRequired > 2)
            return Error(string.Empty, "Files required must be 1 or 2");

        var entity = model.Id > 0 ? await _documentTypeService.GetDocumentTypeByIdAsync(model.Id) : new DocumentType();
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Document type not found");

        entity.Code = model.Code.Trim().ToUpperInvariant();
        entity.Name = model.Name.Trim();
        entity.Description = model.Description?.Trim();
        entity.AppliesToId = model.AppliesToId;
        entity.IsMandatory = model.IsMandatory;
        entity.RequiresNumber = model.RequiresNumber;
        entity.NumberLabel = model.NumberLabel?.Trim();
        entity.NumberRegex = model.NumberRegex?.Trim();
        entity.RequiresExpiry = model.RequiresExpiry;
        entity.FilesRequired = model.FilesRequired;
        entity.AllowedFileTypes = model.AllowedFileTypes?.Trim().ToLowerInvariant() ?? "jpg,jpeg,png,pdf";
        entity.MaxFileSizeKb = model.MaxFileSizeKb;
        entity.DisplayOrder = model.DisplayOrder;
        entity.Active = model.Active;

        if (entity.Id == 0)
            await _documentTypeService.InsertDocumentTypeAsync(entity);
        else
            await _documentTypeService.UpdateDocumentTypeAsync(entity);

        return Success(new { entity.Id }, "Saved");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var entity = await _documentTypeService.GetDocumentTypeByIdAsync(id);
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Document type not found");

        await _documentTypeService.DeleteDocumentTypeAsync(entity);
        return Success(string.Empty, "Deleted");
    }
}
