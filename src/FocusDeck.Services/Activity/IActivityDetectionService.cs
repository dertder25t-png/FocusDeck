using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Services.Activity
{
    /// <summary>
    /// Cross-platform service for detecting and monitoring user activity.
    /// Tracks focused applications, idle status, activity intensity, and context.
    /// </summary>
    public interface IActivityDetectionService
    {
        /// <summary>
        /// Get current activity state across all platforms
        /// </summary>
        Task<ActivityState> GetCurrentActivityAsync(CancellationToken ct);

        /// <summary>
        /// Subscribe to activity changes in real-time
        /// </summary>
        IObservable<ActivityState> ActivityChanged { get; }

        /// <summary>
        /// Get details about the currently focused window/application
        /// </summary>
        Task<FocusedApplication?> GetFocusedApplicationAsync(CancellationToken ct);

        /// <summary>
        /// Detect if user is idle (no activity for N seconds)
        /// </summary>
        Task<bool> IsIdleAsync(int idleThresholdSeconds, CancellationToken ct);

        /// <summary>
        /// Measure activity intensity based on keyboard/mouse activity (0-100)
        /// </summary>
        Task<double> GetActivityIntensityAsync(int minutesWindow, CancellationToken ct);
    }

    /// <summary>
    /// Current activity state across all sensors and platforms
    /// </summary>
    public class ActivityState
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// What app/window is currently focused?
        /// </summary>
        public FocusedApplication? FocusedApp { get; set; }

        /// <summary>
        /// How intense is the current activity? (0-100 scale)
        /// </summary>
        public int ActivityIntensity { get; set; }

        /// <summary>
        /// Is the user idle (no detected activity)?
        /// </summary>
        public bool IsIdle { get; set; }

        /// <summary>
        /// What assignments/notes are currently open?
        /// </summary>
        public List<ContextItem> OpenContexts { get; set; } = [];

        /// <summary>
        /// When was this state captured?
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Details about the currently focused application window
    /// </summary>
    public class FocusedApplication
    {
        /// <summary>
        /// Application name (e.g., " chrome\, \word\, \vscode\)
 /// </summary>
 public string AppName { get; set; } = string.Empty;

 /// <summary>
 /// Full window title (e.g., \Gmail - Mozilla Firefox\)
 /// </summary>
 public string WindowTitle { get; set; } = string.Empty;

 /// <summary>
 /// Full path to executable (e.g., \C:\Program Files\Google\Chrome\chrome.exe\)
 /// </summary>
 public string ProcessPath { get; set; } = string.Empty;

 /// <summary>
 /// Tags classifying the application
 /// Examples: \productivity\, \distraction\, \study\, \focus_music\, \communication\
 /// </summary>
 public string[] Tags { get; set; } = [];

 /// <summary>
 /// When did user switch to this application?
 /// </summary>
 public DateTime SwitchedAt { get; set; } = DateTime.UtcNow;
 }

 /// <summary>
 /// Context item related to current activity (note, assignment, file, etc.)
 /// </summary>
 public class ContextItem
 {
 /// <summary>
 /// Type of context item: \note\, \canvas_assignment\, \file\, etc.
 /// </summary>
 public string Type { get; set; } = string.Empty;

 /// <summary>
 /// Human-readable title
 /// </summary>
 public string Title { get; set; } = string.Empty;

 /// <summary>
 /// Link to database entity (if applicable)
 /// </summary>
 public Guid? RelatedId { get; set; }
 }
}
