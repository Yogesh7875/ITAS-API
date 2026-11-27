using Project.Services.Drivers;

namespace Project.Admin.Models.Common;

/// <summary>
/// A file sent by the mobile apps as base64 inside a JSON body.
/// Base64Data may be raw base64 or a data URI ("data:image/png;base64,....").
/// </summary>
public class Base64FileModel
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public string Base64Data { get; set; }
}

public static class Base64FileHelper
{
    private static readonly Dictionary<string, string> _extensionByMime = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/jpg"] = "jpg",
        ["image/png"] = "png",
        ["application/pdf"] = "pdf"
    };

    private static readonly Dictionary<string, string> _mimeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jpg"] = "image/jpeg",
        ["jpeg"] = "image/jpeg",
        ["png"] = "image/png",
        ["pdf"] = "application/pdf"
    };

    /// <summary>
    /// Decodes the base64 payload into an <see cref="UploadedFile"/> (seekable MemoryStream).
    /// Returns null with an error message when the payload is missing, malformed or too large.
    /// </summary>
    public static UploadedFile Decode(Base64FileModel model, long maxBytes, out string error)
    {
        error = null;
        var data = model?.Base64Data?.Trim();
        if (string.IsNullOrEmpty(data))
        {
            error = "File data is empty";
            return null;
        }

        var contentType = model.ContentType?.Trim();

        //strip a data URI prefix: data:<mime>;base64,<data>
        if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = data.IndexOf(',');
            if (comma < 0)
            {
                error = "Invalid file data";
                return null;
            }

            var header = data[5..comma];
            var semicolon = header.IndexOf(';');
            var uriMime = semicolon >= 0 ? header[..semicolon] : header;
            if (string.IsNullOrEmpty(contentType) && !string.IsNullOrEmpty(uriMime))
                contentType = uriMime;

            data = data[(comma + 1)..];
        }

        //cheap size check before allocating: decoded length is about 3/4 of the base64 length
        if (data.Length / 4L * 3 > maxBytes + 3)
        {
            error = "File is too large";
            return null;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(data);
        }
        catch (FormatException)
        {
            error = "Invalid file data. Files must be base64 encoded";
            return null;
        }

        if (bytes.Length == 0)
        {
            error = "File data is empty";
            return null;
        }

        if (bytes.Length > maxBytes)
        {
            error = "File is too large";
            return null;
        }

        //make sure the file name carries an extension, the document type check relies on it
        var fileName = Path.GetFileName(model.FileName?.Trim() ?? string.Empty);
        var ext = Path.GetExtension(fileName).TrimStart('.');
        if (string.IsNullOrEmpty(ext))
        {
            ext = contentType != null && _extensionByMime.TryGetValue(contentType, out var e) ? e : string.Empty;
            fileName = (string.IsNullOrEmpty(fileName) ? "file" : fileName) + (string.IsNullOrEmpty(ext) ? string.Empty : "." + ext);
        }

        if (string.IsNullOrEmpty(contentType))
            contentType = _mimeByExtension.TryGetValue(ext, out var m) ? m : "application/octet-stream";

        return new UploadedFile
        {
            FileName = fileName,
            ContentType = contentType,
            Length = bytes.Length,
            Content = new MemoryStream(bytes, writable: false)
        };
    }
}
