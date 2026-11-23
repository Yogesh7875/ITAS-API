using Project.Web.Framework.Models;

namespace Project.Admin.Models.Users;

public record CorporateUserDetails : BaseModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
    public long CompanyId { get; set; }
}
