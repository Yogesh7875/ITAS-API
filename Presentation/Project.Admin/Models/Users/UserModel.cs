using Project.Web.Framework.Models;

namespace Project.Admin.Models.Users;

public partial record UserModel : BaseModel
{
    #region Constructor

    public UserModel()
    {
      
        AvailableRoles = new List<SelectListModel>();
    }

    #endregion

    #region Properties

    public string Name { get; set; }
    public string EmailAddress { get; set; }
    public string MobileNumber { get; set; }
    public string UserName { get; set; }
    public bool Active { get; set; }
    public long RoleId { get; set; }
    public string Password { get; set; }
    public string Remark { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool MultiDeviceLoginEnabled { get; set; }

    public IList<SelectListModel> AvailableRoles { get; set; }

    #endregion
}
