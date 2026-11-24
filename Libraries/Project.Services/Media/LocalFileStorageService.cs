using Project.Core.Configuration;
using Project.Core.Infrastructure;

namespace Project.Services.Media;

/// <summary>
/// Saves files on the server's disk. Files are served only through the protected
/// download endpoint, so keep the folder outside wwwroot (default App_Data/Documents).
/// </summary>
public partial class LocalFileStorageService : IFileStorageService
{
    private readonly AppSettings _appSettings;
    private readonly IProjectFileProvider _fileProvider;

    public LocalFileStorageService(AppSettings appSettings, IProjectFileProvider fileProvider)
    {
        _appSettings = appSettings;
        _fileProvider = fileProvider;
    }

    public string Provider => "Local";

    private string Root => Path.GetFullPath(
        _fileProvider.GetAbsolutePath(_appSettings.Get<FileStorageConfig>().LocalRootPath));

    private string ResolvePath(string key)
    {
        var full = Path.GetFullPath(Path.Combine(Root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file key");
        return full;
    }

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, string folder)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
        var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}{ext}";
        var path = ResolvePath(key);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using (var fs = File.Create(path))
        {
            await content.CopyToAsync(fs);
        }

        return new StoredFile
        {
            Provider = Provider,
            Key = key,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = new FileInfo(path).Length
        };
    }

    public Task<Stream> OpenReadAsync(string key)
    {
        Stream stream = File.OpenRead(ResolvePath(key));
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key)
    {
        var path = ResolvePath(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }
}
