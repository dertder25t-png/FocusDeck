namespace FocusDeck.Server.Jobs;

/// <summary>
/// Job interface for transcribing lecture audio/video to text
/// </summary>
public interface ITranscribeLectureJob
{
    /// <summary>
    /// Transcribe a lecture from audio/video file
    /// </summary>
    /// <param name="lectureId">Unique identifier for the lecture</param>
    /// <param name="fileUrl">URL or path to the audio/video file</param>
    /// <param name="language">Language code for transcription (e.g., "en", "es")</param>
    /// <returns>Transcription result with text and metadata</returns>
    Task<TranscriptionResult> TranscribeAsync(string lectureId, string fileUrl, string language = "en");
}

/// <summary>
/// Job interface for summarizing lecture content
/// </summary>
public interface ISummarizeLectureJob
{
    /// <summary>
    /// Summarize a lecture transcript or content
    /// </summary>
    /// <param name="lectureId">Unique identifier for the lecture</param>
    /// <param name="content">Text content to summarize</param>
    /// <param name="maxLength">Maximum length of summary in words</param>
    /// <returns>Summary result with condensed content</returns>
    Task<SummaryResult> SummarizeAsync(string lectureId, string content, int maxLength = 500);
}

/// <summary>
/// Job interface for verifying and fact-checking notes
/// </summary>
public interface IVerifyNoteJob
{
    /// <summary>
    /// Verify the accuracy and completeness of notes
    /// </summary>
    /// <param name="noteId">Unique identifier for the note</param>
    /// <param name="noteContent">Content of the note to verify</param>
    /// <param name="sourceContent">Source content to verify against (optional)</param>
    /// <returns>Verification result with suggestions and corrections</returns>
    Task<VerificationResult> VerifyAsync(string noteId, string noteContent, string? sourceContent = null);
}

/// <summary>
/// Result from transcription job
/// </summary>
public record TranscriptionResult
{
    public bool Success { get; init; }
    public string? TranscribedText { get; init; }
    public int WordCount { get; init; }
    public double DurationSeconds { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Result from summarization job
/// </summary>
public record SummaryResult
{
    public bool Success { get; init; }
    public string? Summary { get; init; }
    public string[]? KeyPoints { get; init; }
    public int OriginalWordCount { get; init; }
    public int SummaryWordCount { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result from verification job
/// </summary>
public record VerificationResult
{
    public bool Success { get; init; }
    public double AccuracyScore { get; init; } // 0.0 to 1.0
    public string[]? Suggestions { get; init; }
    public string[]? Corrections { get; init; }
    public bool IsComplete { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Job interface for generating structured notes from lectures
/// </summary>
public interface IGenerateLectureNoteJob
{
    /// <summary>
    /// Generate a structured note from a lecture's transcript and summary
    /// </summary>
    /// <param name="lectureId">Unique identifier for the lecture</param>
    /// <returns>Note generation result with note ID</returns>
    Task<NoteGenerationResult> GenerateNoteAsync(string lectureId);
}

/// <summary>
/// Result from note generation job
/// </summary>
public record NoteGenerationResult
{
    public bool Success { get; init; }
    public string? NoteId { get; init; }
    public string? ErrorMessage { get; init; }
}
