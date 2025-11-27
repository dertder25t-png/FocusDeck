using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Server.Services.Context
{
    public class GeminiEmbeddingService : IEmbeddingGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly FocusDeck.Persistence.AutomationDbContext _dbContext;
        private readonly ILogger<GeminiEmbeddingService> _logger;

        // "text-embedding-004" output dimension is 768
        public int Dimensions => 768;
        public string ModelName => "gemini-text-embedding-004";

        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:batchEmbedContents";

        public GeminiEmbeddingService(
            HttpClient httpClient,
            FocusDeck.Persistence.AutomationDbContext dbContext,
            ILogger<GeminiEmbeddingService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string inputText)
        {
            var batch = await GenerateBatchEmbeddingsAsync(new[] { inputText });
            return batch.FirstOrDefault() ?? new float[Dimensions];
        }

        public async Task<List<float[]>> GenerateBatchEmbeddingsAsync(IEnumerable<string> inputs)
        {
            var apiKey = await GetApiKeyAsync();
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Gemini API Key is missing. Returning zero vectors.");
                // Fallback or throw? User said to store key in DB. If missing, we can't proceed.
                // Returning empty/zeros might be safer than crashing the job, but throwing makes it obvious.
                // I'll throw to ensure it's visible in job logs.
                throw new InvalidOperationException("Gemini API Key is not configured.");
            }

            var requestUrl = $"{BaseUrl}?key={apiKey}";

            // Gemini Batch Payload:
            // {
            //   "requests": [
            //     { "model": "models/text-embedding-004", "content": { "parts": [ { "text": "..." } ] } },
            //     ...
            //   ]
            // }

            var requestBody = new
            {
                requests = inputs.Select(text => new
                {
                    model = "models/text-embedding-004",
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = text }
                        }
                    }
                }).ToArray()
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API failed: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"Gemini API Error: {response.StatusCode} - {error}");
            }

            var responseString = await response.Content.ReadAsStringAsync();

            // Response format:
            // {
            //   "embeddings": [
            //     { "values": [ ... ] },
            //     ...
            //   ]
            // }

            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("embeddings", out var embeddingsElement))
            {
                var result = new List<float[]>();
                foreach (var embedding in embeddingsElement.EnumerateArray())
                {
                    if (embedding.TryGetProperty("values", out var valuesElement))
                    {
                        var vector = valuesElement.EnumerateArray().Select(x => x.GetSingle()).ToArray();
                        result.Add(vector);
                    }
                    else
                    {
                         // Should not happen if success
                        result.Add(new float[Dimensions]);
                    }
                }
                return result;
            }

            return new List<float[]>();
        }

        private async Task<string?> GetApiKeyAsync()
        {
            // Assuming we use a standard key for the configuration
            var config = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                System.Linq.Queryable.Where(_dbContext.ServiceConfigurations, c => c.ServiceName == "Gemini"));

            return config?.ApiKey;
        }
    }
}
