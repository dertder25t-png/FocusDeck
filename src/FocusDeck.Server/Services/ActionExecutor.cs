using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.ActionHandlers;

namespace FocusDeck.Server.Services
{
    /// <summary>
    /// Central action execution service that routes actions to appropriate handlers
    /// </summary>
    public class ActionExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ActionExecutor> _logger;
        private readonly Dictionary<string, IActionHandler> _handlers;

        public ActionExecutor(IServiceProvider serviceProvider, ILogger<ActionExecutor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _handlers = new Dictionary<string, IActionHandler>();

            // Register handlers
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

            _handlers["Spotify"] = new SpotifyActionHandler(httpClientFactory);
            _handlers["HomeAssistant"] = new HomeAssistantActionHandler(httpClientFactory);
            _handlers["PhilipsHue"] = new PhilipsHueActionHandler(httpClientFactory);
            _handlers["Slack"] = new SlackActionHandler(httpClientFactory);
            _handlers["Discord"] = new DiscordActionHandler(httpClientFactory);
        }

        public async Task<ActionResult> ExecuteActionAsync(AutomationAction action, AutomationDbContext db)
        {
            _logger.LogInformation("Executing action: {ActionType}", action.ActionType);

            try
            {
                // Built-in FocusDeck actions
                if (action.ActionType.StartsWith("focusdeck."))
                {
                    return await ExecuteFocusDeckAction(action, db);
                }

                // Service-specific actions - route to handler
                var serviceType = GetServiceTypeFromAction(action.ActionType);
                if (!string.IsNullOrEmpty(serviceType) && _handlers.ContainsKey(serviceType))
                {
                    return await _handlers[serviceType].ExecuteAsync(action, db, _logger);
                }

                // General actions
                if (action.ActionType.StartsWith("general."))
                {
                    return await ExecuteGeneralAction(action);
                }

                // Windows actions (will be handled by Windows app)
                if (action.ActionType.StartsWith("windows."))
                {
                    return await QueueWindowsAction(action, db);
                }

                _logger.LogWarning("No handler found for action type: {ActionType}", action.ActionType);
                return new ActionResult { Success = false, Message = $"Action type not implemented: {action.ActionType}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action: {ActionType}", action.ActionType);
                return new ActionResult { Success = false, Message = $"Action failed: {ex.Message}" };
            }
        }

        private string GetServiceTypeFromAction(string actionType)
        {
            if (actionType.StartsWith("spotify.")) return "Spotify";
            if (actionType.StartsWith("ha.")) return "HomeAssistant";
            if (actionType.StartsWith("hue.")) return "PhilipsHue";
            if (actionType.StartsWith("slack.")) return "Slack";
            if (actionType.StartsWith("discord.")) return "Discord";
            if (actionType.StartsWith("notion.")) return "Notion";
            if (actionType.StartsWith("todoist.")) return "Todoist";
            if (actionType.StartsWith("calendar.")) return "GoogleCalendar";
            if (actionType.StartsWith("drive.")) return "GoogleDrive";
            if (actionType.StartsWith("canvas.")) return "Canvas";
            if (actionType.StartsWith("gemini.")) return "GoogleGenerativeAI";
            return string.Empty;
        }

        private async Task<ActionResult> ExecuteFocusDeckAction(AutomationAction action, AutomationDbContext db)
        {
            switch (action.ActionType)
            {
                case ActionTypes.ShowNotification:
                    return await ShowNotification(action);

                case ActionTypes.StartTimer:
                    return await StartTimer(action);

                case ActionTypes.StopTimer:
                    return await StopTimer(action);

                case ActionTypes.CreateTask:
                    return await CreateTask(action, db);

                case ActionTypes.CompleteTask:
                    return await CompleteTask(action, db);

                case ActionTypes.Wait:
                    return await WaitAction(action);

                case ActionTypes.LogActivity:
                    return await LogEvent(action);

                case ActionTypes.PlaySound:
                    return await PlaySound(action);

                default:
                    return new ActionResult { Success = false, Message = $"FocusDeck action not implemented: {action.ActionType}" };
            }
        }

        private async Task<ActionResult> ExecuteGeneralAction(AutomationAction action)
        {
            switch (action.ActionType)
            {
                case ActionTypes.HttpRequest:
                    return await MakeHttpRequest(action);

                case ActionTypes.OpenURL:
                    return await OpenUrl(action);

                case ActionTypes.RunCommand:
                    return await WriteToFile(action);

                default:
                    return new ActionResult { Success = false, Message = $"General action not implemented: {action.ActionType}" };
            }
        }

        private Task<ActionResult> ShowNotification(AutomationAction action)
        {
            var title = action.Settings.GetValueOrDefault("title", "FocusDeck");
            var message = action.Settings.GetValueOrDefault("message", "Automation triggered");

            _logger.LogInformation("Notification: {Title} - {Message}", title, message);
            
            // This will be broadcasted to connected clients via SignalR/WebSockets in the future
            return Task.FromResult(new ActionResult { Success = true, Message = "Notification queued", Data = new { title, message } });
        }

        private Task<ActionResult> StartTimer(AutomationAction action)
        {
            var duration = int.Parse(action.Settings.GetValueOrDefault("duration", "25"));
            var timerType = action.Settings.GetValueOrDefault("timerType", "focus");

            _logger.LogInformation("Starting {Type} timer for {Duration} minutes", timerType, duration);
            
            // TODO: Integrate with timer service when it's implemented
            return Task.FromResult(new ActionResult { Success = true, Message = $"Timer started: {duration} minutes", Data = new { duration, timerType } });
        }

