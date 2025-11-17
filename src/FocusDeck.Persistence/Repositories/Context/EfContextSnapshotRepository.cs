using System;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities.Context;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Persistence.Repositories.Context
{
    public class EfContextSnapshotRepository : IEfContextSnapshotRepository
    {
        private readonly AutomationDbContext _context;

        public EfContextSnapshotRepository(AutomationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ContextSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            await _context.ContextSnapshots.AddAsync(snapshot, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<ContextSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ContextSnapshots
                .Include(s => s.Slices)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<ContextSnapshot?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.ContextSnapshots
                .Include(s => s.Slices)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
