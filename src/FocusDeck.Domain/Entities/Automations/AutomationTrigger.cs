namespace FocusDeck.Domain.Entities.Automations
{
    public class AutomationTrigger
    {
        public Guid Id { get; set; }
        public ServiceType Service { get; set; }
        public string TriggerType { get; set; } = null!; // e.g., "EventStart", "NewAssignment"
        public Dictionary<string, string> Settings { get; set; } = new(); // e.g., {"CalendarName": "Work"}
    }

    public enum ServiceType
    {
        FocusDeck,
        GoogleCalendar,
        Canvas,
        GoogleDrive,
        Spotify,
        HomeAssistant,
        Notion,
        Todoist,
        Slack,
        Discord,
        GoogleGenerativeAI,
        IFTTT,
        Zapier,
        PhilipsHue,
        AppleMusic
    }
}
