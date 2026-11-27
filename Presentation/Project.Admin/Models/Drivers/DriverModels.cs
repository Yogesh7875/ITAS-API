using Project.Admin.Models.Common;

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

/// <summary>application/json. Files are base64 encoded, in order: first = front (or single), second = back.</summary>
public class UploadDocumentModel
{
    public long DocumentTypeId { get; set; }
    public string DocumentNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<Base64FileModel> Files { get; set; } = new();
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

public class AdminDriverFilterModel
{
    public string Search { get; set; }
    /// <summary>0 Draft, 1 Submitted, 2 Approved, 3 Rejected</summary>
    public int? OnboardingStatusId { get; set; }
    public long? VehicleCategoryId { get; set; }
    public bool? Active { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}

public class AdminDocumentFilterModel
{
    public string Search { get; set; }
    public long? UserId { get; set; }
    public long? DocumentTypeId { get; set; }
    /// <summary>0 Pending, 1 Approved, 2 Rejected, 3 Expired</summary>
    public int? StatusId { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
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
