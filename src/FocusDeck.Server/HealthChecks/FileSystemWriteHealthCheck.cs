using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FocusDeck.Server.HealthChecks;

public class FileSystemWriteHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public FileSystemWriteHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Get storage root from configuration (same as LocalFileSystemAssetStorage uses)
        var storageRoot = _configuration.GetValue<string>("Storage:Root") ?? "/data/assets";
        
        try
        {
            // Check if parent directory exists and is accessible
            var parentDir = Path.GetDirectoryName(storageRoot);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                return HealthCheckResult.Unhealthy(
                    $"Parent directory does not exist: {parentDir}. Cannot create storage root.",
                    new DirectoryNotFoundException(parentDir));
            }

            // Ensure storage directory exists
            if (!Directory.Exists(storageRoot))
            {
                try
                {
                    Directory.CreateDirectory(storageRoot);
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Failed to create storage directory: {storageRoot}", ex);
                }
            }

            // Try to write a temporary health check file
            var testFile = Path.Combine(storageRoot, "_health.tmp");
            
            try
            {
                await File.WriteAllTextAsync(testFile, "health check", cancellationToken);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"Failed to write test file to storage directory: {storageRoot}. Check permissions and disk space.", ex);
            }
            
            // Try to read it back to verify
            string content;
            try
            {
                content = await File.ReadAllTextAsync(testFile, cancellationToken);
            }
            catch (Exception ex)
            {
                // Try to clean up even if read failed
                try { File.Delete(testFile); } catch { /* Ignore cleanup errors */ }
                return HealthCheckResult.Unhealthy(
                    $"Failed to read test file from storage directory: {storageRoot}", ex);
            }
            
            // Clean up test file
            try
            {
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                // Non-fatal: file was created but cleanup failed
                return HealthCheckResult.Degraded(
                    $"Storage is writable but cleanup failed for: {storageRoot}. Orphaned file: {testFile}", ex);
            }

            // Clean up any orphaned health check files from previous crashes
            try
            {
                var orphanedFiles = Directory.GetFiles(storageRoot, "_health*.tmp", SearchOption.TopDirectoryOnly);
                foreach (var orphan in orphanedFiles)
                {
                    try
                    {
                        // Only delete old orphaned files (older than 1 hour)
                        var fileInfo = new FileInfo(orphan);
                        if (fileInfo.Exists && (DateTime.UtcNow - fileInfo.LastWriteTimeUtc).TotalHours > 1)
                        {
                            File.Delete(orphan);
                        }
                    }
                    catch
                    {
                        // Ignore individual file cleanup failures
                    }
                }
            }
            catch
            {
                // Non-fatal: orphan cleanup is best-effort
            }

            if (content == "health check")
            {
                return HealthCheckResult.Healthy($"Storage write access verified for: {storageRoot}");
            }

            return HealthCheckResult.Degraded($"Storage write verification failed for: {storageRoot}. Content mismatch.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return HealthCheckResult.Unhealthy($"No write access to storage directory: {storageRoot}. Uploads will fail.", ex);
        }
        catch (IOException ex)
        {
            return HealthCheckResult.Unhealthy($"I/O error accessing storage directory: {storageRoot}. Check permissions and disk space.", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Storage health check failed for: {storageRoot}. Error: {ex.Message}", ex);
        }
    }
}
