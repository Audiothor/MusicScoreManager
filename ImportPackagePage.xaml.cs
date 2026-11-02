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
        var loc = LocalizationService.Instance;
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
                PickerTitle = loc.GetString("Import_Package_Picker_Title", "Sélectionner un paquet (.msmsetlist, .msmscore)"),
                FileTypes = customFileType
            });

            if (result != null)
            {
                ImportStatusSection.IsVisible = true;
                string fileName = !string.IsNullOrWhiteSpace(result.FileName) ? result.FileName : loc.GetString("Import_Package_Default_Name", "paquet");
                ImportStatusLabel.Text = string.Format(loc.GetString("Import_Package_Progress_File", "Importation de '{0}' en cours..."), fileName);

                using var stream = await result.OpenReadAsync();
                bool success = await _exportImportService.ImportPackageFromStreamAsync(stream);
                ImportStatusSection.IsVisible = false;

                if (success)
                {
                    await DisplayAlertAsync(
                        loc.GetString("Import_Package_Success_Title", "Import réussi"),
                        string.Format(loc.GetString("Import_Package_Success_Msg", "Le paquet '{0}' a été importé avec succès !"), fileName),
                        loc.GetString("Common_OK", "OK"));
                }
                else
                {
                    await DisplayAlertAsync(
                        loc.GetString("Common_Error", "Erreur"),
                        loc.GetString("Import_Package_Error_Read", "Impossible de lire ou d'importer le paquet sélectionné."),
                        loc.GetString("Common_OK", "OK"));
                }
            }
        }
        catch (Exception ex)
        {
            ImportStatusSection.IsVisible = false;
            await DisplayAlertAsync(
                loc.GetString("Common_Error", "Erreur"),
                $"{loc.GetString("Import_Package_Error", "Erreur lors de l'import")} : {ex.Message}",
                loc.GetString("Common_OK", "OK"));
        }
    }
}
