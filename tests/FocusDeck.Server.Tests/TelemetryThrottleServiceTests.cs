using FocusDeck.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests;

public class TelemetryThrottleServiceTests
{
    [Fact]
    public void CanSendTelemetry_FirstTime_ReturnsTrue()
    {
        // Arrange
        var service = new TelemetryThrottleService(NullLogger<TelemetryThrottleService>.Instance);
        var userId = "user1";

        // Act
        var canSend = service.CanSendTelemetry(userId);

        // Assert
        Assert.True(canSend);
    }

    [Fact]
    public void CanSendTelemetry_WithinOneSecond_ReturnsFalse()
    {
        // Arrange
        var service = new TelemetryThrottleService(NullLogger<TelemetryThrottleService>.Instance);
        var userId = "user1";

        // Act
        service.RecordTelemetrySent(userId);
        var canSend = service.CanSendTelemetry(userId);

        // Assert
        Assert.False(canSend);
    }

    [Fact]
    public async Task CanSendTelemetry_AfterOneSecond_ReturnsTrue()
    {
        // Arrange
        var service = new TelemetryThrottleService(NullLogger<TelemetryThrottleService>.Instance);
        var userId = "user1";

        // Act
        service.RecordTelemetrySent(userId);
        await Task.Delay(1100); // Wait just over 1 second
        var canSend = service.CanSendTelemetry(userId);

        // Assert
        Assert.True(canSend);
    }

    [Fact]
    public void CanSendTelemetry_DifferentUsers_DoesNotThrottle()
    {
        // Arrange
        var service = new TelemetryThrottleService(NullLogger<TelemetryThrottleService>.Instance);
        var user1 = "user1";
        var user2 = "user2";

        // Act
        service.RecordTelemetrySent(user1);
        var canSendUser1 = service.CanSendTelemetry(user1);
        var canSendUser2 = service.CanSendTelemetry(user2);

        // Assert
        Assert.False(canSendUser1); // user1 is throttled
        Assert.True(canSendUser2);  // user2 is not throttled
    }

    [Fact]
    public void RecordTelemetrySent_UpdatesThrottleState()
    {
        // Arrange
        var service = new TelemetryThrottleService(NullLogger<TelemetryThrottleService>.Instance);
        var userId = "user1";

        // Act
        var canSendBefore = service.CanSendTelemetry(userId);
        service.RecordTelemetrySent(userId);
        var canSendAfter = service.CanSendTelemetry(userId);

        // Assert
        Assert.True(canSendBefore);   // Can send first time
        Assert.False(canSendAfter);   // Cannot send immediately after
    }

    [Fact]
    public async Task Throttle_EnforcesOneMessagePerSecond()
    {
        // Arrange
        var service = new TelemetryThrottleService(NullLogger<TelemetryThrottleService>.Instance);
        var userId = "user1";

        // Act & Assert
        // First message should succeed
        Assert.True(service.CanSendTelemetry(userId));
        service.RecordTelemetrySent(userId);

        // Second message within 1 second should be throttled
        Assert.False(service.CanSendTelemetry(userId));

        // Wait 1 second
        await Task.Delay(1100);

        // Third message after 1 second should succeed
        Assert.True(service.CanSendTelemetry(userId));
        service.RecordTelemetrySent(userId);

        // Fourth message within 1 second should be throttled
        Assert.False(service.CanSendTelemetry(userId));
    }

    [Fact]
    public void RecordTelemetrySent_HandlesMultipleUsers()
    {
        // Arrange
        var service = new TelemetryThrottleService(NullLogger<TelemetryThrottleService>.Instance);
        var users = Enumerable.Range(1, 10).Select(i => $"user{i}").ToList();

        // Act - Record telemetry for all users
        foreach (var user in users)
        {
            service.RecordTelemetrySent(user);
        }

        // Assert - All users should be throttled
        foreach (var user in users)
        {
            Assert.False(service.CanSendTelemetry(user));
        }
    }
}
