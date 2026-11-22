using Project.Core.Domain.Common;

namespace Project.Core.Domain.Users;

public partial class Company : BaseEntity, ISoftDeletedEntity
{
    public string Name { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public long CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}
