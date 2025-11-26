using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Privacy;
using FocusDeck.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FocusDeck.Server.Services.Privacy
{
    /// <summary>
    /// SignalR-based implementation of IPrivacyDataNotifier.
    /// Sends privacy data to connected clients via the PrivacyDataHub.
    /// </summary>
    public class SignalRPrivacyDataNotifier : IPrivacyDataNotifier
    {
        private readonly IHubContext<PrivacyDataHub> _hubContext;

        public SignalRPrivacyDataNotifier(IHubContext<PrivacyDataHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendPrivacyDataAsync(string userId, string dataType, string data, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceivePrivacyData", dataType, data, cancellationToken);
        }
    }
}
