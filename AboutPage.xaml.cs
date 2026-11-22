using System.Reflection;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();

        // Résolution robuste de la version de l'application
        string version = "3.3.0";
        try
        {
            var asm = typeof(AboutPage).Assembly;
            var infoVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(infoVersion))
            {
                version = infoVersion.Split('+')[0];
            }
            else
            {
                var v = AppInfo.Current.VersionString;
                if (!string.IsNullOrWhiteSpace(v) && !v.StartsWith("0.0") && !v.StartsWith("2.8"))
                {
                    version = v;
                }
                else
                {
                    version = asm.GetName().Version?.ToString(3) ?? "3.3.0";
                }
            }
        }
        catch
        {
            version = "3.3.0";
        }
        VersionLabel.Text = $"v{version}";

        // Résolution robuste du logo de l'application sur toutes les plateformes (y compris Windows Unpackaged)
        try
        {
            string appDir = AppContext.BaseDirectory;
            string directLogo = Path.Combine(appDir, "app_logo.png");
            string resourcesLogo = Path.Combine(appDir, "Resources", "Images", "app_logo.png");
            string splashLogo = Path.Combine(appDir, "app_splash_padded.png");

            if (File.Exists(directLogo))
            {
                AppLogoImage.Source = ImageSource.FromFile(directLogo);
            }
            else if (File.Exists(resourcesLogo))
            {
                AppLogoImage.Source = ImageSource.FromFile(resourcesLogo);
            }
            else if (File.Exists(splashLogo))
            {
                AppLogoImage.Source = ImageSource.FromFile(splashLogo);
            }
        }
        catch
        {
            // Fallback sur le binding XAML par défaut
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnGitHubClicked(object sender, EventArgs e)
    {
        try
        {
            Uri uri = new Uri("https://github.com/Audiothor/MusicScoreManager");
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(loc.GetString("Common_Error", "Erreur"), $"{loc.GetString("Common_LinkOpenError", "Impossible d'ouvrir le lien")} : {ex.Message}", loc.GetString("Common_OK", "OK"));
        }
    }

    private async void OnDiscordClicked(object sender, EventArgs e)
    {
        try
        {
            Uri uri = new Uri("https://discord.gg/EtZf8VyMz");
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(loc.GetString("Common_Error", "Erreur"), $"{loc.GetString("Common_LinkOpenError", "Impossible d'ouvrir le lien")} : {ex.Message}", loc.GetString("Common_OK", "OK"));
        }
    }

    private async void OnPrivacyPolicyClicked(object sender, EventArgs e)
    {
        try
        {
            Uri uri = new Uri("https://github.com/Audiothor/MusicScoreManager/blob/main/PRIVACY_POLICY.md");
            await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(loc.GetString("Common_Error", "Erreur"), $"{loc.GetString("Common_LinkOpenError", "Impossible d'ouvrir le lien")} : {ex.Message}", loc.GetString("Common_OK", "OK"));
        }
    }
}
