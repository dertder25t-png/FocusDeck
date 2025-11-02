namespace FocusDeck.Domain.Entities.Automations
{
    public class AutomationAction
    {
        public Guid Id { get; set; }
        public string ActionType { get; set; } = null!; // e.g., "StartTimer", "CreateTask"
        public Dictionary<string, string> Settings { get; set; } = new(); // e.g., {"Duration": "25"}
    }
}
