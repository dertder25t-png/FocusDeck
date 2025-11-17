using System;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Persistence;
using FocusDeck.Persistence.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FocusDeck.Server.Tests.Persistence.Repositories
{
    public class EfContextSnapshotRepositoryTests
    {
        private readonly DbContextOptions<AutomationDbContext> _options;

        public EfContextSnapshotRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<AutomationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task AddAsync_ShouldAddSnapshot()
        {
            // Arrange
            await using var context = new AutomationDbContext(_options);
            var repository = new EfContextSnapshotRepository(context);
            var snapshot = new ContextSnapshot { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow };

            // Act
            await repository.AddAsync(snapshot);

            // Assert
            var result = await context.ContextSnapshots.FindAsync(snapshot.Id);
            Assert.NotNull(result);
            Assert.Equal(snapshot.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnSnapshot()
        {
            // Arrange
            var snapshotId = Guid.NewGuid();
            await using (var context = new AutomationDbContext(_options))
            {
                context.ContextSnapshots.Add(new ContextSnapshot { Id = snapshotId, UserId = Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow });
                await context.SaveChangesAsync();
            }
            await using var readContext = new AutomationDbContext(_options);
            var repository = new EfContextSnapshotRepository(readContext);

            // Act
            var result = await repository.GetByIdAsync(snapshotId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(snapshotId, result.Id);
        }

        [Fact]
        public async Task GetLatestForUserAsync_ShouldReturnLatestSnapshot()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var snapshot1 = new ContextSnapshot { Id = Guid.NewGuid(), UserId = userId, Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1) };
            var snapshot2 = new ContextSnapshot { Id = Guid.NewGuid(), UserId = userId, Timestamp = DateTimeOffset.UtcNow };
            await using (var context = new AutomationDbContext(_options))
            {
                context.ContextSnapshots.AddRange(snapshot1, snapshot2);
                await context.SaveChangesAsync();
            }
            await using var readContext = new AutomationDbContext(_options);
            var repository = new EfContextSnapshotRepository(readContext);

            // Act
            var result = await repository.GetLatestForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(snapshot2.Id, result.Id);
        }
    }
}
