using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Contracts.Services.Privacy
{
    /// <summary>
    /// Interface for sending privacy data notifications to connected clients.
    /// This abstraction allows services to notify users about privacy-related data
    /// without directly depending on SignalR infrastructure.
    /// </summary>
    public interface IPrivacyDataNotifier
    {
        /// <summary>
        /// Sends privacy data to a specific user.
        /// </summary>
        /// <param name="userId">The user ID to send the data to.</param>
        /// <param name="dataType">The type of privacy data (e.g., "Spotify", "GoogleCalendar").</param>
        /// <param name="data">The JSON-serialized data payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SendPrivacyDataAsync(string userId, string dataType, string data, CancellationToken cancellationToken = default);
    }
}
