using System;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities
{
    public class AcademicSource : IMustHaveTenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Doi { get; set; } = string.Empty;

        // Extended fields for robust citations
        public string ContainerTitle { get; set; } = string.Empty; // Journal or Book Title
        public string Volume { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
        public string Pages { get; set; } = string.Empty;

        public string NoteId { get; set; } = string.Empty;
        public Note? Note { get; set; }
        public Guid TenantId { get; set; }
    }
}
