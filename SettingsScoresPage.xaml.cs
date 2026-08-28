namespace MusicScoreManager;

public partial class SettingsScoresPage : ContentPage
{
    public SettingsScoresPage()
    {
        InitializeComponent();
        
        // Charger les préférences
        ShowPageNumberSwitch.IsToggled = Preferences.Default.Get("ShowPageNumber", true);
        PageNumberSizeSlider.Value = Preferences.Default.Get("PageNumberSize", 20.0);

        string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
        NextPageGesturePicker.SelectedIndex = nextGesture switch
        {
            "SwipeLeft" => 0,
            "TapRight" => 1,
            "SwipeUp" => 2,
            _ => 0
        };

        string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");
        PrevPageGesturePicker.SelectedIndex = prevGesture switch
        {
            "SwipeRight" => 0,
            "TapLeft" => 1,
            "SwipeDown" => 2,
            _ => 0
        };
    }

    private void OnShowPageNumberToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("ShowPageNumber", e.Value);
    }

    private void OnPageNumberSizeChanged(object sender, ValueChangedEventArgs e)
    {
        Preferences.Default.Set("PageNumberSize", e.NewValue);
    }

    private void OnNextPageGestureChanged(object sender, EventArgs e)
    {
        string value = NextPageGesturePicker.SelectedIndex switch
        {
            0 => "SwipeLeft",
            1 => "TapRight",
            2 => "SwipeUp",
            _ => "SwipeLeft"
        };
        Preferences.Default.Set("NextPageGesture", value);
    }

    private void OnPrevPageGestureChanged(object sender, EventArgs e)
    {
        string value = PrevPageGesturePicker.SelectedIndex switch
        {
            0 => "SwipeRight",
            1 => "TapLeft",
            2 => "SwipeDown",
            _ => "SwipeRight"
        };
        Preferences.Default.Set("PrevPageGesture", value);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
