using System;
using System.Collections.Generic;

namespace FocusDeck.Domain.Entities
{
    /// <summary>
    /// Represents a single to-do item or task
    /// </summary>
    public class TodoItem : IMustHaveTenant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Priority level: 1=Low, 2=Medium, 3=High, 4=Urgent
        /// </summary>
        public int Priority { get; set; } = 2;
        
        public bool IsCompleted { get; set; } = false;
        
        /// <summary>
        /// Optional due date
        /// </summary>
        public DateTime? DueDate { get; set; }
        
        /// <summary>
        /// Date task was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Date task was completed
        /// </summary>
        public DateTime? CompletedDate { get; set; }
        
        /// <summary>
        /// Source: "User", "Canvas", "Study Plan"
        /// </summary>
        public string Source { get; set; } = "User";
        
        /// <summary>
        /// Related Canvas assignment ID if applicable
        /// </summary>
        public string? CanvasAssignmentId { get; set; }
        
        /// <summary>
        /// Related Canvas course ID if applicable
        /// </summary>
        public string? CanvasCourseId { get; set; }
        
        /// <summary>
        /// Tags for organization
        /// </summary>
        public List<string> Tags { get; set; } = new();
        
        /// <summary>
        /// Estimated time in minutes to complete
        /// </summary>
        public int? EstimatedMinutes { get; set; }
        
        /// <summary>
        /// Actual time spent in minutes
        /// </summary>
        public int? ActualMinutes { get; set; }
        
        /// <summary>
        /// Show reminder notification
        /// </summary>
        public bool ShowReminder { get; set; } = true;
        
        /// <summary>
        /// Repeat: "None", "Daily", "Weekly", "Biweekly"
        /// </summary>
        public string Repeat { get; set; } = "None";

        public bool IsOverdue()
        {
            return !IsCompleted && DueDate.HasValue && DateTime.Now > DueDate.Value.AddDays(1);
        }

        public bool IsDueSoon(TimeSpan within)
        {
            return !IsCompleted && DueDate.HasValue && 
                   DueDate.Value > DateTime.Now && 
                   DueDate.Value <= DateTime.Now.Add(within);
        }

        public int? DaysDueIn()
        {
            if (!DueDate.HasValue) return null;
            return (int)(DueDate.Value - DateTime.Now).TotalDays;
        }

        public string PriorityName => Priority switch
        {
            1 => "Low",
            2 => "Medium",
            3 => "High",
            4 => "Urgent",
            _ => "Unknown"
        };

        public Guid TenantId { get; set; }
    }
}
