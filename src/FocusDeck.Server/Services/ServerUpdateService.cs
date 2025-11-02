using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using FocusDeck.Server.Controllers.Models;

namespace FocusDeck.Server.Services
{
    public interface IServerUpdateService
    {
        Task<UpdateResponse> TriggerUpdateAsync(CancellationToken cancellationToken);
        UpdateStatusResponse GetStatus();
        Task<ConfigCheckResponse> CheckConfigurationAsync(CancellationToken cancellationToken);
        Task<UpdateAvailabilityResult> CheckForUpdatesAsync(CancellationToken cancellationToken);
    }

    public sealed class ServerUpdateService : IServerUpdateService
    {
        private const string DefaultRepoPath = "/home/focusdeck/FocusDeck";
        private const string ServiceName = "focusdeck";
        private const string LogDirectory = "/var/log/focusdeck";
        private const string LogFileName = "update.log";

        private readonly ILogger<ServerUpdateService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SemaphoreSlim _updateSemaphore = new(1, 1);

        private volatile bool _isUpdating;

        public ServerUpdateService(
            ILogger<ServerUpdateService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UpdateResponse> TriggerUpdateAsync(CancellationToken cancellationToken)
        {
            if (!OperatingSystem.IsLinux())
            {
                return new UpdateResponse
                {
                    Success = false,
                    Message = "Server updates are only available on Linux. For Windows, pull the latest code and rebuild manually.",
                    IsUpdating = false
                };
            }

            if (!await _updateSemaphore.WaitAsync(0, cancellationToken))
            {
                return new UpdateResponse
                {
                    Success = false,
                    Message = "An update is already in progress.",
                    IsUpdating = true
                };
            }

            _isUpdating = true;

            try
            {
                var repoPath = GetRepositoryPath();
                if (!Directory.Exists(repoPath))
                {
                    _updateSemaphore.Release();
                    _isUpdating = false;

                    return new UpdateResponse
                    {
                        Success = false,
                        Message = $"Repository not found at: {repoPath}. Configure FOCUSDECK_REPO or run configure-update-system.sh",
                        IsUpdating = false
                    };
                }

                _logger.LogInformation("Starting server update from repository: {RepoPath}", repoPath);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteUpdateScriptAsync(repoPath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Update process failed");
                    }
                    finally
                    {
                        _isUpdating = false;
                        _updateSemaphore.Release();
                    }
                }, CancellationToken.None);

                return new UpdateResponse
                {
                    Success = true,
                    Message = "Update process started. The server will restart in approximately 30–60 seconds.",
                    IsUpdating = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger update");
                _isUpdating = false;
                _updateSemaphore.Release();

                return new UpdateResponse
                {
                    Success = false,
                    Message = $"Failed to start update: {ex.Message}",
                    IsUpdating = false
                };
            }
        }

        public UpdateStatusResponse GetStatus()
        {
            return new UpdateStatusResponse
            {
                IsUpdating = _isUpdating,
                IsLinux = OperatingSystem.IsLinux(),
                RepositoryPath = GetRepositoryPath(),
                ConfigurationStatus = GetConfigurationStatus(),
                LastUpdateLog = GetLastUpdateLog()
            };
        }

        public async Task<ConfigCheckResponse> CheckConfigurationAsync(CancellationToken cancellationToken)
        {
            var checks = new List<ConfigCheck>();
            var repoPath = GetRepositoryPath();
            var logDir = LogDirectory;

            var isLinux = OperatingSystem.IsLinux();

            checks.Add(new ConfigCheck
            {
                Name = "Operating System",
                Passed = isLinux,
                Message = isLinux ? "Linux detected" : "Linux is required for automated updates"
            });

            if (isLinux)
            {
                checks.Add(await VerifyCommandAsync("git --version", "Git installed", "Git is required to pull updates", cancellationToken));
                checks.Add(await VerifyCommandAsync("dotnet --version", "dotnet CLI installed", ".NET SDK is required to build the server", cancellationToken));
                checks.Add(await VerifyCommandAsync("systemctl --version", "systemctl available", "systemctl access is required to restart the service", cancellationToken));

                var repoExists = Directory.Exists(repoPath);
                checks.Add(new ConfigCheck
                {
                    Name = "Repository Path",
                    Passed = repoExists,
                    Message = repoExists
                        ? $"Repository found at {repoPath}"
                        : $"Repository not found at {repoPath}"
                });

                var logDirExists = Directory.Exists(logDir);
                checks.Add(new ConfigCheck
                {
                    Name = "Log Directory",
                    Passed = logDirExists,
                    Message = logDirExists
                        ? $"Log directory present at {logDir}"
                        : $"Log directory missing: {logDir}"
                });
            }

            return new ConfigCheckResponse
            {
                IsConfigured = checks.All(c => c.Passed),
                Message = checks.All(c => c.Passed)
                    ? "Update system is ready."
                    : "Update system needs attention. See checks for details.",
                Platform = RuntimeInformation.OSDescription,
                RepositoryPath = repoPath,
                Checks = checks
            };
        }

