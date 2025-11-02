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
[Route("v{version:apiVersion}/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly AutomationDbContext _context;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(
        AutomationDbContext context,
        IIdGenerator idGenerator,
        IClock clock,
        ILogger<CoursesController> logger)
    {
        _context = context;
        _idGenerator = idGenerator;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Create a new course
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var course = new Course
        {
            Id = _idGenerator.NewId().ToString(),
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            Instructor = dto.Instructor,
            CreatedAt = _clock.UtcNow,
            CreatedBy = User.Identity?.Name ?? "system"
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created course {CourseId}: {CourseName}", course.Id, course.Name);

        var courseDto = MapToDto(course);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, courseDto);
    }

    /// <summary>
    /// Get course details
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> GetCourse(string id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound(new { message = "Course not found" });
        }

        return Ok(MapToDto(course));
    }

    /// <summary>
    /// Get all courses
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseDto>>> GetCourses()
    {
        var courses = await _context.Courses
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(courses.Select(MapToDto).ToList());
    }

    private static CourseDto MapToDto(Course course)
    {
        return new CourseDto(
            Id: course.Id,
            Name: course.Name,
            Code: course.Code,
            Description: course.Description,
            Instructor: course.Instructor,
            CreatedAt: course.CreatedAt,
            CreatedBy: course.CreatedBy
        );
    }
}
