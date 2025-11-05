using Asp.Versioning;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly ILogger<JobsController> _logger;

    public JobsController(ILogger<JobsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult ListJobs(
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            var monitoring = JobStorage.Current.GetMonitoringApi();
            
            var from = (page - 1) * perPage;
            var count = perPage;

            var jobs = new List<object>();
            var totalCount = 0;

            // Get jobs based on status filter
            if (status == null || status.Equals("processing", StringComparison.OrdinalIgnoreCase))
            {
                var processingJobs = monitoring.ProcessingJobs(from, count);
                totalCount += (int)monitoring.ProcessingCount();
                
                foreach (var job in processingJobs)
                {
                    jobs.Add(new
                    {
                        id = job.Key,
                        status = "Processing",
                        jobType = job.Value?.Job?.Type?.Name,
                        method = job.Value?.Job?.Method?.Name,
                        args = job.Value?.Job?.Args?.Select(a => a?.ToString()).ToList(),
                        startedAt = job.Value?.StartedAt,
                        server = job.Value?.ServerId
                    });
                }
            }

            if (status == null || status.Equals("scheduled", StringComparison.OrdinalIgnoreCase))
            {
                var scheduledJobs = monitoring.ScheduledJobs(from, count);
                totalCount += (int)monitoring.ScheduledCount();
                
                foreach (var job in scheduledJobs)
                {
                    jobs.Add(new
                    {
                        id = job.Key,
                        status = "Scheduled",
                        jobType = job.Value?.Job?.Type?.Name,
                        method = job.Value?.Job?.Method?.Name,
                        args = job.Value?.Job?.Args?.Select(a => a?.ToString()).ToList(),
                        scheduledAt = job.Value?.EnqueueAt
                    });
                }
            }

            if (status == null || status.Equals("succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var succeededJobs = monitoring.SucceededJobs(from, count);
                totalCount += (int)monitoring.SucceededListCount();
                
                foreach (var job in succeededJobs)
                {
                    jobs.Add(new
                    {
                        id = job.Key,
                        status = "Succeeded",
                        jobType = job.Value?.Job?.Type?.Name,
                        method = job.Value?.Job?.Method?.Name,
                        args = job.Value?.Job?.Args?.Select(a => a?.ToString()).ToList(),
                        succeededAt = job.Value?.SucceededAt,
                        duration = job.Value?.TotalDuration
                    });
                }
            }

            if (status == null || status.Equals("failed", StringComparison.OrdinalIgnoreCase))
            {
                var failedJobs = monitoring.FailedJobs(from, count);
                totalCount += (int)monitoring.FailedCount();
                
                foreach (var job in failedJobs)
                {
                    jobs.Add(new
                    {
                        id = job.Key,
                        status = "Failed",
                        jobType = job.Value?.Job?.Type?.Name,
                        method = job.Value?.Job?.Method?.Name,
                        args = job.Value?.Job?.Args?.Select(a => a?.ToString()).ToList(),
                        failedAt = job.Value?.FailedAt,
                        exceptionMessage = job.Value?.ExceptionMessage,
                        exceptionType = job.Value?.ExceptionType
                    });
                }
            }

            return Ok(new
            {
                jobs = jobs.OrderByDescending(j => GetJobTimestamp(j)).Take(count),
                total = totalCount,
                page,
                perPage,
                totalPages = (int)Math.Ceiling(totalCount / (double)perPage)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Hangfire jobs");
            return StatusCode(500, new { code = "JOB_ERROR", message = "Failed to retrieve job information" });
        }
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            var monitoring = JobStorage.Current.GetMonitoringApi();
            var statistics = monitoring.GetStatistics();

            return Ok(new
            {
                enqueued = statistics.Enqueued,
                scheduled = statistics.Scheduled,
                processing = statistics.Processing,
                succeeded = statistics.Succeeded,
                failed = statistics.Failed,
                deleted = statistics.Deleted,
                recurring = statistics.Recurring,
                servers = statistics.Servers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Hangfire statistics");
            return StatusCode(500, new { code = "STATS_ERROR", message = "Failed to retrieve job statistics" });
        }
    }

    private static DateTime? GetJobTimestamp(object job)
    {
        var type = job.GetType();
        var startedAt = type.GetProperty("startedAt")?.GetValue(job) as DateTime?;
        var scheduledAt = type.GetProperty("scheduledAt")?.GetValue(job) as DateTime?;
        var succeededAt = type.GetProperty("succeededAt")?.GetValue(job) as DateTime?;
        var failedAt = type.GetProperty("failedAt")?.GetValue(job) as DateTime?;

        return startedAt ?? scheduledAt ?? succeededAt ?? failedAt ?? DateTime.MinValue;
    }
}
