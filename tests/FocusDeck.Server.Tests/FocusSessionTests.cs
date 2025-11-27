using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Shared.SignalR.Notifications;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Controllers.v1;
using FocusDeck.Server.Hubs;
using FocusDeck.Services.Activity;
using FocusDeck.Server.Services.Context;
using FocusDeck.Domain.Entities.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests;

public class FocusSessionTests : IDisposable
{
    private readonly AutomationDbContext _db;
    private readonly FocusController _controller;
    private readonly TestHubContext _hubContext;
    private readonly IContextEventBus _eventBus;

    public FocusSessionTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AutomationDbContext(options);

        // Setup test hub context
        _hubContext = new TestHubContext();

        // Mock event bus
        _eventBus = new TestContextEventBus();

        _controller = new FocusController(
            _db,
            NullLogger<FocusController>.Instance,
            _hubContext,
            _eventBus);

        // Set up a mock HttpContext with test user
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private class TestContextEventBus : IContextEventBus
    {
        public event Func<ContextSnapshot, Task> OnContextSnapshotCreated;

        public Task PublishAsync(ContextSnapshot snapshot)
        {
            // Just invoke event for testing
            if (OnContextSnapshotCreated != null)
            {
                return OnContextSnapshotCreated(snapshot);
            }
            return Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    // Simple test hub context that doesn't verify calls
    private class TestHubContext : IHubContext<NotificationsHub, INotificationClient>
    {
        private readonly TestNotificationClient _client = new();

        public IHubClients<INotificationClient> Clients => new TestHubClients(_client);
        public IGroupManager Groups => throw new NotImplementedException();
    }

    private class TestHubClients : IHubClients<INotificationClient>
    {
        private readonly INotificationClient _client;

        public TestHubClients(INotificationClient client)
        {
            _client = client;
        }

        public INotificationClient All => _client;
        public INotificationClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => _client;
        public INotificationClient Client(string connectionId) => _client;
        public INotificationClient Clients(IReadOnlyList<string> connectionIds) => _client;
        public INotificationClient Group(string groupName) => _client;
        public INotificationClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _client;
        public INotificationClient Groups(IReadOnlyList<string> groupNames) => _client;
        public INotificationClient User(string userId) => _client;
        public INotificationClient Users(IReadOnlyList<string> userIds) => _client;
    }

    private class TestNotificationClient : INotificationClient
    {
        public int DistractionCount { get; private set; }
        public int RecoverySuggestionCount { get; private set; }

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
        
        public Task FocusDistraction(string reason, DateTime at)
        {
            DistractionCount++;
            return Task.CompletedTask;
        }
        
        public Task FocusRecoverySuggested(string suggestion)
        {
            RecoverySuggestionCount++;
            return Task.CompletedTask;
        }
        
        public Task FocusStarted(string sessionId, string mode, int durationMinutes) => Task.CompletedTask;
        public Task FocusEnded(string sessionId, int actualMinutes, int distractionCount) => Task.CompletedTask;
        public Task DesignIdeasAdded(string projectId, int ideaCount, string message) => Task.CompletedTask;
        public Task NoteSuggestionReady(string noteId, string suggestionId, string type, string content) => Task.CompletedTask;
        public Task ContextUpdated(FocusDeck.Services.Activity.ActivityState state) => Task.CompletedTask;
        public Task RemoteTelemetry(TelemetryUpdate payload) => Task.CompletedTask;
        public Task ForceLogout(ForceLogoutMessage payload) => Task.CompletedTask;
        public Task JarvisRunUpdated(JarvisRunUpdate payload) => Task.CompletedTask;

        public Task ReceiveNotification(string title, string message, string severity) => Task.CompletedTask;
    }

    [Fact]
    public async Task CreateSession_WithValidRequest_CreatesSession()
    {
        // Arrange
        var request = new CreateFocusSessionDto
        {
            Policy = new FocusPolicyDto
            {
                Strict = true,
                AutoBreak = true,
                AutoDim = false,
                NotifyPhone = false
            }
        };

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<FocusSessionDto>(createdResult.Value);
        Assert.True(dto.Policy.Strict);
        Assert.True(dto.Policy.AutoBreak);
        Assert.Equal("Active", dto.Status);
        
        var sessionInDb = await _db.FocusSessions.FirstOrDefaultAsync(s => s.Id == dto.Id);
        Assert.NotNull(sessionInDb);
        Assert.True(sessionInDb.Policy.Strict);
    }

    [Fact]
    public async Task GetActiveSession_WithActiveSession_ReturnsSession()
    {
        // Arrange
        var session = new FocusSession
        {
            UserId = "test-user",
            StartTime = DateTime.UtcNow,
            Status = FocusSessionStatus.Active
        };
        _db.FocusSessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetActiveSession();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<FocusSessionDto>(okResult.Value);
        Assert.Equal(session.Id, dto.Id);
        Assert.Equal("Active", dto.Status);
    }

    [Fact]
    public async Task GetActiveSession_NoActiveSession_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetActiveSession();

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitSignal_PhoneMotion_InStrictMode_DetectsDistraction()
    {
        // Arrange
        var session = new FocusSession
        {
            UserId = "test-user",
            StartTime = DateTime.UtcNow,
            Status = FocusSessionStatus.Active,
            Policy = new FocusPolicy { Strict = true, AutoBreak = false }
        };
        _db.FocusSessions.Add(session);
        await _db.SaveChangesAsync();

        var signal = new SubmitSignalDto
        {
            DeviceId = "test-device",
            Kind = "PhoneMotion",
            Value = 0.8, // Motion detected
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _controller.SubmitSignal(signal);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Check distraction was recorded
        var updatedSession = await _db.FocusSessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(1, updatedSession.DistractionsCount);
        Assert.Single(updatedSession.Signals);
    }

    [Fact]
    public async Task SubmitSignal_PhoneScreen_InStrictMode_DetectsDistraction()
    {
        // Arrange
        var session = new FocusSession
        {
            UserId = "test-user",
            StartTime = DateTime.UtcNow,
            Status = FocusSessionStatus.Active,
            Policy = new FocusPolicy { Strict = true, AutoBreak = false }
        };
        _db.FocusSessions.Add(session);
        await _db.SaveChangesAsync();

        var signal = new SubmitSignalDto
        {
            DeviceId = "test-device",
            Kind = "PhoneScreen",
            Value = 1.0, // Screen active
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _controller.SubmitSignal(signal);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Check distraction was recorded
        var updatedSession = await _db.FocusSessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(1, updatedSession.DistractionsCount);
    }

    [Fact]
    public async Task SubmitSignal_NonStrictMode_DoesNotDetectDistraction()
    {
        // Arrange
        var session = new FocusSession
        {
            UserId = "test-user",
            StartTime = DateTime.UtcNow,
            Status = FocusSessionStatus.Active,
            Policy = new FocusPolicy { Strict = false } // Not strict
        };
        _db.FocusSessions.Add(session);
        await _db.SaveChangesAsync();

        var signal = new SubmitSignalDto
        {
            DeviceId = "test-device",
            Kind = "PhoneMotion",
            Value = 0.8,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _controller.SubmitSignal(signal);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Check no distraction was recorded
        var updatedSession = await _db.FocusSessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(0, updatedSession.DistractionsCount);
    }

    [Fact]
    public async Task SubmitSignal_MultipleDistractions_SuggestsRecovery()
    {
        // Arrange
        var session = new FocusSession
        {
            UserId = "test-user",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            Status = FocusSessionStatus.Active,
            Policy = new FocusPolicy { Strict = true, AutoBreak = true }
        };
        _db.FocusSessions.Add(session);
        await _db.SaveChangesAsync();

        // Submit 3 phone screen signals to trigger recovery suggestion
        for (int i = 0; i < 3; i++)
        {
            var signal = new SubmitSignalDto
            {
                DeviceId = "test-device",
                Kind = "PhoneScreen",
                Value = 1.0,
                Timestamp = DateTime.UtcNow.AddSeconds(-i * 30)
            };

            await _controller.SubmitSignal(signal);
        }

        // Assert
        var updatedSession = await _db.FocusSessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(3, updatedSession.DistractionsCount);
        Assert.NotNull(updatedSession.LastRecoverySuggestionAt);
    }

    [Fact]
    public async Task SubmitSignal_WithAutoBreakDisabled_DoesNotSuggestRecovery()
    {
        // Arrange
        var session = new FocusSession
        {
            UserId = "test-user",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            Status = FocusSessionStatus.Active,
            Policy = new FocusPolicy { Strict = true, AutoBreak = false } // AutoBreak disabled
        };
        _db.FocusSessions.Add(session);
        await _db.SaveChangesAsync();

        // Submit 3 phone screen signals
        for (int i = 0; i < 3; i++)
        {
            var signal = new SubmitSignalDto
            {
                DeviceId = "test-device",
                Kind = "PhoneScreen",
                Value = 1.0,
                Timestamp = DateTime.UtcNow.AddSeconds(-i * 30)
            };

            await _controller.SubmitSignal(signal);
        }

        // Assert
        var updatedSession = await _db.FocusSessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(3, updatedSession.DistractionsCount);
        Assert.Null(updatedSession.LastRecoverySuggestionAt); // No suggestion
    }

    [Fact]
    public async Task EndSession_ActiveSession_CompletesSession()
    {
        // Arrange
        var session = new FocusSession
        {
            UserId = "test-user",
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            Status = FocusSessionStatus.Active
        };
        _db.FocusSessions.Add(session);
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.EndSession(session.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<FocusSessionDto>(okResult.Value);
        Assert.Equal("Completed", dto.Status);
        Assert.NotNull(dto.EndTime);
        
        var updatedSession = await _db.FocusSessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.Equal(FocusSessionStatus.Completed, updatedSession.Status);
        Assert.NotNull(updatedSession.EndTime);
    }

    [Fact]
    public async Task EndSession_NonExistentSession_ReturnsNotFound()
    {
        // Act
        var result = await _controller.EndSession(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitSignal_InvalidSignalKind_ReturnsBadRequest()
    {
        // Arrange
        var session = new FocusSession
        {
            UserId = "test-user",
            StartTime = DateTime.UtcNow,
            Status = FocusSessionStatus.Active
        };
        _db.FocusSessions.Add(session);
        await _db.SaveChangesAsync();

        var signal = new SubmitSignalDto
        {
            DeviceId = "test-device",
            Kind = "InvalidKind",
            Value = 1.0,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _controller.SubmitSignal(signal);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SubmitSignal_NoActiveSession_ReturnsNotFound()
    {
        // Arrange
        var signal = new SubmitSignalDto
        {
            DeviceId = "test-device",
            Kind = "Keyboard",
            Value = 1.0,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _controller.SubmitSignal(signal);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
