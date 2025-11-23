using System;

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
        public string NoteId { get; set; } = string.Empty;
        public Note? Note { get; set; }
        public Guid TenantId { get; set; }
    }
}
