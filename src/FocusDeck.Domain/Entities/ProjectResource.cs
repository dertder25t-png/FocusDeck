using System;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities
{
    public class ProjectResource : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public ProjectResourceType ResourceType { get; set; }
        public string ResourceValue { get; set; } = string.Empty;
        public string? Title { get; set; }
        public ProjectResourceStatus Status { get; set; } = ProjectResourceStatus.Active;
        public Guid TenantId { get; set; }

        public Project? Project { get; set; }
    }
}
