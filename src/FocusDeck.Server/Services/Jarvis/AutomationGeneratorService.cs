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
using FocusDeck.Server.Services.TextGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis
{
    public class AutomationGeneratorService : IAutomationGeneratorService
    {
        private readonly ITextGen _textGenService;
        private readonly AutomationDbContext _dbContext;
        private readonly ILogger<AutomationGeneratorService> _logger;

        public AutomationGeneratorService(
            ITextGen textGenService,
            AutomationDbContext dbContext,
            ILogger<AutomationGeneratorService> logger)
        {
            _textGenService = textGenService;
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

            // 1. Construct Prompt
            var prompt = ConstructPrompt(cluster);

            // 2. Call Generation Service
            var yamlResponse = await _textGenService.GenerateAsync(prompt, maxTokens: 1000);

            // Clean up Markdown if present
            if (!string.IsNullOrEmpty(yamlResponse))
            {
                yamlResponse = CleanYaml(yamlResponse);
            }

            if (string.IsNullOrEmpty(yamlResponse) || yamlResponse.StartsWith("[Stub Response]"))
            {
                _logger.LogWarning("Failed to generate automation YAML (or stub returned).");
                return;
            }

            // 3. Parse and Save Proposal
            await SaveProposalAsync(cluster.First().UserId.ToString(), yamlResponse);
        }

        public async Task<AutomationProposal> GenerateProposalFromIntentAsync(string intent, string userId)
        {
            // 1. Construct Prompt
            var prompt = $"User Intent: {intent}\n\n" +
                         "Write a FocusDeck YAML automation that achieves this intent.\n" +
                         "Format: YAML only. No markdown code blocks.\n" +
                         "Structure: Title, Description, Trigger (default to Manual/AppOpen if unclear), Actions.\n" +
                         "Available Action Types: email.Send, storage.SaveFile, github.OpenBrowser, spotify.Play, focusdeck.ShowNotification, focusdeck.StartTimer.\n" +
                         "Return ONLY the YAML definition.";

            // 2. Call Generation Service
            var yamlResponse = await _textGenService.GenerateAsync(prompt, maxTokens: 1000);

            // Clean up Markdown if present
            if (!string.IsNullOrEmpty(yamlResponse))
            {
                yamlResponse = CleanYaml(yamlResponse);
            }

            if (string.IsNullOrEmpty(yamlResponse) || yamlResponse.StartsWith("[Stub Response]"))
            {
                throw new InvalidOperationException("Failed to generate automation YAML (or stub returned).");
            }

            // 3. Parse and Save Proposal
            return await SaveProposalAsync(userId, yamlResponse);
        }

        private string CleanYaml(string input)
        {
            return input.Replace("```yaml", "").Replace("```", "").Trim();
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
