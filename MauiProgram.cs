using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using MusicScoreManager.Services;

#pragma warning disable CA1416

namespace MusicScoreManager;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitMediaElement(true)
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<DatabaseService>();
		builder.Services.AddSingleton<ImportService>();
		builder.Services.AddSingleton<ScoresPage>();
		builder.Services.AddSingleton<SetlistsPage>();
		builder.Services.AddSingleton<TagsPage>();
		builder.Services.AddSingleton<ToolsPage>();
		builder.Services.AddSingleton<SettingsPage>();
		builder.Services.AddTransient<SettingsScoresPage>();
		builder.Services.AddTransient<SettingsSetlistsPage>();
		builder.Services.AddTransient<SetlistEditPage>();
		builder.Services.AddTransient<ScoreSelectionPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Configuration WebView pour Android (autoriser l'accès aux fichiers locaux pour PDF.js)
		Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("AllowFileAccess", (handler, view) =>
		{
#if ANDROID
			handler.PlatformView.Settings.AllowFileAccess = true;
			handler.PlatformView.Settings.AllowFileAccessFromFileURLs = true;
			handler.PlatformView.Settings.AllowUniversalAccessFromFileURLs = true;
#endif
		});

		return builder.Build();
	}
}
