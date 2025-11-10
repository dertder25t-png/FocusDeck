using ZXing.Net.Maui;
using FocusDeck.Mobile.Services.Auth;
using FocusDeck.Contracts.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;

namespace FocusDeck.Mobile.Pages;

public partial class ProvisioningPage : ContentPage
{
	private readonly IMobileAuthService _authService;

	public ProvisioningPage()
	{
		InitializeComponent();
		_authService = Application.Current?.Services.GetService<IMobileAuthService>()
		               ?? throw new InvalidOperationException("IMobileAuthService not registered");
		_authService.CurrentTenantChanged += OnTenantChanged;

		barcodeReader.Options = new BarcodeReaderOptions
		{
			Formats = BarcodeFormats.TwoDimensional,
			AutoRotate = true,
			Multiple = false
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_ = _authService.RefreshCurrentTenantAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_authService.CurrentTenantChanged -= OnTenantChanged;
	}

	private void OnTenantChanged(object? sender, CurrentTenantDto? tenant)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			tenantLabel.Text = tenant != null ? $"Tenant: {tenant.Name} /{tenant.Slug}" : "Tenant: unresolved";
		});
	}

	private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			resultLabel.Text = $"{e.Results[0].Value}";
		});
	}

	private void ToggleScanner(object sender, EventArgs e)
	{
		barcodeReader.IsDetecting = !barcodeReader.IsDetecting;
		toggleScannerButton.Text = barcodeReader.IsDetecting ? "Stop Scanner" : "Start Scanner";
	}
}
