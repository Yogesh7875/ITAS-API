using Project.Core.Domain.Common;

namespace Project.Core.Domain.Drivers;

/// <summary>One vehicle per driver for now (unique on UserId).</summary>
public partial class DriverVehicle : BaseEntity, ISoftDeletedEntity
{
    public long UserId { get; set; }
    public long VehicleCategoryId { get; set; }
    public string RegistrationNumber { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public int? Year { get; set; }
    public string Color { get; set; }

    public bool Deleted { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public long CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}
