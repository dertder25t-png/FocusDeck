using System;

namespace FocusDeck.Domain.Entities
{
    public enum CapturedItemType
    {
        Page,
        AiChat,
        CodeSnippet,
        ResearchArticle
    }

    public class CapturedItem : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? Summary { get; set; }
        public string TagsJson { get; set; } = "[]";
        public CapturedItemType Kind { get; set; }

        public Guid? ProjectId { get; set; }
        public Project? Project { get; set; }

        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

        public Guid TenantId { get; set; }
    }
}
