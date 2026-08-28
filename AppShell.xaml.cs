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