        private Task<ActionResult> StopTimer(AutomationAction action)
        {
            _logger.LogInformation("Stopping timer");
            return Task.FromResult(new ActionResult { Success = true, Message = "Timer stopped" });
        }

        private Task<ActionResult> CreateTask(AutomationAction action, AutomationDbContext db)
        {
            var title = action.Settings.GetValueOrDefault("title", "New Task");
            var description = action.Settings.GetValueOrDefault("description", "");
            var priority = action.Settings.GetValueOrDefault("priority", "medium");
            var dueDate = action.Settings.GetValueOrDefault("dueDate", "");

            _logger.LogInformation("Creating task: {Title} (Priority: {Priority})", title, priority);

            // TODO: Integrate with actual task service/database
            return Task.FromResult(new ActionResult { Success = true, Message = "Task created", Data = new { title, description, priority, dueDate } });
        }

        private Task<ActionResult> CompleteTask(AutomationAction action, AutomationDbContext db)
        {
            var taskId = action.Settings.GetValueOrDefault("taskId", "");
            
            _logger.LogInformation("Completing task: {TaskId}", taskId);
            
            // TODO: Integrate with actual task service/database
            return Task.FromResult(new ActionResult { Success = true, Message = "Task completed", Data = new { taskId } });
        }

        private async Task<ActionResult> WaitAction(AutomationAction action)
        {
            var seconds = int.Parse(action.Settings.GetValueOrDefault("seconds", "5"));
            _logger.LogInformation("Waiting {Seconds} seconds", seconds);
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            return new ActionResult { Success = true, Message = $"Waited {seconds} seconds" };
        }

        private Task<ActionResult> LogEvent(AutomationAction action)
        {
            var message = action.Settings.GetValueOrDefault("message", "");
            var level = action.Settings.GetValueOrDefault("level", "info");

            switch (level.ToLower())
            {
                case "info":
                    _logger.LogInformation("Automation Log: {Message}", message);
                    break;
                case "warning":
                    _logger.LogWarning("Automation Log: {Message}", message);
                    break;
                case "error":
                    _logger.LogError("Automation Log: {Message}", message);
                    break;
            }

            return Task.FromResult(new ActionResult { Success = true, Message = "Event logged" });
        }

        private Task<ActionResult> PlaySound(AutomationAction action)
        {
            var soundName = action.Settings.GetValueOrDefault("sound", "notification");
            _logger.LogInformation("Playing sound: {Sound}", soundName);
            
            // This will be sent to connected clients
            return Task.FromResult(new ActionResult { Success = true, Message = "Sound queued", Data = new { soundName } });
        }

        private async Task<ActionResult> MakeHttpRequest(AutomationAction action)
        {
            var url = action.Settings.GetValueOrDefault("url", "");
            var method = action.Settings.GetValueOrDefault("method", "GET");
            var body = action.Settings.GetValueOrDefault("body", "");
            var headers = action.Settings.GetValueOrDefault("headers", "");

            if (string.IsNullOrEmpty(url))
                return new ActionResult { Success = false, Message = "URL is required" };

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            try
            {
                HttpResponseMessage response;

                if (method.ToUpper() == "POST")
                {
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    response = await httpClient.PostAsync(url, content);
                }
                else if (method.ToUpper() == "PUT")
                {
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    response = await httpClient.PutAsync(url, content);
                }
                else
                {
                    response = await httpClient.GetAsync(url);
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                
                return new ActionResult 
                { 
                    Success = response.IsSuccessStatusCode, 
                    Message = $"HTTP {method} to {url}: {response.StatusCode}",
                    Data = new { statusCode = (int)response.StatusCode, responseBody = responseBody.Length > 200 ? responseBody.Substring(0, 200) + "..." : responseBody }
                };
            }
            catch (Exception ex)
            {
                return new ActionResult { Success = false, Message = $"HTTP request failed: {ex.Message}" };
            }
        }

        private Task<ActionResult> OpenUrl(AutomationAction action)
        {
            var url = action.Settings.GetValueOrDefault("url", "");
            _logger.LogInformation("Opening URL: {Url}", url);
            
            // This will be sent to connected clients to open in browser
            return Task.FromResult(new ActionResult { Success = true, Message = "URL open command queued", Data = new { url } });
        }

        private async Task<ActionResult> WriteToFile(AutomationAction action)
        {
            var filePath = action.Settings.GetValueOrDefault("filePath", "");
            var content = action.Settings.GetValueOrDefault("content", "");
            var append = bool.Parse(action.Settings.GetValueOrDefault("append", "false"));

            if (string.IsNullOrEmpty(filePath))
                return new ActionResult { Success = false, Message = "File path is required" };

            try
            {
                if (append)
                {
                    await File.AppendAllTextAsync(filePath, content);
                }
                else
                {
                    await File.WriteAllTextAsync(filePath, content);
                }

                return new ActionResult { Success = true, Message = $"Written to {filePath}" };
            }
            catch (Exception ex)
            {
                return new ActionResult { Success = false, Message = $"File write failed: {ex.Message}" };
            }
        }

        private Task<ActionResult> QueueWindowsAction(AutomationAction action, AutomationDbContext db)
        {
            _logger.LogInformation("Queuing Windows action: {ActionType}", action.ActionType);

            // TODO: Store in a queue table for Windows app to poll/consume
            // For now, just log it
            return Task.FromResult(new ActionResult 
            { 
                Success = true, 
                Message = "Windows action queued for desktop app",
                Data = new { action = action.ActionType, settings = action.Settings }
            });
        }
    }
}
