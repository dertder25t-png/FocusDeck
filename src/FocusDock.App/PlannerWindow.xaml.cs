using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Input;
using FocusDock.Core.Services;
using FocusDock.Data.Models;
using System.Collections.Generic;

namespace FocusDock.App;

public partial class PlannerWindow : Window
{
    private readonly TodoService _todoService;
    private readonly CalendarService _calendarService;
    private string _currentView = "myday"; // myday, today, upcoming, overdue, all, canvas, calendar, completed
    private DateTime? _quickAddDueDate = null;
    private List<string> _quickAddTags = new();
    private int _quickAddPriority = 2; // Default: Normal priority
    private string _quickAddDueDateMode = "timeline";
    private string _quickAddReminderMode = "none";
    private string _calendarRange = "7days"; // today, 3days, 7days, month, year
    private string _sortMode = "priority"; // priority, duedate, created, alphabetical
    private List<string> _activeFilters = new();
    private List<string> _selectedCategories = new(); // Custom categories filter
    private TodoItem? _currentEditingTask = null; // Track currently editing task
    private DateTime? _selectedTimelineDate = DateTime.Today;
    private readonly Dictionary<DateTime, Border> _timelineDayContainers = new();
    private bool _isSyncingQuickControls = false;

    public PlannerWindow(TodoService todoService, CalendarService calendarService)
    {
        InitializeComponent();
        _todoService = todoService;
        _calendarService = calendarService;
        
        // Subscribe to changes
        _todoService.TodosChanged += (s, e) => 
        {
            try 
            { 
                Dispatcher.BeginInvoke(new Action(() => RefreshView())); 
            } 
            catch { /* Ignore if window is closing */ }
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Initialize panel state
            if (RightPanelColumn != null && RightPanelSplitter != null && RightPanel != null && BtnTogglePanel != null)
            {
                // Ensure panel starts visible
                BtnTogglePanel.IsChecked = true;
                RightPanelColumn.Width = new GridLength(450);
                RightPanelSplitter.Visibility = Visibility.Visible;
                RightPanel.Visibility = Visibility.Visible;
            }
            
            // Set default view
            if (CmbViewSelector != null)
            {
                CmbViewSelector.SelectedIndex = 0; // My Day
            }
            
            // Set default calendar range (Week)
            if (CmbCalendarRange != null)
            {
                CmbCalendarRange.SelectedIndex = 2; // This Week
            }
            
            ApplyDueDateMode(_selectedTimelineDate.HasValue ? "timeline" : "none", updateUi: false);
            ApplyReminderMode("none", updateUi: false);
            SyncQuickAddControls();
            UpdateActiveMetadataDisplay();

            RefreshView();
            
            if (TxtQuickAdd != null)
            {
                TxtQuickAdd.Focus();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading planner: {ex.Message}\n\nStack: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== View Management =====
    
    private void OnViewSelectorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbViewSelector.SelectedItem is ComboBoxItem item)
        {
            var view = item.Tag?.ToString() ?? "myday";
            SetActiveView(view);
        }
    }

    private void OnCategoryFilterClick(object sender, RoutedEventArgs e)
    {
        // TODO: Show multi-select category filter dialog
        System.Windows.MessageBox.Show("Category filter coming soon! You'll be able to select multiple custom categories.", "Categories", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnPanelToggle(object sender, RoutedEventArgs e)
    {
        try
        {
            if (RightPanelColumn == null || RightPanelSplitter == null || RightPanel == null || BtnTogglePanel == null)
            {
                return; // Not fully initialized yet
            }

            if (BtnTogglePanel.IsChecked == true)
            {
                // Show panel
                RightPanelColumn.Width = new GridLength(450);
                RightPanelSplitter.Visibility = Visibility.Visible;
                RightPanel.Visibility = Visibility.Visible;
            }
            else
            {
                // Hide panel
                RightPanelColumn.Width = new GridLength(0);
                RightPanelSplitter.Visibility = Visibility.Collapsed;
                RightPanel.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling panel: {ex.Message}");
        }
    }

    private void SetActiveView(string view)
    {
        _currentView = view;

        // Update title and subtitle based on view
        (TxtViewTitle.Text, TxtViewSubtitle.Text) = view switch
        {
            "myday" => ("My Day", GetDayBasedSubtitle()),
            "today" => ("Today", GetDayBasedSubtitle()),
            "upcoming" => ("Upcoming", GetDayBasedSubtitle()),
            "overdue" => ("Overdue", GetDayBasedSubtitle()),
            "all" => ("All Tasks", GetDayBasedSubtitle()),
            "canvas" => ("Canvas Assignments", GetDayBasedSubtitle()),
            "calendar" => ("Calendar & Events", GetDayBasedSubtitle()),
            "completed" => ("Completed", GetDayBasedSubtitle()),
            _ => ("My Planner", "Organize your tasks and calendar")
        };

        RefreshView();
    }

    private string GetDayBasedSubtitle()
    {
        // Generate subtitle based on calendar range
        return _calendarRange switch
        {
            "today" => "Showing tasks for today",
            "3days" => "Showing tasks for the next 3 days",
            "7days" => "Showing tasks for the next 7 days",
            "month" => "Showing tasks for this month",
            "year" => "Showing tasks for this year",
            _ => "Organize your tasks"
        };
    }

    private void OnSortClick(object sender, RoutedEventArgs e)
    {
        // Cycle through sort modes
        _sortMode = _sortMode switch
        {
            "priority" => "duedate",
            "duedate" => "created",
            "created" => "alphabetical",
            _ => "priority"
        };
        
        System.Windows.MessageBox.Show($"Sorted by: {_sortMode}", "Sort Mode", MessageBoxButton.OK, MessageBoxImage.Information);
        RefreshView();
    }

    private void OnClearCompletedClick(object sender, RoutedEventArgs e)
    {
        var completed = _todoService.GetCompletedTodos();
        if (completed.Count == 0)
        {
            System.Windows.MessageBox.Show("No completed tasks to clear.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Delete {completed.Count} completed task(s)?",
            "Clear Completed",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            _todoService.DeleteCompleted();
            RefreshView();
        }
    }

    private void OnFilterClick(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Filter options coming soon!", "Filters", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnQuickDueDateSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingQuickControls)
        {
            return;
        }

        if (CmbQuickDueDate?.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        var previousMode = _quickAddDueDateMode;
        var mode = item.Tag?.ToString() ?? "timeline";

        if (mode == "custom")
        {
            var dialog = new Controls.InputDialog("Custom Due Date", "Enter a date (e.g., 'next Friday' or '11/15'):", "");
            if (dialog.ShowDialog() == true)
            {
                var parsed = ParseNaturalDate(dialog.ResultText ?? string.Empty)?.Date;
                if (parsed.HasValue)
                {
                    ApplyDueDateMode("custom", parsed.Value);
                    return;
                }

                System.Windows.MessageBox.Show("Couldn't understand that date. Please try again.", "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ApplyDueDateMode(previousMode);
            return;
        }

        if (mode == "timeline" && !_selectedTimelineDate.HasValue)
        {
            System.Windows.MessageBox.Show("Select a day in the timeline to use this option.", "No Day Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            ApplyDueDateMode(previousMode);
            return;
        }

        ApplyDueDateMode(mode);
    }

    private void OnQuickPrioritySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingQuickControls)
        {
            return;
        }

        if (CmbQuickPriority?.SelectedValue is not string value)
        {
            return;
        }

        if (int.TryParse(value, out var priority))
        {
            _quickAddPriority = priority;
            UpdateActiveMetadataDisplay();
        }
    }

    private void OnQuickTagSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingQuickControls)
        {
            return;
        }

        if (CmbQuickTags == null)
        {
            return;
        }

        var selected = CmbQuickTags.SelectedItem switch
        {
            string text => text,
            ComboBoxItem combo when combo.Content is string content => content,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(selected))
        {
            AddQuickAddTag(selected);
            CmbQuickTags.Text = string.Join(", ", _quickAddTags);
            CmbQuickTags.SelectedIndex = -1;
            SyncQuickAddControls();
            UpdateActiveMetadataDisplay();
        }
    }

    private void OnQuickTagComboPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.OemComma)
        {
            CommitQuickAddTags();
            if (e.Key != Key.Tab)
            {
                e.Handled = true;
            }
        }
    }

    private void OnQuickTagComboGotFocus(object sender, RoutedEventArgs e)
    {
        if (CmbQuickTags == null)
        {
            return;
        }

        if (CmbQuickTags.Items.Count == 0)
        {
            foreach (var tag in GetKnownTags())
            {
                CmbQuickTags.Items.Add(tag);
            }
        }
    }

    private void OnQuickTagComboLostFocus(object sender, RoutedEventArgs e)
    {
        CommitQuickAddTags();
    }

    private void OnQuickReminderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingQuickControls)
        {
            return;
        }

        if (CmbQuickReminder?.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        var mode = item.Tag?.ToString() ?? "none";
        if (mode == "custom")
        {
            System.Windows.MessageBox.Show("Custom reminders coming soon!", "Reminders", MessageBoxButton.OK, MessageBoxImage.Information);
            ApplyReminderMode(_quickAddReminderMode);
            return;
        }

        ApplyReminderMode(mode);
    }

    private void OnQuickAddClearClick(object sender, RoutedEventArgs e)
    {
        ResetQuickAddMetadata();
        if (TxtQuickAdd != null)
        {
            TxtQuickAdd.Focus();
        }
    }

    private void OnCalendarRangeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbCalendarRange?.SelectedItem is not ComboBoxItem item) return;
        
        var range = item.Tag?.ToString() ?? "7days";
        _calendarRange = range;
        
        // Update subtitle to reflect new range
        if (TxtViewSubtitle != null)
        {
            TxtViewSubtitle.Text = GetDayBasedSubtitle();
        }
        
        // Refresh main view and calendar timeline
        RefreshView();
        RenderCalendarTimeline();
    }

