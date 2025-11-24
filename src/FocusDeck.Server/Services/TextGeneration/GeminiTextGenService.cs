using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.TextGeneration;

public class GeminiTextGenService : ITextGen
{
    private readonly HttpClient _httpClient;
    private readonly AutomationDbContext _dbContext;
    private readonly ILogger<GeminiTextGenService> _logger;

    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    public GeminiTextGenService(
        HttpClient httpClient,
        AutomationDbContext dbContext,
        ILogger<GeminiTextGenService> logger)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt, int maxTokens = 500, double temperature = 0.7, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Gemini API Key not configured. Using Stub.");
            return $"[Stub Response] Gemini Key missing. Prompt: {prompt.Substring(0, Math.Min(prompt.Length, 50))}...";
        }

        var requestUrl = $"{BaseUrl}?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                maxOutputTokens = maxTokens,
                temperature = temperature
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API Error: {StatusCode} - {Error}", response.StatusCode, error);
                throw new Exception($"Gemini API Error: {response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseString);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                    contentElem.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString();
                    return text ?? string.Empty;
                }
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Gemini API");
            throw;
        }
    }

    private async Task<string?> GetApiKeyAsync()
    {
        // Try getting from ServiceConfiguration first
        var config = await _dbContext.ServiceConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ServiceName == "Gemini");

        if (!string.IsNullOrEmpty(config?.ApiKey))
        {
            return config.ApiKey;
        }

        return null;
    }
}
