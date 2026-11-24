using Project.Core.Configuration;

namespace Project.Services.Media;

public partial class FileStorageResolver : IFileStorageResolver
{
    private readonly AppSettings _appSettings;
    private readonly LocalFileStorageService _local;
    private readonly CloudinaryFileStorageService _cloudinary;

    public FileStorageResolver(AppSettings appSettings,
        LocalFileStorageService local,
        CloudinaryFileStorageService cloudinary)
    {
        _appSettings = appSettings;
        _local = local;
        _cloudinary = cloudinary;
    }

    public IFileStorageService Current =>
        For(_appSettings.Get<FileStorageConfig>().Provider);

    public IFileStorageService For(string provider) =>
        string.Equals(provider, "Cloudinary", StringComparison.OrdinalIgnoreCase) ? _cloudinary : _local;
}
