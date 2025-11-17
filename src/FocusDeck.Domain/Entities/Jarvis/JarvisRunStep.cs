using System;

namespace FocusDeck.Domain.Entities.Jarvis
{
    public class JarvisRunStep
    {
        public Guid Id { get; set; }
        public Guid RunId { get; set; }
        public JarvisRun Run { get; set; } = default!;

        public int Order { get; set; }              // 1, 2, 3...
        public JarvisRunStepType StepType { get; set; } // LlmCall, Action, SystemNote

        public DateTimeOffset CreatedAt { get; set; }
        public string? RequestJson { get; set; }      // prompt, action input, etc.
        public string? ResponseJson { get; set; }     // LLM output, action result, etc.
        public string? ErrorJson { get; set; }        // exception, validation error, etc.
    }
}
