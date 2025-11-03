using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FocusDeck.Desktop.Services;

public interface IAudioRecorderService
{
    event EventHandler<float>? AudioLevelChanged;
    event EventHandler<RecordingStateChangedEventArgs>? StateChanged;
    
    List<MMDevice> GetAudioDevices();
    void SelectDevice(MMDevice device);
    void StartRecording();
    void PauseRecording();
    void ResumeRecording();
    void StopRecording();
    RecordingState CurrentState { get; }
    TimeSpan RecordedDuration { get; }
    byte[]? GetRecordedAudio();
}

public class AudioRecorderService : IAudioRecorderService, IDisposable
{
    private WasapiCapture? _capture;
    private WaveFileWriter? _writer;
    private MemoryStream? _recordingStream;
    private MMDevice? _selectedDevice;
    private RecordingState _currentState = RecordingState.Stopped;
    private DateTime _recordingStartTime;
    private TimeSpan _pausedDuration;
    private DateTime _pauseStartTime;
    
    public event EventHandler<float>? AudioLevelChanged;
    public event EventHandler<RecordingStateChangedEventArgs>? StateChanged;
    
    public RecordingState CurrentState => _currentState;
    
    public TimeSpan RecordedDuration
    {
        get
        {
            if (_currentState == RecordingState.Stopped)
                return TimeSpan.Zero;
                
            var elapsed = DateTime.UtcNow - _recordingStartTime - _pausedDuration;
            
            if (_currentState == RecordingState.Paused)
            {
                elapsed -= (DateTime.UtcNow - _pauseStartTime);
            }
            
            return elapsed > TimeSpan.Zero ? elapsed : TimeSpan.Zero;
        }
    }

    public List<MMDevice> GetAudioDevices()
    {
        var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
    }

    public void SelectDevice(MMDevice device)
    {
        if (_currentState != RecordingState.Stopped)
        {
            throw new InvalidOperationException("Cannot change device while recording");
        }
        
        _selectedDevice = device;
    }

    public void StartRecording()
    {
        if (_currentState != RecordingState.Stopped)
        {
            throw new InvalidOperationException("Recording already in progress");
        }

        if (_selectedDevice == null)
        {
            var devices = GetAudioDevices();
            _selectedDevice = devices.FirstOrDefault() ?? throw new InvalidOperationException("No audio device available");
        }

        // Initialize WASAPI capture for 44.1kHz mono PCM
        _capture = new WasapiCapture(_selectedDevice);
        _capture.WaveFormat = new WaveFormat(44100, 16, 1); // 44.1kHz, 16-bit, mono
        
        // Create memory stream for recording
        _recordingStream = new MemoryStream();
        _writer = new WaveFileWriter(_recordingStream, _capture.WaveFormat);

        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;

        _capture.StartRecording();
        _recordingStartTime = DateTime.UtcNow;
        _pausedDuration = TimeSpan.Zero;
        
        SetState(RecordingState.Recording);
    }

    public void PauseRecording()
    {
        if (_currentState != RecordingState.Recording)
        {
            throw new InvalidOperationException("Not currently recording");
        }

        _capture?.StopRecording();
        _pauseStartTime = DateTime.UtcNow;
        SetState(RecordingState.Paused);
    }

    public void ResumeRecording()
    {
        if (_currentState != RecordingState.Paused)
        {
            throw new InvalidOperationException("Not currently paused");
        }

        _pausedDuration += (DateTime.UtcNow - _pauseStartTime);
        _capture?.StartRecording();
        SetState(RecordingState.Recording);
    }

    public void StopRecording()
    {
        if (_currentState == RecordingState.Stopped)
        {
            return;
        }

        _capture?.StopRecording();
        _writer?.Flush();
        
        SetState(RecordingState.Stopped);
    }

    public byte[]? GetRecordedAudio()
    {
        if (_recordingStream == null || _recordingStream.Length == 0)
        {
            return null;
        }

        return _recordingStream.ToArray();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_writer != null && _currentState == RecordingState.Recording)
        {
            _writer.Write(e.Buffer, 0, e.BytesRecorded);
            
            // Calculate audio level for visualization
            float max = 0;
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                float sampleValue = sample / 32768f;
                max = Math.Max(max, Math.Abs(sampleValue));
            }
            
            AudioLevelChanged?.Invoke(this, max);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            // Log error
            SetState(RecordingState.Stopped);
        }
    }

    private void SetState(RecordingState newState)
    {
        if (_currentState != newState)
        {
            var oldState = _currentState;
            _currentState = newState;
            StateChanged?.Invoke(this, new RecordingStateChangedEventArgs(oldState, newState));
        }
    }

    public void Dispose()
    {
        _capture?.Dispose();
        _writer?.Dispose();
        _recordingStream?.Dispose();
        _selectedDevice?.Dispose();
    }
}

public enum RecordingState
{
    Stopped,
    Recording,
    Paused
}

public class RecordingStateChangedEventArgs : EventArgs
{
    public RecordingState OldState { get; }
    public RecordingState NewState { get; }

    public RecordingStateChangedEventArgs(RecordingState oldState, RecordingState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}
