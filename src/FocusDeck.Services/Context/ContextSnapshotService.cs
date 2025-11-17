using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities.Context;
using Hangfire;

namespace FocusDeck.Services.Context
{
    public class ContextSnapshotService : IContextSnapshotService
    {
        private readonly IEnumerable<IContextSnapshotSource> _sources;
        private readonly IContextSnapshotRepository _repository;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ContextSnapshotService(
            IEnumerable<IContextSnapshotSource> sources,
            IContextSnapshotRepository repository,
            IBackgroundJobClient backgroundJobClient)
        {
            _sources = sources;
            _repository = repository;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<ContextSnapshot> CaptureNowAsync(Guid userId, CancellationToken ct)
        {
            var slices = new List<ContextSlice>();
            foreach (var source in _sources)
            {
                var slice = await source.CaptureAsync(userId, ct);
                if (slice != null)
                {
                    slices.Add(slice);
                }
            }

            var snapshot = new ContextSnapshot
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Timestamp = DateTimeOffset.UtcNow,
                Slices = slices.OrderBy(s => s.SourceType).ToList()
            };

            await _repository.AddAsync(snapshot, ct);

            _backgroundJobClient.Enqueue<IVectorizeSnapshotJob>(job => job.Execute(snapshot.Id, CancellationToken.None));

            return snapshot;
        }
    }

    public interface IVectorizeSnapshotJob
    {
        Task Execute(Guid snapshotId, CancellationToken CancellationToken);
    }
}
