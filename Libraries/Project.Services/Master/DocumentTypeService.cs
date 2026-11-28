using LinqToDB;
using Project.Core;
using Project.Core.Domain.Drivers;
using Project.Core.Domain.Master;
using Project.Data;

namespace Project.Services.Master;

public partial class DocumentTypeService : IDocumentTypeService
{
    private readonly IRepository<DocumentType> _repository;
    private readonly IRepository<UserDocument> _userDocumentRepository;

    public DocumentTypeService(IRepository<DocumentType> repository,
        IRepository<UserDocument> userDocumentRepository)
    {
        _repository = repository;
        _userDocumentRepository = userDocumentRepository;
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

    public async Task<DocumentType> GetDocumentTypeByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = code.Trim().ToUpper();
        return await _repository.Table.FirstOrDefaultAsync(d => !d.Deleted && d.Code.ToUpper() == normalized);
    }

    public async Task<IDictionary<long, int>> GetUploadCountsAsync()
    {
        var rows = await _userDocumentRepository.Table
            .Where(d => !d.Deleted)
            .GroupBy(d => d.DocumentTypeId)
            .Select(g => new { DocumentTypeId = g.Key, Count = g.Count() })
            .ToListAsync();

        return rows.ToDictionary(r => r.DocumentTypeId, r => r.Count);
    }

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
