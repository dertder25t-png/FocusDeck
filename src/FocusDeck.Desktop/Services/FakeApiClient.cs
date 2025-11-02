using System.Text.Json;

namespace FocusDeck.Desktop.Services;

/// <summary>
/// Fake API client that returns canned data for development without a backend server.
/// </summary>
public class FakeApiClient : IApiClient
{
    private readonly Dictionary<string, object> _cannedData;
    private readonly JsonSerializerOptions _jsonOptions;

    public string? AccessToken { get; set; }

    public FakeApiClient()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _cannedData = new Dictionary<string, object>
        {
            // Auth endpoints
            ["/v1/auth/login"] = new
            {
                accessToken = "fake-access-token-12345",
                refreshToken = "fake-refresh-token-67890",
                expiresIn = 3600
            },

            // Notes endpoints
            ["/v1/notes"] = new[]
            {
                new
                {
                    id = "note-1",
                    title = "Introduction to Domain-Driven Design",
                    content = "DDD is an approach to software development that centers the development on programming a domain model...",
                    tags = new[] { "architecture", "ddd", "design" },
                    createdAt = DateTime.UtcNow.AddDays(-5),
                    updatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new
                {
                    id = "note-2",
                    title = "Clean Architecture Principles",
                    content = "The core principles include: separation of concerns, dependency inversion, and testability...",
                    tags = new[] { "architecture", "clean-code" },
                    createdAt = DateTime.UtcNow.AddDays(-3),
                    updatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new
                {
                    id = "note-3",
                    title = "SOLID Principles",
                    content = "Single Responsibility, Open-Closed, Liskov Substitution, Interface Segregation, Dependency Inversion",
                    tags = new[] { "principles", "oop" },
                    createdAt = DateTime.UtcNow.AddDays(-7),
                    updatedAt = DateTime.UtcNow.AddDays(-6)
                }
            },

            // Study sessions endpoints
            ["/v1/study-sessions"] = new[]
            {
                new
                {
                    id = "session-1",
                    title = "Morning Review - Architecture Patterns",
                    startTime = DateTime.UtcNow.AddHours(-2),
                    endTime = DateTime.UtcNow.AddHours(-1),
                    durationMinutes = 60,
                    completed = true
                },
                new
                {
                    id = "session-2",
                    title = "Afternoon Deep Dive - Microservices",
                    startTime = DateTime.UtcNow.AddMinutes(-30),
                    endTime = (DateTime?)null,
                    durationMinutes = 0,
                    completed = false
                }
            },

            // Automations endpoints
            ["/v1/automations"] = new[]
            {
                new
                {
                    id = "auto-1",
                    name = "Daily Standup Reminder",
                    description = "Sends a notification at 9 AM daily",
                    isActive = true,
                    triggerType = "Schedule",
                    actions = new[] { "SendNotification" }
                },
                new
                {
                    id = "auto-2",
                    name = "End of Day Summary",
                    description = "Creates a summary note at 6 PM",
                    isActive = true,
                    triggerType = "Schedule",
                    actions = new[] { "CreateNote", "SendEmail" }
                }
            },

            // Dashboard/stats
            ["/v1/dashboard/stats"] = new
            {
                totalNotes = 42,
                totalStudySessions = 18,
                activeAutomations = 5,
                totalStudyHours = 127.5
            }
        };
    }

    public Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        // Simulate network delay
        Task.Delay(100, cancellationToken).Wait(cancellationToken);

        if (_cannedData.TryGetValue(endpoint, out var data))
        {
            // Serialize and deserialize to get proper type conversion
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return Task.FromResult(result);
        }

        // Return default for unknown endpoints
        return Task.FromResult(default(T));
    }

    public Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        // Simulate network delay
        Task.Delay(150, cancellationToken).Wait(cancellationToken);

        // For POST requests, return a success response
        if (endpoint.StartsWith("/v1/notes"))
        {
            var response = new
            {
                id = $"note-{Guid.NewGuid():N}",
                title = "New Note",
                content = "Content",
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            };
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            return Task.FromResult(JsonSerializer.Deserialize<T>(json, _jsonOptions));
        }

        if (endpoint.StartsWith("/v1/study-sessions"))
        {
            var response = new
            {
                id = $"session-{Guid.NewGuid():N}",
                title = "New Study Session",
                startTime = DateTime.UtcNow,
                completed = false
            };
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            return Task.FromResult(JsonSerializer.Deserialize<T>(json, _jsonOptions));
        }

        if (endpoint.Contains("/auth/"))
        {
            // Return auth response from canned data
            return GetAsync<T>("/v1/auth/login", cancellationToken);
        }

        return Task.FromResult(default(T));
    }

    public Task<T?> PutAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        // Simulate network delay
        Task.Delay(150, cancellationToken).Wait(cancellationToken);

        // For PUT requests, echo back the data
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        return Task.FromResult(JsonSerializer.Deserialize<T>(json, _jsonOptions));
    }

    public Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        // Simulate network delay
        Task.Delay(100, cancellationToken).Wait(cancellationToken);

        // Always succeed for delete operations
        return Task.FromResult(true);
    }
}
