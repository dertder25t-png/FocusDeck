using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Controllers.V1
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
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            return await _context.Projects.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Project>> CreateProject(Project project)
        {
            if (string.IsNullOrWhiteSpace(project.Title))
            {
                return BadRequest("Title is required.");
            }

            project.Id = Guid.NewGuid();
            project.CreatedAt = DateTime.UtcNow;

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProjects), new { id = project.Id }, project);
        }
    }
}
