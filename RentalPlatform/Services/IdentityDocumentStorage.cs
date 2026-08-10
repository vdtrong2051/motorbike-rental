using Microsoft.AspNetCore.Http;

namespace RentalPlatform.Services;

public record IdentityDocumentContent(byte[] Bytes, string ContentType, string DownloadName);

public interface IIdentityDocumentStorage
{
    Task<(bool Success, string? Error, string? FileName)> SaveAsync(IFormFile file, Guid userId, string side);
    Task<IdentityDocumentContent?> ReadAsync(string? fileName);
    Task DeleteAsync(string? fileName);
}

public sealed class IdentityDocumentStorage : IIdentityDocumentStorage
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private readonly string _storageRoot;

    public IdentityDocumentStorage(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["IdentityDocuments:StoragePath"];
        _storageRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "IdentityDocuments")
            : configured);
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<(bool Success, string? Error, string? FileName)> SaveAsync(IFormFile file, Guid userId, string side)
    {
        if (file.Length <= 0) return (false, "Ảnh CCCD đang trống.", null);
        if (file.Length > MaxFileSize) return (false, "Mỗi ảnh CCCD không được vượt quá 5 MB.", null);

        await using var input = file.OpenReadStream();
        using var memory = new MemoryStream();
        await input.CopyToAsync(memory);
        var bytes = memory.ToArray();
        var format = DetectFormat(bytes);
        if (format is null)
            return (false, "Ảnh CCCD chỉ chấp nhận JPG, PNG hoặc WebP hợp lệ.", null);

        var safeSide = side == "back" ? "back" : "front";
        var fileName = $"{userId:N}_{safeSide}_{Guid.NewGuid():N}{format.Value.Extension}";
        await File.WriteAllBytesAsync(Path.Combine(_storageRoot, fileName), bytes);
        return (true, null, fileName);
    }

    public async Task<IdentityDocumentContent?> ReadAsync(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName) return null;
        var path = Path.Combine(_storageRoot, fileName);
        if (!File.Exists(path)) return null;
        var bytes = await File.ReadAllBytesAsync(path);
        var format = DetectFormat(bytes);
        return format is null ? null : new IdentityDocumentContent(bytes, format.Value.ContentType, fileName);
    }

    public Task DeleteAsync(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName) return Task.CompletedTask;
        var path = Path.Combine(_storageRoot, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private static (string Extension, string ContentType)? DetectFormat(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return (".jpg", "image/jpeg");
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return (".png", "image/png");
        if (bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return (".webp", "image/webp");
        return null;
    }
}
