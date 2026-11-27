namespace Project.Services.Drivers;

/// <summary>Thrown for expected validation problems; the controller shows the message.</summary>
public class OnboardingException : Exception
{
    public OnboardingException(string message) : base(message)
    {
    }
}

public class UploadedFile
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Length { get; set; }
    public Stream Content { get; set; }
}

public class DocumentFileDataModel
{
    public long Id { get; set; }
    public int Side { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long SizeBytes { get; set; }
}

/// <summary>One row of the driver's document checklist (built from the DocumentType master).</summary>
public class DocumentRequirementDataModel
{
    public long DocumentTypeId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string AppliesTo { get; set; }
    public bool IsMandatory { get; set; }
    public bool RequiresNumber { get; set; }
    public string NumberLabel { get; set; }
    public string NumberRegex { get; set; }
    public bool RequiresExpiry { get; set; }
    public int FilesRequired { get; set; }
    public string AllowedFileTypes { get; set; }
    public int MaxFileSizeKb { get; set; }

    /// <summary>NotUploaded, Pending, Approved, Rejected or Expired</summary>
    public string Status { get; set; }
    public long? UserDocumentId { get; set; }
    public string DocumentNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string RejectionReason { get; set; }
    public IList<DocumentFileDataModel> Files { get; set; } = new List<DocumentFileDataModel>();
}

public class OnboardingSummaryDataModel
{
    /// <summary>Draft, Submitted, Approved or Rejected</summary>
    public string Status { get; set; }
    public string RejectionReason { get; set; }
    public bool HasVehicle { get; set; }
    public int RequiredCount { get; set; }
    public int UploadedCount { get; set; }
    public int ApprovedCount { get; set; }
    public bool CanSubmit { get; set; }

    /// <summary>True once the driver has saved first and last name (UpdateDriverPersonalDetails)</summary>
    public bool PersonalDetailsCompleted { get; set; }
}

public class SubmittedDriverDataModel
{
    public long UserId { get; set; }
    public string Name { get; set; }
    public string MobileNumber { get; set; }
    public DateTime? SubmittedOnUtc { get; set; }
}
