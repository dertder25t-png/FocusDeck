using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Privacy;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Services.Context.Sources
{
    public class CanvasAssignmentsSource : IContextSnapshotSource
    {
        private readonly IPrivacyDataNotifier _privacyNotifier;
        public string SourceName => "CanvasAssignments";

        public CanvasAssignmentsSource(IPrivacyDataNotifier privacyNotifier)
        {
            _privacyNotifier = privacyNotifier;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // TODO: Implement the logic to capture the user's upcoming Canvas assignment.
            // This will involve using the Canvas API.
            var data = new JsonObject
            {
                ["assignment"] = "Finish the context snapshot system",
                ["course"] = "CS 4500",
                ["dueDate"] = DateTimeOffset.UtcNow.AddDays(2).ToString("o")
            };
            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.CanvasAssignments,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            // Send the data to the privacy notifier
            await _privacyNotifier.SendPrivacyDataAsync(userId.ToString(), "CanvasAssignments", data.ToJsonString(), ct);

            return slice;
        }
    }
}
