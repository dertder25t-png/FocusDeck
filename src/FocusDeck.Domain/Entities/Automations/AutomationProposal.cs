using System;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities.Automations
{
    public enum ProposalStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2
    }

    public class AutomationProposal : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string YamlDefinition { get; set; } = string.Empty;
        public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
        public float ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Tenant/User ownership
        public Guid TenantId { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}
