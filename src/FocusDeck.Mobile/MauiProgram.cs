using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Fingerprint;
using ZXing.Net.Maui.Controls;

namespace FocusDeck.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseBarcodeReader()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

        builder.Services.AddSingleton(typeof(IFingerprint), CrossFingerprint.Current);

		// Get cloud server URL from preferences or environment
		var cloudServerUrl = Preferences.Get("cloud_server_url", "");

		// Register all mobile services with cloud sync
		builder.Services.AddMobileServices(cloudServerUrl);

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
