using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Jarvis;

namespace FocusDeck.Contracts.Repositories
{
    public interface IJarvisRunRepository
    {
        Task<JarvisRun> AddAsync(JarvisRun run, CancellationToken ct = default);
        Task<JarvisRun?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task UpdateAsync(JarvisRun run, CancellationToken ct = default);
        Task<IReadOnlyList<JarvisRun>> GetRecentForUserAsync(string userId, int limit, CancellationToken ct = default);
    }
}
