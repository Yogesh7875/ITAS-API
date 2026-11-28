using Microsoft.AspNetCore.Mvc.Rendering;
using Project.Core;
using Project.Core.Domain.Users;
using Project.Services.Common.DataModel;

namespace Project.Services.Users;

/// <summary>
/// Represents company service interface
/// </summary>
public partial interface ICompanyService
{
    /// <summary>
    /// Get company select data model list
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the select data list
    /// </returns>
    Task<IList<SelectListDataModel>> GetSelectDataModelListAsync();

    /// <summary>
    /// Get company select list items
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the select list items
    /// </returns>
    Task<IList<SelectListItem>> GetCompanySelectListAsync();

    /// <summary>
    /// Gets a company by identifier
    /// </summary>
    /// <param name="companyId">Company identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the company
    /// </returns>
    Task<Company> GetCompanyByIdAsync(long companyId);

    /// <summary>
    /// Gets companies by identifier list
    /// </summary>
    /// <param name="companyIds">Company identifiers</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the company list
    /// </returns>
    Task<IList<Company>> GetCompaniesByIdsAsync(IList<long> companyIds);

    /// <summary>
    /// Gets a company by name
    /// </summary>
    /// <param name="name">Company name</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the company
    /// </returns>
    Task<Company> GetCompanyByNameAsync(string name);

    /// <summary>
    /// Inserts a company
    /// </summary>
    /// <param name="company">Company</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertCompanyAsync(Company company);

    /// <summary>
    /// Updates a company
    /// </summary>
    /// <param name="company">Company</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateCompanyAsync(Company company);

    /// <summary>
    /// Deletes a company
    /// </summary>
    /// <param name="company">Company</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteCompanyAsync(Company company);

    /// <summary>
    /// Gets all companies (paged)
    /// </summary>
    /// <param name="companyName">Company name</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">Whether to include inactive companies (deleted ones are never returned)</param>
    /// <param name="active">Only active (true) or only inactive (false) companies; null for both</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<IPagedList<Company>> GetAllCompaniesAsync(string companyName = null,
        int pageIndex = 0, int pageSize = int.MaxValue,
        bool showHidden = false, bool? active = null);

    /// <summary>
    /// Gets all companies queryable
    /// </summary>
    /// <param name="companyName">Company name</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<IQueryable<Company>> GetAllCompaniesAsync(string companyName = null);


    Task<Company> GetCompanyByCodeAsync(string code);
    Task<Company> GetCompanyByEmailDomainAsync(string domain);
    Task<int> GetLinkedUserCountAsync(long companyId);

    /// <summary>Number of (not deleted) users linked to each of the given companies.</summary>
    Task<IDictionary<long, int>> GetLinkedUserCountsAsync(IList<long> companyIds);
}
