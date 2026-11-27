using LinqToDB;
using Project.Core;
using Project.Core.Constant;
using Project.Core.Domain.Catalog;
using Project.Core.Domain.Drivers;
using Project.Core.Domain.Master;
using Project.Core.Domain.Users;
using Project.Data;
using Project.Services.Drivers;

namespace Project.Services.Admin;

public partial class AdminQueryService : IAdminQueryService
{
    #region Fields

    private const int MAX_PAGE_SIZE = 100;

    private readonly IRepository<User> _users;
    private readonly IRepository<UserRole> _roles;
    private readonly IRepository<Company> _companies;
    private readonly IRepository<DriverProfile> _profiles;
    private readonly IRepository<DriverVehicle> _vehicles;
    private readonly IRepository<VehicleCategory> _categories;
    private readonly IRepository<UserDocument> _documents;
    private readonly IRepository<UserDocumentFile> _files;
    private readonly IRepository<DocumentType> _types;
    private readonly IDriverOnboardingService _onboardingService;

    #endregion

    #region Constructor

    public AdminQueryService(IRepository<User> users,
        IRepository<UserRole> roles,
        IRepository<Company> companies,
        IRepository<DriverProfile> profiles,
        IRepository<DriverVehicle> vehicles,
        IRepository<VehicleCategory> categories,
        IRepository<UserDocument> documents,
        IRepository<UserDocumentFile> files,
        IRepository<DocumentType> types,
        IDriverOnboardingService onboardingService)
    {
        _users = users;
        _roles = roles;
        _companies = companies;
        _profiles = profiles;
        _vehicles = vehicles;
        _categories = categories;
        _documents = documents;
        _files = files;
        _types = types;
        _onboardingService = onboardingService;
    }

    #endregion

    #region Helpers

    private static DateTime Today => CommonHelper.IndianTimeNow.Date;

    private static int PageSize(int size) => Math.Clamp(size, 1, MAX_PAGE_SIZE);

    private static int PageIndex(int index) => Math.Max(index, 0);

    /// <summary>Same rule as the driver app: an expired date wins over the stored status.</summary>
    private static string DocumentStatusName(int statusId, DateTime? expiryDate, DateTime today) =>
        expiryDate.HasValue && expiryDate.Value.Date < today
            ? DocumentStatus.Expired.ToString()
            : ((DocumentStatus)statusId).ToString();

    #endregion

    #region Users and corporate users

    public async Task<IPagedList<AdminUserListItem>> GetUsersAsync(AdminUserSearch search)
    {
        search ??= new AdminUserSearch();

        var query = from u in _users.Table
                    join r in _roles.Table on u.RoleId equals r.Id
                    from c in _companies.Table.Where(c => c.Id == u.CompanyId).DefaultIfEmpty()
                    where !u.Deleted
                    select new { u, r, c };

        if (!string.IsNullOrWhiteSpace(search.RoleName))
        {
            var roleName = search.RoleName.Trim();
            query = query.Where(x => x.r.Name == roleName);
        }

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var s = search.SearchText.Trim();
            query = query.Where(x => (x.u.FirstName + " " + x.u.LastName).Contains(s) ||
                                     x.u.MobileNumber.Contains(s) ||
                                     x.u.EmailAddress.Contains(s));
        }

        if (search.CompanyId.HasValue)
            query = query.Where(x => x.u.CompanyId == search.CompanyId.Value);

        if (search.HasCompany.HasValue)
            query = search.HasCompany.Value
                ? query.Where(x => x.u.CompanyId != null)
                : query.Where(x => x.u.CompanyId == null);

        if (search.ApprovalStatusId.HasValue)
            query = query.Where(x => x.u.ApprovalStatusId == search.ApprovalStatusId.Value);

        if (search.Active.HasValue)
            query = query.Where(x => x.u.Active == search.Active.Value);

        var projected = query
            .OrderByDescending(x => x.u.CreatedOnUtc)
            .Select(x => new AdminUserListItem
            {
                Id = x.u.Id,
                FirstName = x.u.FirstName,
                LastName = x.u.LastName,
                EmailAddress = x.u.EmailAddress,
                MobileNumber = x.u.MobileNumber,
                RoleId = x.r.Id,
                RoleName = x.r.Name,
                CompanyId = x.u.CompanyId,
                CompanyName = x.c.Name,
                ApprovalStatusId = x.u.ApprovalStatusId,
                Active = x.u.Active,
                IsBanned = x.u.IsBanned,
                CreatedOnUtc = x.u.CreatedOnUtc
            });

