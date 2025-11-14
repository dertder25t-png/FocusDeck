using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace FocusDeck.Server.Tests;

/// <summary>
/// Custom WebApplicationFactory that bootstraps the host via TestServerProgram.CreateHostBuilder
/// instead of relying on entry point discovery. This keeps tests stable while leaving
/// the production Program.cs minimal-hosting entry point unchanged.
/// </summary>
public class FocusDeckWebApplicationFactory : WebApplicationFactory<TestServerProgram>
{
    protected override IHostBuilder CreateHostBuilder()
    {
        return TestServerProgram.CreateHostBuilder(Array.Empty<string>());
    }
}

