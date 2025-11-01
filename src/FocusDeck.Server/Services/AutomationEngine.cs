using FocusDeck.Shared.Models.Automations;
using FocusDeck.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FocusDeck.Server.Services
{
    /// <summary>
    /// Background service that monitors automation triggers and executes actions
    /// </summary>
    public class AutomationEngine : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutomationEngine> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public AutomationEngine(IServiceProvider serviceProvider, ILogger<AutomationEngine> logger)
        {
            _serviceProvider = serviceProvider;
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
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

            // Get all enabled automations from database
            var automations = await db.Automations
                .Where(a => a.IsEnabled)
                .ToListAsync();

            _logger.LogDebug("Checking {Count} enabled automations", automations.Count);

            foreach (var automation in automations)
            {
                try
                {
                    if (await ShouldTrigger(automation.Trigger, automation.LastRunAt))
                    {
                        _logger.LogInformation("Triggering automation: {Name} (ID: {Id})", automation.Name, automation.Id);
                        await ExecuteAutomation(automation, db);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing automation {Name} (ID: {Id})", automation.Name, automation.Id);
                }
            }
        }

        private async Task<bool> ShouldTrigger(AutomationTrigger trigger, DateTime? lastRunAt)
        {
            return trigger.TriggerType switch
            {
                TriggerTypes.AtSpecificTime => await CheckTimeTrigger(trigger, lastRunAt),
                TriggerTypes.RecurringInterval => await CheckIntervalTrigger(trigger, lastRunAt),
                TriggerTypes.UserIdle => await CheckIdleTrigger(trigger),
                TriggerTypes.FileChanged => await CheckFileTrigger(trigger),
                _ => false
            };
        }

        private async Task<bool> CheckTimeTrigger(AutomationTrigger trigger, DateTime? lastRunAt)
        {
            if (!trigger.Settings.TryGetValue("time", out var timeStr))
                return false;

            if (!TimeSpan.TryParse(timeStr, out var targetTime))
                return false;

            var now = DateTime.Now.TimeOfDay;
            // Check if we're within 1 minute of target time
            var diff = Math.Abs((now - targetTime).TotalMinutes);
            
            // Only trigger if within window and hasn't run in the last 2 minutes
            var canRun = diff < 1;
            if (canRun && lastRunAt.HasValue)
            {
                var timeSinceLastRun = DateTime.UtcNow - lastRunAt.Value;
                canRun = timeSinceLastRun.TotalMinutes >= 2;
            }
            
            return await Task.FromResult(canRun);
        }

        private async Task<bool> CheckIntervalTrigger(AutomationTrigger trigger, DateTime? lastRunAt)
        {
            if (!trigger.Settings.TryGetValue("minutes", out var minutesStr))
                return false;

            if (!int.TryParse(minutesStr, out var minutes))
                return false;

            if (!lastRunAt.HasValue)
                return true; // Never run before, trigger it

            var timeSinceLastRun = DateTime.UtcNow - lastRunAt.Value;
            return await Task.FromResult(timeSinceLastRun.TotalMinutes >= minutes);
        }

        private async Task<bool> CheckIdleTrigger(AutomationTrigger trigger)
        {
            // TODO: Implement system idle time checking
            return await Task.FromResult(false);
        }

        private async Task<bool> CheckFileTrigger(AutomationTrigger trigger)
        {
            // TODO: Implement FileSystemWatcher integration
            return await Task.FromResult(false);
        }

        private async Task ExecuteAutomation(Automation automation, AutomationDbContext db)
        {
            var stopwatch = Stopwatch.StartNew();
            var success = true;
            string? errorMessage = null;

            try
            {
                foreach (var action in automation.Actions)
                {
                    try
                    {
                        await ExecuteAction(action, automation);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing action {ActionType}", action.ActionType);
                        success = false;
                        errorMessage = ex.Message;
                        break; // Stop executing remaining actions
                    }
                }

                // Update last run time
                automation.LastRunAt = DateTime.UtcNow;
                automation.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                _logger.LogInformation("Automation '{Name}' executed successfully", automation.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in automation execution");
                success = false;
                errorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();

                // Log execution to database
                var execution = new AutomationExecution
                {
                    AutomationId = automation.Id,
                    ExecutedAt = DateTime.UtcNow,
                    Success = success,
                    ErrorMessage = errorMessage,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    TriggerData = $"Triggered by: {automation.Trigger.TriggerType}"
                };

                db.AutomationExecutions.Add(execution);
                await db.SaveChangesAsync();

                _logger.LogInformation("Automation execution logged: {Name} - Success: {Success}, Duration: {Duration}ms",
                    automation.Name, success, stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task ExecuteAction(AutomationAction action, Automation automation)
        {
            _logger.LogInformation("Executing action: {ActionType}", action.ActionType);

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
                    _logger.LogWarning("Action type {ActionType} not implemented yet", action.ActionType);
                    break;
            }
        }

        private async Task SendNotification(AutomationAction action)
        {
            var title = action.Settings.GetValueOrDefault("title", "FocusDeck");
            var message = action.Settings.GetValueOrDefault("message", "Automation triggered");
            
            _logger.LogInformation("Notification: {Title} - {Message}", title, message);
            // TODO: Implement actual notification system
            await Task.CompletedTask;
        }

        private async Task StartTimer(AutomationAction action)
        {
            var duration = action.Settings.GetValueOrDefault("duration", "25");
            _logger.LogInformation("Starting timer for {Duration} minutes", duration);
            // TODO: Integrate with timer service
            await Task.CompletedTask;
        }

        private async Task CreateTask(AutomationAction action)
        {
            var title = action.Settings.GetValueOrDefault("title", "New Task");
            var priority = action.Settings.GetValueOrDefault("priority", "medium");
            
            _logger.LogInformation("Creating task: {Title} (Priority: {Priority})", title, priority);
            // TODO: Integrate with task service
            await Task.CompletedTask;
        }

        private async Task RunCommand(AutomationAction action)
        {
            var command = action.Settings.GetValueOrDefault("command", "");
            if (string.IsNullOrEmpty(command))
                return;

            _logger.LogInformation("Running command: {Command}", command);
            // TODO: Implement safe command execution
            await Task.CompletedTask;
        }

        private async Task MakeHttpRequest(AutomationAction action)
        {
            var url = action.Settings.GetValueOrDefault("url", "");
            var method = action.Settings.GetValueOrDefault("method", "GET");
            
            if (string.IsNullOrEmpty(url))
                return;

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            try
            {
                if (method.ToUpper() == "POST")
                {
                    var body = action.Settings.GetValueOrDefault("body", "{}");
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();
                }
                else
                {
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                }
                
                _logger.LogInformation("HTTP {Method} to {Url} successful", method, url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed: {Method} {Url}", method, url);
                throw;
            }
        }

        private async Task WaitAction(AutomationAction action)
        {
            if (action.Settings.TryGetValue("seconds", out var secondsStr))
            {
                if (int.TryParse(secondsStr, out var seconds))
                {
                    _logger.LogInformation("Waiting {Seconds} seconds", seconds);
                    await Task.Delay(TimeSpan.FromSeconds(seconds));
                }
            }
        }
    }
}
