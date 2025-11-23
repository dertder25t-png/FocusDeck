using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Persistence;
using FocusDeck.Server.Hubs;
using FocusDeck.Server.Services.Automations;
using FocusDeck.Server.Services.Context;
using Microsoft.AspNetCore.SignalR;
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
        private readonly ActionExecutor _actionExecutor;
        private readonly IContextEventBus _eventBus;
        private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;
        private readonly IYamlAutomationLoader _yamlLoader;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        // Cache for active automations
        private List<Automation> _activeAutomations = new();

        public AutomationEngine(
            IServiceProvider serviceProvider, 
            ILogger<AutomationEngine> logger,
            ActionExecutor actionExecutor,
            IContextEventBus eventBus,
            IHubContext<NotificationsHub, INotificationClient> hubContext,
            IYamlAutomationLoader yamlLoader)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _actionExecutor = actionExecutor;
            _eventBus = eventBus;
            _hubContext = hubContext;
            _yamlLoader = yamlLoader;

            _eventBus.OnContextSnapshotCreated += HandleContextSnapshot;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await ReloadAutomations();
            await base.StartAsync(cancellationToken);
        }

        private async Task ReloadAutomations()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            var automations = await db.Automations
                .Where(a => a.IsEnabled)
                .ToListAsync();

            // Re-parse YAML for consistency
            foreach (var automation in automations)
            {
                if (!string.IsNullOrWhiteSpace(automation.YamlDefinition))
                {
                    try
                    {
                        _yamlLoader.UpdateAutomationFromYaml(automation, automation.YamlDefinition);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse YAML for automation {Id}", automation.Id);
                    }
                }
            }

            _activeAutomations = automations;
            _logger.LogInformation("Loaded {Count} active automations.", _activeAutomations.Count);
        }

        private async Task HandleContextSnapshot(ContextSnapshot snapshot)
        {
            // We need to iterate over a copy to avoid thread safety issues if reloading
            var automations = _activeAutomations.ToList();

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

            foreach (var automation in automations)
            {
                try
                {
                    bool triggered = EvaluateTrigger(automation, snapshot);

                    if (triggered)
                    {
                        _logger.LogInformation("Triggering automation from context: {Name}", automation.Name);
                        await ExecuteAutomation(automation, db);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating automation {Id}", automation.Id);
                }
            }
        }

        private bool EvaluateTrigger(Automation automation, ContextSnapshot snapshot)
        {
            if (automation.Trigger == null) return false;

            if (automation.Trigger.Type == "AppOpen" || automation.Trigger.TriggerType == "AppOpen")
            {
                var targetApp = automation.Trigger.Settings.GetValueOrDefault("app")
                                ?? automation.Trigger.Settings.GetValueOrDefault("name");

                if (string.IsNullOrEmpty(targetApp)) return false;

                var windowSlice = snapshot.Slices.FirstOrDefault(s => s.SourceType == ContextSourceType.DesktopActiveWindow);
                if (windowSlice != null && windowSlice.Data != null)
                {
                     try
                     {
                         var appName = windowSlice.Data["App"]?.ToString() ??
                                       windowSlice.Data["ActiveApplication"]?.ToString() ??
                                       windowSlice.Data["Application"]?.ToString();

                         if (!string.IsNullOrEmpty(appName) && appName.Contains(targetApp, StringComparison.OrdinalIgnoreCase))
                         {
                             return true;
                         }

                         var title = windowSlice.Data["Title"]?.ToString() ??
                                     windowSlice.Data["ActiveWindowTitle"]?.ToString();

                         if (!string.IsNullOrEmpty(title) && title.Contains(targetApp, StringComparison.OrdinalIgnoreCase))
                         {
                             return true;
                         }
                     }
                     catch (Exception ex)
                     {
                         _logger.LogWarning(ex, "Failed to parse window slice data during trigger evaluation.");
                     }
                }
            }
            else if (automation.Trigger.Type == "CalendarEvent" || automation.Trigger.TriggerType == "CalendarEvent")
            {
                var calendarSlice = snapshot.Slices.FirstOrDefault(s => s.SourceType == ContextSourceType.GoogleCalendar);
                if (calendarSlice != null && calendarSlice.Data != null)
                {
                    try
                    {
                        var eventTitle = calendarSlice.Data["event"]?.ToString();
                        var keyword = automation.Trigger.Settings.GetValueOrDefault("keyword")
                                      ?? automation.Trigger.Settings.GetValueOrDefault("contains");

                        if (string.IsNullOrEmpty(keyword))
                        {
                            // Trigger on ANY calendar event if no keyword specified
                            return true;
                        }

                        if (!string.IsNullOrEmpty(eventTitle) && eventTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse calendar slice data.");
                    }
                }
            }

            return false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Automation Engine started");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ReloadAutomations();
                await CheckAndExecuteAutomations(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Automation Engine stopped");
        }

        private async Task CheckAndExecuteAutomations(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            var automations = _activeAutomations.ToList();

            foreach (var automation in automations)
            {
                try
                {
                    // Check time-based triggers
                    if (automation.Trigger?.Type == "Time" || automation.Trigger?.TriggerType == TriggerTypes.AtSpecificTime)
                    {
                        if (await CheckTimeTrigger(automation.Trigger, automation.LastRunAt))
                        {
                            _logger.LogInformation("Triggering automation: {Name} (ID: {Id})", automation.Name, automation.Id);
                            await ExecuteAutomation(automation, db);
                        }
                    }
                    else if (automation.Trigger?.Type == "Interval" || automation.Trigger?.TriggerType == TriggerTypes.RecurringInterval)
                    {
                        if (await CheckIntervalTrigger(automation.Trigger, automation.LastRunAt))
                        {
                            _logger.LogInformation("Triggering automation: {Name} (ID: {Id})", automation.Name, automation.Id);
                            await ExecuteAutomation(automation, db);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing automation {Name} (ID: {Id})", automation.Name, automation.Id);
                }
            }
        }

        private async Task<bool> CheckTimeTrigger(AutomationTrigger trigger, DateTime? lastRunAt)
        {
            if (!trigger.Settings.TryGetValue("time", out var timeStr))
                return false;

            if (!TimeSpan.TryParse(timeStr, out var targetTime))
                return false;

            var now = DateTime.Now.TimeOfDay;
            var diff = Math.Abs((now - targetTime).TotalMinutes);
            
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
                return true;

            var timeSinceLastRun = DateTime.UtcNow - lastRunAt.Value;
            return await Task.FromResult(timeSinceLastRun.TotalMinutes >= minutes);
        }

        private async Task ExecuteAutomation(Automation automation, AutomationDbContext db)
        {
            var stopwatch = Stopwatch.StartNew();
            var success = true;
            string? errorMessage = null;

            try
            {
                // Only load the automation entity for tracking status updates
                // This avoids attaching the entire object graph with potentially new (untracked) action objects
                // which would cause duplication.
                var trackedAutomation = await db.Automations.FindAsync(automation.Id);

                if (trackedAutomation != null)
                {
                    foreach (var action in automation.Actions)
                    {
                        try
                        {
                            await ExecuteAction(action, automation, db);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing action {ActionType}", action.ActionType);
                            success = false;
                            errorMessage = ex.Message;
                            break;
                        }
                    }

                    trackedAutomation.LastRunAt = DateTime.UtcNow;
                    trackedAutomation.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();

                    _logger.LogInformation("Automation '{Name}' executed successfully", automation.Name);
                }
                else
                {
                    _logger.LogWarning("Automation {Id} not found in database for execution update", automation.Id);
                }
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
            }
        }

        private async Task ExecuteAction(AutomationAction action, Automation automation, AutomationDbContext db)
        {
            _logger.LogInformation("Executing action: {ActionType}", action.ActionType);

            var result = await _actionExecutor.ExecuteActionAsync(action, db);
            
            if (!result.Success)
            {
                _logger.LogWarning("Action failed: {Message}", result.Message);
                throw new Exception(result.Message);
            }
            else
            {
                _logger.LogInformation("Action executed successfully: {Message}", result.Message);
            }
        }
    }
}
