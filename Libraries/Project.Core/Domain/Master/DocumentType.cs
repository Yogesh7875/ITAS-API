using Project.Core.Domain.Common;

namespace Project.Core.Domain.Master;

public enum DocumentAppliesTo
{
    Driver = 1,
    Vehicle = 2
}

/// <summary>
/// Master: a document the admin wants drivers to upload (PAN, RC, Insurance...).
/// The driver app builds its upload screen from these rows.
/// </summary>
public partial class DocumentType : BaseEntity, ISoftDeletedEntity
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public int AppliesToId { get; set; }
    public DocumentAppliesTo AppliesTo
    {
        get => (DocumentAppliesTo)AppliesToId;
        set => AppliesToId = (int)value;
    }

    public bool IsMandatory { get; set; }

    public bool RequiresNumber { get; set; }
    public string NumberLabel { get; set; }
    public string NumberRegex { get; set; }

    public bool RequiresExpiry { get; set; }

    /// <summary>1 = single file, 2 = front and back</summary>
    public int FilesRequired { get; set; }
    public string AllowedFileTypes { get; set; }
    public int MaxFileSizeKb { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public long CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}