    private void RenderCalendarTimeline()
    {
        if (CalendarTimelinePanel == null) return;

        try
        {
            CalendarTimelinePanel.Children.Clear();
            _timelineDayContainers.Clear();

            // Determine date range
            var startDate = DateTime.Today;
            var days = _calendarRange switch
            {
                "today" => 1,
                "3days" => 3,
                "7days" => 7,
                "month" => 30,
                "year" => 365,
                _ => 7
            };

            // Get events and tasks
            var events = _calendarService.GetUpcomingEvents(days);
            var assignments = _calendarService.GetUpcomingAssignments(days);
            var tasks = _todoService.GetActiveTodos().Where(t => 
                t.DueDate.HasValue && 
                t.DueDate.Value.Date >= startDate && 
                t.DueDate.Value.Date < startDate.AddDays(days)
            ).ToList();

            // Render day-by-day view with dividers
            var totalDays = Math.Min(days, 30);
            for (int i = 0; i < totalDays; i++)
            {
                var currentDate = startDate.AddDays(i);
                var dayEvents = events.Where(e => e.StartTime.Date == currentDate).ToList();
                var dayAssignments = assignments.Where(a => a.DueDate.Date == currentDate).ToList();
                var dayTasks = tasks.Where(t => t.DueDate?.Date == currentDate).ToList();

                var section = CreateDaySection(currentDate, dayEvents, dayAssignments, dayTasks);
                CalendarTimelinePanel.Children.Add(section);
            }

            if (_timelineDayContainers.Count > 0)
            {
                if (!_selectedTimelineDate.HasValue || !_timelineDayContainers.ContainsKey(_selectedTimelineDate.Value.Date))
                {
                    UpdateTimelineSelection(startDate, refreshQuickAdd: false);
                }
                else
                {
                    UpdateTimelineSelection(_selectedTimelineDate, refreshQuickAdd: false);
                }
            }
            else
            {
                UpdateTimelineSelection(null, refreshQuickAdd: false);
            }

            SyncQuickAddControls();
            UpdateActiveMetadataDisplay();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error rendering calendar timeline: {ex.Message}");
        }
    }

