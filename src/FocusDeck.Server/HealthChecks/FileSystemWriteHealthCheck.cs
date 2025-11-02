using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FocusDeck.Server.HealthChecks;

public class FileSystemWriteHealthCheck : IHealthCheck
{
    private readonly string _path;

    public FileSystemWriteHealthCheck(string path)
    {
        _path = path;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(_path);

            // Try to write a test file
            var testFile = Path.Combine(_path, $".healthcheck_{Guid.NewGuid()}.tmp");
            await File.WriteAllTextAsync(testFile, "health check", cancellationToken);
            
            // Try to read it back
            var content = await File.ReadAllTextAsync(testFile, cancellationToken);
            
            // Clean up
            File.Delete(testFile);

            if (content == "health check")
            {
                return HealthCheckResult.Healthy($"Write access verified for path: {_path}");
            }

            return HealthCheckResult.Degraded($"Write verification failed for path: {_path}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return HealthCheckResult.Unhealthy($"No write access to path: {_path}", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"File system check failed for path: {_path}", ex);
        }
    }
}
