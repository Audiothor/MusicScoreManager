using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class HelpPage : ContentPage
{
    public HelpPage()
    {
        InitializeComponent();
        LoadHelpContent();
    }

    private async void LoadHelpContent()
    {
        try
        {
            string lang = LocalizationService.Instance.CurrentLanguage;
            if (lang != "fr" && lang != "en" && lang != "es" && lang != "de" && lang != "it" && lang != "pl" && lang != "nl" && lang != "pt")
            {
                lang = "en";
            }

            string filename = $"Help/help_{lang}.html";
            using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
            using var reader = new StreamReader(stream);
            string htmlContent = await reader.ReadToEndAsync();

            HelpWebView.Source = new HtmlWebViewSource
            {
                Html = htmlContent
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HelpPage] Error loading help: {ex.Message}");
            HelpWebView.Source = new HtmlWebViewSource
            {
                Html = $"<html><body style='background-color:#121212;color:white;padding:20px;font-family:sans-serif;'><h2>Guide de l'utilisateur</h2><p>Impossible de charger le guide d'aide : {ex.Message}</p></body></html>"
            };
        }
    }

    private async void OnOpenOnlineDocClicked(object sender, EventArgs e)
    {
        try
        {
            string lang = LocalizationService.Instance.CurrentLanguage;
            string url = (lang == "fr") 
                ? "https://audiothor.github.io/MusicScoreManager/" 
                : "https://audiothor.github.io/MusicScoreManager/en/";
            await Launcher.Default.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HelpPage] Error opening online doc: {ex.Message}");
        }
    }

    private async void OnQuickStartClicked(object sender, EventArgs e)
    {
        try
        {
            string lang = LocalizationService.Instance.CurrentLanguage;
            string url = (lang == "fr") 
                ? "https://audiothor.github.io/MusicScoreManager/guide/quickstart/" 
                : "https://audiothor.github.io/MusicScoreManager/en/guide/quickstart/";
            await Launcher.Default.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HelpPage] Error opening quick start: {ex.Message}");
        }
    }


    private async void OnHelpWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Url) && (e.Url.StartsWith("http://") || e.Url.StartsWith("https://")))
        {
            e.Cancel = true;
            try
            {
                await Launcher.Default.OpenAsync(new Uri(e.Url));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HelpPage] Error opening link: {ex.Message}");
            }
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
