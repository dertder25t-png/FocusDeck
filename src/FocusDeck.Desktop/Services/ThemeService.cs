using System.Windows;

namespace FocusDeck.Desktop.Services;

public interface IThemeService
{
    bool IsDarkTheme { get; }
    void SetTheme(bool isDark);
    void ToggleTheme();
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}

public class ThemeService : IThemeService
{
    public bool IsDarkTheme { get; private set; }

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public void SetTheme(bool isDark)
    {
        IsDarkTheme = isDark;

        var app = Application.Current;
        var dictionaries = app.Resources.MergedDictionaries;

        // Remove old theme
        var oldTheme = dictionaries.FirstOrDefault(d => 
            d.Source?.OriginalString.Contains("Colors") ?? false);
        if (oldTheme != null)
        {
            dictionaries.Remove(oldTheme);
        }

        // Add new theme
        var newTheme = new ResourceDictionary
        {
            Source = new Uri(isDark 
                ? "Themes/ColorsDark.xaml" 
                : "Themes/Colors.xaml", 
                UriKind.Relative)
        };
        dictionaries.Insert(0, newTheme);

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(isDark));
    }

    public void ToggleTheme()
    {
        SetTheme(!IsDarkTheme);
    }
}

public class ThemeChangedEventArgs : EventArgs
{
    public bool IsDarkTheme { get; }

    public ThemeChangedEventArgs(bool isDark)
    {
        IsDarkTheme = isDark;
    }
}
