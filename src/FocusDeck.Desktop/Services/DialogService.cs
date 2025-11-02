using System.Windows;

namespace FocusDeck.Desktop.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmAsync(string title, string message);
    Task ShowAlertAsync(string title, string message);
    Task<string?> ShowPromptAsync(string title, string message, string defaultValue = "");
}

public class DialogService : IDialogService
{
    public Task<bool> ShowConfirmAsync(string title, string message)
    {
        var result = MessageBox.Show(
            message, 
            title, 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task ShowAlertAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    public Task<string?> ShowPromptAsync(string title, string message, string defaultValue = "")
    {
        // For a full implementation, you'd create a custom dialog window
        // This is a simplified version using MessageBox
        var result = Microsoft.VisualBasic.Interaction.InputBox(message, title, defaultValue);
        return Task.FromResult(string.IsNullOrEmpty(result) ? null : result);
    }
}
