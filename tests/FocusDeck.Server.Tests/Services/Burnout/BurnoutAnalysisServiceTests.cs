using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Burnout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests.Services.Burnout;

public class BurnoutAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzePatternsAsync_PersistsUnsustainableMetrics()
    {
        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AutomationDbContext(options);

        var tenantId = Guid.NewGuid();
        const string userId = "burnout-test-user";
        SeedActivitySignals(db, tenantId, userId);
        await db.SaveChangesAsync();

        var service = new BurnoutAnalysisService(db, NullLogger<BurnoutAnalysisService>.Instance);
        await service.AnalyzePatternsAsync(CancellationToken.None);

        var metrics = await db.StudentWellnessMetrics.ToListAsync();
        Assert.Single(metrics);

        var metric = metrics.Single();
        Assert.Equal(tenantId, metric.TenantId);
        Assert.Equal(userId, metric.UserId);
        Assert.True(metric.IsUnsustainable, "Four 14-hour days should trigger the unsustainable detection.");
        Assert.InRange(metric.HoursWorked, 13.0, 15.0);
        Assert.InRange(metric.BreakFrequency, 0.0, 0.1);
        Assert.InRange(metric.SleepHours, 4.0, 12.0);
        Assert.InRange(metric.QualityScore, 0.0, 1.0);
        Assert.False(string.IsNullOrWhiteSpace(metric.Notes));
    }

    private static void SeedActivitySignals(AutomationDbContext db, Guid tenantId, string userId)
    {
        const int hoursPerDay = 14;
        const int signalIntervalMinutes = 10;

        for (var dayOffset = 1; dayOffset <= 4; dayOffset++)
        {
            var startOfDay = DateTime.UtcNow.Date.AddDays(-dayOffset).AddHours(8);
            for (var minute = 0; minute < hoursPerDay * 60; minute += signalIntervalMinutes)
            {
                db.ActivitySignals.Add(new ActivitySignal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = userId,
                    SignalType = "ActiveWindow",
                    SignalValue = "continuous-session",
                    SourceApp = "FocusDeck.Tests",
                    CapturedAtUtc = startOfDay.AddMinutes(minute)
                });
            }
        }
    }
}
