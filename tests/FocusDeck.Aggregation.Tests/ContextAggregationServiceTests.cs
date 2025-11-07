using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Context;
using FocusDeck.Server.Services.Integrations;
using FocusDeck.Services.Activity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FocusDeck.Aggregation.Tests;

public class ContextAggregationServiceTests
{
    private sealed class TestDetector : IActivityDetectionService
    {
        private readonly Subject<ActivityState> _subject = new();
        public IObservable<ActivityState> ActivityChanged => _subject;
        public Task<ActivityState> GetCurrentActivityAsync(CancellationToken ct) => Task.FromResult(new ActivityState { ActivityIntensity = 10, IsIdle = false, Timestamp = DateTime.UtcNow });
        public Task<FocusedApplication?> GetFocusedApplicationAsync(CancellationToken ct) => Task.FromResult<FocusedApplication?>(new FocusedApplication { AppName = "TestApp", WindowTitle = "Test Window" });
        public Task<bool> IsIdleAsync(int idleThresholdSeconds, CancellationToken ct) => Task.FromResult(false);
        public Task<double> GetActivityIntensityAsync(int minutesWindow, CancellationToken ct) => Task.FromResult(10.0);
        public void Emit(ActivityState s) => _subject.OnNext(s);
    }

    private sealed class StubCanvasService : CanvasService
    {
        public StubCanvasService() : base(new LoggerFactory().CreateLogger<CanvasService>()) { }
        public override Task<List<CanvasAssignment>> GetUpcomingAssignments(string canvasDomain, string accessToken)
        {
            return Task.FromResult(new List<CanvasAssignment>
            {
                new CanvasAssignment { Id = "1", Name = "HW 1", DueAt = DateTime.UtcNow.AddDays(1), CourseId = "C1", CourseName = "Math" }
            });
        }
    }

    private static AutomationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AutomationDbContext(options);
    }

    [Fact]
    public async Task Aggregator_Enriches_With_CanvasAssignments_And_Persists()
    {
        using var db = CreateDb();
        // Seed Canvas config
        db.ServiceConfigurations.Add(new FocusDeck.Domain.Entities.ServiceConfiguration
        {
            Id = Guid.NewGuid(),
            ServiceName = "Canvas",
            AdditionalConfig = "{\"domain\":\"example.instructure.com\",\"accessToken\":\"token\"}"
        });
        await db.SaveChangesAsync();
        var logger = new LoggerFactory().CreateLogger<ContextAggregationService>();
        var detector = new TestDetector();
        var aggregator = new ContextAggregationService(
            logger,
            new[] { detector },
            db,
            new StubCanvasService());

        detector.Emit(new ActivityState
        {
            FocusedApp = new FocusedApplication { AppName = "Code", WindowTitle = "Project" },
            ActivityIntensity = 50,
            Timestamp = DateTime.UtcNow
        });

        var received = await aggregator.GetAggregatedActivityAsync(CancellationToken.None);
        Assert.NotNull(received);
        Assert.Contains(received.OpenContexts, c => c.Type == "canvas_assignment");

        // Persist snapshot
        await aggregator.PersistSnapshotAsync(received!, "test-user", CancellationToken.None);
        Assert.True(db.StudentContexts.Any());
    }
}
