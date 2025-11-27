using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
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
        private readonly ILogger<ProjectSortingService> _logger;

        public ProjectSortingService(AutomationDbContext db, ILogger<ProjectSortingService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SortItemAsync(CapturedItem item, CancellationToken cancellationToken)
        {
            // 1. Fetch candidate projects
            var projects = await _db.Projects
                .Where(p => p.TenantId == item.TenantId)
                .ToListAsync(cancellationToken);

            if (!projects.Any())
            {
                return;
            }

            // 2. Score projects
            var bestMatch = CalculateBestMatch(item, projects);

            if (bestMatch.Project == null || bestMatch.Score < 0.5) // Minimum threshold
            {
                return;
            }

            // 3. Apply action based on SortingMode
            var mode = bestMatch.Project.SortingMode;

            switch (mode)
            {
                case ProjectSortingMode.Auto:
                    item.ProjectId = bestMatch.Project.Id;
                    _logger.LogInformation("Auto-sorted item {ItemId} to project {ProjectId} ({Title}) with score {Score}",
                        item.Id, bestMatch.Project.Id, bestMatch.Project.Title, bestMatch.Score);
                    break;

                case ProjectSortingMode.Review:
                    item.SuggestedProjectId = bestMatch.Project.Id;
                    item.SuggestionConfidence = bestMatch.Score;
                    item.SuggestionReason = $"Matched keywords in project title/description. Score: {bestMatch.Score:F2}";
                    _logger.LogInformation("Suggested item {ItemId} for project {ProjectId} ({Title})",
                        item.Id, bestMatch.Project.Id, bestMatch.Project.Title);
                    break;

                case ProjectSortingMode.Off:
                default:
                    // Do nothing
                    break;
            }
        }

        private (Project? Project, double Score) CalculateBestMatch(CapturedItem item, List<Project> projects)
        {
            Project? bestProject = null;
            double bestScore = 0;

            foreach (var project in projects)
            {
                double score = 0;

                // Simple keyword matching for MVP
                // In production, use Vector Search (embedding similarity)

                var textToMatch = (item.Title + " " + item.Content + " " + item.Summary + " " + item.TagsJson).ToLowerInvariant();
                var projectKeywords = (project.Title + " " + project.Description + " " + project.RepoSlug).ToLowerInvariant().Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

                int matchCount = 0;
                foreach (var keyword in projectKeywords)
                {
                    if (keyword.Length > 3 && textToMatch.Contains(keyword))
                    {
                        matchCount++;
                    }
                }

                // Normalize score roughly
                if (matchCount > 0)
                {
                    // Bump base score to ensure single strong keyword match passes threshold
                    score = Math.Min(0.9, 0.4 + (matchCount * 0.2));
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestProject = project;
                }
            }

            return (bestProject, bestScore);
        }
    }
}
