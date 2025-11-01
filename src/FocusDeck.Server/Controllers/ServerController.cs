using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServerController : ControllerBase
    {
        private readonly ILogger<ServerController> _logger;

        public ServerController(ILogger<ServerController> logger)
        {
            _logger = logger;
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateServer()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return BadRequest(new { message = "Server update is only supported on Linux." });
            }

            try
            {
                _logger.LogInformation("Update request received. Starting update process...");

                // Configuration
                var repoPath = Environment.GetEnvironmentVariable("FOCUSDECK_REPO") ?? "/home/focusdeck/FocusDeck";
                var appPath = "/opt/focusdeck";
                var logFile = "/var/log/focusdeck/update.log";

                // Create update script
                var scriptPath = Path.Combine("/tmp", $"focusdeck-update-{DateTime.Now:yyyyMMddHHmmss}.sh");
                var scriptContent = $@"#!/bin/bash
# FocusDeck Server Update Script
# Generated: {DateTime.Now}
# Log file: {logFile}

LOG_FILE=""{logFile}""
REPO_PATH=""{repoPath}""
APP_PATH=""{appPath}""

log() {{
    echo ""$(date '+%Y-%m-%d %H:%M:%S') - $1"" | tee -a ""$LOG_FILE""
}}

log ""========================================""
log ""FocusDeck Server Update Started""
log ""========================================""

# Ensure log directory exists
sudo mkdir -p ""$(dirname ""$LOG_FILE"")""
sudo chown focusdeck:focusdeck ""$(dirname ""$LOG_FILE"")""

# Step 1: Navigate to repository
log ""Step 1: Navigating to repository: $REPO_PATH""
if [ ! -d ""$REPO_PATH"" ]; then
    log ""ERROR: Repository path does not exist: $REPO_PATH""
    exit 1
fi
cd ""$REPO_PATH"" || {{ log ""ERROR: Failed to change directory to $REPO_PATH""; exit 1; }}

# Step 2: Pull latest changes from GitHub
log ""Step 2: Pulling latest changes from GitHub...""
git fetch origin 2>&1 | tee -a ""$LOG_FILE""
git reset --hard origin/master 2>&1 | tee -a ""$LOG_FILE""
if [ $? -ne 0 ]; then
    log ""ERROR: Failed to pull from git""
    exit 1
fi
log ""SUCCESS: Git pull completed""

# Step 3: Build and publish application
log ""Step 3: Building and publishing application...""
cd ""$REPO_PATH/src/FocusDeck.Server"" || {{ log ""ERROR: Failed to navigate to server directory""; exit 1; }}
dotnet publish FocusDeck.Server.csproj -c Release -r linux-x64 --self-contained false -o ""$APP_PATH"" 2>&1 | tee -a ""$LOG_FILE""
if [ $? -ne 0 ]; then
    log ""ERROR: Failed to publish application""
    exit 1
fi
log ""SUCCESS: Application published to $APP_PATH""

# Step 4: Set permissions
log ""Step 4: Setting permissions...""
sudo chown -R focusdeck:focusdeck ""$APP_PATH""
sudo chmod +x ""$APP_PATH/FocusDeck.Server""
log ""SUCCESS: Permissions set""

# Step 5: Restart service
log ""Step 5: Restarting FocusDeck service...""
sudo systemctl restart focusdeck 2>&1 | tee -a ""$LOG_FILE""
if [ $? -ne 0 ]; then
    log ""ERROR: Failed to restart service""
    exit 1
fi

# Wait a moment for service to start
sleep 2

# Check service status
log ""Step 6: Checking service status...""
if sudo systemctl is-active --quiet focusdeck; then
    log ""SUCCESS: FocusDeck service is running""
else
    log ""WARNING: Service may not have started properly. Check: sudo systemctl status focusdeck""
fi

log ""========================================""
log ""FocusDeck Server Update Complete""
log ""========================================""
log ""View logs: journalctl -u focusdeck -f""
log ""Service status: systemctl status focusdeck""

# Clean up this script
rm -f ""$0""
";

                // Write script to temp file
                await System.IO.File.WriteAllTextAsync(scriptPath, scriptContent.Replace("\r\n", "\n"));
                _logger.LogInformation($"Update script created at: {scriptPath}");

                // Make script executable
                var chmodProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x {scriptPath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                chmodProcess.Start();
                await chmodProcess.WaitForExitAsync();

                // Execute update script in background using nohup
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nohup",
                        Arguments = $"/bin/bash {scriptPath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();
                _logger.LogInformation("Update script launched in background");

                // Don't wait for exit - the script will restart the server
                return Ok(new 
                { 
                    message = "Server update started! The server will restart in about 30 seconds. Please wait and then refresh your browser.",
                    logFile = logFile,
                    estimatedTime = "30-60 seconds"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during server update");
                return StatusCode(500, new { message = $"Update failed: {ex.Message}" });
            }
        }

        [HttpGet("update-status")]
        public async Task<IActionResult> GetUpdateStatus()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return BadRequest(new { message = "Server update is only supported on Linux." });
            }

            try
            {
                var logFile = "/var/log/focusdeck/update.log";
                
                if (!System.IO.File.Exists(logFile))
                {
                    return Ok(new { status = "no_updates", message = "No update log found" });
                }

                // Read last 50 lines of log file
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "tail",
                        Arguments = $"-n 50 {logFile}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var lastLine = lines.LastOrDefault() ?? "";

                string status = "unknown";
                if (lastLine.Contains("Update Complete"))
                    status = "completed";
                else if (lastLine.Contains("ERROR"))
                    status = "error";
                else if (lastLine.Contains("Step"))
                    status = "in_progress";

                return Ok(new 
                { 
                    status = status,
                    lastUpdate = lastLine,
                    recentLogs = lines.TakeLast(10).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking update status");
                return StatusCode(500, new { message = $"Failed to check status: {ex.Message}" });
            }
        }

        [HttpGet("check-updates")]
        public async Task<IActionResult> CheckForUpdates()
        {
            try
            {
                _logger.LogInformation("Checking for updates from GitHub...");

                // Get current local commit
                string currentCommit = "unknown";
                string currentCommitDate = "unknown";
                
                var repoPath = Environment.GetEnvironmentVariable("FOCUSDECK_REPO") ?? "/home/focusdeck/FocusDeck";
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Directory.Exists(repoPath))
                {
                    // Get current commit hash
                    var commitProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = "-C " + repoPath + " rev-parse HEAD",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    commitProcess.Start();
                    currentCommit = (await commitProcess.StandardOutput.ReadToEndAsync()).Trim();
                    await commitProcess.WaitForExitAsync();

                    // Get commit date
                    var dateProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = "-C " + repoPath + " log -1 --format=%cd --date=iso",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    dateProcess.Start();
                    currentCommitDate = (await dateProcess.StandardOutput.ReadToEndAsync()).Trim();
                    await dateProcess.WaitForExitAsync();
                }

                // Fetch latest commit from GitHub API
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "FocusDeck-Server");
                
                var response = await httpClient.GetAsync("https://api.github.com/repos/dertder25t-png/FocusDeck/commits/master");
                
                if (!response.IsSuccessStatusCode)
                {
                    return Ok(new 
                    { 
                        updateAvailable = false,
                        message = "Unable to check GitHub for updates",
                        currentCommit = currentCommit.Substring(0, Math.Min(7, currentCommit.Length)),
                        currentDate = currentCommitDate
                    });
                }

                var json = await response.Content.ReadAsStringAsync();
                var githubData = System.Text.Json.JsonDocument.Parse(json);
                var latestCommit = githubData.RootElement.GetProperty("sha").GetString() ?? "";
                var latestDate = githubData.RootElement.GetProperty("commit").GetProperty("committer").GetProperty("date").GetString() ?? "";
                var latestMessage = githubData.RootElement.GetProperty("commit").GetProperty("message").GetString() ?? "";

                bool updateAvailable = !currentCommit.Equals(latestCommit, StringComparison.OrdinalIgnoreCase);

                return Ok(new 
                { 
                    updateAvailable = updateAvailable,
                    currentCommit = currentCommit.Substring(0, Math.Min(7, currentCommit.Length)),
                    currentDate = currentCommitDate,
                    latestCommit = latestCommit.Substring(0, Math.Min(7, latestCommit.Length)),
                    latestDate = latestDate,
                    latestMessage = latestMessage,
                    message = updateAvailable ? "Updates available!" : "You're up to date!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                return Ok(new 
                { 
                    updateAvailable = false,
                    message = "Error checking for updates: " + ex.Message
                });
            }
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new 
            { 
                status = "healthy",
                timestamp = DateTime.UtcNow,
                platform = RuntimeInformation.OSDescription,
                uptime = Environment.TickCount64 / 1000
            });
        }
    }
}
