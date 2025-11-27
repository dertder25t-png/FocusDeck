using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Privacy;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Services.Context.Sources
{
    public class SuggestiveContextSource : IContextSnapshotSource
    {
        private readonly IPrivacyDataNotifier _privacyNotifier;
        public string SourceName => "SuggestiveContext";

        public SuggestiveContextSource(IPrivacyDataNotifier privacyNotifier)
        {
            _privacyNotifier = privacyNotifier;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // TODO: Implement the logic to generate a suggestive context slice.
            // This will involve using an AI model to analyze the user's current context and suggest relevant information.
            var data = new JsonObject
            {
                ["suggestion"] = "Take a break and stretch."
            };
            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.SuggestiveContext,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            // Send the data to the privacy notifier
            await _privacyNotifier.SendPrivacyDataAsync(userId.ToString(), "SuggestiveContext", data.ToJsonString(), ct);

            return slice;
        }
    }
}
