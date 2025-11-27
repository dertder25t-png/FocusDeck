using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Persistence;

namespace FocusDeck.Server.Services.ActionHandlers
{
    public class GitHubActionHandler : IActionHandler
    {
        public string ServiceName => "GitHub";

        public async Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger)
        {
            if (action.ActionType == "github.OpenBrowser")
            {
                var url = action.Settings.GetValueOrDefault("url", "https://github.com");
                logger.LogInformation("Opening GitHub URL: {Url}", url);

                // This is a client-side signal that will be sent to the connected clients (Desktop/Mobile)
                // via SignalR in the ActionExecutor or caller.
                return await Task.FromResult(new ActionResult {
                    Success = true,
                    Message = $"Opened GitHub: {url}",
                    Data = new { url, openInBrowser = true }
                });
            }

            return new ActionResult { Success = false, Message = $"Unknown GitHub action: {action.ActionType}" };
        }
    }
}
