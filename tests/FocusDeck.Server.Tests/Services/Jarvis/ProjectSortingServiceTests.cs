using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Jarvis;
using FocusDeck.Server.Services.TextGeneration;
using FocusDeck.Server.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FocusDeck.Server.Tests.Services.Jarvis
{
    public class ProjectSortingServiceTests
    {
        private readonly Mock<ITextGen> _textGenMock;
        private readonly Mock<ILogger<ProjectSortingService>> _loggerMock;

        public ProjectSortingServiceTests()
        {
            _textGenMock = new Mock<ITextGen>();
            _loggerMock = new Mock<ILogger<ProjectSortingService>>();
        }

        [Fact]
        public async Task SortItemAsync_MatchesProject_AutoMode_UpdatesProjectId()
        {
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            
            // Create a mock tenant that matches the project's tenant
            var mockTenant = new MockCurrentTenant(tenantId);

            // Setup InMemory DB with proper tenant context
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var dbContext = new AutomationDbContext(options, mockTenant);

            var service = new ProjectSortingService(dbContext, _textGenMock.Object, _loggerMock.Object);

            var project = new Project
            {
                Id = projectId,
                TenantId = tenantId,
                Title = "Coding Project",
                Description = "A project about coding",
                SortingMode = ProjectSortingMode.Auto
            };
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();

            var item = new CapturedItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "How to write C#",
                Url = "https://example.com/csharp"
            };

            // Mock LLM response
            var jsonResponse = $@"
            {{
                ""projectId"": ""{projectId}"",
                ""confidence"": 0.95,
                ""reason"": ""Strong match on coding topic.""
            }}";

            _textGenMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jsonResponse);

            // Act
            await service.SortItemAsync(item, CancellationToken.None);

            // Assert
            Assert.Equal(projectId, item.ProjectId);
        }

        [Fact]
        public async Task SortItemAsync_MatchesProject_ReviewMode_UpdatesSuggestedProjectId()
        {
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            // Create a mock tenant that matches the project's tenant
            var mockTenant = new MockCurrentTenant(tenantId);

            // Setup InMemory DB with proper tenant context
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var dbContext = new AutomationDbContext(options, mockTenant);

            var service = new ProjectSortingService(dbContext, _textGenMock.Object, _loggerMock.Object);

            var project = new Project
            {
                Id = projectId,
                TenantId = tenantId,
                Title = "Research",
                Description = "Research papers",
                SortingMode = ProjectSortingMode.Review
            };
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();

            var item = new CapturedItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "New Paper",
            };

            var jsonResponse = $@"
            {{
                ""projectId"": ""{projectId}"",
                ""confidence"": 0.85,
                ""reason"": ""Good match.""
            }}";

            _textGenMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jsonResponse);

            await service.SortItemAsync(item, CancellationToken.None);

            Assert.Null(item.ProjectId);
            Assert.Equal(projectId, item.SuggestedProjectId);
            Assert.Equal(0.85, item.SuggestionConfidence);
            Assert.Equal("Good match.", item.SuggestionReason);
        }

        [Fact]
        public async Task SortItemAsync_LowConfidence_DoesNothing()
        {
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            // Create a mock tenant that matches the project's tenant
            var mockTenant = new MockCurrentTenant(tenantId);

            // Setup InMemory DB with proper tenant context
            var options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var dbContext = new AutomationDbContext(options, mockTenant);

            var service = new ProjectSortingService(dbContext, _textGenMock.Object, _loggerMock.Object);

            var project = new Project
            {
                Id = projectId,
                TenantId = tenantId,
                Title = "Research",
                SortingMode = ProjectSortingMode.Auto
            };
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();

            var item = new CapturedItem { Id = Guid.NewGuid(), TenantId = tenantId, Title = "Random" };

            var jsonResponse = $@"
            {{
                ""projectId"": ""{projectId}"",
                ""confidence"": 0.4,
                ""reason"": ""Weak match.""
            }}";

            _textGenMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jsonResponse);

            await service.SortItemAsync(item, CancellationToken.None);

            Assert.Null(item.ProjectId);
            Assert.Null(item.SuggestedProjectId);
        }
    }
}
