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
#if ANDROID
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#else
            Application.Current?.Quit();
#endif
        }
    }
}
