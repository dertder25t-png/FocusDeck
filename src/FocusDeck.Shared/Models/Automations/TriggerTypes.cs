namespace FocusDeck.Shared.Models.Automations
{
    /// <summary>
    /// Comprehensive trigger type definitions for the automation system
    /// </summary>
    public static class TriggerTypes
    {
        // ===== TIME-BASED TRIGGERS =====
        public const string AtSpecificTime = "time.specific";
        public const string TimeOfDay = "time.of_day";
        public const string Sunrise = "time.sunrise";
        public const string Sunset = "time.sunset";
        public const string OnDate = "time.date";
        public const string RecurringInterval = "time.interval";
        public const string OnAppLaunch = "app.launch";
        public const string OnAppClose = "app.close";
        public const string OnAppUpdate = "app.update";

        // ===== STUDY SESSION TRIGGERS =====
        public const string SessionStarted = "session.started";
        public const string SessionCompleted = "session.completed";
        public const string SessionStopped = "session.stopped";
        public const string SessionPaused = "session.paused";
        public const string SessionResumed = "session.resumed";
        public const string BreakStarted = "break.started";
        public const string BreakCompleted = "break.completed";
        public const string SessionType = "session.type";
        public const string SessionGoal = "session.goal";

        // ===== TASK & TODO TRIGGERS =====
        public const string TaskCreated = "task.created";
        public const string TaskCompleted = "task.completed";
        public const string TaskDue = "task.due";
        public const string TaskDueApproaching = "task.due_approaching";
        public const string TaskPriorityChanged = "task.priority_changed";
        public const string TaskMoved = "task.moved";
        public const string TaskTagged = "task.tagged";

        // ===== NOTE TRIGGERS =====
        public const string NoteCreated = "note.created";
        public const string NoteCreatedInDeck = "note.created_in_deck";
        public const string NoteContent = "note.content";
        public const string NoteTagged = "note.tagged";

        // ===== WORKSPACE & LAYOUT TRIGGERS =====
        public const string WorkspaceLoaded = "workspace.loaded";
        public const string WorkspaceClosed = "workspace.closed";
        public const string DeckOpened = "deck.opened";

        // ===== SYNC & DATA TRIGGERS =====
        public const string SyncCompleted = "sync.completed";
        public const string SyncFailed = "sync.failed";
        public const string NewDataReceived = "sync.new_data";

        // ===== USER & SYSTEM ACTIVITY TRIGGERS =====
        public const string ApplicationLaunched = "system.app_launched";
        public const string ApplicationClosed = "system.app_closed";
        public const string ApplicationInFocus = "system.app_focus";
        public const string WindowTitle = "system.window_title";
        public const string UserIdle = "system.user_idle";
        public const string UserReturnsFromIdle = "system.user_active";
        public const string SystemLocked = "system.locked";
        public const string SystemUnlocked = "system.unlocked";
        public const string SystemShutdown = "system.shutdown";
        public const string SystemSleep = "system.sleep";
        public const string SystemWake = "system.wake";
        public const string FileChanged = "system.file_changed";
        public const string NetworkConnected = "system.network_connected";
        public const string NetworkDisconnected = "system.network_disconnected";
        public const string HotkeyPressed = "system.hotkey";
        public const string MicrophoneInUse = "system.microphone_active";
        public const string CameraInUse = "system.camera_active";
        public const string ClipboardContent = "system.clipboard";

        // ===== EXTERNAL API TRIGGERS =====
        public const string GoogleCalendarEventStart = "google_calendar.event_start";
        public const string GoogleCalendarEventEnd = "google_calendar.event_end";
        public const string GoogleCalendarEventCreated = "google_calendar.event_created";
        public const string CanvasAssignmentDue = "canvas.assignment_due";
        public const string CanvasNewGrade = "canvas.new_grade";
        public const string CanvasNewAnnouncement = "canvas.new_announcement";
        public const string EmailReceived = "email.received";
        public const string GitHubIssueAssigned = "github.issue_assigned";
        public const string GitHubPullRequestReview = "github.pr_review";
        public const string GitHubBuildFailed = "github.build_failed";
        public const string HomeAssistantWebhook = "home_assistant.webhook";
        public const string DiscordMention = "discord.mention";
        public const string SlackMessage = "slack.message";
        public const string TrelloCardMoved = "trello.card_moved";
        public const string TodoistTaskCreated = "todoist.task_created";
        public const string NotionDatabaseUpdated = "notion.database_updated";

        // ===== LOCATION & ENVIRONMENT TRIGGERS =====
        public const string EnterZone = "location.enter_zone";
        public const string LeaveZone = "location.leave_zone";
        public const string WeatherCondition = "weather.condition";
        public const string Temperature = "weather.temperature";
        public const string LightLevel = "sensor.light_level";
        public const string NoiseLevel = "sensor.noise_level";

        // ===== BIOMETRIC & HEALTH TRIGGERS =====
        public const string SleepSessionLogged = "health.sleep_logged";
        public const string WakeUp = "health.wake_up";
        public const string ReadinessScore = "health.readiness_score";
        public const string HeartRate = "health.heart_rate";
        public const string InactivityDetected = "health.inactivity";
        public const string MindfulnessCompleted = "health.mindfulness_completed";

        // ===== COMPLEX & CHAINED TRIGGERS =====
        public const string ConditionalTime = "complex.conditional_time";
        public const string ConsecutiveEvents = "complex.consecutive_events";
        public const string AbsenceOfEvent = "complex.absence_of_event";
        public const string TaskListState = "complex.task_list_state";
        public const string Streak = "complex.streak";
        public const string ManualTrigger = "complex.manual";
        public const string WebhookReceived = "complex.webhook";
    }
}
