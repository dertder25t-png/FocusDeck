using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities.Jarvis;
using FocusDeck.Persistence;

namespace FocusDeck.Persistence.Repositories.Jarvis
{
    public class EfJarvisRunRepository : IJarvisRunRepository
    {
        private readonly AutomationDbContext _db;

        public EfJarvisRunRepository(AutomationDbContext db) => _db = db;

        public Task<JarvisRun> AddAsync(JarvisRun run, CancellationToken ct = default)
        {
            // TODO: add + save
            throw new NotImplementedException();
        }

        public Task<JarvisRun?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            // TODO: include Steps
            throw new NotImplementedException();
        }

        public Task UpdateAsync(JarvisRun run, CancellationToken ct = default)
        {
            // TODO: save changes
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<JarvisRun>> GetRecentForUserAsync(string userId, int limit, CancellationToken ct = default)
        {
            // TODO: filter by userId, order by StartedAt desc
            throw new NotImplementedException();
        }
    }
}
