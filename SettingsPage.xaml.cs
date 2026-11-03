namespace MusicScoreManager;

public partial class SettingsPage : ContentPage
{
    private bool _isNavigating;

    public SettingsPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"v{AppInfo.VersionString}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isNavigating = false;
        if (SettingsAppLoadingIndicator != null)
        {
            SettingsAppLoadingIndicator.IsRunning = false;
            SettingsAppLoadingIndicator.IsVisible = false;
        }
        if (SettingsAppArrowLabel != null)
        {
            SettingsAppArrowLabel.IsVisible = true;
        }
    }

    private async void OnSettingsScoresTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;
        await Navigation.PushAsync(new SettingsScoresPage());
    }

    private async void OnManageTagsTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;
        await Navigation.PushAsync(new TagsPage());
    }

    private async void OnSettingsSetlistsTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;
        await Navigation.PushAsync(new SettingsSetlistsPage());
    }

    private async void OnSettingsAnnotationsTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;
        await Navigation.PushAsync(new SettingsAnnotationsPage());
    }

    private async void OnSettingsPedalsTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;
        await Navigation.PushAsync(new SettingsPedalsPage());
    }

    private async void OnSettingsAppTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;

        // Feedback visuel instantané (<16ms) pour confirmer le clic à l'utilisateur
        if (SettingsAppArrowLabel != null && SettingsAppLoadingIndicator != null)
        {
            SettingsAppArrowLabel.IsVisible = false;
            SettingsAppLoadingIndicator.IsVisible = true;
            SettingsAppLoadingIndicator.IsRunning = true;
        }

        // Permet au thread UI d'afficher le spinner avant de basculer
        await Task.Yield();
        await Navigation.PushAsync(new SettingsAppPage());
    }

    private async void OnSettingsHelpTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;
        await Navigation.PushAsync(new HelpPage());
    }

    private async void OnSettingsAboutTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating) return;
        _isNavigating = true;
        await Navigation.PushAsync(new AboutPage());
    }
}
