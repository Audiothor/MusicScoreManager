namespace MusicScoreManager;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        UpdateTabTitles();
        Services.LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateTabTitles);
    }

    private void UpdateTabTitles()
    {
        var loc = Services.LocalizationService.Instance;
        if (TabScores != null) TabScores.Title = loc.GetString("Nav_Scores", "Partitions");
        if (TabSetlists != null) TabSetlists.Title = loc.GetString("Nav_Setlists", "Setlists");
        if (TabTools != null) TabTools.Title = loc.GetString("Nav_Tools", "Outils");
        if (TabSettings != null) TabSettings.Title = loc.GetString("Nav_Settings", "Paramètres");
        if (TabQuit != null) TabQuit.Title = loc.GetString("Nav_Quit", "Quitter");
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        if (args.Target?.Location?.OriginalString?.Contains("QuitPage") == true)
        {
            args.Cancel();
            _ = HandleQuitAsync();
        }
    }

    private async Task HandleQuitAsync()
    {
        bool confirm = Preferences.Default.Get("ConfirmBeforeQuit", false);
        if (confirm)
        {
            var loc = Services.LocalizationService.Instance;
            bool proceed = await DisplayAlertAsync(
                loc.GetString("Settings_Confirm_Quit_Title", "Quitter l'application"),
                loc.GetString("Settings_Confirm_Quit_Prompt", "Voulez-vous vraiment quitter l'application ?"),
                loc.GetString("Common_Yes", "Oui"),
                loc.GetString("Common_No", "Non"));

            if (!proceed) return;
        }

#if ANDROID
        Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#else
        Application.Current?.Quit();
#endif
    }
}
