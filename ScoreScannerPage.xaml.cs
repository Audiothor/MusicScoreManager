using MusicScoreManager.Models;
using MusicScoreManager.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Path = System.IO.Path;
using Image = SixLabors.ImageSharp.Image;

namespace MusicScoreManager;

public class ScannerPageItem
{
    public string OriginalImagePath { get; set; } = string.Empty;
    public string ProcessedImagePath { get; set; } = string.Empty;
    public int Rotation { get; set; } = 0;
    public int PageIndex { get; set; } = 1;
}

public partial class ScoreScannerPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly PdfService _pdfService;
    private readonly SettingsService _settingsService;
    private readonly List<ScannerPageItem> _scannedPages = new();

    public ScoreScannerPage(DatabaseService? databaseService = null, PdfService? pdfService = null)
    {
        InitializeComponent();
        _databaseService = databaseService ?? new DatabaseService();
        _pdfService = pdfService ?? new PdfService();
        _settingsService = new SettingsService();

        InitFilterPicker();
        UpdateUI();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync(string? preSelectCategory = null)
    {
        var categories = await _databaseService.GetCategoriesAsync();
        ScoreCategoryPicker.ItemsSource = categories;

        if (!string.IsNullOrEmpty(preSelectCategory) && categories.Contains(preSelectCategory))
        {
            ScoreCategoryPicker.SelectedItem = preSelectCategory;
        }
        else if (categories.Contains("Général"))
        {
            ScoreCategoryPicker.SelectedItem = "Général";
        }
        else if (categories.Any())
        {
            ScoreCategoryPicker.SelectedIndex = 0;
        }
    }

    private async void OnAddNewCategoryInScannerClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        string title = loc.GetString("Score_Category_New_Title", "Nouveau dossier");
        string prompt = loc.GetString("Score_Category_New_Prompt", "Entrez le nom du nouveau dossier / catégorie :");
        string ok = loc.GetString("Common_OK", "OK");
        string cancel = loc.GetString("Common_Cancel", "Annuler");

        string? result = await DisplayPromptAsync(title, prompt, ok, cancel);
        if (!string.IsNullOrWhiteSpace(result))
        {
            string clean = result.Trim();
            await _databaseService.CreateCategoryAsync(clean);
            await LoadCategoriesAsync(clean);
        }
    }

    private void InitFilterPicker()
    {
        var loc = LocalizationService.Instance;
        EnhancementFilterPicker.ItemsSource = new List<string>
        {
            loc.GetString("ScoreScanner_Filter_BW", "Partition N&B optimisée (Haut contraste)"),
            loc.GetString("ScoreScanner_Filter_Gray", "Niveaux de gris"),
            loc.GetString("ScoreScanner_Filter_Original", "Couleur originale")
        };
        EnhancementFilterPicker.SelectedIndex = 0; // N&B optimisé par défaut
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnTakePhotoClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;

        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert(loc.GetString("Common_Info", "Information"),
                    loc.GetString("ScoreScanner_No_Camera", "L'appareil photo n'est pas disponible sur cet appareil. Utilisez le bouton 'Choisir une image'."),
                    loc.GetString("Common_OK", "OK"));
                return;
            }

            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert(loc.GetString("Common_Warning", "Attention"),
                    loc.GetString("ScoreScanner_Camera_Permission", "L'autorisation d'accéder à l'appareil photo est requise pour numériser une partition."),
                    loc.GetString("Common_OK", "OK"));
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                await ProcessAndAddCapturedFileAsync(photo);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScoreScanner] Erreur capture photo: {ex.Message}");
            await DisplayAlert(loc.GetString("Common_Error", "Erreur"), ex.Message, loc.GetString("Common_OK", "OK"));
        }
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        try
        {
            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = LocalizationService.Instance.GetString("ScoreScanner_PickImage", "Sélectionner des photos de partition"),
                FileTypes = FilePickerFileType.Images
            });

            if (results != null)
            {
                foreach (var file in results)
                {
                    if (file != null)
                    {
                        await ProcessAndAddCapturedFileAsync(file);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScoreScanner] Erreur sélection photo: {ex.Message}");
        }
    }

    private async Task ProcessAndAddCapturedFileAsync(FileResult file)
    {
        try
        {
            string cacheDir = Path.Combine(FileSystem.CacheDirectory, "Scanner");
            if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

            string originalTempPath = Path.Combine(cacheDir, $"raw_{Guid.NewGuid():N}.jpg");
            using (var stream = await file.OpenReadAsync())
            using (var fileStream = File.Create(originalTempPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            int filterIndex = EnhancementFilterPicker.SelectedIndex;
            string processedPath = await ApplyImageEnhancementAsync(originalTempPath, filterIndex);

            var pageItem = new ScannerPageItem
            {
                OriginalImagePath = originalTempPath,
                ProcessedImagePath = processedPath,
                Rotation = 0,
                PageIndex = _scannedPages.Count + 1
            };

            _scannedPages.Add(pageItem);
            UpdateUI();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScoreScanner] Erreur traitement page: {ex.Message}");
        }
    }

    private async Task<string> ApplyImageEnhancementAsync(string sourcePath, int filterIndex)
    {
        return await Task.Run(() =>
        {
            // 0 = N&B optimisé partition, 1 = Niveaux de gris, 2 = Original
            if (filterIndex == 2)
            {
                return sourcePath;
            }

            string outPath = Path.Combine(Path.GetDirectoryName(sourcePath)!, $"proc_{Guid.NewGuid():N}.jpg");

            try
            {
                using var image = Image.Load(sourcePath);

                if (filterIndex == 0)
                {
                    // Partition N&B optimisée : blanchiment du fond gris/jaune, renforcement noir des portées
                    image.Mutate(ctx => ctx
                        .Grayscale()
                        .Contrast(1.65f)
                        .Brightness(1.15f));
                }
                else if (filterIndex == 1)
                {
                    // Niveaux de gris haute fidélité
                    image.Mutate(ctx => ctx
                        .Grayscale()
                        .Contrast(1.2f));
                }

                image.SaveAsJpeg(outPath);
                return outPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScoreScanner] Échec filtre ImageSharp, utilisation brute: {ex.Message}");
                return sourcePath;
            }
        });
    }

    private async void OnEnhancementFilterChanged(object? sender, EventArgs e)
    {
        if (!_scannedPages.Any()) return;

        int filterIndex = EnhancementFilterPicker.SelectedIndex;
        for (int i = 0; i < _scannedPages.Count; i++)
        {
            var page = _scannedPages[i];
            page.ProcessedImagePath = await ApplyImageEnhancementAsync(page.OriginalImagePath, filterIndex);
        }

        RenderPagesList();
    }

    private void UpdateUI()
    {
        var loc = LocalizationService.Instance;
        int count = _scannedPages.Count;

        bool hasPages = count > 0;
        GenerateScoreTopButton.IsEnabled = hasPages;
        GenerateScoreBottomButton.IsEnabled = hasPages;
        ClearAllButton.IsVisible = hasPages;
        EmptyPagesBox.IsVisible = !hasPages;

        PagesCountLabel.Text = hasPages
            ? string.Format(loc.GetString("ScoreScanner_Pages_Count", "{0} page(s) numérisée(s)"), count)
            : loc.GetString("ScoreScanner_No_Pages", "Aucune page numérisée");

        RenderPagesList();
    }

    private void RenderPagesList()
    {
        PagesContainer.Children.Clear();

        for (int i = 0; i < _scannedPages.Count; i++)
        {
            int index = i;
            var page = _scannedPages[index];
            page.PageIndex = index + 1;

            var card = BuildPageCardUI(page, index);
            PagesContainer.Children.Add(card);
        }
    }

    private View BuildPageCardUI(ScannerPageItem page, int index)
    {
        var border = new Border
        {
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#1E1E2A"),
            Stroke = Microsoft.Maui.Graphics.Color.FromArgb("#333344"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(70, GridUnitType.Absolute)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12,
            VerticalOptions = LayoutOptions.Center
        };

        // Vignette de la page
        var img = new Microsoft.Maui.Controls.Image
        {
            Source = ImageSource.FromFile(page.ProcessedImagePath),
            Aspect = Aspect.AspectFit,
            HeightRequest = 90,
            WidthRequest = 70,
            Rotation = page.Rotation,
            VerticalOptions = LayoutOptions.Center
        };
        grid.Children.Add(img);
        Grid.SetColumn(img, 0);

        // Informations de la page
        var infoStack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 4
        };

        var pageLabel = new Label
        {
            Text = $"Page {page.PageIndex}",
            TextColor = Colors.White,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold
        };
        infoStack.Children.Add(pageLabel);

        var rotLabel = new Label
        {
            Text = $"Rotation: {page.Rotation}°",
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#8888AA"),
            FontSize = 11
        };
        infoStack.Children.Add(rotLabel);

        grid.Children.Add(infoStack);
        Grid.SetColumn(infoStack, 1);

        // Barre de boutons d'action
        var actionsStack = new HorizontalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 6
        };

        // Bouton rotation horaire 90°
        var rotRightBtn = new Button
        {
            Text = "⟳",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2A2D3E"),
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#007ACC"),
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = new Thickness(0),
            CornerRadius = 6
        };
        rotRightBtn.Clicked += (s, e) =>
        {
            page.Rotation = (page.Rotation + 90) % 360;
            rotLabel.Text = $"Rotation: {page.Rotation}°";
            img.Rotation = page.Rotation;
        };
        actionsStack.Children.Add(rotRightBtn);

        // Monter
        if (index > 0)
        {
            var upBtn = new Button
            {
                Text = "▲",
                FontSize = 12,
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2A2D3E"),
                TextColor = Colors.White,
                WidthRequest = 34,
                HeightRequest = 36,
                Padding = new Thickness(0),
                CornerRadius = 6
            };
            upBtn.Clicked += (s, e) =>
            {
                (_scannedPages[index - 1], _scannedPages[index]) = (_scannedPages[index], _scannedPages[index - 1]);
                UpdateUI();
            };
            actionsStack.Children.Add(upBtn);
        }

        // Descendre
        if (index < _scannedPages.Count - 1)
        {
            var downBtn = new Button
            {
                Text = "▼",
                FontSize = 12,
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2A2D3E"),
                TextColor = Colors.White,
                WidthRequest = 34,
                HeightRequest = 36,
                Padding = new Thickness(0),
                CornerRadius = 6
            };
            downBtn.Clicked += (s, e) =>
            {
                (_scannedPages[index + 1], _scannedPages[index]) = (_scannedPages[index], _scannedPages[index + 1]);
                UpdateUI();
            };
            actionsStack.Children.Add(downBtn);
        }

        // Supprimer
        var delBtn = new Button
        {
            Text = "✕",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#3A2226"),
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#FF5252"),
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = new Thickness(0),
            CornerRadius = 6
        };
        delBtn.Clicked += (s, e) =>
        {
            _scannedPages.RemoveAt(index);
            UpdateUI();
        };
        actionsStack.Children.Add(delBtn);

        grid.Children.Add(actionsStack);
        Grid.SetColumn(actionsStack, 2);

        border.Content = grid;
        return border;
    }

    private void OnClearAllPagesClicked(object? sender, EventArgs e)
    {
        _scannedPages.Clear();
        UpdateUI();
    }

    private async void OnGenerateScoreClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;

        if (!_scannedPages.Any())
        {
            await DisplayAlert(loc.GetString("Common_Error", "Erreur"),
                loc.GetString("ScoreScanner_No_Page_Error", "Veuillez numériser au moins une page pour créer la partition."),
                loc.GetString("Common_OK", "OK"));
            return;
        }

        string title = ScoreTitleEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = $"Partition_{DateTime.Now:yyyyMMdd_HHmm}";
        }

        string composer = ScoreComposerEntry.Text?.Trim() ?? string.Empty;

        GenerateScoreTopButton.IsEnabled = false;
        GenerateScoreBottomButton.IsEnabled = false;
        GenerateScoreBottomButton.Text = "⏳ Création du PDF...";

        try
        {
            string selectedCategory = ScoreCategoryPicker.SelectedItem as string ?? "Général";
            var rootDir = _settingsService.ScoresRootDirectory;
            if (!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);

            var destDir = string.Equals(selectedCategory, "Général", StringComparison.OrdinalIgnoreCase)
                ? rootDir
                : Path.Combine(rootDir, selectedCategory);
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            var invalidChars = Path.GetInvalidFileNameChars();
            var cleanTitle = new string(title.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Replace(" ", "_");

            string localPdfPath = Path.Combine(destDir, $"{cleanTitle}.pdf");
            int counter = 1;
            while (File.Exists(localPdfPath))
            {
                localPdfPath = Path.Combine(destDir, $"{cleanTitle}_{counter}.pdf");
                counter++;
            }

            var imagePaths = _scannedPages.Select(p => p.ProcessedImagePath).ToList();
            var rotations = _scannedPages.Select(p => p.Rotation).ToList();

            // Génération haute fidélité du PDF
            await _pdfService.ConvertImagesToPdfAsync(imagePaths, localPdfPath, rotations);

            // Enregistrement dans la bibliothèque de partitions
            var newScore = new Score
            {
                Title = title,
                Composer = composer,
                Category = selectedCategory,
                FilePath = _settingsService.GetRelativePath(localPdfPath),
                Type = ScoreType.PDF,
                DateAdded = DateTime.Now,
                PageCount = _scannedPages.Count,
                BPM = 120,
                ShowAnnotations = true,
                ShowPageNumber = true
            };

            await _databaseService.SaveScoreAsync(newScore);

            bool openNow = await DisplayAlert(
                loc.GetString("ScoreScanner_Success_Title", "Partition créée !"),
                string.Format(loc.GetString("ScoreScanner_Success_Msg", "La partition '{0}' a été créée avec succès ({1} pages) et ajoutée à votre bibliothèque.\n\nSouhaitez-vous l'ouvrir maintenant ?"), title, _scannedPages.Count),
                loc.GetString("Score_Open", "Ouvrir"),
                loc.GetString("Common_Close", "Fermer"));

            if (openNow)
            {
                await Navigation.PushAsync(new ViewerPage(newScore));
            }
            else
            {
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScoreScanner] Erreur génération: {ex.Message}");
            await DisplayAlert(loc.GetString("Common_Error", "Erreur"), ex.Message, loc.GetString("Common_OK", "OK"));
        }
        finally
        {
            GenerateScoreTopButton.IsEnabled = true;
            GenerateScoreBottomButton.IsEnabled = true;
            GenerateScoreBottomButton.Text = LocalizationService.Instance.GetString("ScoreScanner_Generate_Full", "📑 Créer la partition PDF");
        }
    }
}
