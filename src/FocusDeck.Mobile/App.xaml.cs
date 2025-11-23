// FocusDeck Mobile Application - Last Updated: October 29, 2025
namespace FocusDeck.Mobile;

using FocusDeck.Mobile.Services;

public partial class App : Application
{
    // Inject handler to ensure it initializes and subscribes to events
	public App(MobileActionHandler actionHandler)
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}