using Asp.Versioning;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Storage;
using FocusDeck.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly AutomationDbContext _context;
    private readonly IAssetStorage _assetStorage;
    private readonly IClock _clock;
    private readonly ILogger<AssetsController> _logger;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public AssetsController(
        AutomationDbContext context,
        IAssetStorage assetStorage,
        IClock clock,
        ILogger<AssetsController> logger)
    {
        _context = context;
        _assetStorage = assetStorage;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new asset file
    /// </summary>
    [HttpPost("/v{version:apiVersion}/uploads/asset")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AssetUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<AssetUploadResponse>> UploadAsset(
        [FromForm] IFormFile file,
        [FromForm] string? description,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided" });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, 
                new { message = $"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024}MB" });
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            return BadRequest(new { message = "File name is required" });
        }

        var contentType = file.ContentType ?? "application/octet-stream";
        var userName = User.Identity?.Name;

        _logger.LogInformation(
            "Uploading asset: {FileName}, Size: {Size} bytes, ContentType: {ContentType}, User: {User}",
            file.FileName, file.Length, contentType, userName);

        try
        {
            // Store the file
            using var stream = file.OpenReadStream();
            var (assetId, storagePath) = await _assetStorage.StoreAsync(
                stream,
                file.FileName,
                contentType,
                cancellationToken);

            // Create asset entity
            var asset = new Asset
            {
                Id = assetId,
                FileName = file.FileName,
                ContentType = contentType,
                SizeInBytes = file.Length,
                StoragePath = storagePath,
                UploadedAt = _clock.UtcNow,
                UploadedBy = userName,
                Description = description
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new AssetUploadResponse
            {
                Id = asset.Id,
                FileName = asset.FileName,
                SizeInBytes = asset.SizeInBytes,
                Url = Url.Action(nameof(GetAsset), new { id = asset.Id }) ?? $"/v1/assets/{asset.Id}"
            };

            return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload asset {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Failed to upload asset" });
        }
    }

    /// <summary>
    /// Get an asset file by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsset(string id, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (asset == null)
        {
            _logger.LogWarning("Asset not found: {AssetId}", id);
            return NotFound(new { message = $"Asset not found: {id}" });
        }

        if (!_assetStorage.Exists(asset.StoragePath))
        {
            _logger.LogError("Asset file missing: {AssetId}, Path: {Path}", id, asset.StoragePath);
            return NotFound(new { message = "Asset file not found on storage" });
        }

        try
        {
            var stream = await _assetStorage.RetrieveAsync(asset.StoragePath, cancellationToken);
            
            _logger.LogDebug("Serving asset {AssetId}: {FileName}", id, asset.FileName);
            
            return File(stream, asset.ContentType, asset.FileName, enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Asset file not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve asset {AssetId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Failed to retrieve asset" });
        }
    }

    /// <summary>
    /// Delete an asset by ID
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsset(string id, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets.FindAsync(new object[] { id }, cancellationToken);

        if (asset == null)
        {
            _logger.LogWarning("Asset not found for deletion: {AssetId}", id);
            return NotFound(new { message = $"Asset not found: {id}" });
        }

        _logger.LogInformation("Deleting asset {AssetId}: {FileName}", id, asset.FileName);

        try
        {
            // Delete from storage
            if (_assetStorage.Exists(asset.StoragePath))
            {
                await _assetStorage.DeleteAsync(asset.StoragePath, cancellationToken);
            }

            // Delete from database
            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete asset {AssetId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Failed to delete asset" });
        }
    }

    /// <summary>
    /// Get asset metadata
    /// </summary>
    [HttpGet("{id}/metadata")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetAssetMetadata(string id, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (asset == null)
        {
            return NotFound(new { message = $"Asset not found: {id}" });
        }

        var dto = new AssetDto
        {
            Id = asset.Id,
            FileName = asset.FileName,
            ContentType = asset.ContentType,
            SizeInBytes = asset.SizeInBytes,
            UploadedAt = asset.UploadedAt,
            UploadedBy = asset.UploadedBy,
            Description = asset.Description,
            Metadata = asset.Metadata
        };

        return Ok(dto);
    }
}
