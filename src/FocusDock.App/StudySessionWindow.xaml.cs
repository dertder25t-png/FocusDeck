using System;
using System.Windows;
using System.Windows.Threading;
using FocusDock.Core.Services;
using FocusDock.Data.Models;

namespace FocusDock.App
{
    public partial class StudySessionWindow : Window
    {
        private readonly StudyPlanService _studyPlanService;
        private readonly string _subject;
        private readonly string _sessionId;
        
        private DispatcherTimer _timer = null!;
        private DateTime _sessionStart;
        private TimeSpan _pausedDuration = TimeSpan.Zero;
        private bool _isPaused = false;
        private int _breakCount = 0;
        private DateTime? _pauseStartTime;
        private const int BreakReminderMinutes = 25; // Pomodoro technique

        public StudySessionWindow(StudyPlanService studyPlanService, string subject)
        {
            InitializeComponent();
            
            _studyPlanService = studyPlanService;
            _subject = subject;
            _sessionId = Guid.NewGuid().ToString();
            _sessionStart = DateTime.Now;

            TxtSubject.Text = subject;
            TxtCurrentTask.Text = $"Subject: {subject}";

            SetupTimer();
        }

    private void SetupTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500); // Update every 500ms for smooth display
        _timer.Tick += (s, e) => UpdateDisplay();
        _timer.Start();
    }

    private void UpdateDisplay()
    {
        var elapsed = DateTime.Now - _sessionStart - _pausedDuration;
        TxtElapsedTime.Text = elapsed.ToString(@"hh\:mm\:ss");
        
        // Update progress (assume 60-minute goal session)
        var totalMinutes = 60;
        var progressMinutes = Math.Min((int)elapsed.TotalMinutes, totalMinutes);
        ProgressSession.Value = (progressMinutes / (double)totalMinutes) * 100;
        TxtProgressLabel.Text = $"{progressMinutes} / {totalMinutes} minutes";

        // Suggest break at Pomodoro interval (25 min)
        if ((int)elapsed.TotalMinutes == BreakReminderMinutes && elapsed.Seconds < 1)
        {
            ShowBreakReminder();
        }

        // Update focus rate (simple: 100% if not paused, decreases with pauses)
        var focusRate = 100 - (_breakCount * 5); // 5% per break
        focusRate = Math.Max(0, focusRate);
        TxtFocusRate.Text = $"{focusRate}%";
    }

    private void ShowBreakReminder()
    {
        BreakBanner.Visibility = Visibility.Visible;
        BtnBreak.Visibility = Visibility.Visible;
        
        System.Media.SystemSounds.Exclamation.Play(); // Simple beep
    }

    private void OnPlayPauseClick(object sender, RoutedEventArgs e)
    {
        if (_isPaused)
        {
            // Resume
            if (_pauseStartTime.HasValue)
            {
                _pausedDuration += DateTime.Now - _pauseStartTime.Value;
                _pauseStartTime = null;
            }
            _isPaused = false;
            TxtSessionStatus.Text = "Active";
            BtnPlayPause.Content = "⏸ Pause";
            _timer.Start();
        }
        else
        {
            // Pause
            _isPaused = true;
            _pauseStartTime = DateTime.Now;
            TxtSessionStatus.Text = "Paused";
            BtnPlayPause.Content = "▶ Resume";
            _timer.Stop();
        }
    }

    private void OnBreakClick(object sender, RoutedEventArgs e)
    {
        _breakCount++;
        TxtBreakCount.Text = _breakCount.ToString();
        BreakBanner.Visibility = Visibility.Collapsed;
        BtnBreak.Visibility = Visibility.Collapsed;

        // Pause the timer during break
        if (!_isPaused)
        {
            OnPlayPauseClick(null!, null!);
        }

        System.Windows.MessageBox.Show(
            "Great! Take a 5-minute break.\n\n" +
            "Stretch, hydrate, or rest your eyes.\n" +
            "Click OK when you're ready to continue.",
            "Break Time",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);

        // Resume after break
        if (_isPaused)
        {
            OnPlayPauseClick(null!, null!);
        }
    }

    private void OnEndSessionClick(object sender, RoutedEventArgs e)
    {
        _timer.Stop();

        var elapsed = DateTime.Now - _sessionStart - _pausedDuration;

        // Ask for effectiveness rating
        var ratingWindow = new EffectivenessRatingWindow(_subject, elapsed);
        var dialogResult = ratingWindow.ShowDialog();

        if (dialogResult == true)
        {
            var rating = ratingWindow.EffectivenessRating;

            // Create and log the session
            var session = new StudySession
            {
                Id = _sessionId,
                Subject = _subject,
                StartTime = _sessionStart,
                EndTime = DateTime.Now,
                BreaksTaken = _breakCount,
                EffectivenessRating = rating,
                Notes = $"Completed with {_breakCount} breaks"
            };

            _studyPlanService.EndSession(session);

            System.Windows.MessageBox.Show(
                $"Session logged!\n\n" +
                $"Duration: {elapsed.Hours}h {elapsed.Minutes}m\n" +
                $"Breaks: {_breakCount}\n" +
                $"Effectiveness: {rating}/5 ⭐",
                "Session Complete",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        this.Close();
    }
}

/// <summary>
/// Dialog for rating session effectiveness (1-5 stars)
/// </summary>
public class EffectivenessRatingWindow : Window
{
    public int EffectivenessRating { get; private set; }

    public EffectivenessRatingWindow(string subject, TimeSpan duration)
    {
        Title = $"How Effective Was Your Study Session?";
        
        // Build simple UI for rating
        var grid = new System.Windows.Controls.Grid();
        grid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));

        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };

        var header = new System.Windows.Controls.TextBlock
        {
            Text = $"📚 {subject}",
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
            Margin = new Thickness(0, 0, 0, 10)
        };
        panel.Children.Add(header);

        var durationText = new System.Windows.Controls.TextBlock
        {
            Text = $"Duration: {duration.Hours}h {duration.Minutes}m",
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(150, 150, 150)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        panel.Children.Add(durationText);

        var question = new System.Windows.Controls.TextBlock
        {
            Text = "How effective was this session?",
            FontSize = 14,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        panel.Children.Add(question);

        // Star rating buttons
        var buttonPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 30) };
        
        for (int i = 1; i <= 5; i++)
        {
            var btn = new System.Windows.Controls.Button
            {
                Content = $"⭐ {i}",
                Width = 70,
                Height = 50,
                Margin = new Thickness(5),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 99, 156)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                FontSize = 12,
                FontWeight = System.Windows.FontWeights.Bold
            };

            int rating = i;
            btn.Click += (s, e) =>
            {
                EffectivenessRating = rating;
                DialogResult = true;
                Close();
            };

            buttonPanel.Children.Add(btn);
        }

        panel.Children.Add(buttonPanel);

        var note = new System.Windows.Controls.TextBlock
        {
            Text = "1 = Not effective  •  5 = Very effective",
            FontSize = 11,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            TextAlignment = System.Windows.TextAlignment.Center,
            TextWrapping = System.Windows.TextWrapping.Wrap
        };
        panel.Children.Add(note);

        grid.Children.Add(panel);
        Content = grid;

        Width = 450;
        Height = 280;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        ResizeMode = System.Windows.ResizeMode.NoResize;
        ShowInTaskbar = true;
    }
}
}
