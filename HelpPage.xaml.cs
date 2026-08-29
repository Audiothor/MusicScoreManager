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
                lang = "fr";
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

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
