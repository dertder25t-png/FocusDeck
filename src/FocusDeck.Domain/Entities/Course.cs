namespace FocusDeck.Domain.Entities;

public class Course : IMustHaveTenant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? Instructor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
    public Guid TenantId { get; set; }
}
