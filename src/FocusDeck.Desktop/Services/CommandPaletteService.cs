namespace FocusDeck.Desktop.Services;

public interface ICommandPaletteService
{
    void Show();
    void Hide();
    event EventHandler? ShowRequested;
    event EventHandler? HideRequested;
}

public class CommandPaletteService : ICommandPaletteService
{
    public event EventHandler? ShowRequested;
    public event EventHandler? HideRequested;

    public void Show()
    {
        ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Hide()
    {
        HideRequested?.Invoke(this, EventArgs.Empty);
    }
}
