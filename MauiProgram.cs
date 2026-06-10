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
		builder.Services.AddSingleton<IBluetoothTransferService, BluetoothTransferService>();
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

		// Configuration WebView pour Android (autoriser l'accès aux fichiers locaux pour PDF.js et activer le zoom)
		Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("AllowFileAccess", (handler, view) =>
		{
#if ANDROID
			handler.PlatformView.Settings.AllowFileAccess = true;
			handler.PlatformView.Settings.AllowFileAccessFromFileURLs = true;
			handler.PlatformView.Settings.AllowUniversalAccessFromFileURLs = true;
			handler.PlatformView.Settings.BuiltInZoomControls = true;
			handler.PlatformView.Settings.DisplayZoomControls = false; // Masque les boutons +/- laids d'Android
			handler.PlatformView.Settings.SetSupportZoom(true);
#endif
		});

		// Configuration SearchBar pour Android (forcer la loupe et l'icône de fermeture en blanc sur fond sombre)
		Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("CustomSearchIconColor", (handler, view) =>
		{
#if ANDROID
			var searchView = handler.PlatformView;
			if (searchView != null)
			{
				// Icône loupe (recherche)
				int searchIconId = searchView.Context?.Resources?.GetIdentifier("android:id/search_mag_icon", null, null) ?? 0;
				if (searchIconId != 0)
				{
					var searchIcon = searchView.FindViewById<Android.Widget.ImageView>(searchIconId);
					if (searchIcon != null)
					{
						searchIcon.SetColorFilter(Android.Graphics.Color.White, Android.Graphics.PorterDuff.Mode.SrcIn);
					}
				}

				// Icône de fermeture / effacement (croix)
				int closeIconId = searchView.Context?.Resources?.GetIdentifier("android:id/search_close_btn", null, null) ?? 0;
				if (closeIconId != 0)
				{
					var closeIcon = searchView.FindViewById<Android.Widget.ImageView>(closeIconId);
					if (closeIcon != null)
					{
						closeIcon.SetColorFilter(Android.Graphics.Color.White, Android.Graphics.PorterDuff.Mode.SrcIn);
					}
				}
			}
#endif
		});

		return builder.Build();
	}
}
