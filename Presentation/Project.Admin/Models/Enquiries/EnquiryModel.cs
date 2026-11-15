using Project.Web.Framework.Models;

namespace Project.Admin.Models.Enquiries;

public record EnquiryModel : BaseModel
{
    public long? CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
    public long CentralId { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string CustomerName { get; set; }
    public string EmailId { get; set; }
    public string Purpose { get; set; }
    public string MobileNo { get; set; }
    public string Status { get; set; }
    public string Type { get; set; }
    public string ImagePath { get; set; }
}

public record EnquiryListModel : BaseResponseListModel<EnquiryModel>
{
}
