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

namespace FocusDeck.Server.Services.Context
{
    public class KnowledgeVaultService
    {
        private readonly AutomationDbContext _db;
        private readonly ITextGen _textGen;
        private readonly ILogger<KnowledgeVaultService> _logger;

        public KnowledgeVaultService(
            AutomationDbContext db,
            ITextGen textGen,
            ILogger<KnowledgeVaultService> logger)
        {
            _db = db;
            _textGen = textGen;
            _logger = logger;
        }

        public async Task SummarizePendingItemsAsync(CancellationToken cancellationToken = default)
        {
            // Find items without a summary and not empty content
            var pendingItems = await _db.CapturedItems
                .Where(c => string.IsNullOrEmpty(c.Summary) && !string.IsNullOrEmpty(c.Content))
                .OrderBy(c => c.CapturedAt)
                .Take(10) // Process in batches
                .ToListAsync(cancellationToken);

            if (!pendingItems.Any()) return;

            _logger.LogInformation("Processing {Count} items for Knowledge Vault summarization...", pendingItems.Count);

            foreach (var item in pendingItems)
            {
                try
                {
                    var prompt = ConstructPrompt(item);
                    var response = await _textGen.GenerateAsync(prompt, maxTokens: 500, cancellationToken: cancellationToken);

                    var result = ParseResponse(response);

                    item.Summary = result.Summary;
                    item.TagsJson = JsonSerializer.Serialize(result.Tags);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to summarize CapturedItem {Id}", item.Id);
                    // Mark as processed with error or skip?
                    // For now, we leave it null to retry later, or set a flag.
                    // Ideally, we should have a 'ProcessingStatus' field.
                    // To avoid infinite loops on failure, let's set a dummy summary if failed repeatedly.
                    // MVP: Just log.
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private string ConstructPrompt(CapturedItem item)
        {
            return $@"Analyze the following content captured from {item.Kind}:
Title: {item.Title}
URL: {item.Url}
Content:
{item.Content?.Substring(0, Math.Min(item.Content.Length, 2000))}...

INSTRUCTIONS:
1. Summarize the key technical concepts or takeaways in 2-3 sentences.
2. Extract relevant technical tags (e.g., 'C#', 'React', 'Architecture').
3. Return JSON: {{ ""summary"": ""..."", ""tags"": [""tag1"", ""tag2""] }}
";
        }

        private (string Summary, List<string> Tags) ParseResponse(string response)
        {
            try
            {
                var json = response.Replace("```json", "").Replace("```", "").Trim();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var summary = root.GetProperty("summary").GetString() ?? "No summary generated.";
                var tags = new List<string>();

                if (root.TryGetProperty("tags", out var tagsProp))
                {
                    foreach(var t in tagsProp.EnumerateArray())
                    {
                        tags.Add(t.GetString() ?? "");
                    }
                }

                return (summary, tags.Where(t => !string.IsNullOrEmpty(t)).ToList());
            }
            catch
            {
                return (response, new List<string>());
            }
        }
    }
}
