using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Persistence.Repositories
{
    public class EfActivitySignalRepository : IActivitySignalRepository
    {
        private readonly AutomationDbContext _context;

        public EfActivitySignalRepository(AutomationDbContext context)
        {
            _context = context;
        }

        public async Task<ActivitySignal?> GetLatestSignalAsync(Guid userId, string signalType, CancellationToken cancellationToken = default)
        {
            return await _context.ActivitySignals
                .Where(s => s.UserId == userId.ToString() && s.SignalType == signalType)
                .OrderByDescending(s => s.CapturedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
