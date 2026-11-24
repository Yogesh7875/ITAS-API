namespace Project.Core.Configuration;

/// <summary>Where uploaded documents are stored: "Local" or "Cloudinary".</summary>
public partial class FileStorageConfig : IConfig
{
    public string Provider { get; set; } = "Local";

    /// <summary>Folder for the Local provider (relative to the app). Keep it OUT of wwwroot so files are never public.</summary>
    public string LocalRootPath { get; set; } = "App_Data/Documents";
}
