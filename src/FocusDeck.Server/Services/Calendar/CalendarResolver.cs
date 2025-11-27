using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Services.Calendar
{
    public class CalendarResolver
    {
        private readonly AutomationDbContext _db;
        private readonly ILogger<CalendarResolver> _logger;

        public CalendarResolver(AutomationDbContext db, ILogger<CalendarResolver> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<(EventCache? Event, CourseIndex? Course)> ResolveCurrentContextAsync(Guid tenantId)
        {
            var now = DateTime.UtcNow;
            // Window: started within last 15 mins or starts in next 10 mins
            // Logic: Event is "active" if (Start <= Now+10m) AND (End >= Now)
            // But to prioritize "just started" vs "ending soon", we stick to the spec:
            // Window now-15m ... now+10m for START time? No, for overlap.

            // Simple overlap check:
            // Event.Start < WindowEnd (Now + 10m)
            // Event.End > WindowStart (Now - 15m)

            var windowEnd = now.AddMinutes(10);
            var windowStart = now.AddMinutes(-15);

            var candidates = await _db.EventCache
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId)
                .Where(e => e.StartTime < windowEnd && e.EndTime > windowStart)
                .OrderBy(e => e.StartTime) // Prioritize earlier starts (ongoing)
                .Take(5)
                .ToListAsync();

            if (!candidates.Any())
            {
                return (null, null);
            }

            // Score candidates to find the best match (Course > General Event)
            // We also need the CourseIndex to match codes
            var courseIndices = await _db.CourseIndex
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .ToListAsync();

            EventCache? bestEvent = null;
            CourseIndex? bestCourse = null;
            int bestScore = -1;

            foreach (var evt in candidates)
            {
                var (course, score) = ScoreEvent(evt, courseIndices);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestEvent = evt;
                    bestCourse = course;
                }
            }

            return (bestEvent, bestCourse);
        }

        private (CourseIndex? Course, int Score) ScoreEvent(EventCache evt, List<CourseIndex> courses)
        {
            int score = 0;
            CourseIndex? matchedCourse = null;

            // Base score for being an event
            score += 10;

            // Try to match with a course
            foreach (var course in courses)
            {
                if (evt.Title.Contains(course.Code, StringComparison.OrdinalIgnoreCase))
                {
                    score += 50; // Strong match on code
                    matchedCourse = course;
                    break; // Assume only one course per event title for now
                }

                // Check keywords
                foreach (var keyword in course.Keywords)
                {
                    if (evt.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        (evt.Description != null && evt.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    {
                        score += 20; // Keyword match
                        matchedCourse = course;
                        // Keep checking to see if we find a code match (higher score) or just accumulate?
                        // For MVP, stick with first match or simple accumulation
                    }
                }
            }

            return (matchedCourse, score);
        }
    }
}
