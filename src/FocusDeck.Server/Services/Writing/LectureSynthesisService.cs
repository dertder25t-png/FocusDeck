using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Calendar;
using FocusDeck.Server.Services.TextGeneration;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Writing;

public class LectureSynthesisService
{
    private readonly AutomationDbContext _dbContext;
    private readonly CalendarResolver _calendarResolver;
    private readonly ITextGen _textGen;
    private readonly ILogger<LectureSynthesisService> _logger;

    public LectureSynthesisService(
        AutomationDbContext dbContext,
        CalendarResolver calendarResolver,
        ITextGen textGen,
        ILogger<LectureSynthesisService> logger)
    {
        _dbContext = dbContext;
        _calendarResolver = calendarResolver;
        _textGen = textGen;
        _logger = logger;
    }

    public async Task<Note> SynthesizeNoteAsync(
        string transcript,
        Guid? courseId,
        Guid? eventId,
        DateTime timestamp,
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve Context if missing
        if (courseId == null || eventId == null)
        {
            var (evt, course) = await _calendarResolver.ResolveCurrentContextAsync(tenantId);
            if (evt != null)
            {
                if (eventId == null) eventId = evt.Id;
                if (courseId == null && course != null) courseId = course.Id;
            }
        }

        // 2. Synthesize Content via LLM
        var prompt = ConstructSynthesisPrompt(transcript);
        var response = await _textGen.GenerateAsync(prompt, maxTokens: 2000, cancellationToken: cancellationToken);

        var synthesisResult = ParseSynthesisResponse(response);

        // 3. Create Note
        var note = new Note
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            Title = synthesisResult.Title ?? $"Lecture Note - {timestamp:g}",
            Content = synthesisResult.Content,
            Tags = synthesisResult.Tags,
            CreatedDate = DateTime.UtcNow,
            CourseId = courseId,
            EventId = eventId,
            Type = NoteType.QuickNote
        };

        // 4. Save
        _dbContext.Notes.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Synthesized Note {NoteId} for User {UserId}", note.Id, userId);

        return note;
    }

    private string ConstructSynthesisPrompt(string transcript)
    {
        return $@"You are an expert academic scribe. Analyze the following lecture transcript and synthesize a structured note.

TRANSCRIPT:
{transcript}

INSTRUCTIONS:
1. Create a clear, concise title.
2. Structure the content with Markdown (bullet points, headers).
3. Identify ""Key Terms"" mentioned in the lecture.
4. Extract any ""Homework"" or ""Action Items"" mentioned.
5. Return the result as a JSON object with keys: ""title"", ""content"" (markdown string), ""tags"" (array of strings), ""homework"" (array of strings, optional).

JSON Format Example:
{{
  ""title"": ""Introduction to Calculus"",
  ""content"": ""# Key Concepts\n- Limits are..."",
  ""tags"": [""Calculus"", ""Limits"", ""Derivatives""],
  ""homework"": [""Read Chapter 1"", ""Solve problem set 3""]
}}

Return ONLY the JSON.";
    }

    private SynthesisResult ParseSynthesisResponse(string response)
    {
        var result = new SynthesisResult
        {
            Content = response,
            Tags = new List<string>()
        };

        try
        {
            // Clean up Markdown code blocks if present
            var json = response.Replace("```json", "").Replace("```", "").Trim();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("title", out var titleProp))
            {
                result.Title = titleProp.GetString();
            }

            if (root.TryGetProperty("content", out var contentProp))
            {
                result.Content = contentProp.GetString() ?? "";
            }

            if (root.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in tagsProp.EnumerateArray())
                {
                    var t = tag.GetString();
                    if (!string.IsNullOrEmpty(t)) result.Tags.Add(t);
                }
            }

            if (root.TryGetProperty("homework", out var hwProp) && hwProp.ValueKind == JsonValueKind.Array)
            {
                var homeworkList = new List<string>();
                foreach (var item in hwProp.EnumerateArray())
                {
                    var s = item.GetString();
                    if (!string.IsNullOrEmpty(s)) homeworkList.Add(s);
                }

                if (homeworkList.Any())
                {
                    result.Content += "\n\n## Homework\n" + string.Join("\n", homeworkList.Select(h => $"- [ ] {h}"));
                }
            }
        }
        catch (JsonException)
        {
            _logger.LogWarning("Failed to parse JSON from synthesis response. Falling back to raw text.");
            // Fallback: Use raw response as content
            result.Content = response;
            result.Title = "Synthesized Lecture Note";
        }

        return result;
    }

    private class SynthesisResult
    {
        public string? Title { get; set; }
        public string Content { get; set; } = "";
        public List<string> Tags { get; set; } = new();
    }
}
