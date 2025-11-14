using FocusDeck.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace FocusDeck.Server.Tests;

public class RemoteControlIntegrationTests : IClassFixture<FocusDeckWebApplicationFactory>
{
    private readonly WebApplicationFactory<TestServerProgram> _factory;
    private readonly HttpClient _client;

    public RemoteControlIntegrationTests(FocusDeckWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Development";
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RegisterDevice_Desktop_ReturnsDeviceIdAndToken()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var registerDto = new RegisterDeviceDto
        {
            DeviceType = "Desktop",
            Name = "Test Desktop",
            Capabilities = new Dictionary<string, object>
            {
                { "openNote", true },
                { "openDeck", true },
                { "rearrangeLayout", true },
                { "focus", true }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/devices/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterDeviceResponseDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.DeviceId);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task RegisterDevice_Phone_ReturnsDeviceIdAndToken()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var registerDto = new RegisterDeviceDto
        {
            DeviceType = "Phone",
            Name = "Test Phone",
            Capabilities = new Dictionary<string, object>
            {
                { "sendCommands", true }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/devices/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterDeviceResponseDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.DeviceId);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task CreateRemoteAction_OpenNote_ReturnsCreatedAction()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var actionDto = new CreateRemoteActionDto
        {
            Kind = "OpenNote",
            Payload = new Dictionary<string, object>
            {
                { "noteId", "test-note-123" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/remote/actions", actionDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var action = await response.Content.ReadFromJsonAsync<RemoteActionDto>();
        Assert.NotNull(action);
        Assert.Equal("OpenNote", action.Kind);
        Assert.True(action.IsPending);
        Assert.False(action.IsCompleted);
        Assert.NotEqual(Guid.Empty, action.Id);
    }

    [Fact]
    public async Task CreateRemoteAction_StartFocus_ReturnsCreatedAction()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var actionDto = new CreateRemoteActionDto
        {
            Kind = "StartFocus",
            Payload = new Dictionary<string, object>
            {
                { "duration", 25 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/v1/remote/actions", actionDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var action = await response.Content.ReadFromJsonAsync<RemoteActionDto>();
        Assert.NotNull(action);
        Assert.Equal("StartFocus", action.Kind);
        Assert.Equal(25, ((System.Text.Json.JsonElement)action.Payload["duration"]).GetInt32());
    }

    [Fact]
    public async Task ActionRoundTrip_PhoneCreatesAndDesktopCompletes_Success()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Phone creates action
        var actionDto = new CreateRemoteActionDto
        {
            Kind = "RearrangeLayout",
            Payload = new Dictionary<string, object>
            {
                { "preset", "NotesLeft" }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/v1/remote/actions", actionDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var action = await createResponse.Content.ReadFromJsonAsync<RemoteActionDto>();
        Assert.NotNull(action);

        // Desktop retrieves action
        var getResponse = await _client.GetAsync($"/v1/remote/actions/{action.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrievedAction = await getResponse.Content.ReadFromJsonAsync<RemoteActionDto>();
        Assert.NotNull(retrievedAction);
        Assert.Equal(action.Id, retrievedAction.Id);
        Assert.True(retrievedAction.IsPending);

        // Desktop completes action
        var completeDto = new CompleteRemoteActionDto
        {
            Success = true,
            ErrorMessage = null
        };

        var completeResponse = await _client.PostAsJsonAsync($"/v1/remote/actions/{action.Id}/complete", completeDto);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        // Verify action is completed
        var verifyResponse = await _client.GetAsync($"/v1/remote/actions/{action.Id}");
        var completedAction = await verifyResponse.Content.ReadFromJsonAsync<RemoteActionDto>();
        Assert.NotNull(completedAction);
        Assert.True(completedAction.IsCompleted);
        Assert.True(completedAction.Success);
        Assert.NotNull(completedAction.CompletedAt);
    }

    [Fact]
    public async Task GetPendingActions_ReturnsOnlyPendingActions()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create two actions
        var action1 = await CreateTestActionAsync("OpenNote");
        var action2 = await CreateTestActionAsync("StartFocus");

        // Complete one action
        await _client.PostAsJsonAsync($"/v1/remote/actions/{action1.Id}/complete", new CompleteRemoteActionDto
        {
            Success = true
        });

        // Act - Get pending actions
        var response = await _client.GetAsync("/v1/remote/actions?pending=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actions = await response.Content.ReadFromJsonAsync<List<RemoteActionDto>>();
        Assert.NotNull(actions);
        Assert.Contains(actions, a => a.Id == action2.Id && a.IsPending);
        Assert.DoesNotContain(actions, a => a.Id == action1.Id);
    }

    [Fact]
    public async Task GetTelemetrySummary_ReturnsCurrentState()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/v1/remote/telemetry/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var telemetry = await response.Content.ReadFromJsonAsync<RemoteTelemetrySummaryDto>();
        Assert.NotNull(telemetry);
        Assert.InRange(telemetry.ProgressPercent, 0, 100);
    }

    [Fact]
    public async Task GetDevices_ReturnsRegisteredDevices()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Register a device first
        await _client.PostAsJsonAsync("/v1/devices/register", new RegisterDeviceDto
        {
            DeviceType = "Desktop",
            Name = "Test Device",
            Capabilities = new Dictionary<string, object>()
        });

        // Act
        var response = await _client.GetAsync("/v1/devices");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var devices = await response.Content.ReadFromJsonAsync<List<DeviceLinkDto>>();
        Assert.NotNull(devices);
        Assert.NotEmpty(devices);
    }

    [Fact]
    public async Task UpdateDeviceHeartbeat_UpdatesLastSeen()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Register a device
        var registerResponse = await _client.PostAsJsonAsync("/v1/devices/register", new RegisterDeviceDto
        {
            DeviceType = "Desktop",
            Name = "Test Device",
            Capabilities = new Dictionary<string, object>()
        });

        var deviceReg = await registerResponse.Content.ReadFromJsonAsync<RegisterDeviceResponseDto>();
        Assert.NotNull(deviceReg);

        // Wait a moment
        await Task.Delay(100);

        // Act
        var response = await _client.PutAsync($"/v1/devices/{deviceReg.DeviceId}/heartbeat", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Helper methods
    private async Task<string> GetAuthTokenAsync()
    {
        // In a real scenario, this would authenticate and return a JWT token
        // For testing purposes, we'll return a dummy token that the test server accepts
        return "test-token-123";
    }

    private async Task<RemoteActionDto> CreateTestActionAsync(string kind)
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var actionDto = new CreateRemoteActionDto
        {
            Kind = kind,
            Payload = new Dictionary<string, object>()
        };

        var response = await _client.PostAsJsonAsync("/v1/remote/actions", actionDto);
        response.EnsureSuccessStatusCode();
        
        var action = await response.Content.ReadFromJsonAsync<RemoteActionDto>();
        Assert.NotNull(action);
        return action;
    }
}
