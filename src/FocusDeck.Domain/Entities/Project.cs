using System;
using System.Collections.Generic;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities
{
    public class Project : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RepoSlug { get; set; } // e.g. "owner/repo"

        public ProjectSortingMode SortingMode { get; set; } = ProjectSortingMode.Review;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Guid TenantId { get; set; }

        public ICollection<ProjectResource> Resources { get; set; } = new List<ProjectResource>();
    }
}
