using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDeck.Shared.Models;

namespace FocusDock.App.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private string _serverUrl = "http://localhost:5000"; // Default for local testing, will be configurable
        private string? _jwtToken;
        private SyncClientService? _sync;

        public ApiClient()
        {
            _httpClient = new HttpClient();
        }

        public void SetSyncService(SyncClientService sync)
        {
            _sync = sync;
        }

        public void SetServerUrl(string serverUrl)
        {
            if (!string.IsNullOrWhiteSpace(serverUrl))
            {
                _serverUrl = serverUrl;
            }
        }

        public void SetJwtToken(string? jwtToken)
        {
            _jwtToken = jwtToken;
            
            // Update the HttpClient's default Authorization header
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrWhiteSpace(_jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwtToken}");
            }
        }

        public async Task<List<Deck>> GetDecksAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/api/decks");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Deck>>(content, options) ?? new List<Deck>();
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., server not reachable, deserialization error)
                Console.WriteLine($"Error getting decks: {ex.Message}");
                return new List<Deck>();
            }
        }

        public async Task<Deck> CreateDeckAsync(Deck newDeck)
        {
            var json = JsonSerializer.Serialize(newDeck);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_serverUrl}/api/decks", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var created = JsonSerializer.Deserialize<Deck>(responseContent, options);
            if (created != null)
            {
                _sync?.TrackDeckCreated(created.Id, created);
                if (_sync != null) await _sync.PushAsync(_sync.ChangeTracker);
            }
            return created!;
        }

        public async Task UpdateDeckAsync(Guid id, Deck updatedDeck)
        {
            var json = JsonSerializer.Serialize(updatedDeck);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_serverUrl}/api/decks/{id}", content);
            response.EnsureSuccessStatusCode();
            _sync?.TrackDeckUpdated(id, updatedDeck);
            if (_sync != null) await _sync.PushAsync(_sync.ChangeTracker);
        }

        public async Task DeleteDeckAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"{_serverUrl}/api/decks/{id}");
            response.EnsureSuccessStatusCode();
            _sync?.TrackDeckDeleted(id);
            if (_sync != null) await _sync.PushAsync(_sync.ChangeTracker);
        }
    }
}
