using Project.Core;

namespace Project.Services.Admin;

/// <summary>Read-side queries and approvals for the admin panel: users, corporate users, drivers and driver documents.</summary>
public partial interface IAdminQueryService
{
    Task<IPagedList<AdminUserListItem>> GetUsersAsync(AdminUserSearch search);

    Task<AdminUserDetail> GetUserDetailAsync(long userId);

    /// <summary>Approve or reject a corporate user's company link. A reason is required to reject.</summary>
    Task SetCorporateApprovalAsync(long userId, bool approve, string reason, long adminUserId);

    Task<IPagedList<AdminDriverListItem>> GetDriversAsync(AdminDriverSearch search);

    Task<AdminDriverDetail> GetDriverDetailAsync(long userId);

    Task<IPagedList<AdminDocumentListItem>> GetDriverDocumentsAsync(AdminDocumentSearch search);
}

/// <summary>Thrown for expected validation problems; the controller shows the message.</summary>
public class AdminException : Exception
{
    public AdminException(string message) : base(message)
    {
    }
}
