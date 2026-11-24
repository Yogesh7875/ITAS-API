using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Project.Core.Configuration;

namespace Project.Services.Media;

/// <summary>
/// Cloudinary storage using the REST API (no SDK package needed).
/// Files are uploaded as type=authenticated, so they are NOT public: the backend
/// downloads them with a signed request and streams them to the caller.
/// Verify the upload and download once against your Cloudinary account before going live.
/// Key format: {resource_type}|{public_id}|{format}
/// </summary>
public partial class CloudinaryFileStorageService : IFileStorageService
{
    private static readonly HttpClient Http = new();
    private readonly AppSettings _appSettings;

    public CloudinaryFileStorageService(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public string Provider => "Cloudinary";

    private CloudinaryConfig Cfg => _appSettings.Get<CloudinaryConfig>();

    private static string Sha1(string text) =>
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static string Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, string folder)
    {
        var cfg = Cfg;
        var publicId = $"{cfg.Folder.Trim('/')}/{folder.Trim('/')}/{Guid.NewGuid():N}";
        var timestamp = Now();
        var signature = Sha1($"public_id={publicId}&timestamp={timestamp}&type=authenticated{cfg.ApiSecret}");

        using var form = new MultipartFormDataContent
        {
            { new StreamContent(content), "file", fileName },
            { new StringContent(cfg.ApiKey), "api_key" },
            { new StringContent(timestamp), "timestamp" },
            { new StringContent(publicId), "public_id" },
            { new StringContent("authenticated"), "type" },
            { new StringContent(signature), "signature" }
        };

        using var response = await Http.PostAsync(
            $"https://api.cloudinary.com/v1_1/{cfg.CloudName}/auto/upload", form);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Cloudinary upload failed: {json}");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var resourceType = root.GetProperty("resource_type").GetString();
        var returnedId = root.GetProperty("public_id").GetString();
        var format = root.TryGetProperty("format", out var f) ? f.GetString() : string.Empty;
        var bytes = root.TryGetProperty("bytes", out var b) ? b.GetInt64() : 0;

        return new StoredFile
        {
            Provider = Provider,
            Key = $"{resourceType}|{returnedId}|{format}",
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = bytes
        };
    }

    public async Task<Stream> OpenReadAsync(string key)
    {
        var cfg = Cfg;
        var parts = key.Split('|');
        var resourceType = parts[0];
        var publicId = parts[1];
        var format = parts.Length > 2 ? parts[2] : string.Empty;
        var timestamp = Now();

        var toSign = string.IsNullOrEmpty(format)
            ? $"public_id={publicId}&timestamp={timestamp}&type=authenticated"
            : $"format={format}&public_id={publicId}&timestamp={timestamp}&type=authenticated";
        var signature = Sha1(toSign + cfg.ApiSecret);

        var url = $"https://api.cloudinary.com/v1_1/{cfg.CloudName}/{resourceType}/download" +
                  $"?api_key={cfg.ApiKey}&public_id={Uri.EscapeDataString(publicId)}" +
                  (string.IsNullOrEmpty(format) ? string.Empty : $"&format={format}") +
                  $"&timestamp={timestamp}&type=authenticated&signature={signature}";

        var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    public async Task DeleteAsync(string key)
    {
        var cfg = Cfg;
        var parts = key.Split('|');
        var timestamp = Now();
        var signature = Sha1($"public_id={parts[1]}&timestamp={timestamp}&type=authenticated{cfg.ApiSecret}");

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["public_id"] = parts[1],
            ["type"] = "authenticated",
            ["timestamp"] = timestamp,
            ["api_key"] = cfg.ApiKey,
            ["signature"] = signature
        });

        using var response = await Http.PostAsync(
            $"https://api.cloudinary.com/v1_1/{cfg.CloudName}/{parts[0]}/destroy", form);
        response.EnsureSuccessStatusCode();
    }
}
