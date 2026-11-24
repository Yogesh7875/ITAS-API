using Project.Core.Domain.Common;

namespace Project.Core.Domain.Master;

/// <summary>Master: Mini, Sedan, SUV...</summary>
public partial class VehicleCategory : BaseEntity, ISoftDeletedEntity
{
    public string Name { get; set; }
    public int Seats { get; set; }
    public int DisplayOrder { get; set; }
    public bool Active { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public long CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}
