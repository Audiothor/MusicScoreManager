namespace MusicScoreManager;

public partial class QuitPage : ContentPage
{
    public QuitPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#else
        Application.Current?.Quit();
#endif
    }
}
