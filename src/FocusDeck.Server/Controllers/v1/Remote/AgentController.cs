using Asp.Versioning;
using FocusDeck.Domain.Entities.Remote;
using FocusDeck.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1.Remote
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/remote/agent")]
    [Authorize]
    public class AgentController : ControllerBase
    {
        private readonly AutomationDbContext _db;
        private readonly ILogger<AgentController> _logger;

        public AgentController(AutomationDbContext db, ILogger<AgentController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // POST: v1/remote/agent/jobs/queue
        [HttpPost("jobs/queue")]
        public async Task<ActionResult<DeviceJob>> QueueJob([FromBody] QueueJobRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tenantIdStr = User.FindFirst("app_tenant_id")?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(tenantIdStr, out var tenantId))
            {
                return Unauthorized();
            }

            var job = new DeviceJob
            {
                Id = Guid.NewGuid(),
                TargetDeviceId = request.TargetDeviceId,
                JobType = request.JobType,
                PayloadJson = request.PayloadJson,
                Status = JobStatus.Queued,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30), // Default expiration
                TenantId = tenantId,
                UserId = userId
            };

            _db.DeviceJobs.Add(job);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Queued job {JobId} for device {DeviceId} (Type: {Type})", job.Id, job.TargetDeviceId, job.JobType);

            // Ideally: Dispatch SignalR notification to device here "RemoteActionCreated" or "JobQueued"

            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
        }

        // GET: v1/remote/agent/jobs/{id}
        [HttpGet("jobs/{id}")]
        public async Task<ActionResult<DeviceJob>> GetJob(Guid id)
        {
            var job = await _db.DeviceJobs.FindAsync(id);
            if (job == null) return NotFound();
            return Ok(job);
        }

        // GET: v1/remote/agent/jobs/pending?deviceId={deviceId}
        [HttpGet("jobs/pending")]
        public async Task<ActionResult<List<DeviceJob>>> GetPendingJobs([FromQuery] string deviceId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Optional: Verify device ownership if stricter security needed

            var jobs = await _db.DeviceJobs
                .Where(j => j.TargetDeviceId == deviceId && j.Status == JobStatus.Queued && j.ExpiresAt > DateTime.UtcNow)
                .OrderBy(j => j.CreatedAt)
                .ToListAsync();

            return Ok(jobs);
        }

        // PUT: v1/remote/agent/jobs/{id}/status
        [HttpPut("jobs/{id}/status")]
        public async Task<IActionResult> UpdateJobStatus(Guid id, [FromBody] UpdateJobStatusRequest request)
        {
            var job = await _db.DeviceJobs.FindAsync(id);
            if (job == null) return NotFound();

            job.Status = request.Status;
            job.ResultJson = request.ResultJson;
            if (request.Status == JobStatus.Completed || request.Status == JobStatus.Failed)
            {
                job.CompletedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(job);
        }
    }

    public record QueueJobRequest(string TargetDeviceId, string JobType, string PayloadJson);
    public record UpdateJobStatusRequest(JobStatus Status, string? ResultJson);
}
