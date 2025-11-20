using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis
{
    public class AutomationGeneratorService : IAutomationGeneratorService
    {
        private readonly HttpClient _httpClient;
        private readonly AutomationDbContext _dbContext;
        private readonly ILogger<AutomationGeneratorService> _logger;

        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

        public AutomationGeneratorService(
            HttpClient httpClient,
            AutomationDbContext dbContext,
            ILogger<AutomationGeneratorService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task GenerateProposalAsync(List<ContextSnapshot> cluster)
        {
            if (cluster == null || cluster.Count < 2)
            {
                _logger.LogWarning("Not enough data points to generate an automation proposal.");
                return;
            }

            // 1. Get API Key
            var apiKey = await GetApiKeyAsync();
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Gemini API Key not configured. Skipping automation generation.");
                return;
            }

            // 2. Construct Prompt
            var prompt = ConstructPrompt(cluster);

            // 3. Call Gemini API
            var yamlResponse = await CallGeminiAsync(apiKey, prompt);
            if (string.IsNullOrEmpty(yamlResponse))
            {
                _logger.LogError("Failed to generate automation YAML from Gemini.");
                return;
            }

            // 4. Parse and Save Proposal
            await SaveProposalAsync(cluster.First(), yamlResponse);
        }

        private async Task<string?> GetApiKeyAsync()
        {
            var config = await _dbContext.ServiceConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ServiceName == "Gemini");
            return config?.ApiKey;
        }

        private string ConstructPrompt(List<ContextSnapshot> cluster)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Here is a cluster of recurring user behaviors (context snapshots). Detect the pattern and write a FocusDeck YAML automation for it.");
            sb.AppendLine("Format: YAML only. No markdown code blocks.");
            sb.AppendLine("Structure: Title, Description, Trigger (App Open, Time, etc), Actions.");
            sb.AppendLine("Examples of habits: Open VS Code -> Play Lo-Fi Music. Open Slack -> Set Status to 'Busy'.");
            sb.AppendLine();
            sb.AppendLine("User Context Data:");

            foreach (var snapshot in cluster.Take(5)) // Limit to 5 to save tokens
            {
                sb.AppendLine($"- Timestamp: {snapshot.Timestamp:O}");
                if (snapshot.Metadata != null)
                {
                    sb.AppendLine($"  Device: {snapshot.Metadata.DeviceName}");
                }
                foreach (var slice in snapshot.Slices)
                {
                    sb.AppendLine($"  Source: {slice.SourceType}, Data: {slice.Data}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("Return ONLY the YAML definition.");
            return sb.ToString();
        }

        private async Task<string?> CallGeminiAsync(string apiKey, string prompt)
        {
            var requestUrl = $"{BaseUrl}?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(requestUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API Error: {StatusCode} - {Error}", response.StatusCode, error);
                    return null;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                // Response structure: candidates[0].content.parts[0].text
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                        contentElem.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        // Strip markdown if present
                        if (text != null)
                        {
                            text = text.Replace("```yaml", "").Replace("```", "").Trim();
                        }
                        return text;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception calling Gemini API");
                return null;
            }
        }

        private async Task SaveProposalAsync(ContextSnapshot representative, string yaml)
        {
            // Parse title/desc from yaml simply for now
            var title = "New Automation Proposal";
            var description = "Auto-generated based on your habits.";

            // Simple parsing heuristics
            var lines = yaml.Split('\n');
            var titleLine = lines.FirstOrDefault(l => l.Trim().StartsWith("Title:"));
            if (titleLine != null) title = titleLine.Split(':', 2)[1].Trim();

            var descLine = lines.FirstOrDefault(l => l.Trim().StartsWith("Description:"));
            if (descLine != null) description = descLine.Split(':', 2)[1].Trim();

            var proposal = new AutomationProposal
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                YamlDefinition = yaml,
                Status = ProposalStatus.Pending,
                ConfidenceScore = 0.85f, // Placeholder/Mock score
                CreatedAt = DateTime.UtcNow,
                TenantId = Guid.Empty, // Should infer from context/user
                UserId = representative.UserId.ToString()
            };

            _dbContext.AutomationProposals.Add(proposal);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created Automation Proposal {Id}: {Title}", proposal.Id, proposal.Title);
        }
    }
}
