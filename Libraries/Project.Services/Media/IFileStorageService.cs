namespace Project.Services.Media;

public partial class StoredFile
{
    public string Provider { get; set; }
    public string Key { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long SizeBytes { get; set; }
}

public partial interface IFileStorageService
{
    string Provider { get; }

    Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, string folder);

    Task<Stream> OpenReadAsync(string key);

    Task DeleteAsync(string key);
}

/// <summary>Picks the configured provider for new files and the right one for existing files.</summary>
public partial interface IFileStorageResolver
{
    IFileStorageService Current { get; }

    IFileStorageService For(string provider);
}
