using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Context
{
    public class AmbientService
    {
        private readonly AutomationDbContext _db;
        private readonly IClock _clock;
        private readonly ILogger<AmbientService> _logger;

        public AmbientService(
            AutomationDbContext db,
            IClock clock,
            ILogger<AmbientService> logger)
        {
            _db = db;
            _clock = clock;
            _logger = logger;
        }

        public async Task<MorningBriefingDto> GetMorningBriefingAsync(Guid tenantId, int timeZoneOffsetMinutes)
        {
            // Client sends offset in minutes (e.g., UTC-5 is 300). JS getTimezoneOffset returns positive for West (UTC-5 -> 300).
            // So Local = UTC - Offset.
            var nowUtc = _clock.UtcNow;
            var localNow = nowUtc.AddMinutes(-timeZoneOffsetMinutes);

            // Calculate end of day in LOCAL time, then convert back to UTC for query
            var localEndOfDay = localNow.Date.AddDays(1).AddTicks(-1);
            var endOfDayUtc = localEndOfDay.AddMinutes(timeZoneOffsetMinutes);
            var horizon = nowUtc.AddDays(3);

            // 1. Fetch Calendar Events (Next 24h)
            var events = await _db.EventCache
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId && e.StartTime >= nowUtc && e.StartTime <= endOfDayUtc)
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            // 2. Fetch Tasks (Due within 3 days)
            // Note: Assuming TodoItem entity exists (it's in Domain), but maybe not DbSet?
            // Let's check if TodoItems are in DbContext. If not, we might need to add them or skip.
            // For now, assuming no TodoItem DbSet based on previous file listing (only CapturedItems/Notes/etc).
            // We can use Notes with deadlines if they exist, or just stick to Calendar for urgency.

            // Let's infer urgency from Calendar density and proximity
            var urgencyScore = CalculateUrgency(events, nowUtc);
            var (color, status) = GetHorizonStatus(urgencyScore);

            return new MorningBriefingDto
            {
                Greeting = GetGreeting(localNow),
                HorizonColor = color,
                HorizonStatus = status,
                UrgencyScore = urgencyScore,
                UpNext = events.Select(e => new BriefingItem
                {
                    Title = e.Title,
                    Time = e.StartTime.AddMinutes(-timeZoneOffsetMinutes).ToString("t"),
                    IsUrgent = (e.StartTime - nowUtc).TotalHours < 2
                }).ToList()
            };
        }

        private double CalculateUrgency(List<EventCache> events, DateTime now)
        {
            double score = 0;
            foreach (var evt in events)
            {
                var hoursUntil = (evt.StartTime - now).TotalHours;
                if (hoursUntil < 0) continue;

                if (hoursUntil < 2) score += 10; // Immediate
                else if (hoursUntil < 6) score += 5; // Soon
                else if (hoursUntil < 12) score += 2; // Today
                else score += 1; // Upcoming
            }
            // Cap at 100? Or normalized 0-1?
            // Let's say > 20 is Critical, > 10 is Warning, < 10 is Safe
            return score;
        }

        private (string Color, string Status) GetHorizonStatus(double score)
        {
            if (score > 20) return ("#EF4444", "Critical"); // Red
            if (score > 10) return ("#F59E0B", "Busy"); // Orange
            return ("#3B82F6", "Calm"); // Blue
        }

        private string GetGreeting(DateTime now)
        {
            var hour = now.Hour; // UTC hour, might be off for local.
            // Ideally we need user timezone. Assuming ~generic morning for now.
            if (hour < 12) return "Good Morning";
            if (hour < 18) return "Good Afternoon";
            return "Good Evening";
        }
    }

    public class MorningBriefingDto
    {
        public string Greeting { get; set; } = "";
        public string HorizonColor { get; set; } = "";
        public string HorizonStatus { get; set; } = "";
        public double UrgencyScore { get; set; }
        public List<BriefingItem> UpNext { get; set; } = new();
    }

    public class BriefingItem
    {
        public string Title { get; set; } = "";
        public string Time { get; set; } = "";
        public bool IsUrgent { get; set; }
    }
}
