using Project.Web.Framework.Models;

namespace Project.Admin.Models.Users;

public partial record UserModel : BaseModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
    public DateTime? Dob { get; set; }
    public string Gender { get; set; }
    public string Address { get; set; }
}
