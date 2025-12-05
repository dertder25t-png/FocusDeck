using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.TextGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis
{
    public interface IProjectSortingService
    {
        Task SortItemAsync(CapturedItem item, CancellationToken cancellationToken);
    }

    public class ProjectSortingService : IProjectSortingService
    {
        private readonly AutomationDbContext _db;
        private readonly ITextGen _textGen;
        private readonly ILogger<ProjectSortingService> _logger;

        public ProjectSortingService(AutomationDbContext db, ITextGen textGen, ILogger<ProjectSortingService> logger)
        {
            _db = db;
            _textGen = textGen;
            _logger = logger;
        }

        public async Task SortItemAsync(CapturedItem item, CancellationToken cancellationToken)
        {
            // 1. Fetch candidate projects
            var projects = await _db.Projects
                .Where(p => p.TenantId == item.TenantId && p.SortingMode != ProjectSortingMode.Off)
                .Select(p => new { p.Id, p.Title, p.Description, p.SortingMode })
                .ToListAsync(cancellationToken);

            if (!projects.Any())
            {
                return;
            }

            // 2. Use LLM to find best match
            var bestMatch = await DetermineBestMatchAsync(item, projects, cancellationToken);

            if (bestMatch.ProjectId == null || bestMatch.Confidence < 0.5)
            {
                return;
            }

            // 3. Apply action
            var project = projects.First(p => p.Id == bestMatch.ProjectId);

            if (project.SortingMode == ProjectSortingMode.Auto)
            {
                item.ProjectId = project.Id;
                _logger.LogInformation("Auto-sorted item {ItemId} to project {ProjectId} ({Title}) with confidence {Confidence}",
                    item.Id, project.Id, project.Title, bestMatch.Confidence);
            }
            else if (project.SortingMode == ProjectSortingMode.Review)
            {
                item.SuggestedProjectId = project.Id;
                item.SuggestionConfidence = bestMatch.Confidence;
                item.SuggestionReason = bestMatch.Reason;
                _logger.LogInformation("Suggested item {ItemId} for project {ProjectId} ({Title})",
                    item.Id, project.Id, project.Title);
            }
        }

        private async Task<(Guid? ProjectId, double Confidence, string Reason)> DetermineBestMatchAsync(
            CapturedItem item,
            IEnumerable<dynamic> projects,
            CancellationToken cancellationToken)
        {
            var projectList = string.Join("\n", projects.Select(p => $"- ID: {p.Id}, Title: {p.Title}, Description: {p.Description}"));

            var prompt = $@"
You are an intelligent organization assistant. Your task is to match a captured web item to the most relevant project from a list.

Captured Item:
- Title: {item.Title}
- URL: {item.Url}
- Summary: {item.Summary}
- Content Snippet: {(item.Content?.Length > 200 ? item.Content.Substring(0, 200) : item.Content)}

Available Projects:
{projectList}

Analyze the item and projects. Determine which project, if any, is a strong semantic match for this item.
Return a JSON object with the following properties:
- ""projectId"": The ID of the matching project, or null if no strong match found.
- ""confidence"": A number between 0.0 and 1.0 indicating your certainty.
- ""reason"": A brief explanation of why this project was chosen.

JSON Response:
";

            try
            {
                var response = await _textGen.GenerateAsync(prompt, maxTokens: 300, temperature: 0.1, cancellationToken: cancellationToken);

                // Extract JSON from response (handle potential markdown blocks)
                var json = ExtractJson(response);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                Guid? projectId = null;
                if (root.TryGetProperty("projectId", out var pidProp) && pidProp.ValueKind == JsonValueKind.String)
                {
                    if (Guid.TryParse(pidProp.GetString(), out var parsedId))
                    {
                        projectId = parsedId;
                    }
                }

                double confidence = 0;
                if (root.TryGetProperty("confidence", out var confProp))
                {
                    confidence = confProp.GetDouble();
                }

                string reason = "";
                if (root.TryGetProperty("reason", out var reasonProp))
                {
                    reason = reasonProp.GetString() ?? "";
                }

                return (projectId, confidence, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LLM project sorting");
                return (null, 0, "Error during analysis");
            }
        }

        private string ExtractJson(string text)
        {
            var start = text.IndexOf("{");
            var end = text.LastIndexOf("}");
            if (start >= 0 && end > start)
            {
                return text.Substring(start, end - start + 1);
            }
            return text;
        }
    }
}
