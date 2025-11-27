using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace FocusDeck.Server.Hubs
{
    public class PrivacyDataHub : Hub
    {
        public async Task SendPrivacyData(string user, string type, string data)
        {
            await Clients.User(user).SendAsync("ReceivePrivacyData", type, data);
        }
    }
}
