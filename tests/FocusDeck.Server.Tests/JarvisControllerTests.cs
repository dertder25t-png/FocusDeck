using System.Security.Claims;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Server.Controllers.v1;
using FocusDeck.Server.Hubs;
using FocusDeck.Server.Services.Jarvis;
using FocusDeck.Shared.SignalR.Notifications;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests;

public class JarvisControllerTests
{
    private static JarvisController CreateController(
        IJarvisWorkflowRegistry? registry = null,
        bool featureEnabled = true)
    {
        registry ??= new StubJarvisWorkflowRegistry();

        var configDict = new Dictionary<string, string?>
        {
            ["Features:Jarvis"] = featureEnabled ? "true" : "false"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        var controller = new JarvisController(
            registry,
            NullLogger<JarvisController>.Instance,
            configuration,
            new StubSuggestionService(),
            new StubFeedbackService(),
            new StubContextRetrievalService(),
            new AutomationDbContext(new DbContextOptionsBuilder<AutomationDbContext>().UseInMemoryDatabase("JarvisControllerTests").Options),
            new StubCurrentTenant(),
            new StubAutomationGeneratorService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "test-user")
                    }, authenticationType: "TestAuth"))
                }
            }
        };

        return controller;
    }

    [Fact]
    public void JarvisController_IsDecoratedWithAuthorize()
    {
        var attr = typeof(JarvisController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
    }

    [Fact]
    public async Task GetWorkflows_ReturnsNotFound_WhenFeatureDisabled()
    {
        var controller = CreateController(featureEnabled: false);

        var result = await controller.GetWorkflows(CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task GetWorkflows_ReturnsOk_WhenFeatureEnabled()
    {
        var registry = new StubJarvisWorkflowRegistry
        {
            Workflows = new[]
            {
                new JarvisWorkflowSummaryDto("wf-1", "Test Workflow", "Demo")
            }
        };
        var controller = CreateController(registry, featureEnabled: true);

        var result = await controller.GetWorkflows(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<JarvisWorkflowSummaryDto>>(ok.Value);
        Assert.Single(payload);
        Assert.Equal("wf-1", payload[0].Id);
    }

    [Fact]
    public async Task RunWorkflow_ReturnsBadRequest_WhenMissingWorkflowId()
    {
        var controller = CreateController(featureEnabled: true);
        var request = new JarvisRunRequestDto("", null);

        var result = await controller.RunWorkflow(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task RunWorkflow_ReturnsOk_WhenValidRequest()
    {
        var registry = new StubJarvisWorkflowRegistry();
        var controller = CreateController(registry, featureEnabled: true);
        var request = new JarvisRunRequestDto("wf-1", "{}");

        var result = await controller.RunWorkflow(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<JarvisRunResponseDto>(ok.Value);
        Assert.NotEqual(Guid.Empty, payload.RunId);
    }
}

public class JarvisWorkflowJobTests
{
    [Fact]
    public async Task ExecuteAsync_SendsJarvisRunUpdatedNotification()
    {
        var runId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AutomationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.JarvisWorkflowRuns.Add(new JarvisWorkflowRun
        {
            Id = runId,
            WorkflowId = "wf-test",
            Status = "Queued",
            RequestedAtUtc = DateTime.UtcNow,
            RequestedByUserId = "test-user",
            TenantId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        var hub = new FakeHubContext();
        var job = new JarvisWorkflowJob(db, hub, NullLogger<JarvisWorkflowJob>.Instance);

        await job.ExecuteAsync(runId, CancellationToken.None);

        Assert.NotNull(hub.Inner.FakeClientInstance.LastRun);
        var last = hub.Inner.FakeClientInstance.LastRun!;
        Assert.Equal(runId, last.RunId);
        Assert.Equal("wf-test", last.WorkflowId);
        Assert.True(last.Status is "Running" or "Succeeded" or "Failed");
    }
}

file sealed class FakeNotificationClient : INotificationClient
{
    public JarvisRunUpdate? LastRun { get; private set; }

    public Task SessionCreated(string sessionId, string message) => Task.CompletedTask;
    public Task SessionUpdated(string sessionId, string status, string message) => Task.CompletedTask;
    public Task SessionCompleted(string sessionId, int durationMinutes, string message) => Task.CompletedTask;
    public Task AutomationExecuted(string automationId, bool success, string message) => Task.CompletedTask;
    public Task JobCompleted(string jobId, string jobType, bool success, string message, object? result) => Task.CompletedTask;
    public Task JobProgress(string jobId, string jobType, int progressPercent, string message) => Task.CompletedTask;
    public Task NotificationReceived(string title, string message, string severity) => Task.CompletedTask;
    public Task LectureTranscribed(string lectureId, string transcriptionText, string message) => Task.CompletedTask;
    public Task LectureSummarized(string lectureId, string summaryText, string message) => Task.CompletedTask;
    public Task LectureNoteReady(string lectureId, string noteId, string message) => Task.CompletedTask;
    public Task RemoteActionCreated(string actionId, string kind, object payload) => Task.CompletedTask;
    public Task RemoteTelemetry(TelemetryUpdate payload) => Task.CompletedTask;
    public Task ForceLogout(ForceLogoutMessage payload) => Task.CompletedTask;
    public Task FocusDistraction(string reason, DateTime at) => Task.CompletedTask;
    public Task FocusRecoverySuggested(string suggestion) => Task.CompletedTask;
    public Task FocusStarted(string sessionId, string mode, int durationMinutes) => Task.CompletedTask;
    public Task FocusEnded(string sessionId, int actualMinutes, int distractionCount) => Task.CompletedTask;
    public Task DesignIdeasAdded(string projectId, int ideaCount, string message) => Task.CompletedTask;
    public Task NoteSuggestionReady(string noteId, string suggestionId, string type, string content) => Task.CompletedTask;
    public Task ContextUpdated(FocusDeck.Services.Activity.ActivityState state) => Task.CompletedTask;
    public Task JarvisRunUpdated(JarvisRunUpdate payload)
    {
        LastRun = payload;
        return Task.CompletedTask;
    }

    public Task ReceiveNotification(string title, string message, string severity) => Task.CompletedTask;
}

file sealed class FakeHubClients : IHubClients<INotificationClient>
{
    private readonly FakeNotificationClient _client = new();

    public INotificationClient All => _client;
    public INotificationClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => _client;
    public INotificationClient Client(string connectionId) => _client;
    public INotificationClient Clients(IReadOnlyList<string> connectionIds) => _client;
    public INotificationClient Group(string groupName) => _client;
    public INotificationClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _client;
    public INotificationClient Groups(IReadOnlyList<string> groupNames) => _client;
    public INotificationClient User(string userId) => _client;
    public INotificationClient Users(IReadOnlyList<string> userIds) => _client;

    public FakeNotificationClient FakeClientInstance => _client;
}

file sealed class FakeHubContext : IHubContext<NotificationsHub, INotificationClient>
{
    public FakeHubClients Inner { get; } = new();

    public IHubClients<INotificationClient> Clients => Inner;
    public IGroupManager Groups => throw new NotImplementedException();
}

file sealed class StubJarvisWorkflowRegistry : IJarvisWorkflowRegistry
{
    public IReadOnlyList<JarvisWorkflowSummaryDto> Workflows { get; set; } = Array.Empty<JarvisWorkflowSummaryDto>();

    public Task<IReadOnlyList<JarvisWorkflowSummaryDto>> ListWorkflowsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Workflows);

    public Task<JarvisRunResponseDto> EnqueueWorkflowAsync(JarvisRunRequestDto request, CancellationToken cancellationToken = default)
        => Task.FromResult(new JarvisRunResponseDto(Guid.NewGuid()));

    public Task<JarvisRunStatusDto?> GetRunStatusAsync(Guid runId, CancellationToken cancellationToken = default)
        => Task.FromResult<JarvisRunStatusDto?>(new JarvisRunStatusDto(runId, "Pending", null));
}

file sealed class StubContextRetrievalService : FocusDeck.Contracts.Services.Context.IContextRetrievalService
{
    public Task<IEnumerable<FocusDeck.Contracts.DTOs.ContextSnapshotDto>> RetrieveRelatedMomentsAsync(string query, int limit, float minRelevance, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<FocusDeck.Contracts.DTOs.ContextSnapshotDto>());

    public Task<IEnumerable<FocusDeck.Contracts.DTOs.ContextSnapshotDto>> RetrieveRecentSnapshotsAsync(int limit, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<FocusDeck.Contracts.DTOs.ContextSnapshotDto>());

    public Task<FocusDeck.Contracts.DTOs.ContextSnapshotDto?> GetSnapshotAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<FocusDeck.Contracts.DTOs.ContextSnapshotDto?>(null);

    public Task<List<FocusDeck.Domain.Entities.Context.ContextSnapshot>> GetSimilarMomentsAsync(FocusDeck.Domain.Entities.Context.ContextSnapshot current)
        => Task.FromResult(new List<FocusDeck.Domain.Entities.Context.ContextSnapshot>());
}

file sealed class StubCurrentTenant : FocusDeck.SharedKernel.Tenancy.ICurrentTenant
{
    public Guid? TenantId => Guid.Empty;
    public bool HasTenant => false;
    public IDisposable Change(Guid? tenantId) => new StubDisposable();
    public void SetTenant(Guid tenantId) { }
}

file sealed class StubDisposable : IDisposable { public void Dispose() {} }

file sealed class StubAutomationGeneratorService : IAutomationGeneratorService
{
    public Task GenerateProposalAsync(List<FocusDeck.Domain.Entities.Context.ContextSnapshot> cluster) => Task.CompletedTask;
    public Task<FocusDeck.Domain.Entities.Automations.AutomationProposal> GenerateProposalFromIntentAsync(string intent, string userId)
        => Task.FromResult(new FocusDeck.Domain.Entities.Automations.AutomationProposal { Id = Guid.NewGuid(), Title = "Generated Proposal", YamlDefinition = "yaml" });
}
