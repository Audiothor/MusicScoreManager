namespace MusicScoreManager;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}