    private UIElement CreateDaySection(DateTime date, List<FocusDock.Data.Models.CalendarEvent> events, List<FocusDock.Data.Models.CanvasAssignment> assignments, List<TodoItem> tasks)
    {
        var container = new Border
        {
            Tag = date.Date,
            Margin = new Thickness(0, 0, 0, 12),
            BorderBrush = (SolidColorBrush)FindResource("StrokeBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(8),
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = System.Windows.Media.Brushes.Transparent
        };
        container.MouseLeftButtonUp += OnTimelineDayClicked;

        var dayPanel = new StackPanel { Margin = new Thickness(0) };

        var header = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg3"),
            Padding = new Thickness(20, 14, 20, 14),
            CornerRadius = new CornerRadius(8, 8, 0, 0)
        };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var headerStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        headerStack.Children.Add(new TextBlock
        {
            Text = date.ToString("ddd"),
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = (SolidColorBrush)FindResource("AccentBrush"),
            Margin = new Thickness(0, 0, 10, 0)
        });
        headerStack.Children.Add(new TextBlock
        {
            Text = date.ToString("MMM dd"),
            FontSize = 15,
            FontWeight = FontWeights.Medium,
            Foreground = (SolidColorBrush)FindResource("TextPrimary")
        });
        if (date.Date == DateTime.Today)
        {
            headerStack.Children.Add(new TextBlock
            {
                Text = " • Today",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)FindResource("InfoBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });
        }
        Grid.SetColumn(headerStack, 0);
        headerGrid.Children.Add(headerStack);

        var totalCount = events.Count + assignments.Count + tasks.Count;
        var countBadge = new Border
        {
            Background = (SolidColorBrush)FindResource("AccentBrush"),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 4, 10, 4),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        countBadge.Child = new TextBlock
        {
            Text = totalCount.ToString(),
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        };
        Grid.SetColumn(countBadge, 1);
        headerGrid.Children.Add(countBadge);

        header.Child = headerGrid;
        dayPanel.Children.Add(header);

        var contentWrapper = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg2"),
            CornerRadius = new CornerRadius(0, 0, 8, 8),
            Padding = new Thickness(20, 12, 20, 16)
        };

        var contentPanel = new StackPanel { Margin = new Thickness(0) };

        if (events.Any())
        {
            contentPanel.Children.Add(CreateSectionDivider("📅 Events", events.Count));
            foreach (var evt in events.OrderBy(e => e.StartTime))
            {
                contentPanel.Children.Add(CreateTimelineEventItem(evt.StartTime.ToString("h:mm tt"), evt.Title ?? "Event", "InfoBrush"));
            }
        }

        if (assignments.Any())
        {
            contentPanel.Children.Add(CreateSectionDivider("🎓 Assignments", assignments.Count));
            foreach (var assignment in assignments.OrderBy(a => a.DueDate))
            {
                contentPanel.Children.Add(CreateTimelineEventItem("Due", assignment.Title, "SuccessBrush", assignment.CourseName));
            }
        }

        if (tasks.Any())
        {
            contentPanel.Children.Add(CreateSectionDivider("✓ Tasks", tasks.Count));
            foreach (var task in tasks.OrderByDescending(t => t.Priority))
            {
                var priorityEmoji = task.Priority switch
                {
                    1 => "🔴",
                    2 => "🟡",
                    _ => "🟢"
                };
                contentPanel.Children.Add(CreateTimelineEventItem(priorityEmoji, task.Title, "WarningBrush"));
            }
        }

        if (!events.Any() && !assignments.Any() && !tasks.Any())
        {
            contentPanel.Children.Add(new TextBlock
            {
                Text = "No items planned",
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        contentWrapper.Child = contentPanel;
        dayPanel.Children.Add(contentWrapper);

        container.Child = dayPanel;
        _timelineDayContainers[date.Date] = container;

        return container;
    }

    private Border CreateSectionDivider(string title, int count)
    {
        var divider = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg3"),
            BorderBrush = (SolidColorBrush)FindResource("DarkBg4"),
            BorderThickness = new Thickness(0, 1, 0, 1),
            Padding = new Thickness(20, 8, 20, 8),
            Margin = new Thickness(0, 4, 0, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource("TextSecondary")
        };
        Grid.SetColumn(titleText, 0);
        grid.Children.Add(titleText);

        var countText = new TextBlock
        {
            Text = $"({count})",
            FontSize = 11,
            FontWeight = FontWeights.Medium,
            Foreground = (SolidColorBrush)FindResource("TextSecondary"),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(countText, 1);
        grid.Children.Add(countText);

        divider.Child = grid;
        return divider;
    }

    private Border CreateTimelineEventItem(string time, string title, string colorKey, string? subtitle = null)
    {
        var item = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg2"),
            BorderBrush = (SolidColorBrush)FindResource(colorKey),
            BorderThickness = new Thickness(3, 0, 0, 0),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var timeBlock = new TextBlock
        {
            Text = time,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource(colorKey),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 8, 0)
        };
        Grid.SetColumn(timeBlock, 0);
        grid.Children.Add(timeBlock);

        var titleStack = new StackPanel();
        titleStack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            Foreground = (SolidColorBrush)FindResource("TextPrimary"),
            TextWrapping = TextWrapping.Wrap
        });

        if (!string.IsNullOrEmpty(subtitle))
        {
            titleStack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 11,
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        Grid.SetColumn(titleStack, 1);
        grid.Children.Add(titleStack);

        item.Child = grid;
        return item;
    }

    private void UpdateCalendarDisplayMode(string range)
    {
        // This method is no longer needed with the new timeline view
        // Keeping for backwards compatibility
        RenderCalendarTimeline();
    }

    // Keep old method for backwards compatibility if needed
    private void OnCalendarRangeClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        
        var range = btn.Tag?.ToString() ?? "7days";
        _calendarRange = range;
        
        // Update subtitle to reflect new range
        if (TxtViewSubtitle != null)
        {
            TxtViewSubtitle.Text = GetDayBasedSubtitle();
        }
        
        // Refresh both main view and calendar timeline
        RefreshView();
        RenderCalendarTimeline();
    }

    private void OnCalendarDateSelected(object sender, SelectionChangedEventArgs e)
    {
        // Calendar widget removed, but keeping method for compatibility
        RefreshView();
    }

    private string GetPriorityName(int priority)
    {
        return priority switch
        {
            1 => "High",
            2 => "Normal",
            3 => "Low",
            4 => "Lowest",
            _ => "Normal"
        };
    }

    private void UpdateActiveMetadataDisplay()
    {
        if (PnlActiveMetadata == null)
        {
            return;
        }

        PnlActiveMetadata.Children.Clear();
        
        var dueLabel = GetDueDateChipLabel();
        if (_quickAddDueDateMode != "none" && !string.IsNullOrWhiteSpace(dueLabel))
        {
            var chip = CreateMetadataChip($"📅 {dueLabel}", () => ApplyDueDateMode("none"));
            PnlActiveMetadata.Children.Add(chip);
        }

        if (_quickAddPriority != 2)
        {
            var chip = CreateMetadataChip($"🎯 {GetPriorityName(_quickAddPriority)}", () =>
            {
                _quickAddPriority = 2;
                SyncQuickAddControls();
                UpdateActiveMetadataDisplay();
            });
            PnlActiveMetadata.Children.Add(chip);
        }

        if (_quickAddReminderMode != "none")
        {
            var reminderLabel = GetReminderLabel(_quickAddReminderMode);
            if (!string.IsNullOrWhiteSpace(reminderLabel))
            {
                var chip = CreateMetadataChip($"🔔 {reminderLabel}", () => ApplyReminderMode("none"));
                PnlActiveMetadata.Children.Add(chip);
            }
        }

        foreach (var tag in _quickAddTags)
        {
            var localTag = tag;
            var chip = CreateMetadataChip($"🏷️ {tag}", () =>
            {
                _quickAddTags.Remove(localTag);
                SyncQuickAddControls();
                UpdateActiveMetadataDisplay();
            });
            PnlActiveMetadata.Children.Add(chip);
        }
    }

    private Border CreateMetadataChip(string text, Action onRemove)
    {
        var chip = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg4"),
            BorderBrush = (SolidColorBrush)FindResource("AccentBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 0, 6, 0)
        };
        
        var stack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        
        stack.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = (SolidColorBrush)FindResource("TextPrimary"),
            VerticalAlignment = VerticalAlignment.Center
        });
        
        var removeBtn = new System.Windows.Controls.Button
        {
            Content = "×",
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = (SolidColorBrush)FindResource("TextSecondary"),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 0, 0, 0),
            Margin = new Thickness(4, 0, 0, 0),
            FontSize = 14,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        removeBtn.Click += (s, e) => onRemove();
        
        stack.Children.Add(removeBtn);
        chip.Child = stack;
        
        return chip;
    }
    
