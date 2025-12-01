using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FocusDeck.Persistence;

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
        var apiKey = await GetApiKeyAsync(cancellationToken);
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Gemini API Key is not configured (ServiceConfiguration 'Gemini'). Returning error message as text.");
            return "Error: AI service is not configured. Please set the Gemini API key in settings.";
        }

        var requestUrl = $"{BaseUrl}?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
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
                _logger.LogError("Gemini API failed: {StatusCode} - {Error}", response.StatusCode, error);
                return $"Error: AI generation failed ({response.StatusCode}).";
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(responseString);

            // Navigate: candidates[0].content.parts[0].text
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                    contentElem.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString();
                    return text ?? string.Empty;
                }
            }

            _logger.LogWarning("Gemini API response did not contain expected text structure.");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Gemini text generation.");
            return "Error: AI generation encountered an exception.";
        }
    }

    private async Task<string?> GetApiKeyAsync(CancellationToken cancellationToken)
    {
        // Reuse the logic from GeminiEmbeddingService to fetch from DB
        var config = await _dbContext.ServiceConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ServiceName == "Gemini", cancellationToken);

        return config?.ApiKey;
    }
}
