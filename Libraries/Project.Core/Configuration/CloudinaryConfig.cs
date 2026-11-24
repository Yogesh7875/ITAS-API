namespace Project.Core.Configuration;

public partial class CloudinaryConfig : IConfig
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string Folder { get; set; } = "itas";
}
