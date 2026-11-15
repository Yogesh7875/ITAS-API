using Project.Web.Framework.Models;

namespace Project.Admin.Models.Enquiries;

public partial record EnquirySearchModel : BaseSearchModel
{
    public string SearchText { get; set; }
    public string OrderByColumn { get; set; }
}
