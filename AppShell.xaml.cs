namespace MusicScoreManager;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        if (args.Target?.Location?.OriginalString?.Contains("ExitApp") == true)
        {
            Application.Current?.Quit();
        }
    }
}
