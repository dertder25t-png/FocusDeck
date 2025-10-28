using System.Windows;

namespace FocusDock.App.Controls;

public partial class InputDialog : Window
{
    public string? ResultText { get; private set; }

    public InputDialog(string title, string prompt, string? initial = null)
    {
        InitializeComponent();
        Title = title;
        LblPrompt.Text = prompt;
        TxtInput.Text = initial ?? string.Empty;
        Loaded += (_, _) => TxtInput.Focus();
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        ResultText = TxtInput.Text;
        DialogResult = true;
    }
}

