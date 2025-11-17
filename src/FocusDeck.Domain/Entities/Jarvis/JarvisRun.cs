using System;
using System.Collections.Generic;

namespace FocusDeck.Domain.Entities.Jarvis
{
    public class JarvisRun
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = default!;
        public string? TenantId { get; set; }

        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        public JarvisRunStatus Status { get; set; }

        public string EntryPoint { get; set; } = default!; // e.g. "start_study_session", "summarize_lecture"
        public string? InputPayloadJson { get; set; }       // raw request

        public ICollection<JarvisRunStep> Steps { get; set; } = new List<JarvisRunStep>();
    }
}
