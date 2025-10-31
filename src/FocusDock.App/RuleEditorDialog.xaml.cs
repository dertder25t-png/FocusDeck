using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics; // For process detection
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
            CmbTriggerType.SelectedIndex = 0; // Time trigger
            CmbAction.SelectedIndex = 0;
            ChkMonday.IsChecked = ChkTuesday.IsChecked = ChkWednesday.IsChecked = 
                ChkThursday.IsChecked = ChkFriday.IsChecked = true;
        }
        
        LoadPresets();
        LoadMonitors();
        PopulateCommonApplications();
    }

    private void PopulateFields(TimeRule rule)
    {
        TxtRuleName.Text = rule.Name;
        
        // Set trigger type
        CmbTriggerType.SelectedIndex = rule.TriggerType switch
        {
            RuleTriggerType.Time => 0,
            RuleTriggerType.WiFiNetwork => 1,
            RuleTriggerType.ApplicationFocus => 2,
            _ => 0
        };
        
        // Time trigger fields
        if (rule.TriggerType == RuleTriggerType.Time)
        {
            TxtStartTime.Text = rule.Start;
            TxtEndTime.Text = rule.End;
            
            ChkMonday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Monday);
            ChkTuesday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Tuesday);
            ChkWednesday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Wednesday);
            ChkThursday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Thursday);
            ChkFriday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Friday);
            ChkSaturday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Saturday);
            ChkSunday.IsChecked = rule.DaysOfWeek.Contains(DayOfWeek.Sunday);
        }
        
        // WiFi trigger fields
        if (rule.TriggerType == RuleTriggerType.WiFiNetwork)
        {
            CmbWiFiNetwork.Text = rule.WiFiSSID ?? "";
            RbOnConnect.IsChecked = rule.OnConnect;
            RbOnDisconnect.IsChecked = !rule.OnConnect;
        }
        
        // App trigger fields
        if (rule.TriggerType == RuleTriggerType.ApplicationFocus)
        {
            CmbApplication.Text = rule.ApplicationName ?? "";
            TxtProcessName.Text = rule.ProcessName ?? "";
        }
        
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

    private void PopulateCommonApplications()
    {
        // Add common applications to the combo box
        var commonApps = new[]
        {
            "Visual Studio Code",
            "Visual Studio",
            "Google Chrome",
            "Microsoft Edge",
            "Firefox",
            "Slack",
            "Microsoft Teams",
            "Discord",
            "Spotify",
            "Notepad++",
            "Sublime Text",
            "IntelliJ IDEA",
            "PyCharm",
            "Zoom"
        };
        
        foreach (var app in commonApps)
        {
            CmbApplication.Items.Add(new ComboBoxItem { Content = app });
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

    private void OnTriggerTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTriggerType == null) return;
        
        // Hide all trigger panels
        if (TimeTriggerPanel != null) TimeTriggerPanel.Visibility = Visibility.Collapsed;
        if (WiFiTriggerPanel != null) WiFiTriggerPanel.Visibility = Visibility.Collapsed;
        if (AppTriggerPanel != null) AppTriggerPanel.Visibility = Visibility.Collapsed;
        
        // Show the selected trigger panel
        switch (CmbTriggerType.SelectedIndex)
        {
            case 0: // Time
                if (TimeTriggerPanel != null) TimeTriggerPanel.Visibility = Visibility.Visible;
                break;
            case 1: // WiFi
                if (WiFiTriggerPanel != null) WiFiTriggerPanel.Visibility = Visibility.Visible;
                break;
            case 2: // App
                if (AppTriggerPanel != null) AppTriggerPanel.Visibility = Visibility.Visible;
                break;
        }
    }

    private void OnScanNetworksClick(object sender, RoutedEventArgs e)
    {
        try
        {
            CmbWiFiNetwork.Items.Clear();
            
            // Try to get WiFi networks using netsh command
            var processInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show networks mode=bssid",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                // Parse SSIDs from output
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("SSID") && !line.Contains("BSSID"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length > 1)
                        {
                            var ssid = parts[1].Trim();
                            if (!string.IsNullOrEmpty(ssid))
                            {
                                CmbWiFiNetwork.Items.Add(new ComboBoxItem { Content = ssid });
                            }
                        }
                    }
                }
                
                if (CmbWiFiNetwork.Items.Count == 0)
                {
                    CmbWiFiNetwork.Items.Add(new ComboBoxItem { Content = "No networks found" });
                }
                else
                {
                    CmbWiFiNetwork.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to scan networks: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnDetectAppsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            CmbApplication.Items.Clear();
            
            // Get all running processes with main windows
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .OrderBy(p => p.ProcessName)
                .ToList();
            
            var added = new System.Collections.Generic.HashSet<string>();
            
            foreach (var process in processes)
            {
                try
                {
                    var name = string.IsNullOrEmpty(process.MainWindowTitle) 
                        ? process.ProcessName 
                        : process.MainWindowTitle;
                    
                    if (!added.Contains(name) && !string.IsNullOrWhiteSpace(name))
                    {
                        CmbApplication.Items.Add(new ComboBoxItem 
                        { 
                            Content = name,
                            Tag = process.ProcessName + ".exe"
                        });
                        added.Add(name);
                    }
                }
                catch { /* Skip processes we can't access */ }
            }
            
            if (CmbApplication.Items.Count == 0)
            {
                CmbApplication.Items.Add(new ComboBoxItem { Content = "No applications detected" });
            }
            else
            {
                CmbApplication.SelectedIndex = 0;
                
                // Auto-populate process name if available
                if (CmbApplication.SelectedItem is ComboBoxItem item && item.Tag is string processName)
                {
                    TxtProcessName.Text = processName;
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to detect applications: {ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        var triggerType = CmbTriggerType.SelectedIndex switch
        {
            0 => RuleTriggerType.Time,
            1 => RuleTriggerType.WiFiNetwork,
            2 => RuleTriggerType.ApplicationFocus,
            _ => RuleTriggerType.Time
        };

        // Create base rule
        Rule = new TimeRule
        {
            Name = TxtRuleName.Text.Trim(),
            TriggerType = triggerType,
            IsEnabled = true
        };

        // Validate and populate trigger-specific fields
        switch (triggerType)
        {
            case RuleTriggerType.Time:
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

                Rule.DaysOfWeek = days;
                Rule.Start = TxtStartTime.Text.Trim();
                Rule.End = TxtEndTime.Text.Trim();
                break;

            case RuleTriggerType.WiFiNetwork:
                var ssid = CmbWiFiNetwork.Text.Trim();
                if (string.IsNullOrWhiteSpace(ssid) || ssid == "No networks found")
                {
                    System.Windows.MessageBox.Show("Please enter or select a WiFi network name (SSID).", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Rule.WiFiSSID = ssid;
                Rule.OnConnect = RbOnConnect.IsChecked == true;
                break;

            case RuleTriggerType.ApplicationFocus:
                var appName = CmbApplication.Text.Trim();
                if (string.IsNullOrWhiteSpace(appName) || appName == "No applications detected")
                {
                    System.Windows.MessageBox.Show("Please enter or select an application name.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Rule.ApplicationName = appName;
                Rule.ProcessName = TxtProcessName.Text.Trim();
                break;
        }

        // Get action
        var action = CmbAction.SelectedIndex switch
        {
            0 => RuleAction.ApplyPreset,
            1 => RuleAction.FocusModeOn,
            2 => RuleAction.FocusModeOff,
            _ => RuleAction.ApplyPreset
        };

        Rule.Action = action;
        Rule.Action = action;

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
