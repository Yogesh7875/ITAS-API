namespace Project.Core.Domain.Drivers;

/// <summary>One uploaded file of a document (front / back / single).</summary>
public partial class UserDocumentFile : BaseEntity
{
    public long UserDocumentId { get; set; }

    /// <summary>1 = front or single, 2 = back</summary>
    public int Side { get; set; }

    /// <summary>"Local" or "Cloudinary" - so files stay readable if you switch provider.</summary>
    public string StorageProvider { get; set; }
    public string StorageKey { get; set; }

    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public long CreatedBy { get; set; }
}
