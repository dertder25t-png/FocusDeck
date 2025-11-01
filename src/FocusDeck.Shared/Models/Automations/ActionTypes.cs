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
        public const string HomeAssistantSetBrightness = "home_assistant.set_brightness";
        public const string HomeAssistantSetColor = "home_assistant.set_color";
        public const string HomeAssistantActivateScene = "home_assistant.activate_scene";

        // ===== SPOTIFY ACTIONS =====
        public const string SpotifyPlay = "spotify.play";
        public const string SpotifyPause = "spotify.pause";
        public const string SpotifyPlayPlaylist = "spotify.play_playlist";
        public const string SpotifySetVolume = "spotify.set_volume";
        public const string SpotifyNext = "spotify.next";
        public const string SpotifyPrevious = "spotify.previous";
        public const string SpotifySeek = "spotify.seek";
        public const string SpotifyShuffle = "spotify.shuffle";
        public const string SpotifyRepeat = "spotify.repeat";

        // ===== PHILIPS HUE ACTIONS =====
        public const string HueTurnOn = "hue.turn_on";
        public const string HueTurnOff = "hue.turn_off";
        public const string HueSetBrightness = "hue.set_brightness";
        public const string HueSetColor = "hue.set_color";
        public const string HueFlash = "hue.flash";
        public const string HueActivateScene = "hue.activate_scene";

        // ===== SLACK ACTIONS =====
        public const string SlackSendMessage = "slack.send_message";
        public const string SlackUpdateStatus = "slack.update_status";
        public const string SlackSetCustomStatus = "slack.set_custom_status";
        public const string SlackSetPresence = "slack.set_presence";

        // ===== DISCORD ACTIONS =====
        public const string DiscordSendMessage = "discord.send_message";
        public const string DiscordSendEmbed = "discord.send_embed";
        public const string DiscordSetStatus = "discord.set_status";

        // ===== NOTION ACTIONS =====
        public const string NotionCreatePage = "notion.create_page";
        public const string NotionUpdatePage = "notion.update_page";
        public const string NotionCreateDatabase = "notion.create_database";
        public const string NotionAddRow = "notion.add_row";

        // ===== TODOIST ACTIONS =====
        public const string TodoistCreateTask = "todoist.create_task";
        public const string TodoistCompleteTask = "todoist.complete_task";
        public const string TodoistUpdateTask = "todoist.update_task";
        public const string TodoistAddComment = "todoist.add_comment";

        // ===== GOOGLE GENERATIVE AI ACTIONS =====
        public const string GeminiGenerateText = "gemini.generate_text";
        public const string GeminiChat = "gemini.chat";
        public const string GeminiAnalyzeImage = "gemini.analyze_image";

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

        // ===== WINDOWS APP ACTIONS =====
        public const string WindowsShowNotification = "windows.show_notification";
        public const string WindowsLaunchApp = "windows.launch_app";
        public const string WindowsCloseApp = "windows.close_app";
        public const string WindowsFocusApp = "windows.focus_app";
        public const string WindowsMinimizeApp = "windows.minimize_app";
        public const string WindowsMaximizeApp = "windows.maximize_app";
        public const string WindowsLockScreen = "windows.lock_screen";
        public const string WindowsSetVolume = "windows.set_volume";
        public const string WindowsMuteVolume = "windows.mute_volume";
        public const string WindowsEnableFocusAssist = "windows.enable_focus_assist";
        public const string WindowsDisableFocusAssist = "windows.disable_focus_assist";
        public const string WindowsBlockWebsite = "windows.block_website";
        public const string WindowsUnblockWebsite = "windows.unblock_website";
        public const string WindowsRunPowershell = "windows.run_powershell";
        public const string WindowsSetWallpaper = "windows.set_wallpaper";
        public const string WindowsSetTheme = "windows.set_theme";
        public const string WindowsTakeScreenshot = "windows.take_screenshot";
        public const string WindowsStartRecording = "windows.start_recording";
        public const string WindowsStopRecording = "windows.stop_recording";
        public const string WindowsOpenFile = "windows.open_file";
        public const string WindowsMoveFile = "windows.move_file";
        public const string WindowsDeleteFile = "windows.delete_file";
        public const string WindowsEmptyRecycleBin = "windows.empty_recycle_bin";
        public const string WindowsSetMouseSpeed = "windows.set_mouse_speed";
        public const string WindowsDisableNotifications = "windows.disable_notifications";
    }
}
