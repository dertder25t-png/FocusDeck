using System;
using FocusDeck.Domain.Entities;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities
{
    public class CalendarSource : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string Provider { get; set; } = "Google"; // Google, Outlook
        public string ExternalId { get; set; } = ""; // Calendar ID (e.g., email)
        public string Name { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime? TokenExpiry { get; set; }
        public string? SyncToken { get; set; } // For incremental sync
        public DateTime LastSync { get; set; }
        public bool IsPrimary { get; set; }
        public Guid TenantId { get; set; }
    }

    public class EventCache : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid CalendarSourceId { get; set; }
        public CalendarSource? CalendarSource { get; set; }
        public string ExternalEventId { get; set; } = "";
        public string Title { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public bool IsAllDay { get; set; }
        public Guid TenantId { get; set; }
    }

    public class CourseIndex : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = ""; // e.g., "CS 101"
        public string Name { get; set; } = "";
        public List<string> Keywords { get; set; } = new(); // Synonyms for matching
        public string SchedulePattern { get; set; } = ""; // e.g., "MWF 10:00-11:00"
        public Guid TenantId { get; set; }
    }
}
