using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FocusDock.Data;
using FocusDock.Data.Models;

namespace FocusDock.App;

public partial class RuleEditorDialog : Window
{
    public TimeRule? Rule { get; private set; }
    private TimeRule? _existingRule;

    public RuleEditorDialog(TimeRule? existingRule = null)
    {
        InitializeComponent();
        _existingRule = existingRule;
        
        if (existingRule != null)
        {
            TxtTitle.Text = "Edit Automation Rule";
            PopulateFields(existingRule);
        }
        else
        {
            // Set defaults for new rule
            CmbAction.SelectedIndex = 0;
            ChkMonday.IsChecked = ChkTuesday.IsChecked = ChkWednesday.IsChecked = 
                ChkThursday.IsChecked = ChkFriday.IsChecked = true;
        }
        
        LoadPresets();
        LoadMonitors();
    }

    private void PopulateFields(TimeRule rule)
    {
        TxtRuleName.Text = rule.Name;
        TxtStartTime.Text = rule.Start;
        TxtEndTime.Text = rule.End;
        
        // Check days
        ChkMonday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Monday);
        ChkTuesday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Tuesday);
        ChkWednesday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Wednesday);
        ChkThursday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Thursday);
        ChkFriday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Friday);
        ChkSaturday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Saturday);
        ChkSunday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Sunday);
        
        // Set action
        CmbAction.SelectedIndex = rule.Action switch
        {
            RuleAction.ApplyPreset => 0,
            RuleAction.FocusModeOn => 1,
            RuleAction.FocusModeOff => 2,
            _ => 0
        };
        
        // Set preset if applicable
        if (rule.Action == RuleAction.ApplyPreset && !string.IsNullOrEmpty(rule.PresetName))
        {
            for (int i = 0; i < CmbPreset.Items.Count; i++)
            {
                if (CmbPreset.Items[i] is ComboBoxItem item && item.Content.ToString() == rule.PresetName)
                {
                    CmbPreset.SelectedIndex = i;
                    break;
                }
            }
            
            if (rule.MonitorIndex.HasValue)
            {
                CmbMonitor.SelectedIndex = rule.MonitorIndex.Value;
            }
        }
    }

    private void LoadPresets()
    {
        CmbPreset.Items.Clear();
        var presets = PresetService.Load();
        
        if (presets.Count == 0)
        {
            var noneItem = new ComboBoxItem { Content = "No presets available" };
            CmbPreset.Items.Add(noneItem);
            CmbPreset.SelectedIndex = 0;
            CmbPreset.IsEnabled = false;
        }
        else
        {
            foreach (var preset in presets)
            {
                var item = new ComboBoxItem { Content = preset.Name };
                CmbPreset.Items.Add(item);
            }
            CmbPreset.SelectedIndex = 0;
        }
    }

    private void LoadMonitors()
    {
        CmbMonitor.Items.Clear();
        var screens = System.Windows.Forms.Screen.AllScreens;
        
        for (int i = 0; i < screens.Length; i++)
        {
            var screen = screens[i];
            var label = screen.Primary ? $"Monitor {i + 1} (Primary)" : $"Monitor {i + 1}";
            var item = new ComboBoxItem { Content = label, Tag = i };
            CmbMonitor.Items.Add(item);
        }
        
        CmbMonitor.SelectedIndex = 0;
    }

    private void OnActionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbAction.SelectedIndex == 0) // Apply Preset
        {
            PresetPanel.Visibility = Visibility.Visible;
        }
        else
        {
            PresetPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(TxtRuleName.Text))
        {
            System.Windows.MessageBox.Show("Please enter a rule name.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Get selected days
        var days = new System.Collections.Generic.List<DayOfWeek>();
        if (ChkMonday.IsChecked == true) days.Add(DayOfWeek.Monday);
        if (ChkTuesday.IsChecked == true) days.Add(DayOfWeek.Tuesday);
        if (ChkWednesday.IsChecked == true) days.Add(DayOfWeek.Wednesday);
        if (ChkThursday.IsChecked == true) days.Add(DayOfWeek.Thursday);
        if (ChkFriday.IsChecked == true) days.Add(DayOfWeek.Friday);
        if (ChkSaturday.IsChecked == true) days.Add(DayOfWeek.Saturday);
        if (ChkSunday.IsChecked == true) days.Add(DayOfWeek.Sunday);

        if (days.Count == 0)
        {
            System.Windows.MessageBox.Show("Please select at least one day.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate time format
        if (!IsValidTime(TxtStartTime.Text) || !IsValidTime(TxtEndTime.Text))
        {
            System.Windows.MessageBox.Show("Please enter valid times in HH:mm format (e.g., 09:00).", 
                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Get action
        var action = CmbAction.SelectedIndex switch
        {
            0 => RuleAction.ApplyPreset,
            1 => RuleAction.FocusModeOn,
            2 => RuleAction.FocusModeOff,
            _ => RuleAction.ApplyPreset
        };

        // Create rule
        Rule = new TimeRule
        {
            Name = TxtRuleName.Text.Trim(),
            DaysOfWeek = days,
            Start = TxtStartTime.Text.Trim(),
            End = TxtEndTime.Text.Trim(),
            Action = action
        };

        // Set preset-specific fields if applicable
        if (action == RuleAction.ApplyPreset && CmbPreset.SelectedItem is ComboBoxItem presetItem)
        {
            Rule.PresetName = presetItem.Content.ToString();
            if (CmbMonitor.SelectedItem is ComboBoxItem monitorItem && monitorItem.Tag is int monitorIndex)
            {
                Rule.MonitorIndex = monitorIndex;
            }
        }

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool IsValidTime(string time)
    {
        if (string.IsNullOrWhiteSpace(time)) return false;
        
        var parts = time.Split(':');
        if (parts.Length != 2) return false;
        
        if (!int.TryParse(parts[0], out int hours) || hours < 0 || hours > 23)
            return false;
            
        if (!int.TryParse(parts[1], out int minutes) || minutes < 0 || minutes > 59)
            return false;
            
        return true;
    }
}
