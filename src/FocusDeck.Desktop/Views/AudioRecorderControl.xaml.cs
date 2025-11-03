using FocusDeck.Desktop.Services;
using NAudio.CoreAudioApi;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FocusDeck.Desktop.Views;

public partial class AudioRecorderControl : UserControl
{
    private readonly IAudioRecorderService _recorderService;
    private readonly DispatcherTimer _durationTimer;

    public event EventHandler<byte[]>? RecordingCompleted;

    public AudioRecorderControl()
    {
        InitializeComponent();
        
        // Get recorder service from DI (or create new instance)
        _recorderService = App.Current.Services.GetService(typeof(IAudioRecorderService)) as IAudioRecorderService 
                          ?? new AudioRecorderService();
        
        _recorderService.AudioLevelChanged += OnAudioLevelChanged;
        _recorderService.StateChanged += OnStateChanged;

        _durationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _durationTimer.Tick += DurationTimer_Tick;

        LoadAudioDevices();
    }

    private void LoadAudioDevices()
    {
        try
        {
            var devices = _recorderService.GetAudioDevices();
            DeviceComboBox.ItemsSource = devices;
            if (devices.Count > 0)
            {
                DeviceComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading devices: {ex.Message}";
        }
    }

    private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceComboBox.SelectedItem is MMDevice device)
        {
            try
            {
                _recorderService.SelectDevice(device);
                StatusText.Text = $"Selected: {device.FriendlyName}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_recorderService.CurrentState == RecordingState.Paused)
            {
                _recorderService.ResumeRecording();
            }
            else
            {
                _recorderService.StartRecording();
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error starting: {ex.Message}";
        }
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_recorderService.CurrentState == RecordingState.Recording)
            {
                _recorderService.PauseRecording();
            }
            else if (_recorderService.CurrentState == RecordingState.Paused)
            {
                _recorderService.ResumeRecording();
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error pausing: {ex.Message}";
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _recorderService.StopRecording();
            var audioData = _recorderService.GetRecordedAudio();
            if (audioData != null)
            {
                RecordingCompleted?.Invoke(this, audioData);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error stopping: {ex.Message}";
        }
    }

    private void OnAudioLevelChanged(object? sender, float level)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var percentage = (int)(level * 100);
            LevelMeter.Width = Math.Min(percentage * ActualWidth / 100, ActualWidth);
            LevelText.Text = $"Level: {percentage}%";
        });
    }

    private void OnStateChanged(object? sender, RecordingStateChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            switch (e.NewState)
            {
                case RecordingState.Recording:
                    StartButton.IsEnabled = false;
                    PauseButton.IsEnabled = true;
                    PauseButton.Content = "⏸ Pause";
                    StopButton.IsEnabled = true;
                    DeviceComboBox.IsEnabled = false;
                    StatusText.Text = "Recording...";
                    _durationTimer.Start();
                    break;

                case RecordingState.Paused:
                    StartButton.IsEnabled = true;
                    StartButton.Content = "▶ Resume";
                    PauseButton.Content = "▶ Resume";
                    StatusText.Text = "Paused";
                    _durationTimer.Stop();
                    break;

                case RecordingState.Stopped:
                    StartButton.IsEnabled = true;
                    StartButton.Content = "▶ Start";
                    PauseButton.IsEnabled = false;
                    PauseButton.Content = "⏸ Pause";
                    StopButton.IsEnabled = false;
                    DeviceComboBox.IsEnabled = true;
                    StatusText.Text = "Recording stopped";
                    _durationTimer.Stop();
                    DurationText.Text = "00:00:00";
                    LevelMeter.Width = 0;
                    LevelText.Text = "Level: 0%";
                    break;
            }
        });
    }

    private void DurationTimer_Tick(object? sender, EventArgs e)
    {
        var duration = _recorderService.RecordedDuration;
        DurationText.Text = duration.ToString(@"hh\:mm\:ss");
    }
}
