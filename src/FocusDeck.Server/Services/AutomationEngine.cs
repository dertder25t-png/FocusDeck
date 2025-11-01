using FocusDeck.Shared.Models.Automations;

namespace FocusDeck.Server.Services
{
    /// <summary>
    /// Background service that monitors automation triggers and executes actions
    /// </summary>
    public class AutomationEngine : BackgroundService
    {
        private readonly ILogger<AutomationEngine> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public AutomationEngine(ILogger<AutomationEngine> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Automation Engine started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndExecuteAutomations();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in automation engine");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Automation Engine stopped");
        }

        private async Task CheckAndExecuteAutomations()
        {
            // Get all enabled automations from AutomationsController
            var automations = AutomationsController.GetAutomations()
                .Where(a => a.IsEnabled)
                .ToList();

            foreach (var automation in automations)
            {
                try
                {
                    if (await ShouldTrigger(automation.Trigger))
                    {
                        _logger.LogInformation($"Triggering automation: {automation.Name}");
                        await ExecuteAutomation(automation);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing automation {automation.Name}");
                }
            }
        }

        private async Task<bool> ShouldTrigger(AutomationTrigger trigger)
        {
            return trigger.TriggerType switch
            {
                TriggerTypes.AtSpecificTime => await CheckTimeTrigger(trigger),
                TriggerTypes.RecurringInterval => await CheckIntervalTrigger(trigger),
                TriggerTypes.UserIdle => await CheckIdleTrigger(trigger),
                TriggerTypes.FileChanged => await CheckFileTrigger(trigger),
                _ => false
            };
        }

        private async Task<bool> CheckTimeTrigger(AutomationTrigger trigger)
        {
            if (!trigger.Settings.TryGetValue("time", out var timeStr))
                return false;

            if (!TimeSpan.TryParse(timeStr, out var targetTime))
                return false;

            var now = DateTime.Now.TimeOfDay;
            // Check if we're within 1 minute of target time
            var diff = Math.Abs((now - targetTime).TotalMinutes);
            
            return diff < 1;
        }

        private async Task<bool> CheckIntervalTrigger(AutomationTrigger trigger)
        {
            // Check if enough time has passed since last run
            // This would need to track last run times
            return await Task.FromResult(false);
        }

        private async Task<bool> CheckIdleTrigger(AutomationTrigger trigger)
        {
            // Would check system idle time
            return await Task.FromResult(false);
        }

        private async Task<bool> CheckFileTrigger(AutomationTrigger trigger)
        {
            // Would use FileSystemWatcher
            return await Task.FromResult(false);
        }

        private async Task ExecuteAutomation(Automation automation)
        {
            foreach (var action in automation.Actions)
            {
                try
                {
                    await ExecuteAction(action, automation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing action {action.ActionType}");
                }
            }

            automation.LastRunAt = DateTime.UtcNow;
            _logger.LogInformation($"Automation '{automation.Name}' executed successfully");
        }

        private async Task ExecuteAction(AutomationAction action, Automation automation)
        {
            _logger.LogInformation($"Executing action: {action.ActionType}");

            switch (action.ActionType)
            {
                case ActionTypes.ShowNotification:
                    await SendNotification(action);
                    break;
                
                case ActionTypes.StartTimer:
                    await StartTimer(action);
                    break;
                
                case ActionTypes.CreateTask:
                    await CreateTask(action);
                    break;
                
                case ActionTypes.RunCommand:
                    await RunCommand(action);
                    break;
                
                case ActionTypes.HttpRequest:
                    await MakeHttpRequest(action);
                    break;

                case ActionTypes.Wait:
                    await WaitAction(action);
                    break;

                default:
                    _logger.LogWarning($"Action type {action.ActionType} not implemented yet");
                    break;
            }
        }

        private async Task SendNotification(AutomationAction action)
        {
            var title = action.Settings.GetValueOrDefault("title", "FocusDeck");
            var message = action.Settings.GetValueOrDefault("message", "Automation triggered");
            
            _logger.LogInformation($"Notification: {title} - {message}");
            // In production, this would send actual notifications
            await Task.CompletedTask;
        }

        private async Task StartTimer(AutomationAction action)
        {
            var duration = action.Settings.GetValueOrDefault("duration", "25");
            _logger.LogInformation($"Starting timer for {duration} minutes");
            // Would interact with timer service
            await Task.CompletedTask;
        }

        private async Task CreateTask(AutomationAction action)
        {
            var title = action.Settings.GetValueOrDefault("title", "New Task");
            var priority = action.Settings.GetValueOrDefault("priority", "medium");
            
            _logger.LogInformation($"Creating task: {title} (Priority: {priority})");
            // Would interact with task service
            await Task.CompletedTask;
        }

        private async Task RunCommand(AutomationAction action)
        {
            var command = action.Settings.GetValueOrDefault("command", "");
            if (string.IsNullOrEmpty(command))
                return;

            _logger.LogInformation($"Running command: {command}");
            // Would execute system command
            await Task.CompletedTask;
        }

        private async Task MakeHttpRequest(AutomationAction action)
        {
            var url = action.Settings.GetValueOrDefault("url", "");
            var method = action.Settings.GetValueOrDefault("method", "GET");
            
            if (string.IsNullOrEmpty(url))
                return;

            using var httpClient = new HttpClient();
            
            try
            {
                if (method.ToUpper() == "POST")
                {
                    var body = action.Settings.GetValueOrDefault("body", "{}");
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    await httpClient.PostAsync(url, content);
                }
                else
                {
                    await httpClient.GetAsync(url);
                }
                
                _logger.LogInformation($"HTTP {method} to {url} successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"HTTP request failed: {method} {url}");
            }
        }

        private async Task WaitAction(AutomationAction action)
        {
            if (action.Settings.TryGetValue("seconds", out var secondsStr))
            {
                if (int.TryParse(secondsStr, out var seconds))
                {
                    _logger.LogInformation($"Waiting {seconds} seconds");
                    await Task.Delay(TimeSpan.FromSeconds(seconds));
                }
            }
        }
    }
}
