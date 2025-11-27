using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Privacy;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Services.Context.Sources
{
    public class DeviceActivitySource : IContextSnapshotSource
    {
        private readonly IPrivacyDataNotifier _privacyNotifier;
        public string SourceName => "DeviceActivity";

        public DeviceActivitySource(IPrivacyDataNotifier privacyNotifier)
        {
            _privacyNotifier = privacyNotifier;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // TODO: Implement the logic to capture the user's device activity.
            // This will involve monitoring system-level events.
            var data = new JsonObject
            {
                ["status"] = "Active",
                ["lastInputTime"] = DateTimeOffset.UtcNow.ToString("o")
            };
            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.DeviceActivity,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            // Send the data to the privacy notifier
            await _privacyNotifier.SendPrivacyDataAsync(userId.ToString(), "DeviceActivity", data.ToJsonString(), ct);

            return slice;
        }
    }
}
