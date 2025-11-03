namespace FocusDeck.Desktop.Services;

public interface ISnackbarService
{
    void Show(string message, TimeSpan? duration = null);
    event EventHandler<SnackbarMessageEventArgs>? MessageReceived;
}

public class SnackbarService : ISnackbarService
{
    public event EventHandler<SnackbarMessageEventArgs>? MessageReceived;

    public void Show(string message, TimeSpan? duration = null)
    {
        MessageReceived?.Invoke(this, new SnackbarMessageEventArgs(
            message, 
            duration ?? TimeSpan.FromSeconds(3)));
    }
}

public class SnackbarMessageEventArgs : EventArgs
{
    public string Message { get; }
    public TimeSpan Duration { get; }

    public SnackbarMessageEventArgs(string message, TimeSpan duration)
    {
        Message = message;
        Duration = duration;
    }
}
