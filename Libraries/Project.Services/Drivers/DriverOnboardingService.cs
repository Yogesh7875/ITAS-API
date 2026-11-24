using System.Text.RegularExpressions;
using LinqToDB;
using Project.Core;
using Project.Core.Constant;
using Project.Core.Domain.Drivers;
using Project.Core.Domain.Master;
using Project.Core.Domain.Users;
using Project.Data;
using Project.Services.Media;

namespace Project.Services.Drivers;

public partial class DriverOnboardingService : IDriverOnboardingService
{
    #region Fields

    private readonly IRepository<DriverProfile> _profiles;
    private readonly IRepository<DriverVehicle> _vehicles;
    private readonly IRepository<UserDocument> _documents;
    private readonly IRepository<UserDocumentFile> _files;
    private readonly IRepository<DocumentType> _types;
    private readonly IRepository<VehicleCategory> _categories;
    private readonly IRepository<User> _users;
    private readonly IFileStorageResolver _storage;

    #endregion

    #region Constructor

    public DriverOnboardingService(IRepository<DriverProfile> profiles,
        IRepository<DriverVehicle> vehicles,
        IRepository<UserDocument> documents,
        IRepository<UserDocumentFile> files,
        IRepository<DocumentType> types,
        IRepository<VehicleCategory> categories,
        IRepository<User> users,
        IFileStorageResolver storage)
    {
        _profiles = profiles;
        _vehicles = vehicles;
        _documents = documents;
        _files = files;
        _types = types;
        _categories = categories;
        _users = users;
        _storage = storage;
    }

    #endregion

    #region Helpers

    private const string NOT_UPLOADED = "NotUploaded";

    private static DateTime Now => CommonHelper.IndianTimeNow;

    private async Task<DriverProfile> GetProfileAsync(long userId) =>
        await _profiles.Table.FirstOrDefaultAsync(p => p.UserId == userId && !p.Deleted);

    private async Task<DriverProfile> GetOrCreateProfileAsync(long userId)
    {
        var profile = await GetProfileAsync(userId);
        if (profile != null)
            return profile;

        profile = new DriverProfile
        {
            UserId = userId,
            OnboardingStatus = DriverOnboardingStatus.Draft,
            CreatedOnUtc = Now,
            UpdatedOnUtc = Now
        };
        await _profiles.InsertAsync(profile);
        return profile;
    }

    private async Task SetUserApprovalAsync(long userId, int approvalStatusId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            return;

        user.ApprovalStatusId = approvalStatusId;
        user.UpdatedOnUtc = Now;
        await _users.UpdateAsync(user);
    }

    /// <summary>Any change to an approved driver's vehicle or documents needs a fresh review.</summary>
    private async Task ResetApprovalIfApprovedAsync(long userId)
    {
        var profile = await GetProfileAsync(userId);
        if (profile == null || profile.OnboardingStatusId != (int)DriverOnboardingStatus.Approved)
            return;

        profile.OnboardingStatus = DriverOnboardingStatus.Draft;
        profile.UpdatedOnUtc = Now;
        await _profiles.UpdateAsync(profile);
        await SetUserApprovalAsync(userId, 0);
    }

    private static async Task<bool> HasValidSignatureAsync(Stream stream, string ext)
    {
        var buffer = new byte[8];
        stream.Position = 0;
        var read = await stream.ReadAsync(buffer, 0, buffer.Length);
        stream.Position = 0;

        return ext switch
        {
            "jpg" or "jpeg" => read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            "png" => read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47,
            "pdf" => read >= 4 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46,
            _ => false
        };
    }

    private static string StatusName(UserDocument doc)
    {
        if (doc == null)
            return NOT_UPLOADED;

        if (doc.ExpiryDate.HasValue && doc.ExpiryDate.Value.Date < Now.Date)
            return DocumentStatus.Expired.ToString();

        return ((DocumentStatus)doc.StatusId).ToString();
    }

    #endregion

    #region Vehicle

    public async Task<DriverVehicle> GetVehicleAsync(long userId) =>
        await _vehicles.Table.FirstOrDefaultAsync(v => v.UserId == userId && !v.Deleted);

    public async Task SaveVehicleAsync(long userId, long vehicleCategoryId, string registrationNumber,
        string make, string model, int? year, string color)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new OnboardingException("Enter the vehicle registration number");

        var category = await _categories.Table.FirstOrDefaultAsync(c =>
            c.Id == vehicleCategoryId && !c.Deleted && c.Active);
        if (category == null)
            throw new OnboardingException("Select a vehicle category");

        var reg = Regex.Replace(registrationNumber.Trim().ToUpperInvariant(), @"[\s-]", string.Empty);
        if (await _vehicles.Table.AnyAsync(v => v.RegistrationNumber == reg && v.UserId != userId && !v.Deleted))
            throw new OnboardingException("This vehicle is already registered with another driver");

