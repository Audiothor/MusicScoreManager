using CommunityToolkit.Maui.Storage;
using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class BackupsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private readonly FullBackupService _fullBackupService;

    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isOperationRunning = false;

    public BackupsPage(DatabaseService? databaseService = null)
    {
        InitializeComponent();
        _databaseService = databaseService ?? new DatabaseService();
        _settingsService = new SettingsService();
        _fullBackupService = new FullBackupService(_databaseService, _settingsService);

        FolderPathLabel.Text = $"{LocalizationService.Instance.GetString("Scores_Edit_FilePath", "Chemin")} : {_databaseService.GetBackupsFolder()}";
        FullBackupFolderEntry.Text = _settingsService.FullBackupRootDirectory;

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
        FullBackupFolderEntry.Text = _settingsService.FullBackupRootDirectory;
    }

    protected override bool OnBackButtonPressed()
    {
        if (_isOperationRunning)
        {
            return true; // Empêcher de fermer la page pendant la sauvegarde ou la restauration
        }
        return base.OnBackButtonPressed();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_isOperationRunning) return;
        await Navigation.PopAsync();
    }

    private void UpdateWarningText()
    {
        string scoresPath = _settingsService.ScoresRootDirectory;
        string audioPath = _settingsService.AudioRootDirectory;
        var loc = LocalizationService.Instance;

        string template = loc.GetString("Backups_Warning_Full_Notice",
            "Important : Cette sauvegarde rapide ci-dessus concerne UNIQUEMENT la base de données (titres, tags, liens).\n\n" +
            "Les fichiers physiques des partitions ({0}) et audio ({1}) n'y sont pas inclus.\n\n" +
            "POUR UNE SAUVEGARDE COMPLÈTE de l'ensemble de vos fichiers et données, veuillez réaliser la sauvegarde complète ci-dessous.");

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
        if (_isOperationRunning) return;

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
        if (_isOperationRunning) return;

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

    #region Sauvegarde Complète (Export & Import)

    private async void OnChangeFullBackupFolderClicked(object? sender, EventArgs e)
    {
        if (_isOperationRunning) return;

        try
        {
            string startFolder = _settingsService.FullBackupRootDirectory;
            if (string.IsNullOrWhiteSpace(startFolder) || !Directory.Exists(startFolder))
            {
                startFolder = _settingsService.ExportsRootDirectory;
            }

            var result = await FolderPicker.Default.PickAsync(startFolder, default);
            if (result != null && result.IsSuccessful && result.Folder != null)
            {
                string newPath = result.Folder.Path;
                if (!string.IsNullOrWhiteSpace(newPath))
                {
                    _settingsService.FullBackupRootDirectory = newPath;
                    FullBackupFolderEntry.Text = newPath;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", ex.Message, "OK");
        }
    }

    private void OnFullBackupFolderEntryCompleted(object? sender, EventArgs e)
    {
        ApplyFolderFromEntry();
    }

    private void OnFullBackupFolderEntryUnfocused(object? sender, FocusEventArgs e)
    {
        ApplyFolderFromEntry();
    }

    private void ApplyFolderFromEntry()
    {
        string text = FullBackupFolderEntry.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(text))
        {
            _settingsService.FullBackupRootDirectory = text;
        }
    }

    private async void OnFullBackupExportClicked(object? sender, EventArgs e)
    {
        if (_isOperationRunning) return;

        ApplyFolderFromEntry();
        string targetDir = _settingsService.FullBackupRootDirectory;
        if (string.IsNullOrWhiteSpace(targetDir))
        {
            targetDir = _settingsService.ExportsRootDirectory;
            _settingsService.FullBackupRootDirectory = targetDir;
            FullBackupFolderEntry.Text = targetDir;
        }

        var loc = LocalizationService.Instance;
        _cancellationTokenSource = new CancellationTokenSource();
        SetOperationRunning(true, loc.GetString("Backups_Full_Progress_Title_Export", "Sauvegarde complète en cours..."));

        var progress = new Progress<FullBackupProgressInfo>(p =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OperationProgressBar.Progress = Math.Clamp(p.Progress, 0.0, 1.0);
                ProgressPercentageLabel.Text = $"{(int)(Math.Clamp(p.Progress, 0.0, 1.0) * 100)} %";
                ProgressStatusLabel.Text = p.StatusMessage;
            });
        });

        try
        {
            var result = await Task.Run(() => _fullBackupService.CreateFullBackupAsync(
                targetDir, 
                progress, 
                _cancellationTokenSource.Token));

            SetOperationRunning(false);

            if (result.Success)
            {
                string sizeStr = FormatFileSize(result.FileSizeBytes);
                string msg = string.Format(
                    loc.GetString("Backups_Full_Export_Success", "Sauvegarde complète terminée avec succès !\n\nArchive enregistrée dans :\n{0}\n\nTaille : {1}"), 
                    result.FilePath, 
                    sizeStr);

                await DisplayAlertAsync(
                    loc.GetString("Common_Success", "Succès"), 
                    msg, 
                    loc.GetString("Common_OK", "OK"));
            }
            else if (result.IsCancelled)
            {
                await DisplayAlertAsync(
                    loc.GetString("Common_Information", "Information"), 
                    loc.GetString("Backups_Full_Cancelled", "Opération annulée."), 
                    loc.GetString("Common_OK", "OK"));
            }
            else
            {
                await DisplayAlertAsync(
                    loc.GetString("Common_Error", "Erreur"), 
                    result.ErrorMessage ?? loc.GetString("Backups_Error_Msg", "Erreur inconnue."), 
                    loc.GetString("Common_OK", "OK"));
            }
        }
        catch (Exception ex)
        {
            SetOperationRunning(false);
            await DisplayAlertAsync(
                loc.GetString("Common_Error", "Erreur"), 
                ex.Message, 
                loc.GetString("Common_OK", "OK"));
        }
    }

    private async void OnFullBackupImportClicked(object? sender, EventArgs e)
    {
        if (_isOperationRunning) return;

        var loc = LocalizationService.Instance;

        // 1. Choix du fichier zip d'export complet
        FileResult? fileResult;
        try
        {
            var zipFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.zip-archive", "com.pkware.zip-archive", "application/zip" } },
                { DevicePlatform.Android, new[] { "application/zip", "application/x-zip-compressed", "application/octet-stream" } },
                { DevicePlatform.WinUI, new[] { ".zip" } },
                { DevicePlatform.MacCatalyst, new[] { "public.zip-archive", "com.pkware.zip-archive", "zip" } }
            });

            fileResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = loc.GetString("Backups_Full_Import_Btn", "Restaurer une sauvegarde complète (.zip)"),
                FileTypes = zipFileType
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(loc.GetString("Common_Error", "Erreur"), ex.Message, loc.GetString("Common_OK", "OK"));
            return;
        }

        if (fileResult == null)
        {
            return; // Annulé par l'utilisateur
        }

        // 2. Avertissement explicite et confirmation solennelle
        bool confirm = await DisplayAlertAsync(
            loc.GetString("Backups_Full_Confirm_Title", "Confirmation de remplacement complet"),
            loc.GetString("Backups_Full_Confirm_Msg", "ATTENTION : Cette action va SUPPRIMER et REMPLACER INTÉGRALEMENT la base de données actuelle ainsi que tous vos fichiers et partitions existants !\n\nToutes les modifications non sauvegardées seront définitivement perdues.\n\nVoulez-vous vraiment continuer et écraser toutes vos données actuelles ?"),
            loc.GetString("Backups_Full_Confirm_Yes", "Écraser et Restaurer"),
            loc.GetString("Common_Cancel", "Annuler"));

        if (!confirm)
        {
            return;
        }

        // 3. Exécution avec blocage complet de l'interface
        _cancellationTokenSource = new CancellationTokenSource();
        SetOperationRunning(true, loc.GetString("Backups_Full_Progress_Title_Import", "Restauration complète en cours..."));

        var progress = new Progress<FullBackupProgressInfo>(p =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OperationProgressBar.Progress = Math.Clamp(p.Progress, 0.0, 1.0);
                ProgressPercentageLabel.Text = $"{(int)(Math.Clamp(p.Progress, 0.0, 1.0) * 100)} %";
                ProgressStatusLabel.Text = p.StatusMessage;

                // Une fois le remplacement physique entamé (au-delà de 60%), on verrouille le bouton annuler
                if (p.Progress >= 0.60)
                {
                    CancelOperationButton.IsEnabled = false;
                    CancelOperationButton.Opacity = 0.4;
                }
            });
        });

        try
        {
            string archivePath = fileResult.FullPath;
            var result = await Task.Run(() => _fullBackupService.RestoreFullBackupAsync(
                archivePath, 
                progress, 
                _cancellationTokenSource.Token));

            SetOperationRunning(false);

            if (result.Success)
            {
                LoadBackupsList();
                UpdateWarningText();

                string msg = string.Format(
                    loc.GetString("Backups_Full_Import_Success", "Restauration complète terminée avec succès !\n\n{0} partition(s) et {1} fichier(s) audio ont été restaurés et vérifiés avec succès."),
                    result.RestoredScoresCount,
                    result.RestoredAudioFilesCount);

                await DisplayAlertAsync(
                    loc.GetString("Common_Success", "Succès"), 
                    msg, 
                    loc.GetString("Common_OK", "OK"));
            }
            else if (result.IsCancelled)
            {
                await DisplayAlertAsync(
                    loc.GetString("Common_Information", "Information"), 
                    loc.GetString("Backups_Full_Cancelled", "Opération annulée."), 
                    loc.GetString("Common_OK", "OK"));
            }
            else
            {
                await DisplayAlertAsync(
                    loc.GetString("Common_Error", "Erreur"), 
                    result.ErrorMessage ?? loc.GetString("Backups_Error_Msg", "Erreur inconnue."), 
                    loc.GetString("Common_OK", "OK"));
            }
        }
        catch (Exception ex)
        {
            SetOperationRunning(false);
            await DisplayAlertAsync(
                loc.GetString("Common_Error", "Erreur"), 
                ex.Message, 
                loc.GetString("Common_OK", "OK"));
        }
    }

    private void OnCancelOperationClicked(object? sender, EventArgs e)
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            CancelOperationButton.IsEnabled = false;
            ProgressStatusLabel.Text = LocalizationService.Instance.GetString("Backups_Progress_Cancelling", "Annulation en cours...");
            _cancellationTokenSource.Cancel();
        }
    }

    private void SetOperationRunning(bool isRunning, string title = "")
    {
        _isOperationRunning = isRunning;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ProgressOverlay.IsVisible = isRunning;
            if (isRunning)
            {
                ProgressTitleLabel.Text = title;
                OperationProgressBar.Progress = 0.0;
                ProgressPercentageLabel.Text = "0 %";
                ProgressStatusLabel.Text = string.Empty;
                CancelOperationButton.IsEnabled = true;
                CancelOperationButton.Opacity = 1.0;
            }
        });
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F2} Go";
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F2} Mo";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F1} Ko";
        return $"{bytes} octets";
    }

    #endregion
}
