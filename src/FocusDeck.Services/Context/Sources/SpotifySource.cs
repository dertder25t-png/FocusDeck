using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Privacy;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Persistence;
using FocusDeck.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Services.Context.Sources
{
    public class SpotifySource : IContextSnapshotSource
    {
        private readonly IPrivacyDataNotifier _privacyNotifier;
        private readonly IServiceScopeFactory _scopeFactory;

        public string SourceName => "Spotify";

        public SpotifySource(IPrivacyDataNotifier privacyNotifier, IServiceScopeFactory scopeFactory)
        {
            _privacyNotifier = privacyNotifier;
            _scopeFactory = scopeFactory;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var spotifyService = scope.ServiceProvider.GetRequiredService<ISpotifyService>();

            var userIdStr = userId.ToString();
            var service = await db.ConnectedServices
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userIdStr && s.Service == ServiceType.Spotify, ct);

            if (service == null || !service.IsConfigured)
            {
                return null;
            }

            var token = encryptionService.Decrypt(service.AccessToken);
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var playing = await spotifyService.GetCurrentlyPlaying(token);
            if (playing == null)
            {
                return null;
            }

            var data = new JsonObject
            {
                ["artist"] = playing.Artist,
                ["track"] = playing.Track,
                ["album"] = playing.Album,
                ["isPlaying"] = playing.IsPlaying,
                ["progressMs"] = playing.ProgressMs,
                ["durationMs"] = playing.DurationMs,
                ["uri"] = playing.Uri
            };

            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.Spotify,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            // Send the data to the privacy notifier
            await _privacyNotifier.SendPrivacyDataAsync(userId.ToString(), "Spotify", data.ToJsonString(), ct);

            return slice;
        }
    }
}
