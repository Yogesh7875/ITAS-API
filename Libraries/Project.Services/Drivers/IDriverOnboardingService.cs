using Project.Core.Domain.Drivers;

namespace Project.Services.Drivers;

public partial interface IDriverOnboardingService
{
    Task<DriverVehicle> GetVehicleAsync(long userId);

    Task SaveVehicleAsync(long userId, long vehicleCategoryId, string registrationNumber,
        string make, string model, int? year, string color);

    Task<IList<DocumentRequirementDataModel>> GetRequirementsAsync(long userId);

    Task<OnboardingSummaryDataModel> GetSummaryAsync(long userId);

    Task SaveDocumentAsync(long userId, long documentTypeId, string documentNumber,
        DateTime? expiryDate, IList<UploadedFile> files);

    Task SubmitForReviewAsync(long userId);

    Task<(UserDocumentFile File, Stream Stream)> OpenFileAsync(long fileId, long requestingUserId, bool isAdmin);

    // Admin
    Task<IList<SubmittedDriverDataModel>> GetSubmittedDriversAsync();

    Task ReviewDocumentAsync(long userDocumentId, bool approve, string reason, long reviewerUserId);

    Task ReviewDriverAsync(long userId, bool approve, string reason, long reviewerUserId);

    /// <summary>Marks approved documents past their expiry date as Expired and blocks those drivers. Returns the count.</summary>
    Task<int> MarkExpiredDocumentsAsync();
}
