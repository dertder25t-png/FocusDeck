using FocusDeck.Domain.Entities;

namespace FocusDeck.Domain.Entities.Automations
{
    public class Automation : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
        public string YamlDefinition { get; set; } = string.Empty;
        public AutomationTrigger Trigger { get; set; } = null!;
        public List<AutomationAction> Actions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastRunAt { get; set; }
        public Guid TenantId { get; set; }
    }
}
