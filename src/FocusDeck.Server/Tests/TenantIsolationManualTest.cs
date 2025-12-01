using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Controllers.v1;
using FocusDeck.Server.Services.Calendar;
using FocusDeck.Server.Services.Jarvis;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FocusDeck.Server.Tests
{
    public class TenantIsolationManualTest
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting Tenant Isolation Verification...");

            var services = new ServiceCollection();

            // 1. Setup InMemory Database
            var dbName = "TenantTestDb_" + Guid.NewGuid();
            services.AddDbContext<AutomationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: dbName));

            services.AddLogging();
            services.AddScoped<ICurrentTenant, MockCurrentTenant>(); // We will implement a mock that we can control

            var provider = services.BuildServiceProvider();

            // 2. Define Tenants
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            // 3. Create Note as Tenant A
            using (var scope = provider.CreateScope())
            {
                var tenantContext = (MockCurrentTenant)scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                tenantContext.SetTenant(tenantA);

                var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

                // Create controller dependencies
                var logger = NullLogger<NotesV1Controller>.Instance;
                var calendarResolver = new CalendarResolver(db, null, null); // Null logger/sources for mock
                var jarvisService = new MockSuggestionService();

                var controller = new NotesV1Controller(db, logger, calendarResolver, jarvisService);

                // Mock User Claims
                var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "UserA"),
                    new Claim("app_tenant_id", tenantA.ToString())
                }));
                controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

                // Action: Create Note
                Console.WriteLine($"User A ({tenantA}) creating note...");
                var result = await controller.StartNote(new CreateNoteDto { Title = "Secret Note A", Content = "For A Only" });

                if (result is not OkObjectResult)
                {
                    Console.WriteLine("FAILED: Could not create note for Tenant A.");
                    return;
                }
            }

            // 4. Verify as Tenant B
            using (var scope = provider.CreateScope())
            {
                var tenantContext = (MockCurrentTenant)scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                tenantContext.SetTenant(tenantB); // Switch to Tenant B

                var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

                var logger = NullLogger<NotesV1Controller>.Instance;
                var calendarResolver = new CalendarResolver(db, null, null);
                var jarvisService = new MockSuggestionService();

                var controller = new NotesV1Controller(db, logger, calendarResolver, jarvisService);

                // Mock User Claims for B
                var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "UserB"),
                    new Claim("app_tenant_id", tenantB.ToString())
                }));
                controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

                // Action: List Notes
                Console.WriteLine($"User B ({tenantB}) listing notes...");
                var result = await controller.ListNotes();

                if (result is OkObjectResult okResult)
                {
                    // Access the anonymous type via reflection or dynamic is tricky, so we rely on ToString or specific property access if possible.
                    // But 'Value' is object.
                    // Let's rely on checking the DB directly with the scope's context which has the filter applied.

                    var visibleNotes = await db.Notes.ToListAsync();
                    var count = visibleNotes.Count;

                    if (count == 0)
                    {
                        Console.WriteLine("SUCCESS: User B sees 0 notes.");
                    }
                    else
                    {
                        Console.WriteLine($"FAILURE: User B sees {count} notes! Isolation Broken.");
                        foreach(var n in visibleNotes)
                        {
                            Console.WriteLine($" - Leaked Note: {n.Title} (Tenant: {n.TenantId})");
                        }
                    }
                }
            }

            Console.WriteLine("Verification Complete.");
        }
    }

    public class MockCurrentTenant : ICurrentTenant
    {
        private Guid? _tenantId;
        public Guid? TenantId => _tenantId;
        public bool HasTenant => _tenantId.HasValue;

        public void SetTenant(Guid id)
        {
            _tenantId = id;
        }
    }

    public class MockSuggestionService : ISuggestionService
    {
        public Task<SuggestionResponseDto?> GenerateSuggestionAsync(SuggestionRequestDto request) => Task.FromResult<SuggestionResponseDto?>(null);
        public Task<List<NoteSuggestion>> AnalyzeNoteAsync(Note note) => Task.FromResult(new List<NoteSuggestion>());
    }
}
