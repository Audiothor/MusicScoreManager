using MusicScoreManager.Services;
using CommunityToolkit.Maui.Storage;

namespace MusicScoreManager;

public partial class SettingsAppPage : ContentPage
{
    private readonly SettingsService _settingsService;

    public SettingsAppPage()
    {
        InitializeComponent();
        _settingsService = new SettingsService();
        RefreshUI();
    }

    private void RefreshUI()
    {
        ScoresPathLabel.Text = _settingsService.ScoresRootDirectory;
        AudioPathLabel.Text = _settingsService.AudioRootDirectory;
    }

    private async void OnChangeScoresPathClicked(object sender, EventArgs e)
    {
        var result = await FolderPicker.Default.PickAsync(_settingsService.ScoresRootDirectory, default);

        if (result != null && result.IsSuccessful && result.Folder != null)
        {
            string newPath = result.Folder.Path;
            if (await ValidateAndSetPath(newPath, true))
            {
                _settingsService.ScoresRootDirectory = newPath;
                RefreshUI();
            }
        }
    }

    private async void OnChangeAudioPathClicked(object sender, EventArgs e)
    {
        var result = await FolderPicker.Default.PickAsync(_settingsService.AudioRootDirectory, default);

        if (result != null && result.IsSuccessful && result.Folder != null)
        {
            string newPath = result.Folder.Path;
            if (await ValidateAndSetPath(newPath, false))
            {
                _settingsService.AudioRootDirectory = newPath;
                RefreshUI();
            }
        }
    }

    private async Task<bool> ValidateAndSetPath(string newPath, bool isScores)
    {
        try 
        {
            if (!Directory.Exists(newPath))
            {
                bool create = await this.DisplayAlertAsync("Dossier inexistant", "Ce dossier n'existe pas. Voulez-vous le créer ?", "Oui", "Non");
                if (create)
                {
                    Directory.CreateDirectory(newPath);
                    return true;
                }
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            await this.DisplayAlertAsync("Erreur", "Impossible d'utiliser ce répertoire : " + ex.Message, "OK");
            return false;
        }
    }
}
