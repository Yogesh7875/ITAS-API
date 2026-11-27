namespace Project.Admin.Models.Users;

public class AdminUserFilterModel
{
    public string RoleName { get; set; }
    public string Search { get; set; }
    public long? CompanyId { get; set; }
    public bool? HasCompany { get; set; }
    /// <summary>0 Pending, 1 Approved, 2 Rejected</summary>
    public int? ApprovalStatusId { get; set; }
    public bool? Active { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}

public class ReviewCorporateUserModel
{
    public long UserId { get; set; }
    public bool Approve { get; set; }
    public string Reason { get; set; }
}
