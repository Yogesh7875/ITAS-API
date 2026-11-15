using Project.Web.Framework.Models;

namespace Project.Admin.Models.Catalog;

/// <summary>
/// Represents user role permissions
/// </summary>
public partial record UserRolePermissionModel : BaseModel
{
    #region Constructor

    public UserRolePermissionModel()
    {
        Permissions = new List<SelectListModel>();
        AvailablePermissions = new List<SelectListModel>();
    }

    #endregion

    #region Properties

    public IList<SelectListModel> Permissions { get; set; }
    public IList<SelectListModel> AvailablePermissions { get; set; }

    #endregion
}