    // Old RefreshCalendarSidebar method is replaced by RenderCalendarTimeline
    // Keeping this method stub for backwards compatibility
    private void RefreshCalendarSidebar()
    {
        // This method now redirects to RenderCalendarTimeline
        RenderCalendarTimeline();
    }

    private void UpdateStats()
    {
        try
        {
            var allTasks = _todoService.GetAllTodos();
            var activeTasks = _todoService.GetActiveTodos();
            var completedToday = allTasks.Count(t => t.IsCompleted && t.CompletedDate?.Date == DateTime.Today);
            
            TxtStatsActive.Text = activeTasks.Count.ToString();
            TxtStatsCompleted.Text = completedToday.ToString();
            
            if (allTasks.Count > 0)
            {
                var rate = (allTasks.Count(t => t.IsCompleted) * 100.0 / allTasks.Count);
                TxtStatsRate.Text = $"{rate:F0}%";
            }
            else
            {
                TxtStatsRate.Text = "0%";
            }
        }
        catch { /* Ignore stats errors */ }
    }

    // ===== Quick Add Bar =====
    
    private void OnQuickAddKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            OnQuickAddSubmit(sender, e);
        }
    }

    private void CommitQuickAddTags()
    {
        if (CmbQuickTags == null)
        {
            return;
        }

        var text = CmbQuickTags.Text ?? string.Empty;
        var parts = text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().TrimStart('#'))
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

        if (parts.Count > 0)
        {
            _quickAddTags = parts.Select(p => p).ToList();
        }
        else
        {
            _quickAddTags.Clear();
        }

        SyncQuickAddControls();
        UpdateActiveMetadataDisplay();
    }

    private void AddQuickAddTag(string tag)
    {
        var cleaned = tag.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return;
        }

        if (_quickAddTags.Any(t => t.Equals(cleaned, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _quickAddTags.Add(cleaned);
    }

    private IEnumerable<string> GetKnownTags()
    {
        try
        {
            return _todoService.GetAllTodos()
                .SelectMany(t => t.Tags)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    private void ApplyDueDateMode(string mode, DateTime? customDate = null, bool updateUi = true)
    {
        _quickAddDueDateMode = mode;

        switch (mode)
        {
            case "timeline":
                _quickAddDueDate = _selectedTimelineDate;
                break;
            case "none":
                _quickAddDueDate = null;
                break;
            case "today":
                _quickAddDueDate = DateTime.Today;
                break;
            case "tomorrow":
                _quickAddDueDate = DateTime.Today.AddDays(1);
                break;
            case "next3":
                _quickAddDueDate = DateTime.Today.AddDays(3);
                break;
            case "next7":
                _quickAddDueDate = DateTime.Today.AddDays(7);
                break;
            case "custom":
                if (customDate.HasValue)
                {
                    _quickAddDueDate = customDate.Value.Date;
                }
                else if (!_quickAddDueDate.HasValue)
                {
                    _quickAddDueDate = DateTime.Today;
                }
                break;
            default:
                _quickAddDueDateMode = "timeline";
                _quickAddDueDate = _selectedTimelineDate;
                break;
        }

        if (updateUi)
        {
            SyncQuickAddControls();
            UpdateActiveMetadataDisplay();
        }
    }

    private void ApplyReminderMode(string mode, bool updateUi = true)
    {
        _quickAddReminderMode = mode;

        if (updateUi)
        {
            SyncQuickAddControls();
            UpdateActiveMetadataDisplay();
        }
    }

    private void SyncQuickAddControls()
    {
        if (_isSyncingQuickControls)
        {
            return;
        }

        _isSyncingQuickControls = true;

        try
        {
            if (CmbQuickDueDate != null)
            {
                if (DueDateTimelineItem != null)
                {
                    DueDateTimelineItem.Content = _selectedTimelineDate.HasValue
                        ? $"📅 Timeline: {_selectedTimelineDate.Value:MMM dd}"
                        : "📅 Use timeline selection";
                }

                if (DueDateCustomItem != null)
                {
                    DueDateCustomItem.Content = _quickAddDueDateMode == "custom" && _quickAddDueDate.HasValue
                        ? $"📅 Custom: {_quickAddDueDate.Value:MMM dd}"
                        : "📅 Custom date...";
                }

                var target = FindComboItemByTag(CmbQuickDueDate, _quickAddDueDateMode);
                if (target != null)
                {
                    target.IsSelected = true;
                }
                else
                {
                    CmbQuickDueDate.SelectedIndex = -1;
                }
            }

            if (CmbQuickPriority != null)
            {
                CmbQuickPriority.SelectedValue = _quickAddPriority.ToString();
            }

            if (CmbQuickTags != null)
            {
                CmbQuickTags.Text = _quickAddTags.Count == 0 ? string.Empty : string.Join(", ", _quickAddTags);
            }

            if (CmbQuickReminder != null)
            {
                CmbQuickReminder.SelectedValue = _quickAddReminderMode;
            }
        }
        finally
        {
            _isSyncingQuickControls = false;
        }
    }

    private ComboBoxItem? FindComboItemByTag(System.Windows.Controls.ComboBox comboBox, string tag)
    {
        return comboBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));
    }

    private string GetDueDateChipLabel()
    {
        return _quickAddDueDateMode switch
        {
            "timeline" when _selectedTimelineDate.HasValue => $"Timeline · {_selectedTimelineDate.Value:MMM dd}",
            "timeline" => "Timeline · Select a day",
            "today" => "Today",
            "tomorrow" => "Tomorrow",
            "next3" => $"In 3 days ({DateTime.Today.AddDays(3):MMM dd})",
            "next7" => $"Next week ({DateTime.Today.AddDays(7):MMM dd})",
            "custom" when _quickAddDueDate.HasValue => $"Custom · {_quickAddDueDate.Value:MMM dd}",
            _ when _quickAddDueDate.HasValue => _quickAddDueDate.Value.ToString("MMM dd"),
            _ => string.Empty
        };
    }

    private string GetReminderLabel(string mode)
    {
        return mode switch
        {
            "laterToday" => "Later today (2 hrs)",
            "tomorrowMorning" => "Tomorrow morning",
            "dayBefore" => "Day before due",
            "custom" => "Custom",
            _ => string.Empty
        };
    }

    private void UpdateTimelineSelection(DateTime? date, bool refreshQuickAdd = true)
    {
        _selectedTimelineDate = date?.Date;

        if (_quickAddDueDateMode == "timeline")
        {
            _quickAddDueDate = _selectedTimelineDate;
        }

        HighlightTimelineSelection();

        if (refreshQuickAdd)
        {
            SyncQuickAddControls();
            UpdateActiveMetadataDisplay();
        }
    }

    private void HighlightTimelineSelection()
    {
        var accent = (SolidColorBrush)FindResource("AccentBrush");
        var stroke = (SolidColorBrush)FindResource("StrokeBrush");
        var accentColor = accent.Color;
        var accentTint = new SolidColorBrush(System.Windows.Media.Color.FromArgb(36, accentColor.R, accentColor.G, accentColor.B));

        foreach (var kvp in _timelineDayContainers)
        {
            var date = kvp.Key;
            var border = kvp.Value;
            var isSelected = _selectedTimelineDate.HasValue && date == _selectedTimelineDate.Value.Date;

            border.BorderBrush = isSelected ? accent : stroke;
            border.BorderThickness = isSelected ? new Thickness(1.5) : new Thickness(0, 0, 0, 1);
            border.Background = isSelected ? accentTint : System.Windows.Media.Brushes.Transparent;

            if (border.Child is StackPanel panel && panel.Children.Count > 0 && panel.Children[0] is Border header)
            {
                header.Background = isSelected ? (SolidColorBrush)FindResource("DarkBg4") : (SolidColorBrush)FindResource("DarkBg3");
            }

            if (border.Child is StackPanel contentPanel && contentPanel.Children.Count > 1 && contentPanel.Children[1] is Border wrapper)
            {
                wrapper.Background = isSelected ? (SolidColorBrush)FindResource("DarkBg3") : (SolidColorBrush)FindResource("DarkBg2");
            }
        }
    }

    private void OnTimelineDayClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not DateTime date)
        {
            return;
        }

        UpdateTimelineSelection(date, refreshQuickAdd: false);
        ApplyDueDateMode("timeline");
        e.Handled = true;
    }

    private void ResetQuickAddMetadata()
    {
        _quickAddPriority = 2;
        _quickAddTags = new List<string>();

        ApplyReminderMode("none", updateUi: false);

        var defaultMode = _selectedTimelineDate.HasValue ? "timeline" : "none";
        ApplyDueDateMode(defaultMode, updateUi: false);

        SyncQuickAddControls();
        UpdateActiveMetadataDisplay();
    }

    private void OnQuickAddSubmit(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtQuickAdd.Text))
        {
            return;
        }

        var taskText = TxtQuickAdd.Text.Trim();
        
        // Parse natural language
        var (title, dueDate, tags) = ParseTaskInput(taskText);

        var resolvedDueDate = dueDate ?? _quickAddDueDate;
        var resolvedTags = tags.Any() ? new List<string>(tags) : new List<string>(_quickAddTags);

        var newTask = new TodoItem
        {
            Title = title,
            Priority = _quickAddPriority,
            DueDate = resolvedDueDate,
            Tags = resolvedTags,
            ShowReminder = _quickAddReminderMode != "none",
            Source = "User"
        };

        _todoService.AddTodo(newTask);
        
        // Clear input and reset quick add metadata to defaults
        TxtQuickAdd.Text = string.Empty;
    ResetQuickAddMetadata();
        
        // Animate the add
        AnimateAdd();
    }

    private (string title, DateTime? dueDate, List<string> tags) ParseTaskInput(string input)
    {
        var title = input;
        DateTime? dueDate = null;
        var tags = new List<string>();

        // Extract tags (#tag)
        var tagMatches = System.Text.RegularExpressions.Regex.Matches(input, @"#(\w+)");
        foreach (System.Text.RegularExpressions.Match match in tagMatches)
        {
            tags.Add(match.Groups[1].Value);
            title = title.Replace(match.Value, "").Trim();
        }

        // Extract due date patterns (tomorrow, today, date)
        var datePatterns = new[]
        {
            (@"tomorrow", (DateTime?)DateTime.Now.AddDays(1)),
            (@"today", (DateTime?)DateTime.Now),
            (@"next week", (DateTime?)DateTime.Now.AddDays(7)),
            (@"(\d+) days?", (DateTime?)null) // will be parsed further
        };

        foreach (var (pattern, defaultDate) in datePatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (pattern.Contains(@"\d"))
                {
                    // Extract number
                    if (int.TryParse(match.Groups[1].Value, out int days))
                    {
                        dueDate = DateTime.Now.AddDays(days);
                    }
                }
                else
                {
                    dueDate = defaultDate;
                }
                title = title.Replace(match.Value, "").Trim();
                break;
            }
        }

        // Clean up title
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\s+", " ").Trim();

        return (title, dueDate, tags);
    }

    private DateTime? ParseNaturalDate(string dateText)
    {
        dateText = dateText.ToLower().Trim();
        
        if (dateText == "today") return DateTime.Now;
        if (dateText == "tomorrow") return DateTime.Now.AddDays(1);
        if (dateText == "next week") return DateTime.Now.AddDays(7);

        // Try parsing number of days
        var daysMatch = System.Text.RegularExpressions.Regex.Match(dateText, @"(\d+)\s*days?");
        if (daysMatch.Success && int.TryParse(daysMatch.Groups[1].Value, out int days))
        {
            return DateTime.Now.AddDays(days);
        }

        // Try direct date parsing
        if (DateTime.TryParse(dateText, out DateTime parsedDate))
        {
            return parsedDate;
        }

        return null;
    }

    private void AnimateAdd()
    {
        var storyboard = new Storyboard();
        var fadeAnimation = new DoubleAnimation
        {
            From = 0.6,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        
        Storyboard.SetTarget(fadeAnimation, TasksList);
        Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(fadeAnimation);
        storyboard.Begin();
    }
    
    // ===== Main View Rendering =====

    private void RefreshView()
    {
        if (TasksList == null) return;
        
        try
        {
            TasksList.Children.Clear();
            UpdateCounts();
            
            if (_currentView == "calendar")
            {
                RenderCalendarView();
            }
            else
            {
                RenderTasksView();
            }
            
            // Refresh right sidebar - with null checks
            if (CalendarTimelinePanel != null)
            {
                RenderCalendarTimeline();
            }
            
            if (PnlActiveMetadata != null)
            {
                UpdateActiveMetadataDisplay();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in RefreshView: {ex.Message}");
            // Don't show message box on startup errors
        }
    }

    private void RenderTasksView()
    {
        try
        {
            if (_todoService == null)
            {
                System.Diagnostics.Debug.WriteLine("TodoService is null in RenderTasksView");
                return;
            }

            var tasks = GetFilteredTasks();
            if (tasks == null)
            {
                System.Diagnostics.Debug.WriteLine("GetFilteredTasks returned null");
                return;
            }

            tasks = SortTasks(tasks);
            
            if (tasks.Count == 0)
            {
                TxtEmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                TxtEmptyState.Visibility = Visibility.Collapsed;
                
                foreach (var task in tasks)
                {
                    try
                    {
                        var taskCard = CreateTaskCard(task);
                        if (taskCard != null)
                        {
                            TasksList.Children.Add(taskCard);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating task card for '{task.Title}': {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in RenderTasksView: {ex.Message}\n{ex.StackTrace}");
            System.Windows.MessageBox.Show($"Error loading tasks: {ex.Message}", "Task Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RenderCalendarView()
    {
        try
        {
            TxtEmptyState.Visibility = Visibility.Collapsed;
            
            // Get calendar data
            var events = _calendarService.GetUpcomingEvents(14).Take(20).ToList();
            var assignments = _calendarService.GetUpcomingAssignments(14).Take(20).ToList();
            
            // Section: Upcoming Events
            if (events.Any())
            {
                var eventsHeader = CreateSectionHeader("📅 Upcoming Events", events.Count);
                TasksList.Children.Add(eventsHeader);
                
                foreach (var evt in events)
                {
                    var eventCard = CreateEventCard(evt);
                    TasksList.Children.Add(eventCard);
                }
                
                // Spacer
                TasksList.Children.Add(new Border { Height = 20 });
            }
            
            // Section: Canvas Assignments
            if (assignments.Any())
            {
                var assignHeader = CreateSectionHeader("🎓 Canvas Assignments", assignments.Count);
                TasksList.Children.Add(assignHeader);
                
                foreach (var assignment in assignments)
                {
                    var assignCard = CreateAssignmentCard(assignment);
                    TasksList.Children.Add(assignCard);
                }
            }
            
            // Empty state if nothing
            if (!events.Any() && !assignments.Any())
            {
                TxtEmptyState.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in RenderCalendarView: {ex.Message}\n{ex.StackTrace}");
            System.Windows.MessageBox.Show($"Error loading calendar: {ex.Message}", "Calendar Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Border CreateSectionHeader(string title, int count)
    {
        var header = new Border
        {
            Margin = new Thickness(0, 0, 0, 12),
            Child = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 18,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (SolidColorBrush)FindResource("TextPrimary")
                    },
                    new TextBlock
                    {
                        Text = $"({count})",
                        FontSize = 14,
                        Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                        Margin = new Thickness(8, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };
        
        return header;
    }

    private Border CreateEventCard(CalendarEvent evt)
    {
        if (evt == null) return new Border();
        
        var card = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg2"),
            BorderBrush = (SolidColorBrush)FindResource("StrokeBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        var contentStack = new StackPanel();
        
        // Title
        contentStack.Children.Add(new TextBlock
        {
            Text = evt.Title ?? "Untitled Event",
            FontSize = 15,
            FontWeight = FontWeights.Medium,
            Foreground = (SolidColorBrush)FindResource("TextPrimary"),
            TextWrapping = TextWrapping.Wrap
        });
        
        // Date/Time
        contentStack.Children.Add(new TextBlock
        {
            Text = $"📅 {evt.StartTime:MMM dd} at {evt.StartTime:h:mm tt}",
            FontSize = 13,
            Foreground = (SolidColorBrush)FindResource("InfoBrush"),
            Margin = new Thickness(0, 4, 0, 0)
        });
        
        // Location (if any)
        if (!string.IsNullOrEmpty(evt.Location))
        {
            contentStack.Children.Add(new TextBlock
            {
                Text = $"📍 {evt.Location}",
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        
        grid.Children.Add(contentStack);
        Grid.SetColumn(contentStack, 0);
        
        // Convert to Task button
        var convertBtn = new System.Windows.Controls.Button
        {
            Content = "→ Task",
            Background = (SolidColorBrush)FindResource("AccentBrush"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 6, 12, 6),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        convertBtn.Click += (s, e) => ConvertEventToTask(evt);
        
        grid.Children.Add(convertBtn);
        Grid.SetColumn(convertBtn, 1);
        
        card.Child = grid;
        
        return card;
    }

    private Border CreateAssignmentCard(CanvasAssignment assignment)
    {
        if (assignment == null) return new Border();
        
        var card = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg2"),
            BorderBrush = (SolidColorBrush)FindResource("SuccessBrush"),
            BorderThickness = new Thickness(2, 0, 0, 0),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        var contentStack = new StackPanel();
        
        // Course name
        contentStack.Children.Add(new TextBlock
        {
            Text = assignment.CourseName ?? "Unknown Course",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource("SuccessBrush"),
            Margin = new Thickness(0, 0, 0, 4)
        });
        
        // Title
        contentStack.Children.Add(new TextBlock
        {
            Text = assignment.Title ?? "Untitled Assignment",
            FontSize = 15,
            FontWeight = FontWeights.Medium,
            Foreground = (SolidColorBrush)FindResource("TextPrimary"),
            TextWrapping = TextWrapping.Wrap
        });
        
        // Due date
        if (assignment.DueDate != DateTime.MinValue)
        {
            var daysUntil = (assignment.DueDate.Date - DateTime.Now.Date).Days;
            var dueDateText = daysUntil == 0 ? "Due today" :
                             daysUntil == 1 ? "Due tomorrow" :
                             daysUntil < 0 ? $"Overdue by {-daysUntil} day(s)" :
                             $"Due in {daysUntil} day(s) ({assignment.DueDate:MMM dd})";
            
            var dueBrush = daysUntil < 0 ? (SolidColorBrush)FindResource("DangerBrush") :
                          daysUntil <= 1 ? (SolidColorBrush)FindResource("WarningBrush") :
                          (SolidColorBrush)FindResource("TextSecondary");
            
            contentStack.Children.Add(new TextBlock
            {
                Text = $"⏰ {dueDateText}",
                FontSize = 13,
                Foreground = dueBrush,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }
        
        grid.Children.Add(contentStack);
        Grid.SetColumn(contentStack, 0);
        
        // Convert to Task button
        var convertBtn = new System.Windows.Controls.Button
        {
            Content = "→ Task",
            Background = (SolidColorBrush)FindResource("AccentBrush"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(12, 6, 12, 6),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        convertBtn.Click += (s, e) => ConvertAssignmentToTask(assignment);
        
        grid.Children.Add(convertBtn);
        Grid.SetColumn(convertBtn, 1);
        
        card.Child = grid;
        
        return card;
    }

    private void ConvertEventToTask(CalendarEvent evt)
    {
        var task = new TodoItem
        {
            Title = evt.Title,
            Description = evt.Location ?? "",
            DueDate = evt.StartTime,
            Priority = 2,
            Tags = new List<string> { "calendar-event" },
            Source = "Calendar"
        };
        
        _todoService.AddTodo(task);
        System.Windows.MessageBox.Show($"'{evt.Title}' added to tasks!", "Event Converted", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ConvertAssignmentToTask(CanvasAssignment assignment)
    {
        var task = new TodoItem
        {
            Title = assignment.Title,
            Description = assignment.CourseName,
            DueDate = assignment.DueDate == DateTime.MinValue ? null : (DateTime?)assignment.DueDate,
            Priority = 1, // High priority for assignments
            Tags = new List<string> { "canvas", assignment.CourseName.ToLower().Replace(" ", "-") },
            Source = "Canvas"
        };
        
        _todoService.AddTodo(task);
        System.Windows.MessageBox.Show($"'{assignment.Title}' added to tasks!", "Assignment Converted", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ===== Task Filtering & Sorting =====

    private List<TodoItem> GetFilteredTasks()
    {
        var baseTasks = _currentView switch
        {
            "myday" => GetMyDayTasks(),
            "today" => _todoService.GetActiveTodos().Where(t => t.DueDate?.Date == DateTime.Now.Date).ToList(),
            "upcoming" => _todoService.GetDueSoonTodos(TimeSpan.FromDays(7)),
            "overdue" => _todoService.GetOverdueTodos(),
            "canvas" => _todoService.GetCanvasLinkedTodos(),
            "completed" => _todoService.GetCompletedTodos(),
            _ => _todoService.GetAllTodos()
        };

        // Apply calendar range filter
        return ApplyCalendarRangeFilter(baseTasks);
    }

    private List<TodoItem> ApplyCalendarRangeFilter(List<TodoItem> tasks)
    {
        if (_currentView == "completed" || _currentView == "overdue")
        {
            // Don't filter completed or overdue by range
            return tasks;
        }

        var now = DateTime.Now;
        var endDate = _calendarRange switch
        {
            "today" => now.Date.AddDays(1),
            "3days" => now.Date.AddDays(3),
            "7days" => now.Date.AddDays(7),
            "month" => now.Date.AddMonths(1),
            "year" => now.Date.AddYears(1),
            _ => now.Date.AddDays(7)
        };

        return tasks.Where(t => 
            !t.DueDate.HasValue || // Include tasks without due date
            t.DueDate.Value.Date < endDate
        ).ToList();
    }

    private List<TodoItem> GetMyDayTasks()
    {
        var myDay = new List<TodoItem>();
        
        // Add overdue
        myDay.AddRange(_todoService.GetOverdueTodos());
        
        // Add today's tasks
        myDay.AddRange(_todoService.GetActiveTodos().Where(t => t.DueDate?.Date == DateTime.Now.Date));
        
        // Add high priority without duplicates
        var highPri = _todoService.GetActiveTodos().Where(t => t.Priority == 1 && !myDay.Contains(t));
        myDay.AddRange(highPri.Take(5));
        
        return myDay.Distinct().ToList();
    }

    private List<TodoItem> SortTasks(List<TodoItem> tasks)
    {
        return _sortMode switch
        {
            "priority" => tasks.OrderByDescending(t => t.Priority)
                              .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                              .ToList(),
            "duedate" => tasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                             .ThenByDescending(t => t.Priority)
                             .ToList(),
            "created" => tasks.OrderByDescending(t => t.CreatedDate)
                             .ToList(),
            "alphabetical" => tasks.OrderBy(t => t.Title)
                                  .ToList(),
            _ => tasks.OrderByDescending(t => t.Priority)
                     .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                     .ToList()
        };
    }

    private void UpdateCounts()
    {
        // Update quick stats instead of sidebar counts
        UpdateStats();
    }

    // ===== Task Card Rendering =====

    private Border CreateTaskCard(TodoItem task)
    {
        var card = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg2"),
            BorderBrush = (SolidColorBrush)FindResource("StrokeBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Tag = task,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        
        // Make card clickable to open editor
        card.MouseLeftButtonDown += (s, e) =>
        {
            // Ignore clicks on checkbox and delete button
            if (e.OriginalSource is not System.Windows.Controls.CheckBox && 
                e.OriginalSource is not System.Windows.Controls.Button)
            {
                ShowTaskEditor(task);
            }
        };
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // Checkbox
        var checkbox = new System.Windows.Controls.CheckBox
        {
            IsChecked = task.IsCompleted,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 12, 0)
        };
        checkbox.Checked += (s, e) => ToggleTaskComplete(task);
        checkbox.Unchecked += (s, e) => ToggleTaskComplete(task);
        
        grid.Children.Add(checkbox);
        Grid.SetColumn(checkbox, 0);
        
        // Content
        var contentStack = new StackPanel();
        
        var titleBlock = new TextBlock
        {
            Text = task.Title,
            FontSize = 15,
            FontWeight = FontWeights.Medium,
            Foreground = (SolidColorBrush)FindResource("TextPrimary"),
            TextWrapping = TextWrapping.Wrap
        };
        
        if (task.IsCompleted)
        {
            titleBlock.TextDecorations = TextDecorations.Strikethrough;
            titleBlock.Foreground = (SolidColorBrush)FindResource("TextSecondary");
        }
        
        contentStack.Children.Add(titleBlock);
        
        // Meta info (date, tags, priority)
        var metaPanel = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
        
        // Priority indicator
        if (task.Priority == 1)
        {
            metaPanel.Children.Add(CreateBadge("High", (SolidColorBrush)FindResource("DangerBrush")));
        }
        else if (task.Priority == 3)
        {
            metaPanel.Children.Add(CreateBadge("Low", (SolidColorBrush)FindResource("TextSecondary")));
        }
        
        // Due date
        if (task.DueDate.HasValue)
        {
            var daysUntil = (task.DueDate.Value.Date - DateTime.Now.Date).Days;
            var dateStr = daysUntil == 0 ? "Today" :
                         daysUntil == 1 ? "Tomorrow" :
                         daysUntil < 0 ? $"Overdue" :
                         task.DueDate.Value.ToString("MMM dd");
            
            var dateBrush = daysUntil < 0 ? (SolidColorBrush)FindResource("DangerBrush") :
                           daysUntil <= 1 ? (SolidColorBrush)FindResource("WarningBrush") :
                           (SolidColorBrush)FindResource("InfoBrush");
            
            metaPanel.Children.Add(CreateBadge(dateStr, dateBrush));
        }
        
        // Tags
        foreach (var tag in task.Tags)
        {
            metaPanel.Children.Add(CreateBadge($"#{tag}", (SolidColorBrush)FindResource("AccentBrush")));
        }
        
        contentStack.Children.Add(metaPanel);
        
        grid.Children.Add(contentStack);
        Grid.SetColumn(contentStack, 1);
        
        // Edit button (new)
        var editBtn = new System.Windows.Controls.Button
        {
            Content = "✏️",
            Width = 28,
            Height = 28,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = (SolidColorBrush)FindResource("AccentBrush"),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 4, 0),
            ToolTip = "Edit Task"
        };
        editBtn.Click += (s, e) => ShowTaskEditor(task);
        
        grid.Children.Add(editBtn);
        Grid.SetColumn(editBtn, 2);
        
        // Delete button
        var deleteBtn = new System.Windows.Controls.Button
        {
            Content = "✕",
            Width = 28,
            Height = 28,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = (SolidColorBrush)FindResource("DangerBrush"),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Top
        };
        deleteBtn.Click += (s, e) => DeleteTask(task);
        
        grid.Children.Add(deleteBtn);
        Grid.SetColumn(deleteBtn, 3);
        
        card.Child = grid;
        
        return card;
    }

    private Border CreateBadge(string text, SolidColorBrush color)
    {
        return new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, color.Color.R, color.Color.G, color.Color.B)),
            BorderBrush = color,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(0, 0, 6, 0),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Foreground = color
            }
        };
    }

    private void ToggleTaskComplete(TodoItem task)
    {
        _todoService.ToggleTodoComplete(task.Id);
    }

    private void DeleteTask(TodoItem task)
    {
        var result = System.Windows.MessageBox.Show($"Delete '{task.Title}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _todoService.DeleteTodo(task.Id);
        }
    }

    #region Task Editor Panel Methods

    private void OnSwitchPanelMode(object sender, RoutedEventArgs e)
    {
        if (TaskEditorPanel?.Visibility == Visibility.Visible)
        {
            // Close editor
            TaskEditorPanel.Visibility = Visibility.Collapsed;
            CalendarTimelineScroller.Visibility = Visibility.Visible;
            BtnSwitchToEditor.Content = "✏️";
            BtnSwitchToEditor.ToolTip = "Task Editor";
        }
        else if (_currentEditingTask != null)
        {
            // Open editor with current task
            ShowTaskEditor(_currentEditingTask);
        }
        else
        {
            System.Windows.MessageBox.Show("Please select a task from the list first by clicking on it.", "No Task Selected", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public void ShowTaskEditor(TodoItem task)
    {
        _currentEditingTask = task;
        
        // Update UI
        TxtEditorTaskTitle.Text = task.Title;
        
        // Set due date
        if (task.DueDate.HasValue)
        {
            EditorCalendar.SelectedDate = task.DueDate.Value;
            EditorCalendar.DisplayDate = task.DueDate.Value;
        }
        else
        {
            EditorCalendar.SelectedDate = null;
        }
        
        // Set priority
        RbPriorityHigh.IsChecked = task.Priority == 1;
        RbPriorityMedium.IsChecked = task.Priority == 2;
        RbPriorityLow.IsChecked = task.Priority == 3;
        
        // Set tags
        RefreshEditorTags();
        
        // Show editor panel
        TaskEditorPanel.Visibility = Visibility.Visible;
        CalendarTimelineScroller.Visibility = Visibility.Collapsed;
        BtnSwitchToEditor.Content = "📅";
        BtnSwitchToEditor.ToolTip = "Calendar View";
    }

    private void OnCloseTaskEditor(object sender, RoutedEventArgs e)
    {
        TaskEditorPanel.Visibility = Visibility.Collapsed;
        CalendarTimelineScroller.Visibility = Visibility.Visible;
        BtnSwitchToEditor.Content = "✏️";
        BtnSwitchToEditor.ToolTip = "Task Editor";
        RefreshView();
    }

    private void OnEditorDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_currentEditingTask != null && EditorCalendar.SelectedDate.HasValue)
        {
            _currentEditingTask.DueDate = EditorCalendar.SelectedDate.Value;
            _todoService.UpdateTodo(_currentEditingTask);
            RefreshView();
        }
    }

    private void OnSetTodayDate(object sender, RoutedEventArgs e)
    {
        if (_currentEditingTask != null)
        {
            _currentEditingTask.DueDate = DateTime.Today;
            EditorCalendar.SelectedDate = DateTime.Today;
            _todoService.UpdateTodo(_currentEditingTask);
            RefreshView();
        }
    }

    private void OnSetTomorrowDate(object sender, RoutedEventArgs e)
    {
        if (_currentEditingTask != null)
        {
            _currentEditingTask.DueDate = DateTime.Today.AddDays(1);
            EditorCalendar.SelectedDate = DateTime.Today.AddDays(1);
            _todoService.UpdateTodo(_currentEditingTask);
            RefreshView();
        }
    }

    private void OnSetNextWeekDate(object sender, RoutedEventArgs e)
    {
        if (_currentEditingTask != null)
        {
            _currentEditingTask.DueDate = DateTime.Today.AddDays(7);
            EditorCalendar.SelectedDate = DateTime.Today.AddDays(7);
            _todoService.UpdateTodo(_currentEditingTask);
            RefreshView();
        }
    }

    private void OnClearDate(object sender, RoutedEventArgs e)
    {
        if (_currentEditingTask != null)
        {
            _currentEditingTask.DueDate = null;
            EditorCalendar.SelectedDate = null;
            _todoService.UpdateTodo(_currentEditingTask);
            RefreshView();
        }
    }

    private void OnPriorityChanged(object sender, RoutedEventArgs e)
    {
        if (_currentEditingTask == null || sender is not System.Windows.Controls.RadioButton rb) return;

        if (rb == RbPriorityHigh)
            _currentEditingTask.Priority = 1;
        else if (rb == RbPriorityMedium)
            _currentEditingTask.Priority = 2;
        else if (rb == RbPriorityLow)
            _currentEditingTask.Priority = 3;

        _todoService.UpdateTodo(_currentEditingTask);
        RefreshView();
    }

    private void OnNewTagKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnAddTag(sender, e);
        }
    }

    private void OnAddTag(object sender, RoutedEventArgs e)
    {
        if (_currentEditingTask == null || string.IsNullOrWhiteSpace(TxtNewTag.Text)) return;

        var newTag = TxtNewTag.Text.Trim();
        if (!_currentEditingTask.Tags.Contains(newTag))
        {
            _currentEditingTask.Tags.Add(newTag);
            _todoService.UpdateTodo(_currentEditingTask);
            RefreshEditorTags();
            TxtNewTag.Text = "";
            RefreshView();
        }
    }

    private void RefreshEditorTags()
    {
        if (_currentEditingTask == null) return;

        EditorTagsPanel.Children.Clear();

        foreach (var tag in _currentEditingTask.Tags)
        {
            var chip = new Border
            {
                Background = (SolidColorBrush)FindResource("AccentBrush"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 6, 6)
            };

            var stack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            
            stack.Children.Add(new TextBlock
            {
                Text = tag,
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            var removeBtn = new System.Windows.Controls.Button
            {
                Content = "×",
                FontSize = 14,
                Width = 18,
                Height = 18,
                Padding = new Thickness(0),
                Margin = new Thickness(6, 0, 0, 0),
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            removeBtn.Click += (s, e) =>
            {
                if (_currentEditingTask != null)
                {
                    _currentEditingTask.Tags.Remove(tag);
                    _todoService.UpdateTodo(_currentEditingTask);
                    RefreshEditorTags();
                    RefreshView();
                }
            };

            stack.Children.Add(removeBtn);
            chip.Child = stack;
            EditorTagsPanel.Children.Add(chip);
        }
    }

    private void OnDeleteTaskFromEditor(object sender, RoutedEventArgs e)
    {
        if (_currentEditingTask == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete '{_currentEditingTask.Title}'?",
            "Delete Task",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            _todoService.DeleteTodo(_currentEditingTask.Id);
            _currentEditingTask = null;
            OnCloseTaskEditor(sender, e);
        }
    }

    #endregion
}