        var vehicle = await GetVehicleAsync(userId);
        if (vehicle == null)
        {
            vehicle = new DriverVehicle { UserId = userId, CreatedOnUtc = Now };
            apply(vehicle);
            await _vehicles.InsertAsync(vehicle);
        }
        else
        {
            apply(vehicle);
            await _vehicles.UpdateAsync(vehicle);
        }

        await GetOrCreateProfileAsync(userId);
        await ResetApprovalIfApprovedAsync(userId);

        void apply(DriverVehicle v)
        {
            v.VehicleCategoryId = category.Id;
            v.RegistrationNumber = reg;
            v.Make = make?.Trim();
            v.Model = model?.Trim();
            v.Year = year;
            v.Color = color?.Trim();
            v.UpdatedOnUtc = Now;
        }
    }

    #endregion

    #region Documents

    public async Task<IList<DocumentRequirementDataModel>> GetRequirementsAsync(long userId)
    {
        var types = await _types.Table
            .Where(t => !t.Deleted && t.Active)
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id)
            .ToListAsync();

        var docs = await _documents.Table
            .Where(d => d.UserId == userId && !d.Deleted)
            .ToListAsync();

        var docIds = docs.Select(d => d.Id).ToList();
        var files = docIds.Count == 0
            ? new List<UserDocumentFile>()
            : await _files.Table.Where(f => docIds.Contains(f.UserDocumentId)).ToListAsync();

        var result = new List<DocumentRequirementDataModel>();
        foreach (var t in types)
        {
            var doc = docs.FirstOrDefault(d => d.DocumentTypeId == t.Id);
            result.Add(new DocumentRequirementDataModel
            {
                DocumentTypeId = t.Id,
                Code = t.Code,
                Name = t.Name,
                Description = t.Description,
                AppliesTo = ((DocumentAppliesTo)t.AppliesToId).ToString(),
                IsMandatory = t.IsMandatory,
                RequiresNumber = t.RequiresNumber,
                NumberLabel = t.NumberLabel,
                NumberRegex = t.NumberRegex,
                RequiresExpiry = t.RequiresExpiry,
                FilesRequired = t.FilesRequired,
                AllowedFileTypes = t.AllowedFileTypes,
                MaxFileSizeKb = t.MaxFileSizeKb,
                Status = StatusName(doc),
                UserDocumentId = doc?.Id,
                DocumentNumber = doc?.DocumentNumber,
                ExpiryDate = doc?.ExpiryDate,
                RejectionReason = doc?.RejectionReason,
                Files = doc == null
                    ? new List<DocumentFileDataModel>()
                    : files.Where(f => f.UserDocumentId == doc.Id)
                        .OrderBy(f => f.Side)
                        .Select(f => new DocumentFileDataModel
                        {
                            Id = f.Id,
                            Side = f.Side,
                            FileName = f.FileName,
                            ContentType = f.ContentType,
                            SizeBytes = f.SizeBytes
                        }).ToList()
            });
        }

        return result;
    }

    public async Task SaveDocumentAsync(long userId, long documentTypeId, string documentNumber,
        DateTime? expiryDate, IList<UploadedFile> files)
    {
        var type = await _types.Table.FirstOrDefaultAsync(t =>
            t.Id == documentTypeId && !t.Deleted && t.Active);
        if (type == null)
            throw new OnboardingException("Document type not found");

        if (type.AppliesToId == (int)DocumentAppliesTo.Vehicle && await GetVehicleAsync(userId) == null)
            throw new OnboardingException("Add your vehicle details first");

        if (files == null || files.Count != type.FilesRequired)
            throw new OnboardingException($"Please upload {type.FilesRequired} file(s) for {type.Name}");

        var allowed = (type.AllowedFileTypes ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.TrimStart('.').ToLowerInvariant())
            .ToHashSet();

        foreach (var f in files)
        {
            var ext = Path.GetExtension(f.FileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
            if (!allowed.Contains(ext))
                throw new OnboardingException($"Only {string.Join(", ", allowed)} files are allowed");
            if (f.Length <= 0 || f.Length > type.MaxFileSizeKb * 1024L)
                throw new OnboardingException($"Each file must be under {type.MaxFileSizeKb / 1024.0:0.#} MB");
            if (!await HasValidSignatureAsync(f.Content, ext))
                throw new OnboardingException("The file is not a valid image or PDF");
        }

        string number = null;
        if (type.RequiresNumber)
        {
            number = documentNumber?.Trim();
            if (string.IsNullOrEmpty(number))
                throw new OnboardingException($"Enter the {type.NumberLabel ?? "document number"}");

            if (!string.IsNullOrWhiteSpace(type.NumberRegex) &&
                !Regex.IsMatch(number, type.NumberRegex, RegexOptions.None, TimeSpan.FromSeconds(1)))
                throw new OnboardingException($"Invalid {type.NumberLabel ?? "document number"}");
        }

        DateTime? expiry = null;
        if (type.RequiresExpiry)
        {
            if (!expiryDate.HasValue)
                throw new OnboardingException("Enter the expiry date");
            if (expiryDate.Value.Date < Now.Date)
                throw new OnboardingException("This document has already expired");
            expiry = expiryDate.Value.Date;
        }

        // Replace an existing upload of the same document type.
        var doc = await _documents.Table.FirstOrDefaultAsync(d =>
            d.UserId == userId && d.DocumentTypeId == type.Id && !d.Deleted);

        if (doc != null)
        {
            var old = await _files.Table.Where(f => f.UserDocumentId == doc.Id).ToListAsync();
            foreach (var o in old)
            {
                try { await _storage.For(o.StorageProvider).DeleteAsync(o.StorageKey); }
                catch { /* an orphan file is better than a failed upload */ }
                await _files.DeleteAsync(o);
            }
        }

        var folder = $"drivers/{userId}/{type.Code.ToLowerInvariant()}";
        var stored = new List<(int Side, StoredFile File)>();
        var side = 1;
        foreach (var f in files)
        {
            f.Content.Position = 0;
            stored.Add((side++, await _storage.Current.SaveAsync(f.Content, f.FileName, f.ContentType, folder)));
        }

        if (doc == null)
        {
            doc = new UserDocument { UserId = userId, DocumentTypeId = type.Id, CreatedOnUtc = Now };
            applyDoc(doc);
            await _documents.InsertAsync(doc);
        }
        else
        {
            applyDoc(doc);
            await _documents.UpdateAsync(doc);
        }

        foreach (var (s, sf) in stored)
        {
            await _files.InsertAsync(new UserDocumentFile
            {
                UserDocumentId = doc.Id,
                Side = s,
                StorageProvider = sf.Provider,
                StorageKey = sf.Key,
                FileName = sf.FileName,
                ContentType = sf.ContentType,
                SizeBytes = sf.SizeBytes,
                CreatedOnUtc = Now
            });
        }

        await GetOrCreateProfileAsync(userId);
        await ResetApprovalIfApprovedAsync(userId);

        void applyDoc(UserDocument d)
        {
            d.DocumentNumber = number;
            d.ExpiryDate = expiry;
            d.Status = DocumentStatus.Pending;
            d.RejectionReason = null;
            d.ReviewedByUserId = null;
            d.ReviewedOnUtc = null;
            d.UpdatedOnUtc = Now;
        }
    }

    public async Task<(UserDocumentFile File, Stream Stream)> OpenFileAsync(
        long fileId, long requestingUserId, bool isAdmin)
    {
        var file = await _files.GetByIdAsync(fileId);
        if (file == null)
            throw new OnboardingException("File not found");

        if (!isAdmin)
        {
            var doc = await _documents.GetByIdAsync(file.UserDocumentId);
            if (doc == null || doc.UserId != requestingUserId)
                throw new OnboardingException("File not found");
        }

        var stream = await _storage.For(file.StorageProvider).OpenReadAsync(file.StorageKey);
        return (file, stream);
    }

    #endregion

    #region Onboarding status

    public async Task<OnboardingSummaryDataModel> GetSummaryAsync(long userId)
    {
        var profile = await GetProfileAsync(userId);
        var vehicle = await GetVehicleAsync(userId);
        var user = await _users.GetByIdAsync(userId);
        var reqs = await GetRequirementsAsync(userId);
        var mandatory = reqs.Where(r => r.IsMandatory).ToList();

        var uploaded = mandatory.Count(r => r.Status != NOT_UPLOADED);
        var approved = mandatory.Count(r => r.Status == DocumentStatus.Approved.ToString());
        var anyProblem = reqs.Any(r => r.Status == DocumentStatus.Rejected.ToString() ||
                                       r.Status == DocumentStatus.Expired.ToString());
        var status = profile?.OnboardingStatus ?? DriverOnboardingStatus.Draft;
        var canEdit = status == DriverOnboardingStatus.Draft || status == DriverOnboardingStatus.Rejected;

        return new OnboardingSummaryDataModel
        {
            Status = status.ToString(),
            RejectionReason = profile?.RejectionReason,
            HasVehicle = vehicle != null,
            RequiredCount = mandatory.Count,
            UploadedCount = uploaded,
            ApprovedCount = approved,
            CanSubmit = canEdit && vehicle != null && uploaded == mandatory.Count && !anyProblem &&
                        !string.IsNullOrWhiteSpace(user?.FirstName)
        };
    }

    public async Task SubmitForReviewAsync(long userId)
    {
        var summary = await GetSummaryAsync(userId);
        if (summary.Status == DriverOnboardingStatus.Submitted.ToString())
            throw new OnboardingException("Your details are already under review");
        if (summary.Status == DriverOnboardingStatus.Approved.ToString())
            throw new OnboardingException("Your account is already approved");
        if (!summary.CanSubmit)
            throw new OnboardingException("Please complete your profile, vehicle and all required documents, and fix any rejected ones");

        var profile = await GetOrCreateProfileAsync(userId);
        profile.OnboardingStatus = DriverOnboardingStatus.Submitted;
        profile.SubmittedOnUtc = Now;
        profile.RejectionReason = null;
        profile.UpdatedOnUtc = Now;
        await _profiles.UpdateAsync(profile);
        await SetUserApprovalAsync(userId, 0);
    }

    #endregion

    #region Admin review

    public async Task<IList<SubmittedDriverDataModel>> GetSubmittedDriversAsync()
    {
        var submitted = (int)DriverOnboardingStatus.Submitted;
        var query = from p in _profiles.Table
                    join u in _users.Table on p.UserId equals u.Id
                    where p.OnboardingStatusId == submitted && !p.Deleted
                    orderby p.SubmittedOnUtc
                    select new SubmittedDriverDataModel
                    {
                        UserId = u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        MobileNumber = u.MobileNumber,
                        SubmittedOnUtc = p.SubmittedOnUtc
                    };

        return await query.ToListAsync();
    }

    public async Task ReviewDocumentAsync(long userDocumentId, bool approve, string reason, long reviewerUserId)
    {
        var doc = await _documents.GetByIdAsync(userDocumentId);
        if (doc == null || doc.Deleted)
            throw new OnboardingException("Document not found");
        if (!approve && string.IsNullOrWhiteSpace(reason))
            throw new OnboardingException("Enter a reason so the driver knows what to fix");

        doc.Status = approve ? DocumentStatus.Approved : DocumentStatus.Rejected;
        doc.RejectionReason = approve ? null : reason.Trim();
        doc.ReviewedByUserId = reviewerUserId;
        doc.ReviewedOnUtc = Now;
        doc.UpdatedOnUtc = Now;
        await _documents.UpdateAsync(doc);
    }

    public async Task ReviewDriverAsync(long userId, bool approve, string reason, long reviewerUserId)
    {
        var profile = await GetProfileAsync(userId);
        if (profile == null || profile.OnboardingStatusId != (int)DriverOnboardingStatus.Submitted)
            throw new OnboardingException("This driver has not submitted details for review");

        if (approve)
        {
            var reqs = await GetRequirementsAsync(userId);
            var notApproved = reqs.Where(r => r.IsMandatory && r.Status != DocumentStatus.Approved.ToString())
                .Select(r => r.Name).ToList();
            if (notApproved.Count > 0)
                throw new OnboardingException($"Approve these documents first: {string.Join(", ", notApproved)}");
            if (await GetVehicleAsync(userId) == null)
                throw new OnboardingException("The driver has no vehicle details");
        }
        else if (string.IsNullOrWhiteSpace(reason))
        {
            throw new OnboardingException("Enter a reason for rejecting this driver");
        }

        profile.OnboardingStatus = approve ? DriverOnboardingStatus.Approved : DriverOnboardingStatus.Rejected;
        profile.RejectionReason = approve ? null : reason.Trim();
        profile.ReviewedByUserId = reviewerUserId;
        profile.ReviewedOnUtc = Now;
        profile.UpdatedOnUtc = Now;
        await _profiles.UpdateAsync(profile);

        // 1 = approved (driver may go online), 2 = rejected
        await SetUserApprovalAsync(userId, approve ? 1 : 2);
    }

    public async Task<int> MarkExpiredDocumentsAsync()
    {
        var today = Now.Date;
        var approved = (int)DocumentStatus.Approved;
        var expired = await _documents.Table
            .Where(d => !d.Deleted && d.StatusId == approved && d.ExpiryDate != null && d.ExpiryDate < today)
            .ToListAsync();

        foreach (var doc in expired)
        {
            doc.Status = DocumentStatus.Expired;
            doc.UpdatedOnUtc = Now;
            await _documents.UpdateAsync(doc);

            // The driver must upload a fresh document and be reviewed again.
            var profile = await GetProfileAsync(doc.UserId);
            if (profile != null && profile.OnboardingStatusId == (int)DriverOnboardingStatus.Approved)
            {
                profile.OnboardingStatus = DriverOnboardingStatus.Draft;
                profile.UpdatedOnUtc = Now;
                await _profiles.UpdateAsync(profile);
                await SetUserApprovalAsync(doc.UserId, 0);
            }
        }

        return expired.Count;
    }

    #endregion
}
