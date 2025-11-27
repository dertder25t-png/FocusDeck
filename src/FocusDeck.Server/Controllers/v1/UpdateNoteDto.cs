using System.Collections.Generic;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Server.Controllers.v1
{
    public class UpdateNoteDto
    {
        public string? Content { get; set; }
        public NoteType? Type { get; set; }
        public string? CitationStyle { get; set; }
        public List<AcademicSourceDto>? Sources { get; set; }
    }

    public class AcademicSourceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
