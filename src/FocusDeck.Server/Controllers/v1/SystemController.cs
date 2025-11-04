using Asp.Versioning;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/system")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public SystemController(ILogger<SystemController> logger)
    {
        _logger = logger;
    }

    [HttpGet("info")]
    [AllowAnonymous]
    public IActionResult GetSystemInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "Unknown";
        
        // Get git SHA from environment or assembly info
        var gitSha = Environment.GetEnvironmentVariable("GIT_SHA") 
            ?? assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion 
            ?? "dev";
        
        var uptime = DateTime.UtcNow - _startTime;
        
        // Get Hangfire stats
        var monitoring = JobStorage.Current?.GetMonitoringApi();
        var stats = monitoring?.GetStatistics();
        
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        
        return Ok(new
        {
            version,
            gitSha = gitSha.Length > 7 ? gitSha.Substring(0, 7) : gitSha,
            uptime = new
            {
                days = uptime.Days,
                hours = uptime.Hours,
                minutes = uptime.Minutes,
                seconds = uptime.Seconds,
                totalSeconds = (int)uptime.TotalSeconds
            },
            queue = new
            {
                enqueued = stats?.Enqueued ?? 0,
                scheduled = stats?.Scheduled ?? 0,
                processing = stats?.Processing ?? 0,
                failed = stats?.Failed ?? 0
            },
            environment,
            serverTime = DateTime.UtcNow
        });
    }
}