        var page = await projected.ToPagedListAsync(PageIndex(search.PageIndex), PageSize(search.PageSize));
        foreach (var item in page)
            item.ApprovalStatus = ApprovalStatus.Name(item.ApprovalStatusId);

        return page;
    }

    public async Task<AdminUserDetail> GetUserDetailAsync(long userId)
    {
        var query = from u in _users.Table
                    join r in _roles.Table on u.RoleId equals r.Id
                    from c in _companies.Table.Where(c => c.Id == u.CompanyId).DefaultIfEmpty()
                    where u.Id == userId && !u.Deleted
                    select new AdminUserDetail
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        EmailAddress = u.EmailAddress,
                        MobileNumber = u.MobileNumber,
                        RoleId = r.Id,
                        RoleName = r.Name,
                        CompanyId = u.CompanyId,
                        CompanyName = c.Name,
                        CompanyEmailDomain = c.EmailDomain,
                        ApprovalStatusId = u.ApprovalStatusId,
                        Active = u.Active,
                        IsBanned = u.IsBanned,
                        Dob = u.Dob,
                        Gender = u.Gender,
                        Address = u.Address,
                        Remark = u.Remark,
                        CreatedOnUtc = u.CreatedOnUtc,
                        UpdatedOnUtc = u.UpdatedOnUtc
                    };

        var detail = await query.FirstOrDefaultAsync(x => true);
        if (detail != null)
            detail.ApprovalStatus = ApprovalStatus.Name(detail.ApprovalStatusId);

        return detail;
    }

    public async Task SetCorporateApprovalAsync(long userId, bool approve, string reason, long adminUserId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null || user.Deleted)
            throw new AdminException("User not found");

        var role = await _roles.GetByIdAsync(user.RoleId);
        if (role == null || role.Name != ProjectUserDefaults.CorporateRoleName)
            throw new AdminException("This user is not a corporate user");

        if (!user.CompanyId.HasValue)
            throw new AdminException("This user has not linked a company yet");

        if (!approve && string.IsNullOrWhiteSpace(reason))
            throw new AdminException("Enter a reason for rejecting this user");

        user.ApprovalStatusId = approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        user.Remark = approve ? null : reason.Trim();
        user.UpdatedOnUtc = CommonHelper.IndianTimeNow;
        await _users.UpdateAsync(user);
    }

    #endregion

    #region Drivers

    public async Task<IPagedList<AdminDriverListItem>> GetDriversAsync(AdminDriverSearch search)
    {
        search ??= new AdminDriverSearch();
        var driverRole = ProjectUserDefaults.DriverRoleName;

        var query = from u in _users.Table
                    join r in _roles.Table on u.RoleId equals r.Id
                    from p in _profiles.Table.Where(p => p.UserId == u.Id && !p.Deleted).DefaultIfEmpty()
                    from v in _vehicles.Table.Where(v => v.UserId == u.Id && !v.Deleted).DefaultIfEmpty()
                    from vc in _categories.Table.Where(vc => vc.Id == v.VehicleCategoryId).DefaultIfEmpty()
                    where !u.Deleted && r.Name == driverRole
                    select new AdminDriverListItem
                    {
                        UserId = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        MobileNumber = u.MobileNumber,
                        EmailAddress = u.EmailAddress,
                        City = p.City,
                        Active = u.Active,
                        IsBanned = u.IsBanned,
                        // A driver without a profile row has not started onboarding: Draft.
                        OnboardingStatusId = p != null ? p.OnboardingStatusId : (int)DriverOnboardingStatus.Draft,
                        SubmittedOnUtc = p.SubmittedOnUtc,
                        ReviewedOnUtc = p.ReviewedOnUtc,
                        VehicleCategoryId = v != null ? v.VehicleCategoryId : (long?)null,
                        VehicleRegistrationNumber = v.RegistrationNumber,
                        VehicleCategoryName = vc.Name,
                        CreatedOnUtc = u.CreatedOnUtc
                    };

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var s = search.SearchText.Trim();
            query = query.Where(x => (x.FirstName + " " + x.LastName).Contains(s) ||
                                     x.MobileNumber.Contains(s) ||
                                     x.EmailAddress.Contains(s) ||
                                     x.VehicleRegistrationNumber.Contains(s));
        }

        if (search.OnboardingStatusId.HasValue)
            query = query.Where(x => x.OnboardingStatusId == search.OnboardingStatusId.Value);

        if (search.VehicleCategoryId.HasValue)
            query = query.Where(x => x.VehicleCategoryId == search.VehicleCategoryId.Value);

        if (search.Active.HasValue)
            query = query.Where(x => x.Active == search.Active.Value);

        // Submitted drivers waiting for review come first, oldest submission first.
        var submitted = (int)DriverOnboardingStatus.Submitted;
        query = query
            .OrderByDescending(x => x.OnboardingStatusId == submitted)
            .ThenBy(x => x.SubmittedOnUtc)
            .ThenByDescending(x => x.CreatedOnUtc);

        var page = await query.ToPagedListAsync(PageIndex(search.PageIndex), PageSize(search.PageSize));
        if (page.Count == 0)
            return page;

        // Document counts for this page only.
        var ids = page.Select(x => x.UserId).ToList();
        var docs = await _documents.Table
            .Where(d => ids.Contains(d.UserId) && !d.Deleted)
            .Select(d => new { d.UserId, d.StatusId, d.ExpiryDate })
            .ToListAsync();

        var today = Today;
        foreach (var item in page)
        {
            item.OnboardingStatus = ((DriverOnboardingStatus)item.OnboardingStatusId).ToString();

            var statuses = docs.Where(d => d.UserId == item.UserId)
                .Select(d => DocumentStatusName(d.StatusId, d.ExpiryDate, today))
                .ToList();
            item.DocumentsUploaded = statuses.Count;
            item.DocumentsPending = statuses.Count(s => s == nameof(DocumentStatus.Pending));
            item.DocumentsApproved = statuses.Count(s => s == nameof(DocumentStatus.Approved));
            item.DocumentsRejected = statuses.Count(s => s == nameof(DocumentStatus.Rejected));
            item.DocumentsExpired = statuses.Count(s => s == nameof(DocumentStatus.Expired));
        }

        return page;
    }

    public async Task<AdminDriverDetail> GetDriverDetailAsync(long userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null || user.Deleted)
            return null;

        var role = await _roles.GetByIdAsync(user.RoleId);
        if (role == null || role.Name != ProjectUserDefaults.DriverRoleName)
            return null;

        var profile = await _profiles.Table.FirstOrDefaultAsync(p => p.UserId == userId && !p.Deleted);

        string reviewedByName = null;
        if (profile?.ReviewedByUserId != null)
        {
            var reviewer = await _users.GetByIdAsync(profile.ReviewedByUserId.Value);
            reviewedByName = reviewer == null ? null : $"{reviewer.FirstName} {reviewer.LastName}".Trim();
        }

        var vehicle = await (from v in _vehicles.Table
                             from vc in _categories.Table.Where(vc => vc.Id == v.VehicleCategoryId).DefaultIfEmpty()
                             where v.UserId == userId && !v.Deleted
                             select new AdminDriverVehicle
                             {
                                 Id = v.Id,
                                 VehicleCategoryId = v.VehicleCategoryId,
                                 VehicleCategoryName = vc.Name,
                                 RegistrationNumber = v.RegistrationNumber,
                                 Make = v.Make,
                                 Model = v.Model,
                                 Year = v.Year,
                                 Color = v.Color
                             }).FirstOrDefaultAsync(x => true);

        var status = profile?.OnboardingStatus ?? DriverOnboardingStatus.Draft;

        return new AdminDriverDetail
        {
            PersonalDetails = new AdminDriverPersonalDetails
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MobileNumber = user.MobileNumber,
                EmailAddress = user.EmailAddress,
                Dob = user.Dob,
                Gender = user.Gender,
                Address = user.Address,
                City = profile?.City,
                Active = user.Active,
                IsBanned = user.IsBanned,
                ApprovalStatusId = user.ApprovalStatusId,
                ApprovalStatus = ApprovalStatus.Name(user.ApprovalStatusId),
                CreatedOnUtc = user.CreatedOnUtc
            },
            Onboarding = new AdminDriverOnboarding
            {
                OnboardingStatusId = (int)status,
                OnboardingStatus = status.ToString(),
                SubmittedOnUtc = profile?.SubmittedOnUtc,
                ReviewedOnUtc = profile?.ReviewedOnUtc,
                ReviewedByUserId = profile?.ReviewedByUserId,
                ReviewedByName = reviewedByName,
                RejectionReason = profile?.RejectionReason
            },
            Summary = await _onboardingService.GetSummaryAsync(userId),
            Vehicle = vehicle,
            Documents = await _onboardingService.GetRequirementsAsync(userId)
        };
    }

    #endregion

    #region Driver documents

    public async Task<IPagedList<AdminDocumentListItem>> GetDriverDocumentsAsync(AdminDocumentSearch search)
    {
        search ??= new AdminDocumentSearch();
        var today = Today;

        var query = from d in _documents.Table
                    join u in _users.Table on d.UserId equals u.Id
                    join t in _types.Table on d.DocumentTypeId equals t.Id
                    where !d.Deleted && !u.Deleted
                    select new { d, u, t };

        if (search.UserId.HasValue)
            query = query.Where(x => x.d.UserId == search.UserId.Value);

        if (search.DocumentTypeId.HasValue)
            query = query.Where(x => x.d.DocumentTypeId == search.DocumentTypeId.Value);

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var s = search.SearchText.Trim();
            query = query.Where(x => (x.u.FirstName + " " + x.u.LastName).Contains(s) ||
                                     x.u.MobileNumber.Contains(s) ||
                                     x.d.DocumentNumber.Contains(s) ||
                                     x.t.Name.Contains(s));
        }

        if (search.StatusId.HasValue)
        {
            // Filter on the effective status, so a past expiry date counts as Expired.
            var statusId = search.StatusId.Value;
            if (statusId == (int)DocumentStatus.Expired)
                query = query.Where(x => x.d.StatusId == statusId ||
                                         (x.d.ExpiryDate != null && x.d.ExpiryDate < today));
            else
                query = query.Where(x => x.d.StatusId == statusId &&
                                         !(x.d.ExpiryDate != null && x.d.ExpiryDate < today));
        }

        // Pending documents first, oldest first, so reviewers work through the queue in order.
        var pending = (int)DocumentStatus.Pending;
        var projected = query
            .OrderByDescending(x => x.d.StatusId == pending)
            .ThenBy(x => x.d.StatusId == pending ? (DateTime?)(x.d.UpdatedOnUtc ?? x.d.CreatedOnUtc) : null)
            .ThenByDescending(x => x.d.UpdatedOnUtc ?? x.d.CreatedOnUtc)
            .Select(x => new AdminDocumentListItem
            {
                UserDocumentId = x.d.Id,
                UserId = x.u.Id,
                DriverName = x.u.FirstName + " " + x.u.LastName,
                MobileNumber = x.u.MobileNumber,
                DocumentTypeId = x.t.Id,
                DocumentTypeCode = x.t.Code,
                DocumentTypeName = x.t.Name,
                IsMandatory = x.t.IsMandatory,
                DocumentNumber = x.d.DocumentNumber,
                ExpiryDate = x.d.ExpiryDate,
                StatusId = x.d.StatusId,
                RejectionReason = x.d.RejectionReason,
                ReviewedOnUtc = x.d.ReviewedOnUtc,
                ReviewedByUserId = x.d.ReviewedByUserId,
                UploadedOnUtc = x.d.UpdatedOnUtc ?? x.d.CreatedOnUtc
            });

        var page = await projected.ToPagedListAsync(PageIndex(search.PageIndex), PageSize(search.PageSize));
        if (page.Count == 0)
            return page;

        var docIds = page.Select(x => x.UserDocumentId).ToList();
        var files = await _files.Table
            .Where(f => docIds.Contains(f.UserDocumentId))
            .OrderBy(f => f.Side)
            .ToListAsync();

        foreach (var item in page)
        {
            item.Status = DocumentStatusName(item.StatusId, item.ExpiryDate, today);
            item.Files = files.Where(f => f.UserDocumentId == item.UserDocumentId)
                .Select(f => new DocumentFileDataModel
                {
                    Id = f.Id,
                    Side = f.Side,
                    FileName = f.FileName,
                    ContentType = f.ContentType,
                    SizeBytes = f.SizeBytes
                }).ToList();
        }

        return page;
    }

    #endregion
}
