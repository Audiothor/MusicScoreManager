namespace MusicScoreManager;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnSettingsScoresTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new SettingsScoresPage());
    }

    private async void OnNotImplementedTapped(object sender, TappedEventArgs e)
    {
        await DisplayAlert("En construction", "Ces paramètres seront disponibles dans une prochaine mise à jour.", "OK");
    }
}
