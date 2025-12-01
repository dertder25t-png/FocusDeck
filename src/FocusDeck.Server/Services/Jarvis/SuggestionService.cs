using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Server.Services.TextGeneration;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis;

/// <summary>
/// A service that generates suggestions for the user based on their current context.
/// This initial version implements a simple rule-based MVP and connects to LLM for note analysis.
/// </summary>
public class SuggestionService : ISuggestionService
{
    private readonly ILogger<SuggestionService> _logger;
    private readonly ITextGen _textGen;

    public SuggestionService(ILogger<SuggestionService> logger, ITextGen textGen)
    {
        _logger = logger;
        _textGen = textGen;
    }

    /// <summary>
    /// Generates a suggestion based on a simple, rule-based logic.
    /// </summary>
    /// <param name="request">The request containing the user's current context.</param>
    /// <returns>A suggestion if a rule is matched; otherwise, null.</returns>
    public Task<SuggestionResponseDto?> GenerateSuggestionAsync(SuggestionRequestDto request)
    {
        _logger.LogInformation("Generating suggestion for context: {Context}", request.CurrentContext);

        // STEP 1: Implement a simple rule-based MVP.
        // The goal is to check the input context against a set of predefined rules.
        // For example, if the context indicates the user is in a lecture, suggest starting a note.

        if (request.CurrentContext.Contains("lecture", StringComparison.OrdinalIgnoreCase))
        {
            var suggestion = new SuggestionResponseDto(
                Action: "start_note",
                Parameters: new Dictionary<string, object> { { "course", "Lecture" } },
                Confidence: 0.8,
                Evidence: Array.Empty<Guid>()
            );
            return Task.FromResult<SuggestionResponseDto?>(suggestion);
        }

        _logger.LogInformation("No suggestion generated for the given context.");
        return Task.FromResult<SuggestionResponseDto?>(null);
    }

    /// <summary>
    /// Analyzes a note and generates AI-powered suggestions for improvements or additions.
    /// </summary>
    /// <param name="note">The note to analyze.</param>
    /// <returns>A list of generated suggestions.</returns>
    public async Task<List<NoteSuggestion>> AnalyzeNoteAsync(Note note)
    {
        _logger.LogInformation("Analyzing note {NoteId} for suggestions", note.Id);

        if (string.IsNullOrWhiteSpace(note.Content))
        {
            return new List<NoteSuggestion>();
        }

        // Construct prompt for LLM
        var prompt = $@"
You are a helpful teaching assistant. Analyze the following student note and provide 3 suggestions for improvement.
Focus on missing key concepts, definitions that could be expanded, or relevant references.

Note Title: {note.Title}
Note Content:
{note.Content}

Provide the output as a list of suggestions. For each suggestion, specify the type (MissingPoint, Definition, Reference) and the content.
Format: [Type] Content
Example: [Definition] The term 'polymorphism' refers to...
";

        try
        {
            var response = await _textGen.GenerateAsync(prompt, maxTokens: 800, temperature: 0.7);
            return ParseSuggestions(response, note.Id, note.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate suggestions via LLM");
            // Fallback or empty
            return new List<NoteSuggestion>();
        }
    }

    private List<NoteSuggestion> ParseSuggestions(string llmOutput, string noteId, Guid tenantId)
    {
        var suggestions = new List<NoteSuggestion>();
        var lines = llmOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var regex = new System.Text.RegularExpressions.Regex(
            @"^\s*(\[|\*\*\[?)?(?<type>Missing\s?Point|Definition|Reference|Clarification)(\]|\*\*\]?)?\s*(?<content>.*)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var match = regex.Match(trimmed);
            if (match.Success)
            {
                var typeStr = match.Groups["type"].Value.Replace(" ", ""); // Normalize "Missing Point" to "MissingPoint"
                var content = match.Groups["content"].Value.Trim();

                if (Enum.TryParse<NoteSuggestionType>(typeStr, true, out var type) && content.Length > 10)
                {
                    suggestions.Add(new NoteSuggestion
                    {
                        Id = Guid.NewGuid().ToString(),
                        NoteId = noteId,
                        Type = type,
                        ContentMarkdown = content,
                        Source = "Jarvis AI Analysis",
                        Confidence = 0.85,
                        CreatedAt = DateTime.UtcNow,
                        TenantId = tenantId
                    });
                }
            }
            else if (trimmed.Length > 20 && !trimmed.StartsWith("Note Title:") && !trimmed.StartsWith("Note Content:"))
            {
                // Fallback for lines that look like content but missed the type prefix
                suggestions.Add(new NoteSuggestion
                {
                    Id = Guid.NewGuid().ToString(),
                    NoteId = noteId,
                    Type = NoteSuggestionType.MissingPoint,
                    ContentMarkdown = trimmed,
                    Source = "Jarvis AI Analysis",
                    Confidence = 0.70, // Lower confidence for fallback
                    CreatedAt = DateTime.UtcNow,
                    TenantId = tenantId
                });
            }
        }

        // Limit to 3, but after loop to handle filtered results
        if (suggestions.Count > 3)
        {
            suggestions = suggestions.GetRange(0, 3);
        }

        return suggestions;
    }
}
