using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UpdateController : ControllerBase
    {
        private readonly ILogger<UpdateController> _logger;
        private readonly IConfiguration _config;
        private static bool _isUpdating = false;
        private static readonly object _updateLock = new object();

        public UpdateController(ILogger<UpdateController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Trigger server update (Linux only)
        /// POST /api/update/trigger
        /// </summary>
        [HttpPost("trigger")]
        public async Task<ActionResult<UpdateResponse>> TriggerUpdate()
        {
            lock (_updateLock)
            {
                if (_isUpdating)
                {
                    return BadRequest(new UpdateResponse
                    {
                        Success = false,
                        Message = "Update already in progress",
                        IsUpdating = true
                    });
                }
                _isUpdating = true;
            }

            try
            {
                if (!OperatingSystem.IsLinux())
                {
                    _isUpdating = false;
                    return BadRequest(new UpdateResponse
                    {
                        Success = false,
                        Message = "Server updates are only available on Linux. For Windows, pull the latest code from GitHub and rebuild manually.",
                        IsUpdating = false
                    });
                }

                var repoPath = Environment.GetEnvironmentVariable("FOCUSDECK_REPO") 
                    ?? "/home/focusdeck/FocusDeck";

                if (!Directory.Exists(repoPath))
                {
                    _isUpdating = false;
                    return BadRequest(new UpdateResponse
                    {
                        Success = false,
                        Message = $"Repository not found at: {repoPath}. Please configure FOCUSDECK_REPO environment variable or run configure-update-system.sh",
                        IsUpdating = false
                    });
                }

                _logger.LogInformation("Starting server update from repository: {RepoPath}", repoPath);

                // Start update process in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteUpdateScript(repoPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Update process failed");
                    }
                    finally
                    {
                        _isUpdating = false;
                    }
                });

                return Ok(new UpdateResponse
                {
                    Success = true,
                    Message = "Update process started. Server will restart in approximately 30-60 seconds.",
                    IsUpdating = true
                });
            }
            catch (Exception ex)
            {
                _isUpdating = false;
                _logger.LogError(ex, "Failed to trigger update");
                return StatusCode(500, new UpdateResponse
                {
                    Success = false,
                    Message = $"Failed to start update: {ex.Message}",
                    IsUpdating = false
                });
            }
        }

        /// <summary>
        /// Get update status
        /// GET /api/update/status
        /// </summary>
        [HttpGet("status")]
        public ActionResult<UpdateStatusResponse> GetStatus()
        {
            return Ok(new UpdateStatusResponse
            {
                IsUpdating = _isUpdating,
                IsLinux = OperatingSystem.IsLinux(),
                RepositoryPath = Environment.GetEnvironmentVariable("FOCUSDECK_REPO") 
                    ?? "/home/focusdeck/FocusDeck",
                ConfigurationStatus = GetConfigurationStatus()
            });
        }

        /// <summary>
        /// Check if update system is configured
        /// GET /api/update/check-config
        /// </summary>
        [HttpGet("check-config")]
        public ActionResult<ConfigCheckResponse> CheckConfiguration()
        {
            if (!OperatingSystem.IsLinux())
            {
                return Ok(new ConfigCheckResponse
                {
                    IsConfigured = false,
                    Message = "Update system is only available on Linux",
                    Platform = "Windows/Other",
                    Checks = new List<ConfigCheck>()
                });
            }

            var repoPath = Environment.GetEnvironmentVariable("FOCUSDECK_REPO") 
                ?? "/home/focusdeck/FocusDeck";

            var checks = new List<ConfigCheck>
            {
                new ConfigCheck
                {
                    Name = "Repository exists",
                    Passed = Directory.Exists(repoPath),
                    Message = Directory.Exists(repoPath) 
                        ? $"Found at {repoPath}" 
                        : $"Not found at {repoPath}"
                },
                new ConfigCheck
                {
                    Name = "Git available",
                    Passed = CommandExists("git"),
                    Message = CommandExists("git") ? "Git is installed" : "Git not found"
                },
                new ConfigCheck
                {
                    Name = "Dotnet SDK available",
                    Passed = CommandExists("dotnet"),
                    Message = CommandExists("dotnet") ? ".NET SDK is installed" : ".NET SDK not found"
                }
            };

            var isConfigured = checks.All(c => c.Passed);
            var message = isConfigured 
                ? "Update system is configured and ready" 
                : "Please run configure-update-system.sh to complete setup";

            return Ok(new ConfigCheckResponse
            {
                IsConfigured = isConfigured,
                Message = message,
                Platform = "Linux",
                RepositoryPath = repoPath,
                Checks = checks
            });
        }

        private string GetConfigurationStatus()
        {
            if (!OperatingSystem.IsLinux())
                return "Not Available (Windows)";

            var repoPath = Environment.GetEnvironmentVariable("FOCUSDECK_REPO") 
                ?? "/home/focusdeck/FocusDeck";

            if (!Directory.Exists(repoPath))
                return "Not Configured - Repository not found";

            if (!CommandExists("git") || !CommandExists("dotnet"))
                return "Incomplete - Missing dependencies";

            return "Configured";
        }

        private bool CommandExists(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = command,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task ExecuteUpdateScript(string repoPath)
        {
            var logPath = "/var/log/focusdeck/update.log";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

            try
            {
                // Ensure log directory exists
                var logDir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logBuilder = new StringBuilder();
                logBuilder.AppendLine($"=== Update Started: {timestamp} ===");

                // Step 1: Git pull
                _logger.LogInformation("Pulling latest code from GitHub...");
                logBuilder.AppendLine("Step 1: Pulling from GitHub...");
                var pullResult = await RunCommand("git", "pull origin master", repoPath);
                logBuilder.AppendLine(pullResult);

                // Step 2: Build server
                _logger.LogInformation("Building FocusDeck.Server...");
                logBuilder.AppendLine("\nStep 2: Building server...");
                var buildResult = await RunCommand("dotnet", 
                    "build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release", 
                    repoPath);
                logBuilder.AppendLine(buildResult);

                // Step 3: Restart service
                _logger.LogInformation("Restarting service...");
                logBuilder.AppendLine("\nStep 3: Restarting service...");
                var restartResult = await RunCommand("sudo", 
                    "systemctl restart focusdeck", 
                    repoPath);
                logBuilder.AppendLine(restartResult);

                logBuilder.AppendLine($"\n=== Update Completed: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} ===");

                // Write log
                await System.IO.File.WriteAllTextAsync(logPath, logBuilder.ToString());

                _logger.LogInformation("Update completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update script execution failed");
                try
                {
                    await System.IO.File.AppendAllTextAsync(logPath, 
                        $"\n\nERROR: {ex.Message}\n{ex.StackTrace}");
                }
                catch { }
                throw;
            }
        }

        private async Task<string> RunCommand(string command, string arguments, string workingDirectory)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var output = new StringBuilder();
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    output.AppendLine(e.Data);
                    _logger.LogInformation("  {Output}", e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    output.AppendLine($"ERROR: {e.Data}");
                    _logger.LogWarning("  {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Command '{command} {arguments}' failed with exit code {process.ExitCode}");
            }

            return output.ToString();
        }
    }

    public class UpdateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public bool IsUpdating { get; set; }
    }

    public class UpdateStatusResponse
    {
        public bool IsUpdating { get; set; }
        public bool IsLinux { get; set; }
        public string RepositoryPath { get; set; } = null!;
        public string ConfigurationStatus { get; set; } = null!;
    }

    public class ConfigCheckResponse
    {
        public bool IsConfigured { get; set; }
        public string Message { get; set; } = null!;
        public string Platform { get; set; } = null!;
        public string? RepositoryPath { get; set; }
        public List<ConfigCheck> Checks { get; set; } = new();
    }

    public class ConfigCheck
    {
        public string Name { get; set; } = null!;
        public bool Passed { get; set; }
        public string Message { get; set; } = null!;
    }
}
