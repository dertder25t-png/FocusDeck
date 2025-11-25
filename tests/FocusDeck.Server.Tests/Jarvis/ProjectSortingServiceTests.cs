using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Jarvis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FocusDeck.Server.Tests.Jarvis
{
    public class ProjectSortingServiceTests
    {
        [Fact]
        public async Task SortItemAsync_AutoMode_LinksProject()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var db = new AutomationDbContext(options);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectSortingService>();
            var service = new ProjectSortingService(db, logger);

            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var project = new Project
            {
                Id = projectId,
                Title = "FocusDeck Development",
                SortingMode = ProjectSortingMode.Auto,
                TenantId = tenantId
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            var item = new CapturedItem
            {
                Id = Guid.NewGuid(),
                Title = "How to build a SaaS",
                Content = "This article discusses FocusDeck architecture.",
                TenantId = tenantId
            };

            // Act
            await service.SortItemAsync(item, CancellationToken.None);

            // Assert
            Assert.Equal(projectId, item.ProjectId);
        }

        [Fact]
        public async Task SortItemAsync_ReviewMode_SuggestsProject()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var db = new AutomationDbContext(options);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectSortingService>();
            var service = new ProjectSortingService(db, logger);

            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var project = new Project
            {
                Id = projectId,
                Title = "Marketing Campaign",
                SortingMode = ProjectSortingMode.Review,
                TenantId = tenantId
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            var item = new CapturedItem
            {
                Id = Guid.NewGuid(),
                Title = "SEO Strategies",
                Content = "Marketing tips for 2025.",
                TenantId = tenantId
            };

            // Act
            await service.SortItemAsync(item, CancellationToken.None);

            // Assert
            Assert.Null(item.ProjectId);
            Assert.Equal(projectId, item.SuggestedProjectId);
            Assert.NotNull(item.SuggestionReason);
        }
    }
}
