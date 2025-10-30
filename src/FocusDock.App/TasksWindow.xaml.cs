using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FocusDock.Core.Services;
using FocusDock.Data.Models;
using System.Collections.Generic;

namespace FocusDock.App;

public partial class TasksWindow : Window
{
    private readonly TodoService _todoService;
    private string _currentView = "myday"; // myday, today, upcoming, overdue, all, canvas, completed
    private string _sortBy = "priority"; // priority, dueDate, created, title
    private string _searchQuery = "";
    private DateTime? _quickAddDueDate = null;
    private List<string> _quickAddTags = new();

    public TasksWindow(TodoService todoService)
    {
        InitializeComponent();
        _todoService = todoService;
        
        // Subscribe to changes
        _todoService.TodosChanged += (s, e) => 
        {
            try 
            { 
                Dispatcher.BeginInvoke(new Action(() => RefreshTaskList())); 
            } 
            catch { /* Ignore if window is closing */ }
        };
        
        Loaded += (s, e) => 
        {
            try
            {
                RefreshTaskList();
                TxtQuickAdd.Focus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
    }

    // ===== Sidebar Navigation =====
    
    private void OnMyDayClick(object sender, RoutedEventArgs e)
    {
        SetActiveView("myday", "My Day", BtnMyDay);
    }

    private void OnTodayClick(object sender, RoutedEventArgs e)
    {
        SetActiveView("today", "Today", BtnTodayView);
    }

    private void OnUpcomingClick(object sender, RoutedEventArgs e)
    {
        SetActiveView("upcoming", "Upcoming", BtnUpcoming);
    }

    private void OnOverdueClick(object sender, RoutedEventArgs e)
    {
        SetActiveView("overdue", "Overdue", BtnOverdue);
    }

    private void OnAllTasksClick(object sender, RoutedEventArgs e)
    {
        SetActiveView("all", "All Tasks", BtnAllTasks);
    }

    private void OnCanvasClick(object sender, RoutedEventArgs e)
    {
        SetActiveView("canvas", "Canvas", BtnCanvas);
    }

    private void OnCompletedClick(object sender, RoutedEventArgs e)
    {
        SetActiveView("completed", "Completed", BtnCompletedView);
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
            RefreshTaskList();
        }
    }

    private void SetActiveView(string view, string title, System.Windows.Controls.Button activeButton)
    {
        _currentView = view;
        TxtViewTitle.Text = title;

        // Reset all sidebar buttons
        foreach (System.Windows.Controls.Button btn in new[] { BtnMyDay, BtnTodayView, BtnUpcoming, BtnOverdue, BtnAllTasks, BtnCanvas, BtnCompletedView })
        {
            btn.Tag = null;
        }

        // Set active button
        activeButton.Tag = "active";

        RefreshTaskList();
    }

    // ===== Quick Add Bar =====
    
    private void OnQuickAddKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            OnQuickAddSubmit(sender, e);
        }
    }

    private void OnQuickAddDateClick(object sender, RoutedEventArgs e)
    {
        // Simple date picker dialog
        var dialog = new Controls.InputDialog("Due Date", "Enter date (e.g., 'tomorrow', '11/15', or '2 days'):", "");
        if (dialog.ShowDialog() == true)
        {
            var dateText = dialog.ResultText?.ToLower().Trim() ?? "";
            _quickAddDueDate = ParseNaturalDate(dateText);
            if (_quickAddDueDate.HasValue)
            {
                System.Windows.MessageBox.Show($"Due date set to: {_quickAddDueDate.Value:MMM dd, yyyy}", "Date Set", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void OnQuickAddTagClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Controls.InputDialog("Tags", "Enter tags (comma-separated):", "");
        if (dialog.ShowDialog() == true)
        {
            var tags = (dialog.ResultText ?? "").Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            _quickAddTags = tags;
            if (tags.Any())
            {
                System.Windows.MessageBox.Show($"Tags: {string.Join(", ", tags)}", "Tags Set", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void OnQuickAddSubmit(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtQuickAdd.Text))
        {
            return;
        }

        var taskText = TxtQuickAdd.Text.Trim();
        
        // Parse natural language (basic implementation)
        var (title, dueDate, tags) = ParseTaskInput(taskText);

        var newTask = new TodoItem
        {
            Title = title,
            Priority = 2,
            DueDate = dueDate ?? _quickAddDueDate,
            Tags = tags.Any() ? tags : _quickAddTags,
            Source = "User"
        };

        _todoService.AddTodo(newTask);
        
        // Clear input and temp data
        TxtQuickAdd.Text = "";
        _quickAddDueDate = null;
        _quickAddTags = new();
        
        // Animate the add
        AnimateTaskAdded();
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

    private void AnimateTaskAdded()
    {
        // Simple visual feedback
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
    
    // ===== Task List Rendering =====

    private void RefreshTaskList()
    {
        if (TasksList == null) return; // Not loaded yet
        
        TasksList.Children.Clear();
        
        // Get filtered tasks
        var tasks = GetFilteredTasks();
        
        // Apply sorting
        tasks = SortTasks(tasks);
        
        // Update stats and counts
        UpdateStats(tasks);
        UpdateCounts();
        
        // Render tasks
        if (tasks.Count == 0)
        {
            TxtEmptyState.Visibility = Visibility.Visible;
            TxtEmptyState.Text = _currentView switch
            {
                "myday" => "No tasks in My Day. Add tasks or suggestions will appear here!",
                "completed" => "No completed tasks yet.",
                "canvas" => "No Canvas assignments linked. Sync with Canvas to see them here.",
                _ => "No tasks match this view. Try adding one above!"
            };
        }
        else
        {
            TxtEmptyState.Visibility = Visibility.Collapsed;
            
            foreach (var task in tasks)
            {
                var taskCard = CreateTaskCard(task);
                TasksList.Children.Add(taskCard);
            }
        }
    }

    private List<TodoItem> GetFilteredTasks()
    {
        return _currentView switch
        {
            "myday" => GetMyDayTasks(),
            "today" => _todoService.GetActiveTodos().Where(t => t.DueDate?.Date == DateTime.Now.Date).ToList(),
            "upcoming" => _todoService.GetDueSoonTodos(TimeSpan.FromDays(7)),
            "overdue" => _todoService.GetOverdueTodos(),
            "canvas" => _todoService.GetCanvasLinkedTodos(),
            "completed" => _todoService.GetCompletedTodos(),
            _ => _todoService.GetAllTodos()
        };
    }

    private List<TodoItem> GetMyDayTasks()
    {
        // My Day: overdue + today + high priority + flagged
        var myDay = new List<TodoItem>();
        
        myDay.AddRange(_todoService.GetOverdueTodos());
        myDay.AddRange(_todoService.GetActiveTodos().Where(t => t.DueDate?.Date == DateTime.Now.Date));
        myDay.AddRange(_todoService.GetActiveTodos().Where(t => t.Priority >= 3 && !myDay.Contains(t)));
        
        return myDay.Distinct().ToList();
    }

    private List<TodoItem> SortTasks(List<TodoItem> tasks)
    {
        return _sortBy switch
        {
            "dueDate" => tasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList(),
            "created" => tasks.OrderByDescending(t => t.CreatedDate).ToList(),
            "title" => tasks.OrderBy(t => t.Title).ToList(),
            _ => tasks.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate).ToList()
        };
    }

    private void UpdateStats(List<TodoItem> tasks)
    {
        var activeCount = _todoService.GetActiveTodos().Count;
        var completedCount = _todoService.GetCompletedTodos().Count;
        var overdueCount = _todoService.GetOverdueTodos().Count;
        
        TxtTaskStats.Text = $"{activeCount} active • {completedCount} completed" +
            (overdueCount > 0 ? $" • {overdueCount} overdue" : "");
    }

    private void UpdateCounts()
    {
        TxtMyDayCount.Text = GetMyDayTasks().Count.ToString();
        TxtTodayCount.Text = _todoService.GetActiveTodos().Count(t => t.DueDate?.Date == DateTime.Now.Date).ToString();
        TxtUpcomingCount.Text = _todoService.GetDueSoonTodos(TimeSpan.FromDays(7)).Count.ToString();
        TxtOverdueCount.Text = _todoService.GetOverdueTodos().Count.ToString();
        TxtCanvasCount.Text = _todoService.GetCanvasLinkedTodos().Count.ToString();
        TxtCompletedCount.Text = _todoService.GetCompletedTodos().Count.ToString();
    }
    
    // ===== Task Card Creation =====

    private Border CreateTaskCard(TodoItem task)
    {
        var card = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg2"),
            BorderBrush = (SolidColorBrush)FindResource("StrokeBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20, 16, 20, 16),
            Margin = new Thickness(0, 0, 0, 12),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        
        card.MouseEnter += (s, e) => card.Background = (SolidColorBrush)FindResource("DarkBg3");
        card.MouseLeave += (s, e) => card.Background = (SolidColorBrush)FindResource("DarkBg2");
        card.MouseLeftButtonDown += (s, e) => EditTask(task);
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // Left content
        var leftStack = new StackPanel();
        
        // Title row with checkbox and priority
        var titleRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        
        var checkBox = new System.Windows.Controls.CheckBox
        {
            IsChecked = task.IsCompleted,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };
        checkBox.Checked += (s, e) => ToggleTaskComplete(task);
        checkBox.Unchecked += (s, e) => ToggleTaskComplete(task);
        titleRow.Children.Add(checkBox);
        
        // Priority indicator
        var priorityDot = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = GetPriorityBrush(task.Priority),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        titleRow.Children.Add(priorityDot);
        
        var titleText = new TextBlock
        {
            Text = task.Title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource("TextPrimary"),
            VerticalAlignment = VerticalAlignment.Center
        };
        if (task.IsCompleted)
        {
            titleText.TextDecorations = TextDecorations.Strikethrough;
            titleText.Foreground = (SolidColorBrush)FindResource("TextSecondary");
        }
        titleRow.Children.Add(titleText);
        
        leftStack.Children.Add(titleRow);
        
        // Description
        if (!string.IsNullOrWhiteSpace(task.Description))
        {
            var descText = new TextBlock
            {
                Text = task.Description,
                FontSize = 13,
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(32, 6, 0, 0),
                MaxHeight = 40
            };
            leftStack.Children.Add(descText);
        }
        
        // Metadata row
        var metaRow = new StackPanel 
        { 
            Orientation = System.Windows.Controls.Orientation.Horizontal, 
            Margin = new Thickness(32, 8, 0, 0) 
        };
        
        // Due date
        if (task.DueDate.HasValue)
        {
            var dueChip = CreateMetaChip(
                $"📅 {task.DueDate.Value:MMM dd}", 
                task.IsOverdue() ? (SolidColorBrush)FindResource("DangerBrush") : (SolidColorBrush)FindResource("TextSecondary")
            );
            metaRow.Children.Add(dueChip);
        }
        
        // Tags
        foreach (var tag in task.Tags.Take(3))
        {
            var tagChip = CreateMetaChip($"#{tag}", (SolidColorBrush)FindResource("AccentBrush"));
            metaRow.Children.Add(tagChip);
        }
        
        // Estimate
        if (task.EstimatedMinutes.HasValue)
        {
            var estChip = CreateMetaChip($"⏱ {task.EstimatedMinutes}m", (SolidColorBrush)FindResource("TextSecondary"));
            metaRow.Children.Add(estChip);
        }
        
        leftStack.Children.Add(metaRow);
        
        Grid.SetColumn(leftStack, 0);
        grid.Children.Add(leftStack);
        
        // Right actions
        var actionsStack = new StackPanel 
        { 
            Orientation = System.Windows.Controls.Orientation.Horizontal, 
            VerticalAlignment = VerticalAlignment.Center 
        };
        
        var editBtn = new System.Windows.Controls.Button
        {
            Content = "✏",
            Style = (Style)FindResource("IconButton"),
            ToolTip = "Edit",
            Margin = new Thickness(4, 0, 0, 0)
        };
        editBtn.Click += (s, e) => { e.Handled = true; EditTask(task); };
        actionsStack.Children.Add(editBtn);
        
        var deleteBtn = new System.Windows.Controls.Button
        {
            Content = "🗑",
            Style = (Style)FindResource("IconButton"),
            ToolTip = "Delete",
            Margin = new Thickness(4, 0, 0, 0)
        };
        deleteBtn.Click += (s, e) => { e.Handled = true; DeleteTask(task); };
        actionsStack.Children.Add(deleteBtn);
        
        Grid.SetColumn(actionsStack, 1);
        grid.Children.Add(actionsStack);
        
        card.Child = grid;
        return card;
    }

    private Border CreateMetaChip(string text, SolidColorBrush color)
    {
        var chip = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, color.Color.R, color.Color.G, color.Color.B)),
            BorderBrush = color,
            BorderThickness = new Thickness(1, 1, 1, 1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(0, 0, 6, 0)
        };
        
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 11,
            Foreground = color
        };
        
        chip.Child = textBlock;
        return chip;
    }

    private SolidColorBrush GetPriorityBrush(int priority)
    {
        return priority switch
        {
            4 => (SolidColorBrush)FindResource("PriorityUrgent"),
            3 => (SolidColorBrush)FindResource("PriorityHigh"),
            2 => (SolidColorBrush)FindResource("PriorityMedium"),
            _ => (SolidColorBrush)FindResource("PriorityLow")
        };
    }

    private void RenderNotesList(List<TodoItem> notes)
    {
        // Notes view - similar to tasks but optimized for note-taking
        if (notes.Count == 0)
        {
            TxtEmptyState.Visibility = Visibility.Visible;
            TxtEmptyState.Text = "No notes yet. Create your first note!";
            return;
        }
        
        TxtEmptyState.Visibility = Visibility.Collapsed;
        
        foreach (var note in notes)
        {
            var noteCard = CreateTaskCard(note);
            TasksList.Children.Add(noteCard);
        }
    }

    private void EditTask(TodoItem task)
    {
        var dialog = new TaskEditorDialog(_todoService, task);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            RefreshTaskList();
        }
    }

    private void ToggleTaskComplete(TodoItem task)
    {
        _todoService.ToggleTodoComplete(task.Id);
        RefreshTaskList();
    }

    private void DeleteTask(TodoItem task)
    {
        var result = System.Windows.MessageBox.Show(
            $"Delete task '{task.Title}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );
        
        if (result == MessageBoxResult.Yes)
        {
            _todoService.DeleteTodo(task.Id);
            RefreshTaskList();
        }
    }

    // ===== View Toggle Handlers =====
    
    private void OnListViewClick(object sender, RoutedEventArgs e)
    {
        BtnListView.Tag = "active";
        BtnGridView.Tag = null;
        // List view is already the default, no additional action needed
    }

    private void OnGridViewClick(object sender, RoutedEventArgs e)
    {
        BtnGridView.Tag = "active";
        BtnListView.Tag = null;
        // Grid view implementation can be added later
        System.Windows.MessageBox.Show("Grid view coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
