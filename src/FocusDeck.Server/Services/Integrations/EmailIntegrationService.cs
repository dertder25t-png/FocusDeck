using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Persistence;

namespace FocusDeck.Server.Services.Integrations
{
    public interface IEmailProvider
    {
        Task<List<EmailMessage>> GetRecentEmailsAsync(string accessToken, int limit = 10);
        Task SendEmailAsync(string accessToken, string to, string subject, string body);
    }

    public class EmailMessage
    {
        public string Id { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public string Snippet { get; set; } = string.Empty;
    }

    public class GmailProvider : IEmailProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GmailProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<EmailMessage>> GetRecentEmailsAsync(string accessToken, int limit = 10)
        {
            // TODO: Implement Gmail API call using accessToken
            // GET https://gmail.googleapis.com/gmail/v1/users/me/messages
            return await Task.FromResult(new List<EmailMessage>());
        }

        public async Task SendEmailAsync(string accessToken, string to, string subject, string body)
        {
            // TODO: Implement Gmail API call to send email
            await Task.CompletedTask;
        }
    }

    public class EmailIntegrationService
    {
        private readonly IEmailProvider _emailProvider;
        private readonly AutomationDbContext _db;

        public EmailIntegrationService(IEmailProvider emailProvider, AutomationDbContext db)
        {
            _emailProvider = emailProvider;
            _db = db;
        }

        // Helper methods to fetch credentials from ConnectedService and call provider
    }
}
