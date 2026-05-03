namespace MusicScoreManager;

public partial class SettingsScoresPage : ContentPage
{
    public SettingsScoresPage()
    {
        InitializeComponent();
        
        // Charger les préférences
        ShowPageNumberSwitch.IsToggled = Preferences.Default.Get("ShowPageNumber", true);
        PageNumberSizeSlider.Value = Preferences.Default.Get("PageNumberSize", 20.0);
    }

    private void OnShowPageNumberToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("ShowPageNumber", e.Value);
    }

    private void OnPageNumberSizeChanged(object sender, ValueChangedEventArgs e)
    {
        Preferences.Default.Set("PageNumberSize", e.NewValue);
    }
}
