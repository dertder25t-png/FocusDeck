using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Controllers.V1
{
    [ApiController]
    [Route("v1/projects/{projectId}/resources")]
    [Authorize]
    public class ProjectResourcesController : ControllerBase
    {
        private readonly AutomationDbContext _db;
        private readonly ICurrentTenant _currentTenant;

        public ProjectResourcesController(AutomationDbContext db, ICurrentTenant currentTenant)
        {
            _db = db;
            _currentTenant = currentTenant;
        }

        [HttpGet]
        public async Task<ActionResult<List<ProjectResource>>> GetResources(Guid projectId, CancellationToken ct)
        {
            var project = await _db.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId, ct);

            if (project == null) return NotFound("Project not found");

            var resources = await _db.ProjectResources
                .Where(r => r.ProjectId == projectId)
                .ToListAsync(ct);

            return Ok(resources);
        }

        [HttpPost]
        public async Task<ActionResult<ProjectResource>> AddResource(Guid projectId, [FromBody] CreateProjectResourceDto dto, CancellationToken ct)
        {
            var project = await _db.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId, ct);

            if (project == null) return NotFound("Project not found");

            var resource = new ProjectResource
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                ResourceType = dto.ResourceType,
                ResourceValue = dto.ResourceValue,
                Title = dto.Title,
                Status = ProjectResourceStatus.Active,
                TenantId = project.TenantId
            };

            _db.ProjectResources.Add(resource);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetResources), new { projectId }, resource);
        }

        [HttpDelete("{resourceId}")]
        public async Task<ActionResult> RemoveResource(Guid projectId, Guid resourceId, CancellationToken ct)
        {
             var resource = await _db.ProjectResources
                .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.Id == resourceId, ct);

            if (resource == null) return NotFound();

            _db.ProjectResources.Remove(resource);
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }
    }

    public class CreateProjectResourceDto
    {
        public ProjectResourceType ResourceType { get; set; }
        public string ResourceValue { get; set; } = string.Empty;
        public string? Title { get; set; }
    }
}
