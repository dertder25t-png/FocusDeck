using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Controllers.v1
{
    [ApiController]
    [Route("v1/projects")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly AutomationDbContext _context;

        public ProjectsController(AutomationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Project>>> GetProjects()
        {
            var projects = await _context.Projects
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();

            return Ok(projects);
        }

        [HttpPost]
        public async Task<ActionResult<Project>> CreateProject([FromBody] CreateProjectRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Title is required.");
            }

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                RepoSlug = request.RepoSlug,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // TenantId will be set by DbContext automatically via ICurrentTenant
            // or we can set it explicitly if we injected ICurrentTenant

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProjects), new { id = project.Id }, project);
        }
    }

    public class CreateProjectRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RepoSlug { get; set; }
    }
}