        public async Task<UpdateAvailabilityResult> CheckForUpdatesAsync(CancellationToken cancellationToken)
        {
            var result = new UpdateAvailabilityResult();
            var repoPath = GetRepositoryPath();

            try
            {
                if (OperatingSystem.IsLinux() && Directory.Exists(repoPath))
                {
                    result.CurrentCommit = await RunGitCommandAsync($"-C \"{repoPath}\" rev-parse HEAD", cancellationToken);
                    result.CurrentDate = await RunGitCommandAsync($"-C \"{repoPath}\" log -1 --format=%cd --date=iso", cancellationToken);
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FocusDeck-Server");

                using var response = await httpClient.GetAsync("https://api.github.com/repos/dertder25t-png/FocusDeck/commits/master", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    result.Message = "Unable to reach GitHub to check for updates.";
                    return result;
                }

                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                using var githubData = System.Text.Json.JsonDocument.Parse(payload);

                result.LatestCommit = githubData.RootElement.GetProperty("sha").GetString() ?? string.Empty;
                result.LatestDate = githubData.RootElement.GetProperty("commit").GetProperty("committer").GetProperty("date").GetString() ?? string.Empty;
                result.LatestMessage = githubData.RootElement.GetProperty("commit").GetProperty("message").GetString() ?? string.Empty;

                result.UpdateAvailable = !string.IsNullOrWhiteSpace(result.CurrentCommit) &&
                                          !result.CurrentCommit.Equals(result.LatestCommit, StringComparison.OrdinalIgnoreCase);

                result.CurrentCommit = TrimCommit(result.CurrentCommit);
                result.LatestCommit = TrimCommit(result.LatestCommit);
                result.Message = result.UpdateAvailable ? "Updates available!" : "You're up to date.";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking for updates");
                result.Message = $"Error checking for updates: {ex.Message}";
                return result;
            }
        }

        private async Task ExecuteUpdateScriptAsync(string repoPath, CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var logDir = LogDirectory;
            var logPath = Path.Combine(logDir, LogFileName);

            Directory.CreateDirectory(logDir);

            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"=== Update Started: {timestamp} ===");

            try
            {
                logBuilder.AppendLine("Step 1: Pulling from GitHub...");
                var pullResult = await RunCommandAsync("git", "pull origin master", repoPath, cancellationToken);
                logBuilder.AppendLine(pullResult);

                logBuilder.AppendLine("\nStep 2: Building server (Release)...");
                var buildResult = await RunCommandAsync(
                    "dotnet",
                    "build src/FocusDeck.Server/FocusDeck.Server.csproj -c Release",
                    repoPath,
                    cancellationToken);
                logBuilder.AppendLine(buildResult);

                logBuilder.AppendLine("\nStep 3: Restarting FocusDeck service...");
                var restartResult = await RunCommandAsync("sudo", $"systemctl restart {ServiceName}", repoPath, cancellationToken);
                logBuilder.AppendLine(restartResult);

                logBuilder.AppendLine($"\n=== Update Completed: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} ===");
                await File.WriteAllTextAsync(logPath, logBuilder.ToString(), cancellationToken);
                _logger.LogInformation("Update completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update script execution failed");
                logBuilder.AppendLine($"\nERROR: {ex.Message}\n{ex.StackTrace}");
                await File.WriteAllTextAsync(logPath, logBuilder.ToString(), cancellationToken);
                throw;
            }
        }

        private async Task<string> RunCommandAsync(string command, string arguments, string workingDirectory, CancellationToken cancellationToken)
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

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                    _logger.LogInformation("{Command}: {Line}", command, e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine($"ERROR: {e.Data}");
                    _logger.LogWarning("{Command}: {Error}", command, e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

#if NET8_0_OR_GREATER
            await process.WaitForExitAsync(cancellationToken);
#else
            await process.WaitForExitAsync();
#endif

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Command '{command} {arguments}' failed with exit code {process.ExitCode}.");
            }

            return output.ToString();
        }

        private async Task<ConfigCheck> VerifyCommandAsync(string command, string successMessage, string failureMessage, CancellationToken cancellationToken)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
#if NET8_0_OR_GREATER
                await process.WaitForExitAsync(cancellationToken);
#else
                await process.WaitForExitAsync();
#endif

                return new ConfigCheck
                {
                    Name = command,
                    Passed = process.ExitCode == 0,
                    Message = process.ExitCode == 0 ? successMessage : failureMessage
                };
            }
            catch (Exception ex)
            {
                return new ConfigCheck
                {
                    Name = command,
                    Passed = false,
                    Message = $"{failureMessage} ({ex.Message})"
                };
            }
        }

        private string GetRepositoryPath() =>
            Environment.GetEnvironmentVariable("FOCUSDECK_REPO")
            ?? _configuration["Update:RepositoryPath"]
            ?? DefaultRepoPath;

        private string GetConfigurationStatus()
        {
            if (!OperatingSystem.IsLinux())
            {
                return "Updates are only available on Linux.";
            }

            var repoPath = GetRepositoryPath();
            return Directory.Exists(repoPath)
                ? $"Repository detected at {repoPath}."
                : $"Repository not found. Expected at {repoPath}.";
        }

        private string? GetLastUpdateLog()
        {
            try
            {
                var logPath = Path.Combine(LogDirectory, LogFileName);
                if (!File.Exists(logPath))
                {
                    return null;
                }

                var lines = File.ReadAllLines(logPath);
                return lines.Length > 0 ? lines[^1] : null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unable to read last update log entry.");
                return null;
            }
        }

        private async Task<string> RunGitCommandAsync(string arguments, CancellationToken cancellationToken)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
#if NET8_0_OR_GREATER
            await process.WaitForExitAsync(cancellationToken);
#else
            await process.WaitForExitAsync();
#endif

            return output.Trim();
        }

        private static string TrimCommit(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? "unknown"
                : value.Length <= 7 ? value : value[..7];
    }
}
