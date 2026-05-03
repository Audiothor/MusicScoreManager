namespace MusicScoreManager;

public partial class ToolsPage : ContentPage
{
    public ToolsPage()
    {
        InitializeComponent();
    }

    private async void OnBackupClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Sauvegarde", "Fonction de sauvegarde à venir.", "OK");
    }

    private async void OnRestoreClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Restauration", "Fonction de restauration à venir.", "OK");
    }
}
