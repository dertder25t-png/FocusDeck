namespace FocusDeck.Server.Services.Storage;

public interface IAssetStorage
{
    /// <summary>
    /// Stores an asset file to the file system
    /// </summary>
    /// <param name="stream">Stream containing the file data</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME content type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Asset ID and storage path</returns>
    Task<(string AssetId, string StoragePath)> StoreAsync(
        Stream stream, 
        string fileName, 
        string contentType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an asset file stream
    /// </summary>
    /// <param name="storagePath">Path to the stored asset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the file data</returns>
    Task<Stream> RetrieveAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an asset file
    /// </summary>
    /// <param name="storagePath">Path to the stored asset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an asset file exists
    /// </summary>
    /// <param name="storagePath">Path to check</param>
    /// <returns>True if the file exists</returns>
    bool Exists(string storagePath);
}
