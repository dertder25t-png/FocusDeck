namespace FocusDeck.Contracts.DTOs;

public class AssetDto
{
    public string Id { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeInBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class AssetUploadResponse
{
    public string Id { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public long SizeInBytes { get; set; }
    public string Url { get; set; } = null!;
}
