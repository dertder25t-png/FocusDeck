using Asp.Versioning;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Models;
using FocusDeck.Server.Services.Storage;
using FocusDeck.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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

    // Content-type whitelist with extension mapping
    private static readonly Dictionary<string, string[]> AllowedContentTypes = new()
    {
        // Audio files
        ["audio/wav"] = new[] { ".wav" },
        ["audio/wave"] = new[] { ".wav" },
        ["audio/x-wav"] = new[] { ".wav" },
        ["audio/mpeg"] = new[] { ".mp3" },
        ["audio/mp3"] = new[] { ".mp3" },
        ["audio/mp4"] = new[] { ".m4a", ".mp4" },
        ["audio/x-m4a"] = new[] { ".m4a" },
        
        // Image files
        ["image/jpeg"] = new[] { ".jpg", ".jpeg" },
        ["image/png"] = new[] { ".png" },
        ["image/gif"] = new[] { ".gif" },
        ["image/webp"] = new[] { ".webp" },
        
        // Document files
        ["application/pdf"] = new[] { ".pdf" },
        ["text/plain"] = new[] { ".txt" },
        ["text/markdown"] = new[] { ".md" },
        ["application/json"] = new[] { ".json" }
    };

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
    [ProducesResponseType(typeof(ErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorEnvelope), StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<AssetUploadResponse>> UploadAsset(
        [FromForm] IFormFile file,
        [FromForm] string? description,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (file == null || file.Length == 0)
        {
            return BadRequest(new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "FILE_REQUIRED",
                Message = "No file provided or file is empty"
            });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "FILE_TOO_LARGE",
                Message = $"File size ({file.Length / 1024 / 1024}MB) exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024}MB",
                Details = new { maxSizeBytes = MaxFileSizeBytes, actualSizeBytes = file.Length }
            });
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            return BadRequest(new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "FILENAME_REQUIRED",
                Message = "File name is required"
            });
        }

        // Validate content type
        var contentType = file.ContentType ?? "application/octet-stream";
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!AllowedContentTypes.TryGetValue(contentType.ToLowerInvariant(), out var allowedExtensions))
        {
            var allowedTypes = string.Join(", ", AllowedContentTypes.Keys);
            return BadRequest(new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "INVALID_CONTENT_TYPE",
                Message = $"Content type '{contentType}' is not allowed",
                Details = new 
                { 
                    allowedContentTypes = AllowedContentTypes.Keys.ToArray(),
                    providedContentType = contentType
                }
            });
        }

        // Validate file extension matches content type
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "EXTENSION_MISMATCH",
                Message = $"File extension '{fileExtension}' does not match content type '{contentType}'",
                Details = new
                {
                    contentType,
                    fileExtension,
                    expectedExtensions = allowedExtensions
                }
            });
        }

        var userName = User.Identity?.Name;

        _logger.LogInformation(
            "Uploading asset: {FileName}, Size: {Size} bytes, ContentType: {ContentType}, User: {User}",
            file.FileName, file.Length, contentType, userName);

        try
        {
            // Store the file with streaming and size validation
            using var stream = file.OpenReadStream();
            
            // Guard against size during streaming (defense in depth)
            var guardedStream = new SizeGuardStream(stream, MaxFileSizeBytes);
            
            var (assetId, storagePath) = await _assetStorage.StoreAsync(
                guardedStream,
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("exceeds maximum"))
        {
            _logger.LogWarning(ex, "File size exceeded during streaming: {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "FILE_TOO_LARGE",
                Message = "File size exceeded maximum allowed size during upload",
                Details = new { maxSizeBytes = MaxFileSizeBytes }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload asset {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "UPLOAD_FAILED",
                Message = "Failed to upload asset"
            });
        }
    }

    /// <summary>
    /// Get an asset file by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorEnvelope), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsset(string id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var asset = await _context.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (asset == null)
        {
            _logger.LogWarning("Asset not found: {AssetId}", id);
            return NotFound(new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "ASSET_NOT_FOUND",
                Message = $"Asset not found: {id}"
            });
        }

        if (!_assetStorage.Exists(asset.StoragePath))
        {
            _logger.LogError("Asset file missing: {AssetId}, Path: {Path}", id, asset.StoragePath);
            return NotFound(new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "ASSET_FILE_MISSING",
                Message = "Asset file not found on storage"
            });
        }

        try
        {
            var stream = await _assetStorage.RetrieveAsync(asset.StoragePath, cancellationToken);
            
            _logger.LogDebug("Serving asset {AssetId}: {FileName}", id, asset.FileName);
            
            return File(stream, asset.ContentType, asset.FileName, enableRangeProcessing: true);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "ASSET_FILE_NOT_FOUND",
                Message = "Asset file not found"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve asset {AssetId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "RETRIEVAL_FAILED",
                Message = "Failed to retrieve asset"
            });
        }
    }

    /// <summary>
    /// Delete an asset by ID
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorEnvelope), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsset(string id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var asset = await _context.Assets.FindAsync(new object[] { id }, cancellationToken);

        if (asset == null)
        {
            _logger.LogWarning("Asset not found for deletion: {AssetId}", id);
            return NotFound(new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "ASSET_NOT_FOUND",
                Message = $"Asset not found: {id}"
            });
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
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "DELETION_FAILED",
                Message = "Failed to delete asset"
            });
        }
    }

    /// <summary>
    /// Get asset metadata
    /// </summary>
    [HttpGet("{id}/metadata")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorEnvelope), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetAssetMetadata(string id, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var asset = await _context.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (asset == null)
        {
            return NotFound(new ErrorEnvelope
            {
                TraceId = traceId,
                Code = "ASSET_NOT_FOUND",
                Message = $"Asset not found: {id}"
            });
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

/// <summary>
/// Stream wrapper that enforces a maximum size limit
/// </summary>
internal class SizeGuardStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long _maxSize;
    private long _bytesRead;

    public SizeGuardStream(Stream innerStream, long maxSize)
    {
        _innerStream = innerStream;
        _maxSize = maxSize;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _innerStream.Length;
    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        _bytesRead += bytesRead;
        
        if (_bytesRead > _maxSize)
        {
            throw new InvalidOperationException($"Stream size exceeds maximum allowed size of {_maxSize} bytes");
        }
        
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        _bytesRead += bytesRead;
        
        if (_bytesRead > _maxSize)
        {
            throw new InvalidOperationException($"Stream size exceeds maximum allowed size of {_maxSize} bytes");
        }
        
        return bytesRead;
    }

    public override void Flush() => _innerStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
