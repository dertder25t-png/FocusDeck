using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Contracts.Services.Privacy;

namespace FocusDeck.Services.Context.Sources
{
    public class DesktopActiveWindowSource : IContextSnapshotSource
    {
        public string SourceName => "DesktopActiveWindow";

        private readonly IActivitySignalRepository _repository;
        private readonly IPrivacyDataNotifier _privacyNotifier;

        public DesktopActiveWindowSource(IActivitySignalRepository repository, IPrivacyDataNotifier privacyNotifier)
        {
            _repository = repository;
            _privacyNotifier = privacyNotifier;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // Query the latest 'ActiveWindow' signal for this user
            var latestSignal = await _repository.GetLatestSignalAsync(userId, "ActiveWindow", ct);

            if (latestSignal == null)
            {
                return null;
            }

            // Send the data to the privacy notifier
            await _privacyNotifier.SendPrivacyDataAsync(userId.ToString(), "ActiveWindow", latestSignal.SignalValue, ct);

            var data = new JsonObject
            {
                ["application"] = latestSignal.SourceApp,
                ["title"] = latestSignal.SignalValue,
                ["metadata"] = latestSignal.MetadataJson
            };

            var slice = new ContextSlice
            {
                Id = Guid.NewGuid(),
                SourceType = ContextSourceType.DesktopActiveWindow,
                Timestamp = latestSignal.CapturedAtUtc,
                Data = data
            };

            return slice;
        }
    }
}
