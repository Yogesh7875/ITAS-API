using Project.Core.Constant;
using Project.Core.Domain.Common;

namespace Project.Core.Domain.Drivers;


public partial class DriverProfile : BaseEntity, ISoftDeletedEntity
{
    public long UserId { get; set; }
    public string City { get; set; }

    public int OnboardingStatusId { get; set; }
    public DriverOnboardingStatus OnboardingStatus
    {
        get => (DriverOnboardingStatus)OnboardingStatusId;
        set => OnboardingStatusId = (int)value;
    }

    public DateTime? SubmittedOnUtc { get; set; }
    public DateTime? ReviewedOnUtc { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string RejectionReason { get; set; }

    public bool Deleted { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public long CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}
