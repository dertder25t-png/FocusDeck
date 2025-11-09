namespace FocusDeck.Domain.Entities;

public class DesignProject : IMustHaveTenant
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? GoalsText { get; set; }
    public List<string> Vibes { get; set; } = new();
    public string? RequirementsText { get; set; }
    public List<string> BrandKeywords { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DesignIdea> Ideas { get; set; } = new List<DesignIdea>();
    public Guid TenantId { get; set; }
}

public enum DesignIdeaType
{
    Thumbnail,
    Prompt,
    Moodboard,
    Reference
}

public class DesignIdea : IMustHaveTenant
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public DesignIdeaType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? AssetId { get; set; }
    public double? Score { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }

    public DesignProject Project { get; set; } = null!;
    public Guid TenantId { get; set; }
}
