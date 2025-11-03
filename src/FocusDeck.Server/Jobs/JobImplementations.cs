using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Hubs;
using FocusDeck.Server.Services.Storage;
using FocusDeck.Server.Services.Transcription;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Jobs;

/// <summary>
/// Implementation of lecture transcription job using Whisper adapter
/// </summary>
public class TranscribeLectureJob : ITranscribeLectureJob
{
    private readonly ILogger<TranscribeLectureJob> _logger;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;
    private readonly IWhisperAdapter _whisperAdapter;
    private readonly IAssetStorage _assetStorage;
    private readonly AutomationDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public TranscribeLectureJob(
        ILogger<TranscribeLectureJob> logger,
        IHubContext<NotificationsHub, INotificationClient> hubContext,
        IWhisperAdapter whisperAdapter,
        IAssetStorage assetStorage,
        AutomationDbContext context,
        IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _hubContext = hubContext;
        _whisperAdapter = whisperAdapter;
        _assetStorage = assetStorage;
        _context = context;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<TranscriptionResult> TranscribeAsync(string lectureId, string fileUrl, string language = "en")
    {
        _logger.LogInformation(
            "Starting transcription job for lecture {LectureId}, file: {FileUrl}, language: {Language}",
            lectureId, fileUrl, language);

        try
        {
            // Update lecture status
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture == null)
            {
                throw new Exception($"Lecture {lectureId} not found");
            }

            lecture.Status = LectureStatus.Transcribing;
            await _context.SaveChangesAsync();

            // Notify progress
            await _hubContext.Clients.All.JobProgress(lectureId, "TranscribeLecture", 0, "Starting transcription...");

            // Get audio file path from storage
            await _hubContext.Clients.All.JobProgress(lectureId, "TranscribeLecture", 10, "Loading audio file...");
            var audioFilePath = _assetStorage.GetPhysicalPath(lecture.AudioAssetId!);

            // Transcribe using Whisper
            await _hubContext.Clients.All.JobProgress(lectureId, "TranscribeLecture", 30, "Transcribing audio...");
            var transcribedText = await _whisperAdapter.TranscribeAsync(audioFilePath, language);

            if (string.IsNullOrWhiteSpace(transcribedText))
            {
                throw new Exception("Transcription resulted in empty text");
            }

            await _hubContext.Clients.All.JobProgress(lectureId, "TranscribeLecture", 90, "Saving transcription...");

            // Update lecture with transcription
            lecture.TranscriptionText = transcribedText;
            lecture.Status = LectureStatus.Transcribed;
            await _context.SaveChangesAsync();

            var wordCount = transcribedText.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            var result = new TranscriptionResult
            {
                Success = true,
                TranscribedText = transcribedText,
                WordCount = wordCount,
                DurationSeconds = 0,
                Metadata = new Dictionary<string, object>
                {
                    ["Language"] = language,
                    ["FileUrl"] = fileUrl,
                    ["ProcessedAt"] = DateTime.UtcNow,
                    ["LectureId"] = lectureId
                }
            };

            _logger.LogInformation(
                "Transcription job completed successfully for lecture {LectureId}, word count: {WordCount}",
                lectureId, result.WordCount);

            // Send SignalR event
            await _hubContext.Clients.All.LectureTranscribed(lectureId, transcribedText, "Transcription completed successfully");

            // Notify completion
            await _hubContext.Clients.All.JobCompleted(
                lectureId,
                "TranscribeLecture",
                true,
                "Transcription completed successfully",
                result);

            // Enqueue summarization job
            _backgroundJobClient.Enqueue<ISummarizeLectureJob>(x => x.SummarizeAsync(lectureId, transcribedText, 500));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing lecture {LectureId}", lectureId);

            // Update lecture status to failed
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture != null)
            {
                lecture.Status = LectureStatus.Failed;
                await _context.SaveChangesAsync();
            }

            await _hubContext.Clients.All.JobCompleted(
                lectureId,
                "TranscribeLecture",
                false,
                $"Transcription failed: {ex.Message}",
                null);

            return new TranscriptionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Implementation of lecture summarization job using ITextGen
/// </summary>
public class SummarizeLectureJob : ISummarizeLectureJob
{
    private readonly ILogger<SummarizeLectureJob> _logger;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;
    private readonly Services.TextGeneration.ITextGen _textGen;
    private readonly AutomationDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public SummarizeLectureJob(
        ILogger<SummarizeLectureJob> logger,
        IHubContext<NotificationsHub, INotificationClient> hubContext,
        Services.TextGeneration.ITextGen textGen,
        AutomationDbContext context,
        IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _hubContext = hubContext;
        _textGen = textGen;
        _context = context;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<SummaryResult> SummarizeAsync(string lectureId, string content, int maxLength = 500)
    {
        _logger.LogInformation(
            "Starting summarization job for lecture {LectureId}, content length: {ContentLength}, max length: {MaxLength}",
            lectureId, content.Length, maxLength);

        try
        {
            // Update lecture status
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture == null)
            {
                throw new Exception($"Lecture {lectureId} not found");
            }

            lecture.Status = LectureStatus.Summarizing;
            await _context.SaveChangesAsync();

            // Notify progress
            await _hubContext.Clients.All.JobProgress(lectureId, "SummarizeLecture", 0, "Starting summarization...");

            var originalWordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            // Generate summary using ITextGen
            await _hubContext.Clients.All.JobProgress(lectureId, "SummarizeLecture", 30, "Analyzing content...");
            
            var prompt = $"Summarize the following lecture transcription in {maxLength} words or less. Include key points:\n\n{content}";
            var summary = await _textGen.GenerateAsync(prompt, maxLength, 0.5);

            await _hubContext.Clients.All.JobProgress(lectureId, "SummarizeLecture", 80, "Extracting key points...");

            // Extract key points (simple heuristic: split by sentences and take first 3-5)
            var sentences = summary.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Take(5)
                .ToArray();

            var result = new SummaryResult
            {
                Success = true,
                Summary = summary,
                KeyPoints = sentences,
                OriginalWordCount = originalWordCount,
                SummaryWordCount = summary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
            };

            await _hubContext.Clients.All.JobProgress(lectureId, "SummarizeLecture", 95, "Saving summary...");

            // Update lecture with summary
            lecture.SummaryText = summary;
            lecture.Status = LectureStatus.Summarized;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Summarization job completed successfully for lecture {LectureId}, original: {Original} words, summary: {Summary} words",
                lectureId, result.OriginalWordCount, result.SummaryWordCount);

            // Send SignalR event
            await _hubContext.Clients.All.LectureSummarized(lectureId, summary, "Summarization completed successfully");

            // Notify completion
            await _hubContext.Clients.All.JobCompleted(
                lectureId,
                "SummarizeLecture",
                true,
                "Summarization completed successfully",
                result);

            // Enqueue note generation job
            _backgroundJobClient.Enqueue<IGenerateLectureNoteJob>(x => x.GenerateNoteAsync(lectureId));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing lecture {LectureId}", lectureId);

            // Update lecture status to failed
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture != null)
            {
                lecture.Status = LectureStatus.Failed;
                await _context.SaveChangesAsync();
            }

            await _hubContext.Clients.All.JobCompleted(
                lectureId,
                "SummarizeLecture",
                false,
                $"Summarization failed: {ex.Message}",
                null);

            return new SummaryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Stub implementation of note verification job
/// </summary>
public class VerifyNoteJob : IVerifyNoteJob
{
    private readonly ILogger<VerifyNoteJob> _logger;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;

    public VerifyNoteJob(
        ILogger<VerifyNoteJob> logger,
        IHubContext<NotificationsHub, INotificationClient> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<VerificationResult> VerifyAsync(string noteId, string noteContent, string? sourceContent = null)
    {
        _logger.LogInformation(
            "Starting verification job for note {NoteId}, content length: {ContentLength}, has source: {HasSource}",
            noteId, noteContent.Length, sourceContent != null);

        // Notify progress
        await _hubContext.Clients.All.JobProgress(noteId, "VerifyNote", 0, "Starting verification...");

        // Simulate work
        await Task.Delay(100);

        await _hubContext.Clients.All.JobProgress(noteId, "VerifyNote", 50, "Analyzing content...");
        await Task.Delay(100);

        await _hubContext.Clients.All.JobProgress(noteId, "VerifyNote", 90, "Generating suggestions...");
        await Task.Delay(100);

        var result = new VerificationResult
        {
            Success = true,
            AccuracyScore = 0.85,
            IsComplete = true,
            Suggestions = new[]
            {
                "[Stub] Consider adding more context to section 1",
                "[Stub] Clarify terminology in paragraph 3"
            },
            Corrections = new[]
            {
                "[Stub] Suggested correction: Replace 'their' with 'there' in line 5"
            }
        };

        _logger.LogInformation(
            "Verification job completed successfully for note {NoteId}, accuracy: {Accuracy:P0}",
            noteId, result.AccuracyScore);

        // Notify completion
        await _hubContext.Clients.All.JobCompleted(
            noteId,
            "VerifyNote",
            true,
            "Verification completed successfully",
            result);

        return result;
    }
}

/// <summary>
/// Implementation of lecture note generation job
/// </summary>
public class GenerateLectureNoteJob : IGenerateLectureNoteJob
{
    private readonly ILogger<GenerateLectureNoteJob> _logger;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;
    private readonly Services.TextGeneration.ITextGen _textGen;
    private readonly AutomationDbContext _context;
    private readonly SharedKernel.IIdGenerator _idGenerator;

    public GenerateLectureNoteJob(
        ILogger<GenerateLectureNoteJob> logger,
        IHubContext<NotificationsHub, INotificationClient> hubContext,
        Services.TextGeneration.ITextGen textGen,
        AutomationDbContext context,
        SharedKernel.IIdGenerator idGenerator)
    {
        _logger = logger;
        _hubContext = hubContext;
        _textGen = textGen;
        _context = context;
        _idGenerator = idGenerator;
    }

    public async Task<NoteGenerationResult> GenerateNoteAsync(string lectureId)
    {
        _logger.LogInformation("Starting note generation job for lecture {LectureId}", lectureId);

        try
        {
            // Check if note already generated (idempotency)
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture == null)
            {
                throw new Exception($"Lecture {lectureId} not found");
            }

            if (!string.IsNullOrEmpty(lecture.GeneratedNoteId))
            {
                _logger.LogInformation(
                    "Note already generated for lecture {LectureId}, note ID: {NoteId}. Returning existing.",
                    lectureId, lecture.GeneratedNoteId);
                
                return new NoteGenerationResult
                {
                    Success = true,
                    NoteId = lecture.GeneratedNoteId
                };
            }

            if (string.IsNullOrWhiteSpace(lecture.TranscriptionText))
            {
                throw new Exception($"Lecture {lectureId} has no transcription");
            }

            lecture.Status = LectureStatus.GeneratingNotes;
            await _context.SaveChangesAsync();

            // Notify progress
            await _hubContext.Clients.All.JobProgress(lectureId, "GenerateLectureNote", 0, "Starting note generation...");

            var transcript = lecture.TranscriptionText;
            var summary = lecture.SummaryText ?? "";

            // Generate Key Points section
            await _hubContext.Clients.All.JobProgress(lectureId, "GenerateLectureNote", 20, "Generating key points...");
            var keyPointsPrompt = $"Extract 5-7 key points from this lecture in bullet format:\n\n{transcript}";
            var keyPoints = await _textGen.GenerateAsync(keyPointsPrompt, 300, 0.5);

            // Generate Definitions section
            await _hubContext.Clients.All.JobProgress(lectureId, "GenerateLectureNote", 40, "Extracting definitions...");
            var definitionsPrompt = $"List important terms and their definitions from this lecture:\n\n{transcript}";
            var definitions = await _textGen.GenerateAsync(definitionsPrompt, 400, 0.5);

            // Generate Likely Test Questions
            await _hubContext.Clients.All.JobProgress(lectureId, "GenerateLectureNote", 60, "Creating test questions...");
            var questionsPrompt = $"Generate 5 potential test questions based on this lecture:\n\n{transcript}";
            var questions = await _textGen.GenerateAsync(questionsPrompt, 300, 0.5);

            // Generate References
            await _hubContext.Clients.All.JobProgress(lectureId, "GenerateLectureNote", 80, "Compiling references...");
            var referencesPrompt = $"Suggest reading materials or topics for further study based on this lecture:\n\n{summary}";
            var references = await _textGen.GenerateAsync(referencesPrompt, 200, 0.5);

            await _hubContext.Clients.All.JobProgress(lectureId, "GenerateLectureNote", 90, "Creating note...");

            // Create structured note
            var noteId = _idGenerator.NewId().ToString();
            var courseTitle = lecture.Course?.Name ?? "Unknown Course";
            
            var noteContent = $@"# {lecture.Title}

**Course:** {courseTitle}
**Date:** {lecture.RecordedAt:yyyy-MM-dd}

## Summary

{summary}

## Key Points

{keyPoints}

## Definitions

{definitions}

## Likely Test Questions

{questions}

## References & Further Study

{references}

---
*Auto-generated from lecture recording on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*
";

            var note = new Note
            {
                Id = noteId,
                Title = $"📚 {lecture.Title}",
                Content = noteContent,
                Tags = new List<string> { "lecture", "auto-generated", courseTitle.ToLower().Replace(" ", "-") },
                Color = "#4F46E5", // Indigo for lecture notes
                IsPinned = false,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _context.Notes.Add(note);
            
            lecture.GeneratedNoteId = noteId;
            lecture.Status = LectureStatus.Completed;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Note generation completed successfully for lecture {LectureId}, note ID: {NoteId}",
                lectureId, noteId);

            // Send SignalR event
            await _hubContext.Clients.All.LectureNoteReady(lectureId, noteId, "Note generated successfully");

            // Notify completion
            await _hubContext.Clients.All.JobCompleted(
                lectureId,
                "GenerateLectureNote",
                true,
                "Note generation completed successfully",
                new { NoteId = noteId });

            return new NoteGenerationResult
            {
                Success = true,
                NoteId = noteId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating note for lecture {LectureId}", lectureId);

            // Update lecture status to failed
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture != null)
            {
                lecture.Status = LectureStatus.Failed;
                await _context.SaveChangesAsync();
            }

            await _hubContext.Clients.All.JobCompleted(
                lectureId,
                "GenerateLectureNote",
                false,
                $"Note generation failed: {ex.Message}",
                null);

            return new NoteGenerationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
