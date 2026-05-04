namespace MusicScoreManager;

public partial class SettingsSetlistsPage : ContentPage
{
    public SettingsSetlistsPage()
    {
        InitializeComponent();
        
        // Charger la préférence
        DefaultContinuousSwitch.IsToggled = Preferences.Default.Get("DefaultContinuousReading", true);
    }

    private void OnDefaultContinuousToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("DefaultContinuousReading", e.Value);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
