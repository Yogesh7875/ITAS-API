using Project.Services.Drivers;

namespace Project.Services.Admin;

/// <summary>Approval status stored on User.ApprovalStatusId (corporate users and drivers).</summary>
public static class ApprovalStatus
{
    public const int Pending = 0;
    public const int Approved = 1;
    public const int Rejected = 2;

    public static string Name(int id) => id switch
    {
        Approved => "Approved",
        Rejected => "Rejected",
        _ => "Pending"
    };
}

public class AdminUserSearch
{
    /// <summary>Role name filter, for example "User" or "Corporate". Null = all roles.</summary>
    public string RoleName { get; set; }
    public string SearchText { get; set; }
    public long? CompanyId { get; set; }
    /// <summary>True = only users linked to a company (corporate users who submitted details).</summary>
    public bool? HasCompany { get; set; }
    public int? ApprovalStatusId { get; set; }
    public bool? Active { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; } = 20;
}

public class AdminUserListItem
{
    public long Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
    public string MobileNumber { get; set; }
    public long RoleId { get; set; }
    public string RoleName { get; set; }
    public long? CompanyId { get; set; }
    public string CompanyName { get; set; }
    public int ApprovalStatusId { get; set; }
    public string ApprovalStatus { get; set; }
    public bool Active { get; set; }
    public bool IsBanned { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

public class AdminUserDetail : AdminUserListItem
{
    public DateTime? Dob { get; set; }
    public string Gender { get; set; }
    public string Address { get; set; }
    public string Remark { get; set; }
    public string CompanyEmailDomain { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
}

public class AdminDriverSearch
{
    public string SearchText { get; set; }
    /// <summary>DriverOnboardingStatus: 0 Draft, 1 Submitted, 2 Approved, 3 Rejected. Null = all.</summary>
    public int? OnboardingStatusId { get; set; }
    public long? VehicleCategoryId { get; set; }
    public bool? Active { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; } = 20;
}

public class AdminDriverListItem
{
    public long UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MobileNumber { get; set; }
    public string EmailAddress { get; set; }
    public string City { get; set; }
    public bool Active { get; set; }
    public bool IsBanned { get; set; }
    public int OnboardingStatusId { get; set; }
    public string OnboardingStatus { get; set; }
    public DateTime? SubmittedOnUtc { get; set; }
    public DateTime? ReviewedOnUtc { get; set; }
    public long? VehicleCategoryId { get; set; }
    public string VehicleRegistrationNumber { get; set; }
    public string VehicleCategoryName { get; set; }
    public int DocumentsUploaded { get; set; }
    public int DocumentsPending { get; set; }
    public int DocumentsApproved { get; set; }
    public int DocumentsRejected { get; set; }
    public int DocumentsExpired { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

public class AdminDriverPersonalDetails
{
    public long UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MobileNumber { get; set; }
    public string EmailAddress { get; set; }
    public DateTime? Dob { get; set; }
    public string Gender { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public bool Active { get; set; }
    public bool IsBanned { get; set; }
    public int ApprovalStatusId { get; set; }
    public string ApprovalStatus { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

public class AdminDriverVehicle
{
    public long Id { get; set; }
    public long VehicleCategoryId { get; set; }
    public string VehicleCategoryName { get; set; }
    public string RegistrationNumber { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public int? Year { get; set; }
    public string Color { get; set; }
}

public class AdminDriverOnboarding
{
    public int OnboardingStatusId { get; set; }
    public string OnboardingStatus { get; set; }
    public DateTime? SubmittedOnUtc { get; set; }
    public DateTime? ReviewedOnUtc { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string ReviewedByName { get; set; }
    public string RejectionReason { get; set; }
}

public class AdminDriverDetail
{
    public AdminDriverPersonalDetails PersonalDetails { get; set; }
    public AdminDriverOnboarding Onboarding { get; set; }
    public OnboardingSummaryDataModel Summary { get; set; }
    public AdminDriverVehicle Vehicle { get; set; }
    public IList<DocumentRequirementDataModel> Documents { get; set; }
}

public class AdminDocumentSearch
{
    public string SearchText { get; set; }
    public long? UserId { get; set; }
    public long? DocumentTypeId { get; set; }
    /// <summary>DocumentStatus: 0 Pending, 1 Approved, 2 Rejected, 3 Expired. Null = all.</summary>
    public int? StatusId { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; } = 20;
}

public class AdminDocumentListItem
{
    public long UserDocumentId { get; set; }
    public long UserId { get; set; }
    public string DriverName { get; set; }
    public string MobileNumber { get; set; }
    public long DocumentTypeId { get; set; }
    public string DocumentTypeCode { get; set; }
    public string DocumentTypeName { get; set; }
    public bool IsMandatory { get; set; }
    public string DocumentNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int StatusId { get; set; }
    public string Status { get; set; }
    public string RejectionReason { get; set; }
    public DateTime? ReviewedOnUtc { get; set; }
    public long? ReviewedByUserId { get; set; }
    public DateTime UploadedOnUtc { get; set; }
    public IList<DocumentFileDataModel> Files { get; set; } = new List<DocumentFileDataModel>();
}
