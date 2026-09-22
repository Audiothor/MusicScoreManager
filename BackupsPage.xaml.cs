using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class BackupsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;

    public BackupsPage(DatabaseService? databaseService = null)
    {
        InitializeComponent();
        _databaseService = databaseService ?? new DatabaseService();
        _settingsService = new SettingsService();

        FolderPathLabel.Text = $"{LocalizationService.Instance.GetString("Scores_Edit_FilePath", "Chemin")} : {_databaseService.GetBackupsFolder()}";
        LoadSettings();
        LoadBackupsList();
        UpdateWarningText();

        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            FolderPathLabel.Text = $"{LocalizationService.Instance.GetString("Scores_Edit_FilePath", "Chemin")} : {_databaseService.GetBackupsFolder()}";
            UpdateWarningText();
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSettings();
        LoadBackupsList();
        UpdateWarningText();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void UpdateWarningText()
    {
        string scoresPath = _settingsService.ScoresRootDirectory;
        string audioPath = _settingsService.AudioRootDirectory;
        var loc = LocalizationService.Instance;

        string template = loc.GetString("Backups_Warning_Full_Notice",
            "Important : La sauvegarde ne concerne UNIQUEMENT que la base de données (titres, tags, liens).\n\n" +
            "Les fichiers physiques des partitions situés dans :\n{0}\n\n" +
            "ainsi que les fichiers audio situés dans :\n{1}\n\n" +
            "NE SONT PAS PRIS EN COMPTE. Vous devez les sauvegarder manuellement.");

        BackupWarningLabel.Text = string.Format(template, scoresPath, audioPath);
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

    private void OnCancelSettingsClicked(object? sender, EventArgs e)
    {
        LoadSettings();
    }

    private async void OnSaveSettingsClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        if (int.TryParse(IntervalEntry.Text, out int interval) && int.TryParse(MaxKeepEntry.Text, out int maxKeep))
        {
            Preferences.Default.Set("BackupIntervalDays", interval);
            Preferences.Default.Set("MaxBackupFiles", maxKeep);
            await DisplayAlertAsync(
                loc.GetString("Common_Success", "Succès"), 
                loc.GetString("Backups_Settings_Saved", "Paramètres enregistrés."), 
                loc.GetString("Common_OK", "OK"));
        }
        else
        {
            await DisplayAlertAsync(
                loc.GetString("Common_Error", "Erreur"), 
                loc.GetString("Backups_Invalid_Numbers", "Veuillez entrer des nombres valides."), 
                loc.GetString("Common_OK", "OK"));
        }
    }

    private async void OnBackupClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        try
        {
            await _databaseService.BackupDatabaseAsync();
            int maxKeep = Preferences.Default.Get("MaxBackupFiles", 6);
            _databaseService.PurgeBackups(maxKeep);
            
            LoadBackupsList();
            Preferences.Default.Set("LastBackupDate", DateTime.Now);
            
            await DisplayAlertAsync(
                loc.GetString("Backups_Page_Title", "Sauvegarde"), 
                loc.GetString("Backups_Success", "Base de données sauvegardée avec succès."), 
                loc.GetString("Common_OK", "OK"));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                loc.GetString("Common_Error", "Erreur"), 
                string.Format(loc.GetString("Backups_Error_Msg", "Erreur lors de la sauvegarde : {0}"), ex.Message), 
                loc.GetString("Common_OK", "OK"));
        }
    }

    private async void OnItemRestoreClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is BackupFile backup)
        {
            var loc = LocalizationService.Instance;
            bool confirm = await DisplayAlertAsync(
                loc.GetString("Backups_Restore_Btn", "Restauration"), 
                string.Format(loc.GetString("Backups_Restore_Confirm", "Voulez-vous vraiment restaurer la sauvegarde du {0} ?\n\nATTENTION : La base de données actuelle sera écrasée."), backup.DisplayDate), 
                loc.GetString("Backups_Restore_Btn", "Restaurer"), 
                loc.GetString("Common_Cancel", "Annuler"));

            if (confirm)
            {
                try
                {
                    await _databaseService.RestoreBackupAsync(backup.FullPath);
                    await DisplayAlertAsync(
                        loc.GetString("Common_Success", "Succès"), 
                        loc.GetString("Backups_Restore_Success", "Base de données restaurée avec succès."), 
                        loc.GetString("Common_OK", "OK"));
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync(
                        loc.GetString("Common_Error", "Erreur"), 
                        string.Format(loc.GetString("Backups_Error_Msg", "Erreur lors de la restauration : {0}"), ex.Message), 
                        loc.GetString("Common_OK", "OK"));
                }
            }
        }
    }
}
