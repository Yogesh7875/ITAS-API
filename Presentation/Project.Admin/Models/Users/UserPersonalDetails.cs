using Project.Web.Framework.Models;

namespace Project.Admin.Models.Users;

public record UserPersonalDetails:BaseModel
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string EmailAddress { get; set; }
}
