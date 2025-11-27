using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Server.Services.Calendar
{
    public class CalendarWarmSyncJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CalendarWarmSyncJob> _logger;

        public CalendarWarmSyncJob(IServiceProvider serviceProvider, ILogger<CalendarWarmSyncJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            var googleAuth = scope.ServiceProvider.GetRequiredService<GoogleAuthService>();

            var sources = await db.CalendarSources.ToListAsync(cancellationToken);
            foreach (var source in sources)
            {
                try
                {
                    await SyncSource(source, db, googleAuth, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync calendar source {Id}", source.Id);
                }
            }
        }

        private async Task SyncSource(CalendarSource source, AutomationDbContext db, GoogleAuthService googleAuth, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(source.RefreshToken))
            {
                _logger.LogWarning("Skipping calendar source {Id} (No refresh token)", source.Id);
                return;
            }

            // Refresh token if needed (e.g. expires in < 5 mins or already expired)
            if (!source.TokenExpiry.HasValue || source.TokenExpiry.Value < DateTime.UtcNow.AddMinutes(5))
            {
                var tokenResponse = await googleAuth.RefreshTokenAsync(source.RefreshToken);
                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    source.AccessToken = tokenResponse.AccessToken;
                    source.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                    // Some providers rotate refresh tokens
                    if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    {
                        source.RefreshToken = tokenResponse.RefreshToken;
                    }
                    await db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Failed to refresh token for source {Id}", source.Id);
                    return;
                }
            }

            // Create Google Calendar Service
            var credential = GoogleCredential.FromAccessToken(source.AccessToken);
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "FocusDeck"
            });

            var calendarId = "primary"; // Assuming primary calendar for now, or source.ExternalId if multiple
            var request = service.Events.List(calendarId);
            request.TimeMinDateTimeOffset = DateTimeOffset.UtcNow.AddMinutes(-15);
            request.TimeMaxDateTimeOffset = DateTimeOffset.UtcNow.AddDays(14);
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            if (!string.IsNullOrEmpty(source.SyncToken))
            {
                // Incremental sync is complex with TimeMin/Max because syncToken invalidates them.
                // For "Warm Sync" (next 14 days), strictly fetching the window is safer/simpler than handling deleted events via SyncToken globally.
                // We will clear cache for this window and rebuild it, or use simple upsert.
                // Given the requirement "incremental with syncToken", we should try to use it,
                // BUT TimeMin is ignored if SyncToken is present.
                // So if we use SyncToken, we get ALL changes since then.
                // For a 14-day rolling window, it's better to just fetch the window.
                // Let's stick to window fetch for now (Resetting logic).
                // To properly support "SyncToken" we would need to store ALL future events, not just 14 days.
                // The roadmap says "next 14 days (incremental with syncToken)".
                // This is contradictory. SyncToken gives changes for the whole calendar.
                // I will prioritize the "next 14 days" requirement as it's more efficient for the Resolver.
            }

            var events = await request.ExecuteAsync(cancellationToken);

            if (events.Items != null)
            {
                foreach (var evt in events.Items)
                {
                    // Skip cancelled/deleted if returned (SingleEvents=true usually filters them unless showDeleted=true)
                    if (evt.Status == "cancelled") continue;

                    var start = evt.Start.DateTimeDateTimeOffset?.UtcDateTime ?? (evt.Start.Date != null ? DateTime.Parse(evt.Start.Date).ToUniversalTime() : DateTime.MinValue);
                    var end = evt.End.DateTimeDateTimeOffset?.UtcDateTime ?? (evt.End.Date != null ? DateTime.Parse(evt.End.Date).ToUniversalTime() : DateTime.MinValue);

                    // Google API DateTime is typically DateTimeOffset-ish or local. Convert to UTC.
                    if (evt.Start.DateTimeRaw != null) start = DateTime.Parse(evt.Start.DateTimeRaw).ToUniversalTime();
                    if (evt.End.DateTimeRaw != null) end = DateTime.Parse(evt.End.DateTimeRaw).ToUniversalTime();

                    var existing = await db.EventCache
                        .FirstOrDefaultAsync(e => e.CalendarSourceId == source.Id && e.ExternalEventId == evt.Id, cancellationToken);

                    if (existing == null)
                    {
                        existing = new EventCache
                        {
                            Id = Guid.NewGuid(),
                            CalendarSourceId = source.Id,
                            TenantId = source.TenantId,
                            ExternalEventId = evt.Id
                        };
                        db.EventCache.Add(existing);
                    }

                    existing.Title = evt.Summary ?? "(No Title)";
                    existing.Description = evt.Description;
                    existing.Location = evt.Location;
                    existing.StartTime = start;
                    existing.EndTime = end;
                    existing.IsAllDay = evt.Start.Date != null; // Heuristic
                }
            }

            source.LastSync = DateTime.UtcNow;
            source.SyncToken = events.NextSyncToken;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Synced {Count} events for {Name}", events.Items?.Count ?? 0, source.Name);
        }
    }
}
