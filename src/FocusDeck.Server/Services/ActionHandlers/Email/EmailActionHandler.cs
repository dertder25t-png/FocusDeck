using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace FocusDeck.Server.Services.ActionHandlers.Email
{
    public interface IEmailProvider
    {
        string ProviderName { get; }
        Task SendEmailAsync(ConnectedService service, string to, string subject, string body);
    }

    public class EmailActionHandler : IActionHandler
    {
        private readonly IEnumerable<IEmailProvider> _providers;

        public string ServiceName => "Email";

        public EmailActionHandler(IEnumerable<IEmailProvider> providers)
        {
            _providers = providers;
        }

        public async Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger)
        {
            if (action.ActionType == "email.Send")
            {
                var providerType = "Gmail"; // Default to Gmail for now
                var to = action.Settings.GetValueOrDefault("to", "");
                var subject = action.Settings.GetValueOrDefault("subject", "FocusDeck Notification");
                var body = action.Settings.GetValueOrDefault("body", "");

                var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerType, StringComparison.OrdinalIgnoreCase));
                if (provider == null)
                    return new ActionResult { Success = false, Message = $"Email provider not found: {providerType}" };

                // Assuming Gmail for now since ServiceType.Email doesn't exist, we use ServiceType.GoogleCalendar (Google account) or check metadata
                // This is a simplification. Ideally we'd have ServiceType.Gmail.
                // Using GoogleCalendar (Google) connection for now.
                var service = await db.ConnectedServices.FirstOrDefaultAsync(s => s.Service == ServiceType.GoogleCalendar);

                if (service == null)
                    return new ActionResult { Success = false, Message = "Google account not connected" };

                try
                {
                    await provider.SendEmailAsync(service, to, subject, body);
                    return new ActionResult { Success = true, Message = $"Email sent to {to}" };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send email");
                    return new ActionResult { Success = false, Message = $"Failed to send email: {ex.Message}" };
                }
            }

            return new ActionResult { Success = false, Message = $"Unknown email action: {action.ActionType}" };
        }
    }

    public class GmailProvider : IEmailProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string ProviderName => "Gmail";

        public GmailProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendEmailAsync(ConnectedService service, string to, string subject, string body)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", service.AccessToken);

            // Gmail API requires RFC 822 formatted message encoded in URL-safe Base64
            var msg = $"To: {to}\r\nSubject: {subject}\r\n\r\n{body}";
            var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg))
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            var content = new StringContent(JsonSerializer.Serialize(new { raw }), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://gmail.googleapis.com/gmail/v1/users/me/messages/send", content);
            response.EnsureSuccessStatusCode();
        }
    }
}
