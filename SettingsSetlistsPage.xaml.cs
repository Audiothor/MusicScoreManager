namespace MusicScoreManager;

public partial class SettingsSetlistsPage : ContentPage
{
    private bool _isInitializing = true;

    public SettingsSetlistsPage()
    {
        InitializeComponent();
        
        var loc = Services.LocalizationService.Instance;

        DefaultSortPicker.ItemsSource = new List<string>
        {
            loc.GetString("Setlist_Sort_DateCreatedDesc", "Date de création (Récent d'abord)"),
            loc.GetString("Setlist_Sort_DateCreatedAsc", "Date de création (Ancien d'abord)"),
            loc.GetString("Setlist_Sort_ConcertDateAsc", "Date du concert (Prochain d'abord)"),
            loc.GetString("Setlist_Sort_ConcertDateDesc", "Date du concert (Lointain d'abord)"),
            loc.GetString("Setlist_Sort_NameAsc", "Nom (A-Z)"),
            loc.GetString("Setlist_Sort_NameDesc", "Nom (Z-A)"),
            loc.GetString("Setlist_Sort_Status", "Statut")
        };

        string defaultSort = Preferences.Default.Get("DefaultSetlistSort", "DateCreatedDesc");
        DefaultSortPicker.SelectedIndex = defaultSort switch
        {
            "DateCreatedDesc" or "DateDesc" => 0,
            "DateCreatedAsc" or "DateAsc" => 1,
            "ConcertDateAsc" => 2,
            "ConcertDateDesc" => 3,
            "NameAsc" => 4,
            "NameDesc" => 5,
            "Status" => 6,
            _ => 0
        };

        DoneAtBottomSwitch.IsToggled = Preferences.Default.Get("SetlistsDoneAtBottom", true);

        // Charger les préférences
        DefaultContinuousSwitch.IsToggled = Preferences.Default.Get("DefaultContinuousReading", true);
        ReturnToSetlistSwitch.IsToggled = Preferences.Default.Get("ReturnToSetlistOnEndOfScore", false);
        ShowSetlistProgressSwitch.IsToggled = Preferences.Default.Get("ShowSetlistProgressOverlay", true);

        _isInitializing = false;
    }

    private void OnDefaultSortChanged(object sender, EventArgs e)
    {
        if (_isInitializing) return;

        string value = DefaultSortPicker.SelectedIndex switch
        {
            0 => "DateCreatedDesc",
            1 => "DateCreatedAsc",
            2 => "ConcertDateAsc",
            3 => "ConcertDateDesc",
            4 => "NameAsc",
            5 => "NameDesc",
            6 => "Status",
            _ => "DateCreatedDesc"
        };
        Preferences.Default.Set("DefaultSetlistSort", value);
    }

    private void OnDoneAtBottomToggled(object sender, ToggledEventArgs e)
    {
        if (_isInitializing) return;
        Preferences.Default.Set("SetlistsDoneAtBottom", e.Value);
    }

    private void OnDefaultContinuousToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("DefaultContinuousReading", e.Value);
    }

    private void OnReturnToSetlistToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("ReturnToSetlistOnEndOfScore", e.Value);
    }

    private void OnShowSetlistProgressToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("ShowSetlistProgressOverlay", e.Value);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
