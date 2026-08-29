using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class ToolsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private readonly ExportImportService _exportImportService;

    public ToolsPage(DatabaseService databaseService, ExportImportService exportImportService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _settingsService = new SettingsService();
        _exportImportService = exportImportService;
        FolderPathLabel.Text = $"Chemin : {_databaseService.GetBackupsFolder()}";
        LoadSettings();
        LoadBackupsList();
    }

    public ToolsPage(DatabaseService databaseService) : this(databaseService, new ExportImportService(databaseService, new SettingsService()))
    {
    }

    public ToolsPage() : this(new DatabaseService(), new ExportImportService(new DatabaseService(), new SettingsService()))
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTagsAsync(SearchTagBar?.Text ?? string.Empty);
        LoadSettings();
        LoadBackupsList();
        UpdateWarningText();
    }

    #region Gestion des Étiquettes

    private async Task LoadTagsAsync(string query = "")
    {
        var tags = await _databaseService.GetTagsAsync();
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            tags = tags.Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        TagsCollectionView.ItemsSource = tags.OrderBy(t => t.Name).ToList();
    }

    private async void OnSearchTagBarTextChanged(object? sender, TextChangedEventArgs e)
    {
        await LoadTagsAsync(e.NewTextValue);
    }

    private async void OnAddTagClicked(object? sender, EventArgs e)
    {
        var newTag = new Tag { Name = "" };
        await Navigation.PushAsync(new TagEditPage(newTag, _databaseService));
    }

    private async void OnTagSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Tag tag)
        {
            await Navigation.PushAsync(new TagEditPage(tag, _databaseService));
            TagsCollectionView.SelectedItem = null;
        }
    }

    private async void OnRenameTagInvoked(object? sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Tag tag)
        {
            await Navigation.PushAsync(new TagEditPage(tag, _databaseService));
        }
    }

    private async void OnDeleteTagInvoked(object? sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Tag tag)
        {
            bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer l'étiquette '{tag.Name}' ?", "Oui", "Non");
            if (answer)
            {
                await _databaseService.DeleteTagAsync(tag);
                await LoadTagsAsync(SearchTagBar.Text);
            }
        }
    }

    #endregion

    #region Gestion des Sauvegardes

    private void UpdateWarningText()
    {
        string scoresPath = _settingsService.ScoresRootDirectory;
        string audioPath = _settingsService.AudioRootDirectory;

        BackupWarningLabel.Text = $"Important : La sauvegarde ne concerne UNIQUEMENT que la base de données (titres, tags, liens).\n\n" +
                                  $"Les fichiers physiques des partitions situés dans :\n{scoresPath}\n\n" +
                                  $"ainsi que les fichiers audio situés dans :\n{audioPath}\n\n" +
                                  $"NE SONT PAS PRIS EN COMPTE. Vous devez les sauvegarder manuellement.";
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

    private async void OnBackupClicked(object? sender, EventArgs e)
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

    private async void OnItemRestoreClicked(object? sender, EventArgs e)
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
                    await DisplayAlertAsync("Succès", "Base de données restaurée avec succès.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Erreur", $"Erreur lors de la restauration : {ex.Message}", "OK");
                }
            }
        }
    }

    #endregion

    #region Import de Packages

    private async void OnImportPackageClicked(object? sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/zip", "application/octet-stream", "*/*" } },
                { DevicePlatform.WinUI, new[] { ".msmsetlist", ".msmscore", ".msmscores", ".zip" } },
                { DevicePlatform.iOS, new[] { "public.archive", "public.zip-archive" } },
                { DevicePlatform.MacCatalyst, new[] { "msmsetlist", "msmscore", "msmscores", "zip" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Sélectionner un paquet (.msmsetlist, .msmscore)",
                FileTypes = customFileType
            });

            if (result != null)
            {
                bool success = await _exportImportService.ImportPackageFromFileAsync(result.FullPath);
                if (success)
                {
                    await DisplayAlertAsync("Import réussi", $"Le paquet '{result.FileName}' a été importé avec succès !", "OK");
                }
                else
                {
                    await DisplayAlertAsync("Erreur", "Impossible de lire ou d'importer le paquet sélectionné.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors de l'import : {ex.Message}", "OK");
        }
    }

    #endregion

    #region Accordéons (Dépliage / Repliage)

    private void OnToggleTagsSectionTapped(object? sender, EventArgs e)
    {
        TagsContentSection.IsVisible = !TagsContentSection.IsVisible;
        TagsChevronLabel.Text = TagsContentSection.IsVisible ? "▼" : "▶";
    }

    private void OnToggleImportSectionTapped(object? sender, EventArgs e)
    {
        ImportContentSection.IsVisible = !ImportContentSection.IsVisible;
        ImportChevronLabel.Text = ImportContentSection.IsVisible ? "▼" : "▶";
    }

    private void OnToggleBackupSectionTapped(object? sender, EventArgs e)
    {
        BackupContentSection.IsVisible = !BackupContentSection.IsVisible;
        BackupChevronLabel.Text = BackupContentSection.IsVisible ? "▼" : "▶";
    }

    #endregion
}
