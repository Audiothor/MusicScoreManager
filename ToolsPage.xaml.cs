using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class ToolsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ExportImportService _exportImportService;
    private readonly PdfService _pdfService;

    public ToolsPage(DatabaseService databaseService, ExportImportService exportImportService, PdfService? pdfService = null)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _exportImportService = exportImportService;
        _pdfService = pdfService ?? new PdfService();
    }

    public ToolsPage(DatabaseService databaseService) : this(databaseService, new ExportImportService(databaseService, new SettingsService()), new PdfService())
    {
    }

    public ToolsPage() : this(new DatabaseService(), new ExportImportService(new DatabaseService(), new SettingsService()), new PdfService())
    {
    }

    /// <summary>
    /// Ouvre directement l'atelier d'assemblage PDF pour modifier une partition spécifique.
    /// </summary>
    public async Task OpenScoreInAssemblerAsync(Score score)
    {
        var page = new PdfAssemblerPage(_databaseService, _pdfService, score);
        await Navigation.PushAsync(page);
    }

    private async void OnToolsPdfAssemblerTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new PdfAssemblerPage(_databaseService, _pdfService));
    }

    private async void OnToolsTagsTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new TagsPage(_databaseService));
    }

    private async void OnToolsImportPackageTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new ImportPackagePage(_exportImportService));
    }

    private async void OnToolsDuplicatesTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new DuplicatesPage(_databaseService));
    }

    private async void OnToolsBackupsTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new BackupsPage(_databaseService));
    }
}
