using System.Text.RegularExpressions;
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
    private static readonly HashSet<string> KnownFileTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "webp", "heic", "pdf"
    };

    private readonly IWorkContext _workContext;
    private readonly IDocumentTypeService _documentTypeService;

    public DocumentTypeController(IWorkContext workContext, IDocumentTypeService documentTypeService)
    {
        _workContext = workContext;
        _documentTypeService = documentTypeService;
    }

    /// <summary>All document types (active and inactive) with how many uploads each has.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var types = await _documentTypeService.GetAllDocumentTypesAsync(showHidden: true);
        var counts = await _documentTypeService.GetUploadCountsAsync();

        return Success(types.Select(t => new
        {
            t.Id,
            t.Code,
            t.Name,
            t.Description,
            t.AppliesToId,
            t.IsMandatory,
            t.RequiresNumber,
            t.NumberLabel,
            t.NumberRegex,
            t.RequiresExpiry,
            t.FilesRequired,
            t.AllowedFileTypes,
            t.MaxFileSizeKb,
            t.DisplayOrder,
            t.Active,
            t.CreatedOnUtc,
            t.UpdatedOnUtc,
            UploadCount = counts.TryGetValue(t.Id, out var n) ? n : 0
        }).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Save(DocumentTypeSaveModel model)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var code = model.Code?.Trim().ToUpperInvariant();
        var name = model.Name?.Trim();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return Error(string.Empty, "Code and name are required");
        if (!Regex.IsMatch(code, "^[A-Z0-9_]{2,30}$"))
            return Error(string.Empty, "Code can only use letters, numbers and underscores (2-30 characters)");
        if (model.AppliesToId != (int)DocumentAppliesTo.Driver && model.AppliesToId != (int)DocumentAppliesTo.Vehicle)
            return Error(string.Empty, "Applies to must be Driver (1) or Vehicle (2)");
        if (model.FilesRequired < 1 || model.FilesRequired > 2)
            return Error(string.Empty, "Files required must be 1 or 2");
        if (model.MaxFileSizeKb < 100 || model.MaxFileSizeKb > 20480)
            return Error(string.Empty, "Max file size must be between 100 KB and 20 MB");

        var regex = model.NumberRegex?.Trim();
        if (model.RequiresNumber && !string.IsNullOrEmpty(regex))
        {
            try { _ = new Regex(regex); }
            catch (ArgumentException) { return Error(string.Empty, "Number format is not a valid regular expression"); }
        }

        var fileTypes = (model.AllowedFileTypes ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.TrimStart('.').ToLowerInvariant())
            .Distinct()
            .ToList();
        if (fileTypes.Count == 0)
            fileTypes = ["jpg", "jpeg", "png", "pdf"];
        var unknown = fileTypes.Where(x => !KnownFileTypes.Contains(x)).ToList();
        if (unknown.Count > 0)
            return Error(string.Empty, $"Unsupported file type: {string.Join(", ", unknown)}");

        var byCode = await _documentTypeService.GetDocumentTypeByCodeAsync(code);
        if (byCode != null && byCode.Id != model.Id)
            return Error(string.Empty, "A document type with this code already exists");

        var entity = model.Id > 0 ? await _documentTypeService.GetDocumentTypeByIdAsync(model.Id) : new DocumentType();
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Document type not found");

        entity.Code = code;
        entity.Name = name;
        entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        entity.AppliesToId = model.AppliesToId;
        entity.IsMandatory = model.IsMandatory;
        entity.RequiresNumber = model.RequiresNumber;
        entity.NumberLabel = model.RequiresNumber ? model.NumberLabel?.Trim() : null;
        entity.NumberRegex = model.RequiresNumber && !string.IsNullOrEmpty(regex) ? regex : null;
        entity.RequiresExpiry = model.RequiresExpiry;
        entity.FilesRequired = model.FilesRequired;
        entity.AllowedFileTypes = string.Join(",", fileTypes);
        entity.MaxFileSizeKb = model.MaxFileSizeKb;
        entity.DisplayOrder = model.DisplayOrder;
        entity.Active = model.Active;

        var currentAdmin = await _workContext.GetCurrentUserAsync();
        entity.UpdatedBy = currentAdmin?.Id;

        if (entity.Id == 0)
        {
            entity.CreatedBy = currentAdmin?.Id ?? 0;
            await _documentTypeService.InsertDocumentTypeAsync(entity);
        }
        else
        {
            await _documentTypeService.UpdateDocumentTypeAsync(entity);
        }

        return Success(new { entity.Id }, "Saved");
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(long id, bool active)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var entity = await _documentTypeService.GetDocumentTypeByIdAsync(id);
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Document type not found");

        entity.Active = active;
        entity.UpdatedBy = (await _workContext.GetCurrentUserAsync())?.Id;
        await _documentTypeService.UpdateDocumentTypeAsync(entity);
        return Success(string.Empty, active ? "Document type activated" : "Document type deactivated");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        if (!await _workContext.IsCurrentUserIsAdmin())
            return Error(string.Empty, "Admin only");

        var entity = await _documentTypeService.GetDocumentTypeByIdAsync(id);
        if (entity == null || entity.Deleted)
            return Error(string.Empty, "Document type not found");

        // Uploaded documents still point at this type; deleting it would orphan them in review.
        var counts = await _documentTypeService.GetUploadCountsAsync();
        if (counts.TryGetValue(id, out var uploads) && uploads > 0)
            return Error(string.Empty, $"{uploads} driver upload(s) use this document type. Deactivate it instead.");

        var currentAdmin = await _workContext.GetCurrentUserAsync();
        entity.UpdatedBy = currentAdmin?.Id;
        entity.UpdatedOnUtc = CommonHelper.IndianTimeNow;

        await _documentTypeService.DeleteDocumentTypeAsync(entity);
        return Success(string.Empty, "Deleted");
    }
}
