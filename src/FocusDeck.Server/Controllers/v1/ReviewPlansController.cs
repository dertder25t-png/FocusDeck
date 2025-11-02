using Asp.Versioning;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v1/review-plans")]
[Authorize]
public class ReviewPlansController : ControllerBase
{
    private readonly AutomationDbContext _context;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly ILogger<ReviewPlansController> _logger;

    public ReviewPlansController(
        AutomationDbContext context,
        IIdGenerator idGenerator,
        IClock clock,
        ILogger<ReviewPlansController> logger)
    {
        _context = context;
        _idGenerator = idGenerator;
        _clock = clock;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ReviewPlanDto>> CreateReviewPlan(CreateReviewPlanDto dto)
    {
        _logger.LogInformation("Creating review plan for {EntityType} {EntityId}", dto.EntityType, dto.TargetEntityId);

        // Parse entity type
        if (!Enum.TryParse<ReviewPlanEntityType>(dto.EntityType, true, out var entityType))
        {
            return BadRequest($"Invalid entity type: {dto.EntityType}. Must be 'Lecture' or 'Note'.");
        }

        // Validate entity exists
        var entityExists = entityType == ReviewPlanEntityType.Lecture
            ? await _context.Lectures.AnyAsync(l => l.Id == dto.TargetEntityId)
            : await _context.Notes.AnyAsync(n => n.Id == dto.TargetEntityId);

        if (!entityExists)
        {
            return NotFound($"{dto.EntityType} with ID {dto.TargetEntityId} not found.");
        }

        // Create review plan
        var userId = User.Identity?.Name ?? "anonymous";
        var planId = _idGenerator.NewId().ToString();

        var reviewPlan = new ReviewPlan
        {
            Id = planId,
            UserId = userId,
            TargetEntityId = dto.TargetEntityId,
            EntityType = entityType,
            Title = dto.Title,
            CreatedAt = _clock.UtcNow,
            Status = ReviewPlanStatus.Active,
            ReviewSessions = dto.ScheduledDates.Select(date => new ReviewSession
            {
                Id = _idGenerator.NewId().ToString(),
                ReviewPlanId = planId,
                ScheduledDate = date,
                Status = ReviewSessionStatus.Pending
            }).ToList()
        };

        _context.ReviewPlans.Add(reviewPlan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Review plan {PlanId} created with {SessionCount} sessions", planId, reviewPlan.ReviewSessions.Count);

        return CreatedAtAction(
            nameof(GetReviewPlan),
            new { id = planId },
            MapToDto(reviewPlan));
    }

    [HttpPost("compute-spaced")]
    public ActionResult<CreateReviewPlanDto> ComputeSpacedPlan(ComputeSpacedPlanRequest request)
    {
        _logger.LogInformation("Computing spaced repetition plan for {EntityType} {EntityId}", request.EntityType, request.TargetEntityId);

        var startDate = request.StartDate ?? _clock.UtcNow.Date;

        // Spaced repetition schedule: Day 0, Day+2, Day+7
        var scheduledDates = new[]
        {
            startDate, // D0
            startDate.AddDays(2), // D+2
            startDate.AddDays(7)  // D+7
        };

        var dto = new CreateReviewPlanDto
        {
            TargetEntityId = request.TargetEntityId,
            EntityType = request.EntityType,
            Title = request.Title,
            ScheduledDates = scheduledDates
        };

        return Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<List<ReviewPlanDto>>> ListReviewPlans(
        [FromQuery] string? entityType = null,
        [FromQuery] string? status = null)
    {
        var userId = User.Identity?.Name ?? "anonymous";

        var query = _context.ReviewPlans
            .Include(rp => rp.ReviewSessions)
            .Where(rp => rp.UserId == userId);

        if (!string.IsNullOrEmpty(entityType) && Enum.TryParse<ReviewPlanEntityType>(entityType, true, out var et))
        {
            query = query.Where(rp => rp.EntityType == et);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReviewPlanStatus>(status, true, out var st))
        {
            query = query.Where(rp => rp.Status == st);
        }

        var plans = await query.OrderByDescending(rp => rp.CreatedAt).ToListAsync();

        return Ok(plans.Select(MapToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReviewPlanDto>> GetReviewPlan(string id)
    {
        var userId = User.Identity?.Name ?? "anonymous";

        var plan = await _context.ReviewPlans
            .Include(rp => rp.ReviewSessions)
            .FirstOrDefaultAsync(rp => rp.Id == id && rp.UserId == userId);

        if (plan == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(plan));
    }

    [HttpPatch("{planId}/sessions/{sessionId}")]
    public async Task<ActionResult<ReviewSessionDto>> UpdateReviewSession(
        string planId,
        string sessionId,
        UpdateReviewSessionDto dto)
    {
        var userId = User.Identity?.Name ?? "anonymous";

        var plan = await _context.ReviewPlans
            .Include(rp => rp.ReviewSessions)
            .FirstOrDefaultAsync(rp => rp.Id == planId && rp.UserId == userId);

        if (plan == null)
        {
            return NotFound("Review plan not found.");
        }

        var session = plan.ReviewSessions.FirstOrDefault(rs => rs.Id == sessionId);
        if (session == null)
        {
            return NotFound("Review session not found.");
        }

        // Parse and update status
        if (!Enum.TryParse<ReviewSessionStatus>(dto.Status, true, out var status))
        {
            return BadRequest($"Invalid status: {dto.Status}");
        }

        session.Status = status;
        session.CompletedDate = status == ReviewSessionStatus.Completed ? _clock.UtcNow : null;
        session.Score = dto.Score;
        session.Notes = dto.Notes;

        // Check if all sessions are completed
        var allCompleted = plan.ReviewSessions.All(rs => rs.Status != ReviewSessionStatus.Pending);
        if (allCompleted && plan.Status == ReviewPlanStatus.Active)
        {
            plan.Status = ReviewPlanStatus.Completed;
            plan.CompletedAt = _clock.UtcNow;
            _logger.LogInformation("Review plan {PlanId} completed", planId);
        }

        await _context.SaveChangesAsync();

        return Ok(MapToSessionDto(session));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReviewPlan(string id)
    {
        var userId = User.Identity?.Name ?? "anonymous";

        var plan = await _context.ReviewPlans
            .Include(rp => rp.ReviewSessions)
            .FirstOrDefaultAsync(rp => rp.Id == id && rp.UserId == userId);

        if (plan == null)
        {
            return NotFound();
        }

        _context.ReviewPlans.Remove(plan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Review plan {PlanId} deleted", id);

        return NoContent();
    }

    private static ReviewPlanDto MapToDto(ReviewPlan plan)
    {
        return new ReviewPlanDto
        {
            Id = plan.Id,
            UserId = plan.UserId,
            TargetEntityId = plan.TargetEntityId,
            EntityType = plan.EntityType.ToString(),
            Title = plan.Title,
            CreatedAt = plan.CreatedAt,
            CompletedAt = plan.CompletedAt,
            Status = plan.Status.ToString(),
            ReviewSessions = plan.ReviewSessions
                .Select(MapToSessionDto)
                .OrderBy(rs => rs.ScheduledDate)
                .ToList()
        };
    }

    private static ReviewSessionDto MapToSessionDto(ReviewSession session)
    {
        return new ReviewSessionDto
        {
            Id = session.Id,
            ReviewPlanId = session.ReviewPlanId,
            ScheduledDate = session.ScheduledDate,
            CompletedDate = session.CompletedDate,
            Status = session.Status.ToString(),
            Score = session.Score,
            Notes = session.Notes
        };
    }
}
