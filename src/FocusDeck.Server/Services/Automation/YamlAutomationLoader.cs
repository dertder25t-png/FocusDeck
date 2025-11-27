using FocusDeck.Domain.Entities.Automations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FocusDeck.Server.Services.Automations
{
    public interface IYamlAutomationLoader
    {
        ParsedAutomationDto Parse(string yaml);
        void UpdateAutomationFromYaml(FocusDeck.Domain.Entities.Automations.Automation automation, string yaml);
    }

    public class YamlAutomationLoader : IYamlAutomationLoader
    {
        private readonly IDeserializer _deserializer;
        private readonly ISerializer _serializer;

        public YamlAutomationLoader()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public ParsedAutomationDto Parse(string yaml)
        {
            return _deserializer.Deserialize<ParsedAutomationDto>(yaml);
        }

        public void UpdateAutomationFromYaml(FocusDeck.Domain.Entities.Automations.Automation automation, string yaml)
        {
            var dto = Parse(yaml);

            automation.Name = dto.Name ?? automation.Name;
            automation.Description = dto.Description ?? automation.Description;
            automation.YamlDefinition = yaml;

            // Update Trigger
            if (dto.Trigger != null)
            {
                if (automation.Trigger == null)
                    automation.Trigger = new AutomationTrigger();

                automation.Trigger.Type = dto.Trigger.Type;
                automation.Trigger.TriggerType = dto.Trigger.Type; // Keeping both for now due to legacy field
                automation.Trigger.Service = ParseServiceType(dto.Trigger.Type);
                automation.Trigger.Settings = dto.Trigger.Settings ?? new Dictionary<string, string>();
            }

            // Update Actions
            if (dto.Actions != null)
            {
                automation.Actions = dto.Actions.Select(a => new AutomationAction
                {
                    ActionType = a.Type,
                    Settings = a.Settings ?? new Dictionary<string, string>()
                }).ToList();
            }
        }

        private ServiceType ParseServiceType(string triggerType)
        {
            // Simple heuristic to map trigger string to ServiceType enum
            if (string.IsNullOrEmpty(triggerType)) return ServiceType.FocusDeck;

            if (triggerType.StartsWith("Google", StringComparison.OrdinalIgnoreCase)) return ServiceType.GoogleCalendar;
            if (triggerType.StartsWith("Canvas", StringComparison.OrdinalIgnoreCase)) return ServiceType.Canvas;
            if (triggerType.StartsWith("Spotify", StringComparison.OrdinalIgnoreCase)) return ServiceType.Spotify;
            if (triggerType.StartsWith("HA", StringComparison.OrdinalIgnoreCase)) return ServiceType.HomeAssistant;

            return ServiceType.FocusDeck;
        }
    }

    public class ParsedAutomationDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public TriggerDto Trigger { get; set; } = null!;
        public List<ActionDto> Actions { get; set; } = new();
    }

    public class TriggerDto
    {
        public string Type { get; set; } = null!;
        public Dictionary<string, string> Settings { get; set; } = new();
    }

    public class ActionDto
    {
        public string Type { get; set; } = null!;
        public Dictionary<string, string> Settings { get; set; } = new();
    }
}
