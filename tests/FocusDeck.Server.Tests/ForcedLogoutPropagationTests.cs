using FocusDeck.Persistence;
using FocusDeck.Server.Hubs;
using FocusDeck.Server.Services.Auth;
using FocusDeck.Shared.SignalR.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests;

public class ForcedLogoutPropagationTests
{
    private sealed class FakeClient : INotificationClient
    {
        public ForceLogoutMessage? Last { get; private set; }

        public Task AutomationExecuted(string automationId, bool success, string message) => Task.CompletedTask;
        public Task ContextUpdated(FocusDeck.Services.Activity.ActivityState state) => Task.CompletedTask;
        public Task DesignIdeasAdded(string projectId, int ideaCount, string message) => Task.CompletedTask;
        public Task FocusDistraction(string reason, DateTime at) => Task.CompletedTask;
        public Task FocusEnded(string sessionId, int actualMinutes, int distractionCount) => Task.CompletedTask;
        public Task FocusRecoverySuggested(string suggestion) => Task.CompletedTask;
        public Task FocusStarted(string sessionId, string mode, int durationMinutes) => Task.CompletedTask;
        public Task JobCompleted(string jobId, string jobType, bool success, string message, object? result) => Task.CompletedTask;
        public Task JobProgress(string jobId, string jobType, int progressPercent, string message) => Task.CompletedTask;
        public Task LectureNoteReady(string lectureId, string noteId, string message) => Task.CompletedTask;
        public Task LectureSummarized(string lectureId, string summaryText, string message) => Task.CompletedTask;
        public Task LectureTranscribed(string lectureId, string transcriptionText, string message) => Task.CompletedTask;
        public Task NotificationReceived(string title, string message, string severity) => Task.CompletedTask;
        public Task NoteSuggestionReady(string noteId, string suggestionId, string type, string content) => Task.CompletedTask;
        public Task RemoteActionCreated(string ActionId, string Kind, object Payload) => Task.CompletedTask;
        public Task RemoteTelemetry(TelemetryUpdate payload) => Task.CompletedTask;
        public Task SessionCompleted(string sessionId, int durationMinutes, string message) => Task.CompletedTask;
        public Task SessionCreated(string sessionId, string message) => Task.CompletedTask;
        public Task SessionUpdated(string sessionId, string status, string message) => Task.CompletedTask;
        public Task ForceLogout(ForceLogoutMessage payload)
        {
            Last = payload;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHubClients : IHubClients<INotificationClient>
    {
        private FakeClient _fakeClient = new();
        
        public INotificationClient All => _fakeClient;
        public INotificationClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => _fakeClient;
        public INotificationClient Client(string connectionId) => _fakeClient;
        public INotificationClient Clients(IReadOnlyList<string> connectionIds) => _fakeClient;
        public INotificationClient Group(string groupName) => _fakeClient;
        public INotificationClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _fakeClient;
        public INotificationClient Groups(IReadOnlyList<string> groupNames) => _fakeClient;
        public INotificationClient User(string userId) => _fakeClient;
        public INotificationClient Users(IReadOnlyList<string> userIds) => _fakeClient;
        
        public FakeClient FakeClientInstance => _fakeClient;
    }

    private sealed class FakeHubContext : IHubContext<NotificationsHub, INotificationClient>
    {
        public FakeHubClients Inner { get; } = new();
        public IHubClients<INotificationClient> Clients => Inner;
        public IGroupManager Groups => throw new NotImplementedException();
    }

    private static AutomationDbContext CreateDb(out SqliteConnection conn)
    {
        conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AutomationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Revoke_Sends_ForceLogout_To_User_Group()
    {
        using var db = CreateDb(out var conn);
        await using var _ = conn;

        var hub = new FakeHubContext();
        var svc = new AccessTokenRevocationService(db, NullLogger<AccessTokenRevocationService>.Instance, hub, redis: null);

        var jti = Guid.NewGuid().ToString("N");
        var userId = "user@example.com";
        var expires = DateTime.UtcNow.AddMinutes(10);

        await svc.RevokeAsync(jti, userId, expires, CancellationToken.None, reason: "Test", deviceId: "dev1");

        Assert.NotNull(hub.Inner.FakeClientInstance.Last);
        Assert.Equal("Test", hub.Inner.FakeClientInstance.Last!.Reason);
        Assert.Equal("dev1", hub.Inner.FakeClientInstance.Last!.DeviceId);
    }
}

