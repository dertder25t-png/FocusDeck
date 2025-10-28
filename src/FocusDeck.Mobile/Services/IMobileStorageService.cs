namespace FocusDeck.Mobile.Services;

/// <summary>
/// Mobile storage service for file and app data management.
/// Handles platform-specific file paths, reading, writing, and storage queries.
/// </summary>
public interface IMobileStorageService
{
    Task<string> GetAppDataPathAsync();
    Task<string> GetCachePathAsync();
    Task<bool> FileExistsAsync(string filePath);
    Task<byte[]> ReadFileAsync(string filePath);
    Task WriteFileAsync(string filePath, byte[] data);
    Task DeleteFileAsync(string filePath);
    Task<long> GetStorageUsageAsync();
}
