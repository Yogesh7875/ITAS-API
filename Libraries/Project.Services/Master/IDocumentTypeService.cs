using Project.Core.Domain.Master;

namespace Project.Services.Master;

public partial interface IDocumentTypeService
{
    Task<IList<DocumentType>> GetAllDocumentTypesAsync(bool showHidden = false);

    Task<DocumentType> GetDocumentTypeByIdAsync(long id);

    Task InsertDocumentTypeAsync(DocumentType documentType);

    Task UpdateDocumentTypeAsync(DocumentType documentType);

    Task DeleteDocumentTypeAsync(DocumentType documentType);
}
