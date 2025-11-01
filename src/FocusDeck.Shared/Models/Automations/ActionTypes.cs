namespace FocusDeck.Shared.Models.Automations
{
    /// <summary>
    /// Comprehensive action type definitions for the automation system
    /// </summary>
    public static class ActionTypes
    {
        // ===== TIMER & SESSION ACTIONS =====
        public const string StartTimer = "timer.start";
        public const string StopTimer = "timer.stop";
        public const string PauseTimer = "timer.pause";
        public const string StartBreak = "timer.start_break";
        public const string StartPomodoroSession = "timer.start_pomodoro";

        // ===== TASK ACTIONS =====
        public const string CreateTask = "task.create";
        public const string CompleteTask = "task.complete";
        public const string UpdateTask = "task.update";
        public const string DeleteTask = "task.delete";
        public const string SetTaskPriority = "task.set_priority";
        public const string AddTaskTag = "task.add_tag";
        public const string MoveTask = "task.move";

        // ===== NOTE ACTIONS =====
        public const string CreateNote = "note.create";
        public const string AppendToNote = "note.append";
        public const string CreateNoteInDeck = "note.create_in_deck";

        // ===== WORKSPACE ACTIONS =====
        public const string LoadWorkspace = "workspace.load";
        public const string CloseWorkspace = "workspace.close";
        public const string OpenDeck = "deck.open";

        // ===== NOTIFICATION ACTIONS =====
        public const string ShowNotification = "notification.show";
        public const string PlaySound = "notification.sound";
        public const string ShowAlert = "notification.alert";
        public const string SendEmail = "notification.email";

        // ===== SYSTEM ACTIONS =====
        public const string RunCommand = "system.run_command";
        public const string OpenApplication = "system.open_app";
        public const string CloseApplication = "system.close_app";
        public const string OpenURL = "system.open_url";
        public const string SetClipboard = "system.set_clipboard";
        public const string PressHotkey = "system.press_hotkey";

        // ===== HOME ASSISTANT ACTIONS =====
        public const string HomeAssistantTurnOn = "home_assistant.turn_on";
        public const string HomeAssistantTurnOff = "home_assistant.turn_off";
        public const string HomeAssistantSetState = "home_assistant.set_state";
        public const string HomeAssistantCallService = "home_assistant.call_service";

        // ===== SPOTIFY ACTIONS =====
        public const string SpotifyPlay = "spotify.play";
        public const string SpotifyPause = "spotify.pause";
        public const string SpotifyPlayPlaylist = "spotify.play_playlist";
        public const string SpotifySetVolume = "spotify.set_volume";

        // ===== SMART LIGHTING ACTIONS =====
        public const string SetLightingScene = "lights.set_scene";
        public const string DimLights = "lights.dim";
        public const string SetLightColor = "lights.set_color";

        // ===== GOOGLE CALENDAR ACTIONS =====
        public const string CreateCalendarEvent = "google_calendar.create_event";
        public const string UpdateCalendarEvent = "google_calendar.update_event";

        // ===== CANVAS ACTIONS =====
        public const string CanvasSubmitAssignment = "canvas.submit_assignment";
        public const string CanvasPostDiscussion = "canvas.post_discussion";

        // ===== DATA & SYNC ACTIONS =====
        public const string SyncData = "sync.sync_now";
        public const string ExportData = "data.export";
        public const string BackupData = "data.backup";

        // ===== PRODUCTIVITY ACTIONS =====
        public const string LogActivity = "productivity.log_activity";
        public const string IncrementCounter = "productivity.increment_counter";
        public const string SetGoal = "productivity.set_goal";

        // ===== ADVANCED ACTIONS =====
        public const string Wait = "advanced.wait";
        public const string HttpRequest = "advanced.http_request";
        public const string RunWebhook = "advanced.webhook";
        public const string ConditionalAction = "advanced.conditional";
        public const string LoopAction = "advanced.loop";
        public const string TriggerAutomation = "advanced.trigger_automation";
    }
}
