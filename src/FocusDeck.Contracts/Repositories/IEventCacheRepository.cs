using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Contracts.Repositories
{
    public interface IEventCacheRepository
    {
        Task<IEnumerable<EventCache>> GetActiveEventsAsync(Guid userId, DateTimeOffset timestamp, CancellationToken ct = default);
    }
}
