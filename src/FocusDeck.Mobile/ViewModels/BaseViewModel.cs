using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FocusDeck.Mobile.ViewModels;

/// <summary>
/// Base class for all ViewModels in the mobile application.
/// Provides INotifyPropertyChanged implementation for data binding.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Called when a property changes. Override to perform specific actions.
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string name = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Sets a property and raises PropertyChanged event if value changed.
    /// </summary>
    protected bool SetProperty<T>(ref T backingStore, T value,
        [CallerMemberName] string propertyName = "",
        Action? onChanged = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        onChanged?.Invoke();
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Command implementation for MAUI data binding.
    /// </summary>
    protected Command CreateCommand(Action action)
    {
        return new Command(action);
    }

    /// <summary>
    /// Async command implementation for MAUI data binding.
    /// </summary>
    protected Command CreateAsyncCommand(Func<Task> action)
    {
        return new Command(async () => await action());
    }
}
