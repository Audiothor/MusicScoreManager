namespace MusicScoreManager;

public partial class ToolsPage : ContentPage
{
    public ToolsPage()
    {
        InitializeComponent();
    }

    private async void OnBackupClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Sauvegarde", "Fonction de sauvegarde à venir.", "OK");
    }

    private async void OnRestoreClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Restauration", "Fonction de restauration à venir.", "OK");
    }
}
