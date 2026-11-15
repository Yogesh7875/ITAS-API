using System.Text;
using DocumentFormat.OpenXml.Spreadsheet;
using Project.Core.Domain.Catalog;
using Project.Core.Domain.Localization;
using Project.Core.Domain.Users;
using Project.Services.ExportImport.Help;
using Project.Services.Localization;
using QuestPDF.Drawing;
using QuestPDF.Fluent;

namespace Project.Services.ExportImport;

/// <summary>
/// Export manager interface
/// </summary>
public class ExportManager : IExportManager
{
    #region Fields

    protected readonly CatalogSettings _catalogSettings;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;

    #endregion

    #region Constructor

    public ExportManager(CatalogSettings catalogSettings,
        ILanguageService languageService,
        ILocalizationService localizationService)
    {
        _catalogSettings = catalogSettings;
        _languageService = languageService;
        _localizationService = localizationService;
    }

    #endregion

    #region Methods

    #region Users

    /// <summary>
    /// Export users to XLSX
    /// </summary>
    /// <param name="users">Users</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<byte[]> ExportUsersToXlsxAsync(IList<User> users)
    {
        //property manager 
        var manager = new PropertyManager<User>(new[]
        {
            new PropertyByName<User>("Id", (p) => p.Id),
            new PropertyByName<User>("FirstName", (p) => p.FirstName),
            new PropertyByName<User>("LastName", (p) => p.LastName),
            new PropertyByName<User>("Email", (p) => p.EmailAddress),
        }, _catalogSettings);

        return await manager.ExportToXlsxAsync(users);
    }

    #endregion
    #endregion
}
