using FocusDeck.SharedKernel;

namespace FocusDeck.Server.Services.Storage;

public class LocalFileSystemAssetStorage : IAssetStorage
{
    private readonly string _storageRoot;
    private readonly IIdGenerator _idGenerator;
    private readonly ILogger<LocalFileSystemAssetStorage> _logger;

    public LocalFileSystemAssetStorage(
        IConfiguration configuration,
        IIdGenerator idGenerator,
        ILogger<LocalFileSystemAssetStorage> logger)
    {
        _storageRoot = configuration["Storage:Root"] ?? "/data/assets";
        _idGenerator = idGenerator;
        _logger = logger;

        // Ensure storage root exists
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<(string AssetId, string StoragePath)> StoreAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var assetId = _idGenerator.NewId().ToString();
        var now = DateTime.UtcNow;
        var extension = Path.GetExtension(fileName);

        // Create path: /data/assets/{yyyy}/{MM}/{id.ext}
        var yearMonth = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"));
        var directory = Path.Combine(_storageRoot, yearMonth);
        Directory.CreateDirectory(directory);

        var fileNameWithExt = $"{assetId}{extension}";
        var fullPath = Path.Combine(directory, fileNameWithExt);
        var relativePath = Path.Combine(yearMonth, fileNameWithExt);

        _logger.LogInformation("Storing asset {AssetId} to {Path}", assetId, relativePath);

        try
        {
            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await stream.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store asset {AssetId}", assetId);
            
            // Clean up partial file if it exists
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            
            throw;
        }

        return (assetId, relativePath);
    }

    public Task<Stream> RetrieveAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageRoot, storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Asset not found at {Path}", storagePath);
            throw new FileNotFoundException($"Asset not found: {storagePath}");
        }

        _logger.LogDebug("Retrieving asset from {Path}", storagePath);

        // Open file with async support and allow read sharing
        var fileStream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        return Task.FromResult<Stream>(fileStream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageRoot, storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Asset not found for deletion at {Path}", storagePath);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Deleting asset at {Path}", storagePath);

        try
        {
            File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete asset at {Path}", storagePath);
            throw;
        }

        return Task.CompletedTask;
    }

    public bool Exists(string storagePath)
    {
        var fullPath = Path.Combine(_storageRoot, storagePath);
        return File.Exists(fullPath);
    }
}
