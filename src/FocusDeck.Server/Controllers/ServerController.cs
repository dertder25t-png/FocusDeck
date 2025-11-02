using FocusDeck.Server.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServerController : ControllerBase
    {
        private const string UpdateLogPath = "/var/log/focusdeck/update.log";
        private readonly ILogger<ServerController> _logger;
        private readonly IServerUpdateService _updateService;

        public ServerController(ILogger<ServerController> logger, IServerUpdateService updateService)
        {
            _logger = logger;
            _updateService = updateService;
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateServer()
        {
            var result = await _updateService.TriggerUpdateAsync(HttpContext.RequestAborted);
            if (!result.Success && !result.IsUpdating)
            {
                return BadRequest(new { message = result.Message, result.IsUpdating });
            }

            return Ok(new { message = result.Message, result.IsUpdating });
        }

        [HttpGet("update-status")]
        public async Task<IActionResult> GetUpdateStatus()
        {
            if (!OperatingSystem.IsLinux())
            {
                return BadRequest(new { message = "Server update is only supported on Linux." });
            }

            if (!System.IO.File.Exists(UpdateLogPath))
            {
                return Ok(new { status = "no_updates", message = "No update log found." });
            }

            var lines = await System.IO.File.ReadAllLinesAsync(UpdateLogPath, HttpContext.RequestAborted);
            var recent = lines.TakeLast(20).ToArray();
            var lastLine = recent.LastOrDefault() ?? string.Empty;

            string status = "unknown";
            if (lastLine.Contains("Update Completed", StringComparison.OrdinalIgnoreCase) ||
                lastLine.Contains("Update Complete", StringComparison.OrdinalIgnoreCase))
            {
                status = "completed";
            }
            else if (recent.Any(line => line.Contains("ERROR", StringComparison.OrdinalIgnoreCase)))
            {
                status = "error";
            }
            else if (recent.Any(line => line.Contains("Step", StringComparison.OrdinalIgnoreCase)))
            {
                status = "in_progress";
            }

            return Ok(new
            {
                status,
                lastUpdate = lastLine,
                recentLogs = recent.TakeLast(10).ToArray()
            });
        }

        [HttpGet("check-updates")]
        public async Task<IActionResult> CheckForUpdates()
        {
            var result = await _updateService.CheckForUpdatesAsync(HttpContext.RequestAborted);
            return Ok(new
            {
                updateAvailable = result.UpdateAvailable,
                message = result.Message,
                currentCommit = result.CurrentCommit,
                currentDate = result.CurrentDate,
                latestCommit = result.LatestCommit,
                latestDate = result.LatestDate,
                latestMessage = result.LatestMessage
            });
        }

        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            var repoPath = Environment.GetEnvironmentVariable("FOCUSDECK_REPO") ?? "/home/focusdeck/FocusDeck";
            var commit = await TryGetCurrentCommitAsync(repoPath, HttpContext.RequestAborted);

            return Ok(new
            {
                status = "healthy",
                version = commit,
                timestamp = DateTime.UtcNow,
                platform = RuntimeInformation.OSDescription,
                uptime = Environment.TickCount64 / 1000
            });
        }

        private static async Task<string> TryGetCurrentCommitAsync(string repoPath, CancellationToken cancellationToken)
        {
            if (!OperatingSystem.IsLinux() || !Directory.Exists(repoPath))
            {
                return "unknown";
            }

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"-C \"{repoPath}\" rev-parse --short=7 HEAD",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
#if NET8_0_OR_GREATER
                await process.WaitForExitAsync(cancellationToken);
#else
                await process.WaitForExitAsync();
#endif
                return output.Trim().Length > 0 ? output.Trim() : "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
