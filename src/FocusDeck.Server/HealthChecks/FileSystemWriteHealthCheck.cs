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
            // Ensure directory exists
            Directory.CreateDirectory(storageRoot);

            // Try to write a temporary health check file
            var testFile = Path.Combine(storageRoot, "_health.tmp");
            await File.WriteAllTextAsync(testFile, "health check", cancellationToken);
            
            // Try to read it back to verify
            var content = await File.ReadAllTextAsync(testFile, cancellationToken);
            
            // Clean up
            File.Delete(testFile);

            if (content == "health check")
            {
                return HealthCheckResult.Healthy($"Storage write access verified for: {storageRoot}");
            }

            return HealthCheckResult.Degraded($"Storage write verification failed for: {storageRoot}");
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
