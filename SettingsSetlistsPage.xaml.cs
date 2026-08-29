namespace MusicScoreManager;

public partial class SettingsSetlistsPage : ContentPage
{
    public SettingsSetlistsPage()
    {
        InitializeComponent();
        
        // Charger les préférences
        DefaultContinuousSwitch.IsToggled = Preferences.Default.Get("DefaultContinuousReading", true);
        ReturnToSetlistSwitch.IsToggled = Preferences.Default.Get("ReturnToSetlistOnEndOfScore", false);
    }

    private void OnDefaultContinuousToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("DefaultContinuousReading", e.Value);
    }

    private void OnReturnToSetlistToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("ReturnToSetlistOnEndOfScore", e.Value);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
