using FocusDeck.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Mobile.Pages;

/// <summary>
/// Settings page for cloud synchronization configuration and data management.
/// </summary>
public partial class SettingsPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of SettingsPage.
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();

        var viewModel = Application.Current?
            .Handler?
            .MauiContext?
            .Services?
            .GetService<CloudSettingsViewModel>();

        if (viewModel == null)
        {
            throw new InvalidOperationException("CloudSettingsViewModel service not registered");
        }

        BindingContext = viewModel;
    }

    /// <summary>
    /// Called when the page appears.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Load ViewModel and refresh data
        if (BindingContext is CloudSettingsViewModel viewModel)
        {
            viewModel.LoadSettingsCommand.Execute(null);
        }
    }
}
