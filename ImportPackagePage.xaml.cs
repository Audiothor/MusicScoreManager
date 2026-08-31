using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class ImportPackagePage : ContentPage
{
    private readonly ExportImportService _exportImportService;

    public ImportPackagePage(ExportImportService? exportImportService = null)
    {
        InitializeComponent();
        _exportImportService = exportImportService ?? new ExportImportService(new DatabaseService(), new SettingsService());
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

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
                ImportStatusSection.IsVisible = true;
                ImportStatusLabel.Text = $"Importation de '{result.FileName}' en cours...";

                bool success = await _exportImportService.ImportPackageFromFileAsync(result.FullPath);
                ImportStatusSection.IsVisible = false;

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
            ImportStatusSection.IsVisible = false;
            await DisplayAlertAsync("Erreur", $"Erreur lors de l'import : {ex.Message}", "OK");
        }
    }
}
