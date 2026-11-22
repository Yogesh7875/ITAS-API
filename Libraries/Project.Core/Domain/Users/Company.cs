using Project.Core.Domain.Common;

namespace Project.Core.Domain.Users;

public partial class Company : BaseEntity, ISoftDeletedEntity
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string EmailDomain { get; set; }   // "infosys.com"
    public decimal CreditLimit { get; set; }
    public int BillingCycleDays { get; set; }
    public string Remark { get; set; }
    public bool Active { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
}
