using FocusDeck.Server.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FocusDeck.Server.Services.Auth
{
    public class GoogleAuthService
    {
        private readonly GoogleOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(IOptions<GoogleOptions> options, IHttpClientFactory httpClientFactory, ILogger<GoogleAuthService> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string GetAuthorizationUrl(string state)
        {
            var scope = "https://www.googleapis.com/auth/calendar.readonly https://www.googleapis.com/auth/userinfo.email";
            return $"https://accounts.google.com/o/oauth2/v2/auth?client_id={_options.ClientId}&redirect_uri={_options.RedirectUri}&response_type=code&scope={scope}&access_type=offline&state={state}&prompt=consent";
        }

        public async Task<GoogleTokenResponse?> ExchangeCodeForTokenAsync(string code)
        {
            var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
                new KeyValuePair<string, string>("redirect_uri", _options.RedirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            var response = await client.PostAsync("https://oauth2.googleapis.com/token", content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to exchange Google code: {Error}", error);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GoogleTokenResponse>(json);
        }
    }

    public class GoogleTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}
