using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http.Headers;

namespace FocusDeck.Server.Services.ActionHandlers
{
    public interface IFileStorageProvider
    {
        string ProviderName { get; }
        Task SaveFileAsync(ConnectedService service, string path, string content);
        Task<List<string>> ListFilesAsync(ConnectedService service, string path);
    }

    public class FileStorageActionHandler : IActionHandler
    {
        private readonly IEnumerable<IFileStorageProvider> _providers;

        public string ServiceName => "Storage";

        public FileStorageActionHandler(IEnumerable<IFileStorageProvider> providers)
        {
            _providers = providers;
        }

        public async Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger)
        {
            if (action.ActionType == "storage.SaveFile")
            {
                var providerType = action.Settings.GetValueOrDefault("provider", "GoogleDrive");
                var path = action.Settings.GetValueOrDefault("path", "/");
                var content = action.Settings.GetValueOrDefault("content", "");

                var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerType, StringComparison.OrdinalIgnoreCase));
                if (provider == null)
                    return new ActionResult { Success = false, Message = $"Storage provider not found: {providerType}" };

                var serviceType = providerType switch {
                    "GoogleDrive" => ServiceType.GoogleDrive,
                    _ => ServiceType.GoogleDrive // Default or error
                };

                var service = await db.ConnectedServices.FirstOrDefaultAsync(s => s.Service == serviceType);
                if (service == null)
                    return new ActionResult { Success = false, Message = $"{providerType} not connected" };

                try
                {
                    await provider.SaveFileAsync(service, path, content);
                    return new ActionResult { Success = true, Message = $"File saved to {providerType}" };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to save file to {Provider}", providerType);
                    return new ActionResult { Success = false, Message = $"Failed to save file: {ex.Message}" };
                }
            }

            return new ActionResult { Success = false, Message = $"Unknown storage action: {action.ActionType}" };
        }
    }

    public class GoogleDriveProvider : IFileStorageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string ProviderName => "GoogleDrive";

        public GoogleDriveProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task SaveFileAsync(ConnectedService service, string path, string content)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", service.AccessToken);

            // Simple multipart upload for Google Drive API v3
            var metadata = new { name = Path.GetFileName(path) };
            var multipartContent = new MultipartFormDataContent();

            multipartContent.Add(new StringContent(JsonSerializer.Serialize(metadata), System.Text.Encoding.UTF8, "application/json"), "metadata");
            multipartContent.Add(new StringContent(content, System.Text.Encoding.UTF8, "text/plain"), "file");

            var response = await client.PostAsync("https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart", multipartContent);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<string>> ListFilesAsync(ConnectedService service, string path)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", service.AccessToken);

            var response = await client.GetAsync("https://www.googleapis.com/drive/v3/files");
            response.EnsureSuccessStatusCode();

            // Simplified parsing
            return new List<string>();
        }
    }
}
