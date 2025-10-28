using System.Diagnostics;

namespace FocusDeck.Mobile.Services;

/// <summary>
/// Stub implementation of mobile storage service.
/// Provides file and app data management across platforms.
/// </summary>
public class MobileStorageService : IMobileStorageService
{
    public Task<string> GetAppDataPathAsync()
    {
        // Platform-specific app data path
        var path = FileSystem.AppDataDirectory;
        return Task.FromResult(path);
    }

    public Task<string> GetCachePathAsync()
    {
        // Platform-specific cache path
        var path = FileSystem.CacheDirectory;
        return Task.FromResult(path);
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var exists = File.Exists(filePath);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking file existence: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<byte[]> ReadFileAsync(string filePath)
    {
        try
        {
            var data = File.ReadAllBytes(filePath);
            return Task.FromResult(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading file: {ex.Message}");
            return Task.FromResult(Array.Empty<byte>());
        }
    }

    public Task WriteFileAsync(string filePath, byte[] data)
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllBytes(filePath, data);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing file: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    public Task DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting file: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    public Task<long> GetStorageUsageAsync()
    {
        try
        {
            var appDataPath = FileSystem.AppDataDirectory;
            var dirInfo = new DirectoryInfo(appDataPath);
            
            long totalSize = 0;
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                totalSize += file.Length;
            }
            
            return Task.FromResult(totalSize);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error calculating storage usage: {ex.Message}");
            return Task.FromResult(0L);
        }
    }
}
