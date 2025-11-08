using ZXing.Net.Maui;

namespace FocusDeck.Mobile.Pages;

public partial class ProvisioningPage : ContentPage
{
	public ProvisioningPage()
	{
		InitializeComponent();

		barcodeReader.Options = new BarcodeReaderOptions
		{
			Formats = BarcodeFormats.TwoDimensional,
			AutoRotate = true,
			Multiple = false
		};
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
