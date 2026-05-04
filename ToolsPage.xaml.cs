using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class ToolsPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public ToolsPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        FolderPathLabel.Text = $"Chemin : {_databaseService.GetBackupsFolder()}";
        LoadSettings();
        LoadBackupsList();
    }

    private void LoadSettings()
    {
        IntervalEntry.Text = Preferences.Default.Get("BackupIntervalDays", 30).ToString();
        MaxKeepEntry.Text = Preferences.Default.Get("MaxBackupFiles", 6).ToString();
    }

    private void LoadBackupsList()
    {
        BackupsCollectionView.ItemsSource = _databaseService.GetBackups();
    }

    private void OnCancelSettingsClicked(object sender, EventArgs e)
    {
        LoadSettings();
    }

    private async void OnSaveSettingsClicked(object sender, EventArgs e)
    {
        if (int.TryParse(IntervalEntry.Text, out int interval) && int.TryParse(MaxKeepEntry.Text, out int maxKeep))
        {
            Preferences.Default.Set("BackupIntervalDays", interval);
            Preferences.Default.Set("MaxBackupFiles", maxKeep);
            await DisplayAlertAsync("Succès", "Paramètres enregistrés.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Erreur", "Veuillez entrer des nombres valides.", "OK");
        }
    }

    private async void OnBackupClicked(object sender, EventArgs e)
    {
        try
        {
            await _databaseService.BackupDatabaseAsync();
            int maxKeep = Preferences.Default.Get("MaxBackupFiles", 6);
            _databaseService.PurgeBackups(maxKeep);
            
            LoadBackupsList();
            Preferences.Default.Set("LastBackupDate", DateTime.Now);
            
            await DisplayAlertAsync("Sauvegarde", "Base de données sauvegardée avec succès.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors de la sauvegarde : {ex.Message}", "OK");
        }
    }

    private async void OnItemRestoreClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is BackupFile backup)
        {
            bool confirm = await DisplayAlertAsync("Restauration", 
                $"Voulez-vous vraiment restaurer la sauvegarde du {backup.DisplayDate} ?\n\nATTENTION : La base de données actuelle sera écrasée.", 
                "Restaurer", "Annuler");

            if (confirm)
            {
                try
                {
                    await _databaseService.RestoreBackupAsync(backup.FullPath);
                    await DisplayAlertAsync("Succès", "Restauration terminée. L'application va maintenant redémarrer pour appliquer les changements.", "OK");
                    
                    await Navigation.PopToRootAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Erreur", $"Échec de la restauration : {ex.Message}", "OK");
                }
            }
        }
    }
}
