using FocusDeck.Mobile.ViewModels;

namespace FocusDeck.Mobile.Pages;

public partial class CommandDeckPage : ContentPage
{
    public CommandDeckPage(CommandDeckViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
