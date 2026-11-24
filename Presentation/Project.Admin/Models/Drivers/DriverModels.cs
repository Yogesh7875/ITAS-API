using Microsoft.AspNetCore.Http;

namespace Project.Admin.Models.Drivers;

public class DriverVehicleModel
{
    public long VehicleCategoryId { get; set; }
    public string RegistrationNumber { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public int? Year { get; set; }
    public string Color { get; set; }
}

/// <summary>multipart/form-data. Files in order: first = front (or single), second = back.</summary>
public class UploadDocumentModel
{
    public long DocumentTypeId { get; set; }
    public string DocumentNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<IFormFile> Files { get; set; } = new();
}

public class ReviewDocumentModel
{
    public long UserDocumentId { get; set; }
    public bool Approve { get; set; }
    public string Reason { get; set; }
}

public class ReviewDriverModel
{
    public long UserId { get; set; }
    public bool Approve { get; set; }
    public string Reason { get; set; }
}

public class DocumentTypeSaveModel
{
    public long Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int AppliesToId { get; set; }
    public bool IsMandatory { get; set; }
    public bool RequiresNumber { get; set; }
    public string NumberLabel { get; set; }
    public string NumberRegex { get; set; }
    public bool RequiresExpiry { get; set; }
    public int FilesRequired { get; set; } = 1;
    public string AllowedFileTypes { get; set; } = "jpg,jpeg,png,pdf";
    public int MaxFileSizeKb { get; set; } = 5120;
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
}

public class VehicleCategorySaveModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Seats { get; set; } = 4;
    public int DisplayOrder { get; set; }
    public bool Active { get; set; } = true;
}
