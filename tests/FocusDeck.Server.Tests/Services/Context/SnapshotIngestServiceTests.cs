using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Server.Services.Context;
using FocusDeck.Services.Context;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FocusDeck.Server.Tests.Services.Context
{
    public class SnapshotIngestServiceTests
    {
        private readonly Mock<ILogger<SnapshotIngestService>> _loggerMock;
        private readonly Mock<IBackgroundJobClient> _jobClientMock;
        private readonly Mock<IContextSnapshotRepository> _repositoryMock;
        private readonly Mock<IContextEventBus> _eventBusMock;
        private readonly SnapshotIngestService _service;

        public SnapshotIngestServiceTests()
        {
            _loggerMock = new Mock<ILogger<SnapshotIngestService>>();
            _jobClientMock = new Mock<IBackgroundJobClient>();
            _repositoryMock = new Mock<IContextSnapshotRepository>();
            _eventBusMock = new Mock<IContextEventBus>();

            _service = new SnapshotIngestService(
                _loggerMock.Object,
                _jobClientMock.Object,
                _repositoryMock.Object,
                _eventBusMock.Object);
        }

        [Fact]
        public async Task IngestSnapshotAsync_ShouldPersistFeatureSummaryFields()
        {
            // Arrange
            var appStateDetails = new JsonObject
            {
                ["openTabs"] = 5,
                ["editorLineCount"] = 120
            };

            var featureSummary = new FeatureSummaryDto(
                TypingVelocity: 120.5,
                MouseEntropy: 0.85,
                ContextSwitchCount: 3,
                DevicePosture: "Standing",
                AudioContext: "Music",
                PhysicalLocation: "Home Office",
                ApplicationStateDetails: appStateDetails
            );

            var dto = new ContextSnapshotDto(
                EventType: "TestEvent",
                Timestamp: DateTime.UtcNow,
                ActiveApplication: "VS Code",
                ActiveWindowTitle: "FocusDeck.sln",
                CalendarEventId: null,
                CourseContext: null,
                MachineState: null,
                FeatureSummary: featureSummary
            );

            ContextSnapshot? capturedSnapshot = null;
            _repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<ContextSnapshot>(), It.IsAny<CancellationToken>()))
                .Callback<ContextSnapshot, CancellationToken>((s, c) => capturedSnapshot = s)
                .Returns(Task.CompletedTask);

            // Act
            await _service.IngestSnapshotAsync(dto, CancellationToken.None);

            // Assert
            Assert.NotNull(capturedSnapshot);
            Assert.Contains(capturedSnapshot.Slices, s => s.SourceType == ContextSourceType.DeviceActivity);

            var featureSlice = capturedSnapshot.Slices.First(s => s.SourceType == ContextSourceType.DeviceActivity);
            var data = featureSlice.Data;

            Assert.NotNull(data);
            Assert.Equal(120.5, (double?)data["TypingVelocity"]);
            Assert.Equal(0.85, (double?)data["MouseEntropy"]);
            Assert.Equal("Standing", (string?)data["DevicePosture"]);
            Assert.Equal("Music", (string?)data["AudioContext"]);
            Assert.NotNull(data["ApplicationStateDetails"]);
            Assert.Equal(5, (int?)data["ApplicationStateDetails"]?["openTabs"]);
        }
    }
}
