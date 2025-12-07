using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Visio.Services.Interfaces;
using Visio.Services.Implementations;
using Visio.ViewModels;
using Visio.Views;

namespace Visio;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<IRtspStreamService, VlcRtspStreamService>();
		
		builder.Services.AddTransient<MainViewModel>();
		
		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
