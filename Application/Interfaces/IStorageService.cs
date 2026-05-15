namespace invoice.Application.Interfaces;

public interface IStorageService
{
    Task<(string ImageHash, string FilePath)> SaveAsync(
        Stream file,
        string ownerPhone,
        string extension
    );

    Task<string> SaveRawAzureAsync(
    string ownerPhone,
    string imageHash,
    string json
);
    Task<string> SaveRawJsonAsync(
    string imageHash,
    string rawJson
);
}