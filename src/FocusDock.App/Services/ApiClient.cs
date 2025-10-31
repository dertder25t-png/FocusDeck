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

        public ApiClient()
        {
            _httpClient = new HttpClient();
        }

        public void SetServerUrl(string serverUrl)
        {
            if (!string.IsNullOrWhiteSpace(serverUrl))
            {
                _serverUrl = serverUrl;
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
            return JsonSerializer.Deserialize<Deck>(responseContent, options);
        }

        public async Task UpdateDeckAsync(Guid id, Deck updatedDeck)
        {
            var json = JsonSerializer.Serialize(updatedDeck);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_serverUrl}/api/decks/{id}", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteDeckAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"{_serverUrl}/api/decks/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
