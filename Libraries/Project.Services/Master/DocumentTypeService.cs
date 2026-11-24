using LinqToDB;
using Project.Core;
using Project.Core.Domain.Master;
using Project.Data;

namespace Project.Services.Master;

public partial class DocumentTypeService : IDocumentTypeService
{
    private readonly IRepository<DocumentType> _repository;

    public DocumentTypeService(IRepository<DocumentType> repository)
    {
        _repository = repository;
    }

    public async Task<IList<DocumentType>> GetAllDocumentTypesAsync(bool showHidden = false)
    {
        var query = _repository.Table.Where(d => !d.Deleted);
        if (!showHidden)
            query = query.Where(d => d.Active);

        return await query.OrderBy(d => d.DisplayOrder).ThenBy(d => d.Id).ToListAsync();
    }

    public async Task<DocumentType> GetDocumentTypeByIdAsync(long id) =>
        await _repository.GetByIdAsync(id);

    public async Task InsertDocumentTypeAsync(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        documentType.CreatedOnUtc = CommonHelper.IndianTimeNow;
        documentType.UpdatedOnUtc = CommonHelper.IndianTimeNow;
        await _repository.InsertAsync(documentType);
    }

    public async Task UpdateDocumentTypeAsync(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        documentType.UpdatedOnUtc = CommonHelper.IndianTimeNow;
        await _repository.UpdateAsync(documentType);
    }

    public async Task DeleteDocumentTypeAsync(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        await _repository.DeleteAsync(documentType);
    }
}
