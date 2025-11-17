using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Services.Context;
using Hangfire;
using Hangfire.Common;
using Moq;
using Xunit;

namespace FocusDeck.Server.Tests.Services.Context
{
    public class ContextSnapshotServiceTests
    {
        [Fact]
        public async Task CaptureNowAsync_ShouldCaptureAndStoreSnapshot()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var source1 = new Mock<IContextSnapshotSource>();
            source1.Setup(s => s.CaptureAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ContextSlice { SourceType = ContextSourceType.DesktopActiveWindow });
            var source2 = new Mock<IContextSnapshotSource>();
            source2.Setup(s => s.CaptureAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ContextSlice { SourceType = ContextSourceType.GoogleCalendar });
            var sources = new[] { source1.Object, source2.Object };
            var repository = new Mock<IContextSnapshotRepository>();
            var backgroundJobClient = new Mock<IBackgroundJobClient>();
            var service = new ContextSnapshotService(sources, repository.Object, backgroundJobClient.Object);

            // Act
            var result = await service.CaptureNowAsync(userId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(2, result.Slices.Count);
            repository.Verify(r => r.AddAsync(result, It.IsAny<CancellationToken>()), Times.Once);
            backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<Hangfire.States.EnqueuedState>()), Times.Once);
        }
    }
}
