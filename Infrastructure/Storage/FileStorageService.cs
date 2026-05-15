using System.Security.Cryptography;
using invoice.Application.Interfaces;

namespace invoice.Infrastructure.Storage;

public class FileStorageService : IStorageService
{
    public async Task<(string ImageHash, string FilePath)> SaveAsync(
        Stream file,
        string ownerPhone,
        string extension
    )
    {
        var memory = new MemoryStream();
        await file.CopyToAsync(memory);

        var bytes = memory.ToArray();

        // 1. HASH SHA256
        var hash = ComputeSha256(bytes);

        // 2. PATH
        var folder = BuildFolder(ownerPhone);
        EnsureFolder(folder);

        var shortHash = hash[..8];
        var fileName = $"invoice-{shortHash}.{extension}";
        var fullPath = Path.Combine(folder, fileName);

        // 3. GUARDAR (evita duplicados)
        if (!File.Exists(fullPath))
            await File.WriteAllBytesAsync(fullPath, bytes);

        return (hash, fullPath);
    }

    private static string ComputeSha256(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return Convert.ToHexString(hash); // HEX limpio
    }

    private static string BuildFolder(string ownerPhone)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "invoices");
        var month = DateTime.UtcNow.ToString("yyyy-MM");

        return Path.Combine(basePath, ownerPhone, month);
    }

    private static void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public async Task<string> SaveRawAzureAsync(string ownerPhone,string imageHash,string json)
    {
        var folder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Storage",
            ownerPhone,
            DateTime.UtcNow.ToString("yyyy-MM"),
            "raw"
        );

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, $"{imageHash}.json");

        await File.WriteAllTextAsync(filePath, json);

        return filePath;
    }

    public async Task<string> SaveRawJsonAsync(
    string imageHash,
    string rawJson)
    {
        var folder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Storage",
            "ocr");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var path = Path.Combine(
            folder,
            $"{imageHash}.json");

        await File.WriteAllTextAsync(path, rawJson);

        return path;
    }
}