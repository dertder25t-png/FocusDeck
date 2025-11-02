namespace FocusDeck.Domain.Entities;

public class Asset
{
    public string Id { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeInBytes { get; set; }
    public string StoragePath { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
