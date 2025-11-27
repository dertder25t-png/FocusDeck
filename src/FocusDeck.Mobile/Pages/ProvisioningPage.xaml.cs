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

	private bool _isProcessing = false;

	private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
	{
		var result = e.Results?.FirstOrDefault()?.Value;
		if (string.IsNullOrEmpty(result) || _isProcessing) return;

		MainThread.BeginInvokeOnMainThread(async () =>
		{
			if (_isProcessing) return; // Double check on UI thread
			_isProcessing = true;
			barcodeReader.IsDetecting = false;
			toggleScannerButton.Text = "Processing...";
			resultLabel.Text = "Redeeming code...";

			try
			{
				var success = await _authService.RedeemClaimCodeAsync(result);
				if (success)
				{
					resultLabel.Text = "Success! Logging in...";
					await Task.Delay(1000);
					await Shell.Current.GoToAsync("//MainPage"); // Or wherever the home is
				}
				else
				{
					resultLabel.Text = "Failed to redeem code.";
					barcodeReader.IsDetecting = true;
					toggleScannerButton.Text = "Stop Scanner";
					_isProcessing = false;
				}
			}
			catch (Exception ex)
			{
				resultLabel.Text = $"Error: {ex.Message}";
				barcodeReader.IsDetecting = true;
				toggleScannerButton.Text = "Stop Scanner";
				_isProcessing = false;
			}
		});
	}

	private void ToggleScanner(object sender, EventArgs e)
	{
		barcodeReader.IsDetecting = !barcodeReader.IsDetecting;
		toggleScannerButton.Text = barcodeReader.IsDetecting ? "Stop Scanner" : "Start Scanner";
	}
}
