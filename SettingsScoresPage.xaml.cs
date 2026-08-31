namespace MusicScoreManager;

public partial class SettingsScoresPage : ContentPage
{
    public SettingsScoresPage()
    {
        InitializeComponent();
        
        // Charger les préférences
        string defaultSort = Preferences.Default.Get("DefaultScoreSort", "DateDesc");
        DefaultSortPicker.SelectedIndex = defaultSort switch
        {
            "DateDesc" => 0,
            "DateAsc" => 1,
            "TitleAsc" => 2,
            "TitleDesc" => 3,
            "ModifiedDesc" => 4,
            "RatingDesc" => 5,
            "ComposerAsc" => 6,
            _ => 0
        };

        string subtitleMode = Preferences.Default.Get("ScoreSubtitleDisplay", "DateAdded");
        ScoreSubtitlePicker.SelectedIndex = subtitleMode switch
        {
            "DateAdded" => 0,
            "Composer" => 1,
            "ComposerAndDate" => 2,
            _ => 0
        };

        ShowPageNumberSwitch.IsToggled = Preferences.Default.Get("ShowPageNumber", true);
        TwoPagesLandscapeSwitch.IsToggled = Preferences.Default.Get("TwoPagesLandscape", true);
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

    private void OnScoreSubtitleChanged(object sender, EventArgs e)
    {
        string value = ScoreSubtitlePicker.SelectedIndex switch
        {
            0 => "DateAdded",
            1 => "Composer",
            2 => "ComposerAndDate",
            _ => "DateAdded"
        };
        Preferences.Default.Set("ScoreSubtitleDisplay", value);
    }

    private void OnDefaultSortChanged(object sender, EventArgs e)
    {
        string value = DefaultSortPicker.SelectedIndex switch
        {
            0 => "DateDesc",
            1 => "DateAsc",
            2 => "TitleAsc",
            3 => "TitleDesc",
            4 => "ModifiedDesc",
            5 => "RatingDesc",
            6 => "ComposerAsc",
            _ => "DateDesc"
        };
        Preferences.Default.Set("DefaultScoreSort", value);
    }

    private void OnShowPageNumberToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("ShowPageNumber", e.Value);
    }

    private void OnTwoPagesLandscapeToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("TwoPagesLandscape", e.Value);
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
