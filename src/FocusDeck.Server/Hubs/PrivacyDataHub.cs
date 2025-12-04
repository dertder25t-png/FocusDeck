using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Threading.Tasks;

namespace FocusDeck.Server.Hubs
{
    public class PrivacyDataHub : Hub
    {
        private readonly ILogger<PrivacyDataHub> _logger;

        public PrivacyDataHub(ILogger<PrivacyDataHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Send privacy data to a specific user.
        /// SECURITY: Validates that the sender can only send data to themselves.
        /// </summary>
        public async Task SendPrivacyData(string user, string type, string data)
        {
            // SECURITY: Validate user identity - only allow sending to self
            var authenticatedUserId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(authenticatedUserId))
            {
                _logger.LogWarning("SendPrivacyData blocked: No authenticated user for connection {ConnectionId}", Context.ConnectionId);
                throw new HubException("Authentication required");
            }

            if (authenticatedUserId != user)
            {
                _logger.LogWarning("SendPrivacyData blocked: User {AuthUserId} attempted to send privacy data to {TargetUserId}", 
                    authenticatedUserId, user);
                throw new HubException("Cannot send privacy data to another user");
            }

            // Validate input parameters
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new HubException("Type parameter is required");
            }

            // Limit type to known values to prevent injection
            var validTypes = new[] { "activity", "location", "preferences", "session" };
            if (!validTypes.Contains(type.ToLowerInvariant()))
            {
                _logger.LogWarning("SendPrivacyData blocked: Invalid type '{Type}' from user {UserId}", type, authenticatedUserId);
                throw new HubException("Invalid privacy data type");
            }

            await Clients.User(user).SendAsync("ReceivePrivacyData", type, data);
            _logger.LogDebug("Privacy data sent to user {UserId}, type={Type}", user, type);
        }
    }
}
