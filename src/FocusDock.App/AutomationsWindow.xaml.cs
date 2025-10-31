using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FocusDock.Data;
using FocusDock.Data.Models;

namespace FocusDock.App;

public partial class AutomationsWindow : Window
{
    private AutomationConfig _config;

    public AutomationsWindow()
    {
        InitializeComponent();
        LoadRules();
    }

    private void LoadRules()
    {
        _config = AutomationStore.Load();
        RenderRules();
    }

    private void RenderRules()
    {
        RulesList.Children.Clear();

        if (_config.Rules.Count == 0)
        {
            EmptyState.Visibility = Visibility.Visible;
            return;
        }

        EmptyState.Visibility = Visibility.Collapsed;

        foreach (var rule in _config.Rules)
        {
            var ruleCard = CreateRuleCard(rule);
            RulesList.Children.Add(ruleCard);
        }
    }

    private Border CreateRuleCard(TimeRule rule)
    {
        var card = new Border
        {
            Background = (SolidColorBrush)FindResource("DarkBg2"),
            BorderBrush = (SolidColorBrush)FindResource("StrokeBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left side: Rule info
        var leftStack = new StackPanel();

        // Rule name with status indicator
        var namePanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        
        var statusDot = new TextBlock
        {
            Text = rule.IsEnabled ? "🟢" : "🔴",
            FontSize = 12,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };
        namePanel.Children.Add(statusDot);
        
        var nameText = new TextBlock
        {
            Text = rule.Name,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)FindResource("TextPrimary")
        };
        namePanel.Children.Add(nameText);
        leftStack.Children.Add(namePanel);

        // Rule details
        var detailsStack = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // Trigger type chip
        var triggerIcon = rule.TriggerType switch
        {
            RuleTriggerType.Time => "⏰",
            RuleTriggerType.WiFiNetwork => "📶",
            RuleTriggerType.ApplicationFocus => "💻",
            _ => "❓"
        };

        var triggerText = rule.TriggerType switch
        {
            RuleTriggerType.Time => $"Time: {rule.Start} - {rule.End}",
            RuleTriggerType.WiFiNetwork => $"WiFi: {rule.WiFiSSID} ({(rule.OnConnect ? "Connect" : "Disconnect")})",
            RuleTriggerType.ApplicationFocus => $"App: {rule.ApplicationName}",
            _ => "Unknown Trigger"
        };

        var triggerChip = CreateChip($"{triggerIcon} {triggerText}", (SolidColorBrush)FindResource("DarkBg3"));
        detailsStack.Children.Add(triggerChip);

        // Days chip (only for time-based triggers)
        if (rule.TriggerType == RuleTriggerType.Time)
        {
            var daysText = string.Join(", ", rule.DaysOfWeek.Select(d => d.ToString().Substring(0, 3)));
            var daysChip = CreateChip($"📅 {daysText}", (SolidColorBrush)FindResource("DarkBg3"));
            detailsStack.Children.Add(daysChip);
        }

        // Action chip
        var actionColor = rule.Action switch
        {
            RuleAction.ApplyPreset => (SolidColorBrush)FindResource("AccentBrush"),
            RuleAction.FocusModeOn => (SolidColorBrush)FindResource("SuccessBrush"),
            RuleAction.FocusModeOff => (SolidColorBrush)FindResource("WarningBrush"),
            _ => (SolidColorBrush)FindResource("DarkBg3")
        };

        var actionText = rule.Action switch
        {
            RuleAction.ApplyPreset => $"🎨 Apply: {rule.PresetName}",
            RuleAction.FocusModeOn => "🎯 Focus Mode ON",
            RuleAction.FocusModeOff => "🔓 Focus Mode OFF",
            _ => "Unknown"
        };

        var actionChip = CreateChip(actionText, actionColor);
        detailsStack.Children.Add(actionChip);

        leftStack.Children.Add(detailsStack);
        grid.Children.Add(leftStack);
        Grid.SetColumn(leftStack, 0);

        // Right side: Actions
        var rightStack = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Toggle Enable/Disable button
        var toggleBtn = new System.Windows.Controls.Button
        {
            Content = rule.IsEnabled ? "⏸️ Disable" : "▶️ Enable",
            Background = rule.IsEnabled 
                ? (SolidColorBrush)FindResource("WarningBrush") 
                : (SolidColorBrush)FindResource("SuccessBrush"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 13
        };
        toggleBtn.Click += (s, e) => ToggleRule(rule);
        ApplyButtonStyle(toggleBtn);
        rightStack.Children.Add(toggleBtn);

        // Edit button
        var editBtn = new System.Windows.Controls.Button
        {
            Content = "✏️ Edit",
            Background = (SolidColorBrush)FindResource("DarkBg4"),
            Foreground = (SolidColorBrush)FindResource("TextPrimary"),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 13
        };
        editBtn.Click += (s, e) => EditRule(rule);
        ApplyButtonStyle(editBtn);
        rightStack.Children.Add(editBtn);

        // Delete button
        var deleteBtn = new System.Windows.Controls.Button
        {
            Content = "🗑️ Delete",
            Background = (SolidColorBrush)FindResource("DangerBrush"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(16, 8, 16, 8),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 13
        };
        deleteBtn.Click += (s, e) => DeleteRule(rule);
        ApplyButtonStyle(deleteBtn);
        rightStack.Children.Add(deleteBtn);

        grid.Children.Add(rightStack);
        Grid.SetColumn(rightStack, 1);

        card.Child = grid;
        return card;
    }

    private void ToggleRule(TimeRule rule)
    {
        rule.IsEnabled = !rule.IsEnabled;
        AutomationStore.Save(_config);
        RenderRules();
    }

    private Border CreateChip(string text, SolidColorBrush background)
    {
        var chip = new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 8, 0)
        };

        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = (SolidColorBrush)FindResource("TextPrimary")
        };

        chip.Child = textBlock;
        return chip;
    }

    private void ApplyButtonStyle(System.Windows.Controls.Button button)
    {
        var template = new ControlTemplate(typeof(System.Windows.Controls.Button));
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(System.Windows.Controls.Button.BackgroundProperty));
        factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        factory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(System.Windows.Controls.Button.PaddingProperty));

        var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
        factory.AppendChild(contentFactory);

        template.VisualTree = factory;
        button.Template = template;
    }

    private void OnAddRuleClick(object sender, RoutedEventArgs e)
    {
        var editor = new RuleEditorDialog();
        editor.Owner = this;
        
        if (editor.ShowDialog() == true && editor.Rule != null)
        {
            _config.Rules.Add(editor.Rule);
            AutomationStore.Save(_config);
            RenderRules();
        }
    }

    private void EditRule(TimeRule rule)
    {
        var editor = new RuleEditorDialog(rule);
        editor.Owner = this;
        
        if (editor.ShowDialog() == true && editor.Rule != null)
        {
            var index = _config.Rules.IndexOf(rule);
            if (index >= 0)
            {
                _config.Rules[index] = editor.Rule;
                AutomationStore.Save(_config);
                RenderRules();
            }
        }
    }

    private void DeleteRule(TimeRule rule)
    {
        var result = System.Windows.MessageBox.Show(
            $"Delete automation rule '{rule.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            _config.Rules.Remove(rule);
            AutomationStore.Save(_config);
            RenderRules();
        }
    }
}
