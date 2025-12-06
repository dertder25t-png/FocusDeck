using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Jarvis;
using FocusDeck.Server.Services.TextGeneration;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FocusDeck.Server.Tests.Jarvis
{
    /// <summary>
    /// Mock implementation of ITextGen for testing purposes
    /// </summary>
    internal class MockTextGen : ITextGen
    {
        private readonly Guid? _matchProjectId;
        private readonly double _confidence;
        private readonly string _reason;

        public MockTextGen(Guid? matchProjectId = null, double confidence = 0.9, string reason = "Test match")
        {
            _matchProjectId = matchProjectId;
            _confidence = confidence;
            _reason = reason;
        }

        public Task<string> GenerateAsync(string prompt, int maxTokens = 500, double temperature = 0.7, CancellationToken cancellationToken = default)
        {
            var response = _matchProjectId.HasValue
                ? $"{{\"projectId\": \"{_matchProjectId}\", \"confidence\": {_confidence}, \"reason\": \"{_reason}\"}}"
                : "{\"projectId\": null, \"confidence\": 0.0, \"reason\": \"No match found\"}";
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Mock implementation of ICurrentTenant for testing purposes
    /// </summary>
    internal class MockCurrentTenant : ICurrentTenant
    {
        private Guid? _tenantId;

        public MockCurrentTenant(Guid tenantId)
        {
            _tenantId = tenantId;
        }

        public Guid? TenantId => _tenantId;
        public bool HasTenant => _tenantId.HasValue;

        public void SetTenant(Guid tenantId)
        {
            _tenantId = tenantId;
        }
    }

    public class ProjectSortingServiceTests
    {
        [Fact]
        public async Task SortItemAsync_AutoMode_LinksProject()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            
            // Create a mock tenant that matches the project's tenant
            var mockTenant = new MockCurrentTenant(tenantId);
            
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var db = new AutomationDbContext(options, mockTenant);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectSortingService>();
            
            // Create mock that will return the project ID with high confidence
            var mockTextGen = new MockTextGen(projectId, 0.9, "FocusDeck mentioned in content");
            var service = new ProjectSortingService(db, mockTextGen, logger);
            
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
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            
            // Create a mock tenant that matches the project's tenant
            var mockTenant = new MockCurrentTenant(tenantId);
            
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var db = new AutomationDbContext(options, mockTenant);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectSortingService>();
            
            // Create mock that will return the project ID with high confidence
            var mockTextGen = new MockTextGen(projectId, 0.85, "Marketing content matches campaign");
            var service = new ProjectSortingService(db, mockTextGen, logger);

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
