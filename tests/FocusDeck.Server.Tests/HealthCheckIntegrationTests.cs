using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FocusDeck.Server.Tests;

/// <summary>
/// Integration tests for the /v1/system/health endpoint
/// </summary>
public class HealthCheckIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                    // Use in-memory database for tests
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                    // Configure data path to a temp directory for tests
                    ["Storage:DataPath"] = Path.Combine(Path.GetTempPath(), "focusdeck-test-data")
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthStatus()
    {
        // Act
        var response = await _client.GetAsync("/v1/system/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        Assert.Contains("status", content.ToLower());
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsJsonResponse()
    {
        // Act
        var response = await _client.GetAsync("/v1/system/health");

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task HealthEndpoint_DoesNotRequireAuthentication()
    {
        // Arrange - No authentication header

        // Act
        var response = await _client.GetAsync("/v1/system/health");

        // Assert - Should not return 401 Unauthorized
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_IncludesHealthChecks()
    {
        // Act
        var response = await _client.GetAsync("/v1/system/health");
        var json = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        Assert.NotNull(json);
        Assert.NotNull(json.Status);
        Assert.NotNull(json.Checks);
        Assert.NotEmpty(json.Checks);
        
        // Verify database check exists
        var dbCheck = json.Checks.FirstOrDefault(c => c.Name == "database");
        Assert.NotNull(dbCheck);
        Assert.NotNull(dbCheck.Status);
    }

    [Fact]
    public async Task HealthEndpoint_IncludesTotalDuration()
    {
        // Act
        var response = await _client.GetAsync("/v1/system/health");
        var json = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        Assert.NotNull(json);
        Assert.True(json.TotalDuration >= 0, "TotalDuration should be non-negative");
    }
}

/// <summary>
/// DTO for health check response deserialization
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public List<HealthCheckEntry> Checks { get; set; } = new();
    public double TotalDuration { get; set; }
}

/// <summary>
/// DTO for individual health check entry
/// </summary>
public class HealthCheckEntry
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Duration { get; set; }
}
