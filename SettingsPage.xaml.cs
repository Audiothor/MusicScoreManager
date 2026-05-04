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
        await DisplayAlertAsync("En construction", "Ces paramètres seront disponibles prochainement.", "OK");
    }

    private async void OnSettingsAppTapped(object sender, TappedEventArgs e)
    {
        await DisplayAlertAsync("En construction", "Les paramètres globaux de l'application seront disponibles prochainement.", "OK");
    }
    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}
