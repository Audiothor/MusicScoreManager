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
				void TintImageViews(Android.Views.View? v)
				{
					if (v == null) return;
					if (v is Android.Widget.ImageView img)
					{
						img.SetColorFilter(Android.Graphics.Color.White, Android.Graphics.PorterDuff.Mode.SrcIn);
					}
					else if (v is Android.Views.ViewGroup group)
					{
						for (int i = 0; i < group.ChildCount; i++)
						{
							TintImageViews(group.GetChildAt(i));
						}
					}
				}
				
				// Apply tint initially
				TintImageViews(searchView);

				// Re-apply tint when layout is fully populated, just in case
				searchView.LayoutChange += (sender, args) =>
				{
					TintImageViews(searchView);
				};
			}
#endif
		});

		return builder.Build();
	}
}
