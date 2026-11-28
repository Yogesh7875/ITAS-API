using Project.Core.Domain.Master;

namespace Project.Services.Master;

public partial interface IDocumentTypeService
{
    Task<IList<DocumentType>> GetAllDocumentTypesAsync(bool showHidden = false);

    Task<DocumentType> GetDocumentTypeByIdAsync(long id);

    /// <summary>Not-deleted document type with this code, or null.</summary>
    Task<DocumentType> GetDocumentTypeByCodeAsync(string code);

    /// <summary>Number of (not deleted) driver uploads per document type id.</summary>
    Task<IDictionary<long, int>> GetUploadCountsAsync();

    Task InsertDocumentTypeAsync(DocumentType documentType);

    Task UpdateDocumentTypeAsync(DocumentType documentType);

    Task DeleteDocumentTypeAsync(DocumentType documentType);
}
