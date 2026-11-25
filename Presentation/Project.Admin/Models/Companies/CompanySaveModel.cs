using Project.Web.Framework.Models;

namespace Project.Admin.Models.Companies;

public record CompanySaveModel : BaseModel
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string EmailDomain { get; set; }   // "infosys.com"
    public decimal CreditLimit { get; set; }
    public int BillingCycleDays { get; set; } = 30;
    public string Remark { get; set; }
    public bool Active { get; set; } = true;
}
