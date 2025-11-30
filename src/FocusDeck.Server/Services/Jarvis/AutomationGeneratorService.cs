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
            await SaveProposalAsync(cluster.First().UserId.ToString(), yamlResponse);
        }

        public async Task<AutomationProposal> GenerateProposalFromIntentAsync(string intent, string userId)
        {
             // 1. Get API Key
            var apiKey = await GetApiKeyAsync();
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Gemini API Key not configured. Skipping automation generation.");
                throw new InvalidOperationException("Gemini API Key not configured.");
            }

            // 2. Construct Prompt
            var prompt = $"User Intent: {intent}\n\n" +
                         "Write a FocusDeck YAML automation that achieves this intent.\n" +
                         "Format: YAML only. No markdown code blocks.\n" +
                         "Structure: Title, Description, Trigger (default to Manual/AppOpen if unclear), Actions.\n" +
                         "Available Action Types: email.Send, storage.SaveFile, github.OpenBrowser, spotify.Play, focusdeck.ShowNotification, focusdeck.StartTimer.\n" +
                         "Return ONLY the YAML definition.";

            // 3. Call Gemini API
            var yamlResponse = await CallGeminiAsync(apiKey, prompt);
            if (string.IsNullOrEmpty(yamlResponse))
            {
                throw new InvalidOperationException("Failed to generate automation YAML from Gemini.");
            }

            // 4. Parse and Save Proposal
            return await SaveProposalAsync(userId, yamlResponse);
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
            sb.AppendLine("Analyze the following user behavior patterns and generate a useful automation.");
            sb.AppendLine("STRATEGY: Identify the 'initiating event' (Trigger) and the 'subsequent response' (Action).");
            sb.AppendLine("Example: If 'VS Code' and 'Spotify' appear together, the Trigger is 'App Open: VS Code' and Action is 'Spotify: Play'.");
            sb.AppendLine("CRITICAL RULES:");
            sb.AppendLine("1. DO NOT create automations that simply repeat the user's action (e.g. If User opens Chrome -> Open Chrome).");
            sb.AppendLine("2. Look for complementary actions (e.g. If User opens IDE -> Turn on DND, Start Focus Timer, Play Music).");
            sb.AppendLine("3. If the pattern implies a 'work mode', suggest actions like blocking distractions or setting status.");
            sb.AppendLine("4. Format: YAML only. No markdown code blocks.");
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

        private async Task<AutomationProposal> SaveProposalAsync(string userId, string yaml)
        {
            // Parse title/desc from yaml simply for now
            var title = "New Automation Proposal";
            var description = "Auto-generated.";

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
                UserId = userId
            };

            _dbContext.AutomationProposals.Add(proposal);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created Automation Proposal {Id}: {Title}", proposal.Id, proposal.Title);
            return proposal;
        }
    }
}
