namespace MusicScoreManager;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"v{AppInfo.Current.VersionString}";
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
            await DisplayAlertAsync("Erreur", $"Impossible d'ouvrir le lien : {ex.Message}", "OK");
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
            await DisplayAlertAsync("Erreur", $"Impossible d'ouvrir le lien : {ex.Message}", "OK");
        }
    }
}
