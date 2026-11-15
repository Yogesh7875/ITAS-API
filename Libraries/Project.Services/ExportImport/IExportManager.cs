using Project.Core.Domain.Users;

namespace Project.Services.ExportImport;

/// <summary>
/// Export manager interface
/// </summary>
public interface IExportManager
{
    /// <summary>
    /// Export users to XLSX
    /// </summary>
    /// <param name="users">Users</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<byte[]> ExportUsersToXlsxAsync(IList<User> users);

}
