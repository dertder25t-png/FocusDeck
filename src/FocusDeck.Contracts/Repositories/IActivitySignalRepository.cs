using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Contracts.Repositories
{
    public interface IActivitySignalRepository
    {
        Task<ActivitySignal?> GetLatestSignalAsync(Guid userId, string signalType, CancellationToken cancellationToken = default);
    }
}
