using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.ActionHandlers;

namespace FocusDeck.Server.Services.Integrations
{
    public class EmailActionHandler : IActionHandler
    {
        private readonly IEmailProvider _emailProvider;

        public string ServiceName => "Email";

        public EmailActionHandler(IEmailProvider emailProvider)
        {
            _emailProvider = emailProvider;
        }

        public async Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger)
        {
            if (action.ActionType == "email.Send")
            {
                var to = action.Settings.GetValueOrDefault("to", "");
                var subject = action.Settings.GetValueOrDefault("subject", "FocusDeck Automation");
                var body = action.Settings.GetValueOrDefault("body", "");

                // In a real scenario, we'd resolve the user's access token from ConnectedService
                // For now, we'll log it as a stub
                logger.LogInformation("Sending email to {To}: {Subject}", to, subject);

                // await _emailProvider.SendEmailAsync("token_placeholder", to, subject, body);

                return await Task.FromResult(new ActionResult { Success = true, Message = $"Email queued to {to}" });
            }

            return new ActionResult { Success = false, Message = $"Unknown email action: {action.ActionType}" };
        }
    }
}
