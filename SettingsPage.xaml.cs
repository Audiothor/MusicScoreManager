namespace MusicScoreManager;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"v{AppInfo.VersionString}";
    }

    private async void OnSettingsScoresTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new SettingsScoresPage());
    }

    private async void OnSettingsSetlistsTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new SettingsSetlistsPage());
    }

    private async void OnSettingsTagsTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new TagsPage());
    }

    private async void OnSettingsAnnotationsTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new SettingsAnnotationsPage());
    }

    private async void OnSettingsAppTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new SettingsAppPage());
    }

    private async void OnSettingsHelpTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new HelpPage());
    }

    private async void OnSettingsAboutTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new AboutPage());
    }
}
