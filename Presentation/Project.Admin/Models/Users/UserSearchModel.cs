using Project.Web.Framework.Models;

namespace Project.Admin.Models.Users;

/// <summary>
/// Represents user search model
/// </summary>
public partial record UserSearchModel : BaseSearchModel
{
    #region Properties
    public string SearchText { get; set; }
    #endregion
}
