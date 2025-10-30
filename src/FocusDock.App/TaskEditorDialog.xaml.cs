using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FocusDock.Core.Services;
using FocusDock.Data.Models;

namespace FocusDock.App;

public partial class TaskEditorDialog : Window
{
    private readonly TodoService _todoService;
    private readonly TodoItem? _existingTask;
    private readonly bool _isEdit;

    public TaskEditorDialog(TodoService todoService, TodoItem? existingTask)
    {
        _todoService = todoService;
        _existingTask = existingTask;
        _isEdit = existingTask != null;
        
        InitializeComponent();

        if (_isEdit && TxtHeader != null && BtnSave != null)
        {
            TxtHeader.Text = "Edit Task";
            BtnSave.Content = "Update Task";
            LoadTaskData();
        }
    }

    private void LoadTaskData()
    {
        if (_existingTask == null || TxtTitle == null) return;

        TxtTitle.Text = _existingTask.Title;
        if (TxtDescription != null) TxtDescription.Text = _existingTask.Description;
        
        // Select priority
        if (CboPriority != null)
        {
            foreach (ComboBoxItem item in CboPriority.Items)
            {
                if (int.Parse(item.Tag.ToString()!) == _existingTask.Priority)
                {
                    CboPriority.SelectedItem = item;
                    break;
                }
            }
        }

        if (_existingTask.DueDate.HasValue && DueDatePicker != null)
        {
            DueDatePicker.SelectedDate = _existingTask.DueDate.Value;
        }

        if (_existingTask.EstimatedMinutes.HasValue && TxtEstimatedMinutes != null)
        {
            TxtEstimatedMinutes.Text = _existingTask.EstimatedMinutes.Value.ToString();
        }

        if (_existingTask.Tags.Any() && TxtTags != null)
        {
            TxtTags.Text = string.Join(", ", _existingTask.Tags);
        }

        // Select repeat
        if (CboRepeat != null)
        {
            foreach (ComboBoxItem item in CboRepeat.Items)
            {
                if (item.Content.ToString() == _existingTask.Repeat)
                {
                    CboRepeat.SelectedItem = item;
                    break;
                }
            }
        }

        if (ChkReminder != null) ChkReminder.IsChecked = _existingTask.ShowReminder;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // Guard against null controls
        if (TxtTitle == null || CboPriority == null || CboRepeat == null) return;
        
        // Validation
        if (string.IsNullOrWhiteSpace(TxtTitle.Text))
        {
            System.Windows.MessageBox.Show("Please enter a title for the task.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtTitle.Focus();
            return;
        }

        // Parse priority
        var priorityItem = (ComboBoxItem)CboPriority.SelectedItem;
        int priority = priorityItem?.Tag != null ? int.Parse(priorityItem.Tag.ToString()!) : 2;

        // Parse estimated minutes
        int? estimatedMinutes = null;
        if (TxtEstimatedMinutes != null && !string.IsNullOrWhiteSpace(TxtEstimatedMinutes.Text))
        {
            if (int.TryParse(TxtEstimatedMinutes.Text, out int minutes))
            {
                estimatedMinutes = minutes;
            }
        }

        // Parse tags
        var tags = TxtTags != null ? TxtTags.Text
            .Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList() : new List<string>();

        // Get repeat
        var repeat = ((ComboBoxItem?)CboRepeat.SelectedItem)?.Content?.ToString() ?? "None";

        if (_isEdit && _existingTask != null)
        {
            // Update existing task
            _existingTask.Title = TxtTitle.Text.Trim();
            _existingTask.Description = TxtDescription?.Text?.Trim() ?? "";
            _existingTask.Priority = priority;
            _existingTask.DueDate = DueDatePicker?.SelectedDate;
            _existingTask.EstimatedMinutes = estimatedMinutes;
            _existingTask.Tags = tags;
            _existingTask.Repeat = repeat;
            _existingTask.ShowReminder = ChkReminder?.IsChecked == true;

            _todoService.UpdateTodo(_existingTask);
        }
        else
        {
            // Create new task
            var newTask = new TodoItem
            {
                Title = TxtTitle.Text.Trim(),
                Description = TxtDescription?.Text?.Trim() ?? "",
                Priority = priority,
                DueDate = DueDatePicker?.SelectedDate,
                EstimatedMinutes = estimatedMinutes,
                Tags = tags,
                Repeat = repeat,
                ShowReminder = ChkReminder?.IsChecked == true,
                Source = "User",
                CreatedDate = DateTime.Now
            };

            _todoService.AddTodo(newTask);
        }

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
