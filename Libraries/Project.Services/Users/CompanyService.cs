using LinqToDB;
using Microsoft.AspNetCore.Mvc.Rendering;
using Project.Core;
using Project.Core.Caching;
using Project.Core.Domain.Users;
using Project.Data;
using Project.Services.Common.DataModel;
using Project.Services.Directory;

namespace Project.Services.Users;

/// <summary>
/// Represents company service
/// </summary>
public partial class CompanyService : ICompanyService
{
    #region Fields

    private readonly IRepository<Company> _companyRepository;
    private readonly IStaticCacheManager _staticCacheManager;

    #endregion

    #region Constructor

    public CompanyService(IRepository<Company> companyRepository,
        IStaticCacheManager staticCacheManager)
    {
        _companyRepository = companyRepository;
        _staticCacheManager = staticCacheManager;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Get company select data model list
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the select data list
    /// </returns>
    public async Task<IList<SelectListDataModel>> GetSelectDataModelListAsync()
    {
        var query = from c in _companyRepository.Table
                    where c.Deleted == false
                    orderby c.Name
                    select new SelectListDataModel
                    {
                        Id = c.Id,
                        Name = c.Name
                    };

        return await query.ToListAsync();
    }

    /// <summary>
    /// Get company select list items
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the select list items
    /// </returns>
    public async Task<IList<SelectListItem>> GetCompanySelectListAsync()
    {
        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(DirectoryDefault.CompanySelectDataAllCacheKey);
        var list = await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            var query = _companyRepository.Table
                .Where(c => !c.Deleted)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });

            return await query.ToListAsync();
        });

        return list;
    }

    /// <summary>
    /// Gets a company by ID
    /// </summary>
    /// <param name="companyId">Company identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the company
    /// </returns>
    public async Task<Company> GetCompanyByIdAsync(long companyId)
    {
        return await _companyRepository.GetByIdAsync(companyId);
    }

    /// <summary>
    /// Gets companies by identifier list
    /// </summary>
    /// <param name="companyIds">Company identifiers</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the company list
    /// </returns>
    public async Task<IList<Company>> GetCompaniesByIdsAsync(IList<long> companyIds)
    {
        return await _companyRepository.GetByIdsAsync(companyIds);
    }

    /// <summary>
    /// Gets a company by name
    /// </summary>
    /// <param name="name">Company name</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the company
    /// </returns>
    public async Task<Company> GetCompanyByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await _companyRepository.Table
            .FirstOrDefaultAsync(c => c.Name == name && c.Deleted == false);
    }

    /// <summary>
    /// Inserts a company
    /// </summary>
    /// <param name="company">Company</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task InsertCompanyAsync(Company company)
    {
        ArgumentNullException.ThrowIfNull(company);
        await _companyRepository.InsertAsync(company);
    }

    /// <summary>
    /// Updates a company
    /// </summary>
    /// <param name="company">Company</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task UpdateCompanyAsync(Company company)
    {
        ArgumentNullException.ThrowIfNull(company);
        await _companyRepository.UpdateAsync(company);
    }

    /// <summary>
    /// Deletes a company
    /// </summary>
    /// <param name="company">Company</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task DeleteCompanyAsync(Company company)
    {
        ArgumentNullException.ThrowIfNull(company);
        await _companyRepository.DeleteAsync(company);
    }

    /// <summary>
    /// Gets all companies (paged)
    /// </summary>
    /// <param name="companyName">Company name</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">Whether to show deleted records</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IPagedList<Company>> GetAllCompaniesAsync(string companyName = null,
        int pageIndex = 0, int pageSize = int.MaxValue,
        bool showHidden = false)
    {
        var result = await _companyRepository.GetAllPagedAsync(query =>
        {
            if (!string.IsNullOrWhiteSpace(companyName))
                query = query.Where(c => c.Name.Contains(companyName));

            if (!showHidden)
                query = query.Where(c => !c.Deleted);

            query = query.OrderByDescending(c => c.Id);

            return query;
        }, pageIndex, pageSize);

        return result;
    }

    /// <summary>
    /// Gets all companies queryable
    /// </summary>
    /// <param name="companyName">Company name</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IQueryable<Company>> GetAllCompaniesAsync(string companyName = null)
    {
        var query = from c in _companyRepository.Table
                    where c.Deleted == false && (string.IsNullOrEmpty(companyName) || c.Name.Contains(companyName))
                    select c;

        return await Task.FromResult(query);
    }

    #endregion
}
