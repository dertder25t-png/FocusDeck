using FocusDeck.Mobile.Pages;

namespace FocusDeck.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(ProvisioningPage), typeof(ProvisioningPage));
	}
}
