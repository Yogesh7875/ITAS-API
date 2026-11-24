using Project.Core.Domain.Common;

namespace Project.Core.Domain.Drivers;

public enum DocumentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Expired = 3
}

public partial class UserDocument : BaseEntity, ISoftDeletedEntity
{
    public long UserId { get; set; }
    public long DocumentTypeId { get; set; }
    public string DocumentNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public int StatusId { get; set; }
    public DocumentStatus Status
    {
        get => (DocumentStatus)StatusId;
        set => StatusId = (int)value;
    }

    public string RejectionReason { get; set; }
    public long? ReviewedByUserId { get; set; }
    public DateTime? ReviewedOnUtc { get; set; }

    public bool Deleted { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public long CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}
