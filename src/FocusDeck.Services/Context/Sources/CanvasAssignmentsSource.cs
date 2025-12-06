using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Services.Privacy;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Persistence;
using FocusDeck.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Services.Context.Sources
{
    public class CanvasAssignmentsSource : IContextSnapshotSource
    {
        private readonly IPrivacyDataNotifier _privacyNotifier;
        private readonly IServiceScopeFactory _scopeFactory;

        public string SourceName => "CanvasAssignments";

        public CanvasAssignmentsSource(IPrivacyDataNotifier privacyNotifier, IServiceScopeFactory scopeFactory)
        {
            _privacyNotifier = privacyNotifier;
            _scopeFactory = scopeFactory;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // We need to resolve scoped services manually since this might be called from a background worker
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var canvasService = scope.ServiceProvider.GetRequiredService<ICanvasService>();

            // Find the connected service for this user
            var userIdStr = userId.ToString();
            var service = await db.ConnectedServices
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userIdStr && s.Service == ServiceType.Canvas, ct);

            if (service == null || !service.IsConfigured)
            {
                return null;
            }

            // Decrypt token
            var token = encryptionService.Decrypt(service.AccessToken);
            string? domain = null;

            if (!string.IsNullOrEmpty(service.MetadataJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(service.MetadataJson);
                    if (doc.RootElement.TryGetProperty("domain", out var d))
                    {
                        domain = d.GetString();
                    }
                }
                catch { /* ignore parse error */ }
            }

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(domain))
            {
                return null;
            }

            // Fetch data
            var assignments = await canvasService.GetUpcomingAssignments(domain, token);

            if (assignments.Count == 0)
            {
                return null;
            }

            // Map to ContextSlice
            // We'll just take the next upcoming assignment for the "Context"
            var nextAssignment = assignments.OrderBy(a => a.DueAt).FirstOrDefault();

            if (nextAssignment == null) return null;

            var data = new JsonObject
            {
                ["assignment"] = nextAssignment.Name,
                ["course"] = nextAssignment.CourseName,
                ["dueDate"] = nextAssignment.DueAt?.ToString("o"),
                ["assignmentId"] = nextAssignment.Id,
                ["courseId"] = nextAssignment.CourseId
            };

            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.CanvasAssignments,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            // Send the data to the privacy notifier
            await _privacyNotifier.SendPrivacyDataAsync(userId.ToString(), "CanvasAssignments", data.ToJsonString(), ct);

            return slice;
        }
    }
}
