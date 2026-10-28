using MusicScoreManager.Services;
using MusicScoreManager.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO.Compression;
using System.ComponentModel;

namespace MusicScoreManager;

public partial class ScoresPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ImportService _importService;
    private readonly IWifiDirectTransferService _wifiService;
    private readonly ExportImportService _exportImportService;
    private readonly SettingsService _settingsService = new();
    private readonly PdfService _pdfService = new();
    private string _currentSort = "DateDesc";
    private bool _hasUserCustomSort = false;
    private readonly List<int> _selectedTagIds = new();
    private List<Models.Tag> _allTags = new();
    private string _tagSearchQuery = string.Empty;
    private string _tagSortType = "NameAsc"; // "NameAsc", "NameDesc", "SelectedFirst", "Color"
    private Models.Score? _selectedScoreForMenu;
    private CancellationTokenSource? _searchCts;
    private bool _cachedShowScrollTop = true;
    private bool _cachedShowAlphabeticalIndex = true;
    private readonly Dictionary<string, int> _alphabeticalIndexMap = new();

    private readonly ObservableCollection<WifiDeviceInfo> _discoveredDevices = new();

    public ObservableCollection<ScoreCategoryGroup> CategoryGroups { get; } = new();
    private bool _isFoldersView = false;
    private string? _selectedCategory = null;
    private List<Models.Score> _lastLoadedScores = new();

    public static readonly BindableProperty IsMultiSelectActiveProperty =
        BindableProperty.Create(nameof(IsMultiSelectActive), typeof(bool), typeof(ScoresPage), false, propertyChanged: OnIsMultiSelectActiveChanged);

    public bool IsMultiSelectActive
    {
        get => (bool)GetValue(IsMultiSelectActiveProperty);
        set => SetValue(IsMultiSelectActiveProperty, value);
    }

    private static void OnIsMultiSelectActiveChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ScoresPage page)
        {
            page.UpdateMultiSelectUI();
        }
    }

    public System.Windows.Input.ICommand ScoreTappedCommand { get; }
    public System.Windows.Input.ICommand ScoreLongPressCommand { get; }

    public ScoresPage(DatabaseService databaseService, ImportService importService, IWifiDirectTransferService wifiService, ExportImportService exportImportService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _importService = importService;
        _wifiService = wifiService;
        _exportImportService = exportImportService;

        ScoreTappedCommand = new Command<Models.Score>(async (score) =>
        {
            if (score != null) await HandleScoreTappedAsync(score);
        });

        ScoreLongPressCommand = new Command<Models.Score>((score) =>
        {
            OnScoreLongPressed(score);
        });

        _currentSort = Preferences.Default.Get("DefaultScoreSort", "DateDesc");

        _importService.ConversionStateChanged += OnConversionStateChanged;
        _importService.ConfirmDialogHandler = ShowModernConfirmDialogAsync;
        _importService.ActionSheetDialogHandler = ShowModernChoiceDialogAsync;
        _importService.PromptDialogHandler = ShowModernPromptDialogAsync;
        _importService.AlertHandler = ShowModernAlertDialogAsync;

        WifiDevicesListView.ItemsSource = _discoveredDevices;
        _cachedShowScrollTop = Preferences.Default.Get("ShowScrollTop", true);
        _cachedShowAlphabeticalIndex = Preferences.Default.Get("ShowAlphabeticalIndex", true);
        BuildAlphabeticalIndex();

        string defaultView = Preferences.Default.Get("DefaultScoresViewMode", "List");
        string currentView = Preferences.Default.Get("ScoresViewMode", defaultView);
        _isFoldersView = (currentView == "Folders");
        UpdateViewModeUI();
    }

    public ScoresPage() : this(
        new DatabaseService(), 
        new ImportService(new DatabaseService()), 
        new WifiDirectTransferService(), 
        new ExportImportService(new DatabaseService(), new SettingsService()))
    {
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // S'abonner aux événements Wi-Fi Direct
        _wifiService.DeviceDiscovered += OnWifiDeviceDiscovered;
        _wifiService.ScanFinished += OnWifiScanFinished;
        _wifiService.TransferProgressChanged += OnWifiTransferProgressChanged;

        string defaultView = Preferences.Default.Get("DefaultScoresViewMode", "List");
        string currentView = Preferences.Default.Get("ScoresViewMode", defaultView);
        _isFoldersView = (currentView == "Folders");
        UpdateViewModeUI();

        if (!_hasUserCustomSort)
        {
            _currentSort = Preferences.Default.Get("DefaultScoreSort", "DateDesc");
        }

        _ = Task.Run(async () =>
        {
            try 
            {
                _allTags = await _databaseService.GetTagsAsync();
                
                string query = string.Empty;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateActiveTagsChips();
                });

                var scores = await _databaseService.SearchScoresAsync(query, _selectedTagIds);
                scores = SortScores(scores, _currentSort);

                string subtitlePref = Preferences.Default.Get("ScoreSubtitleDisplay", "DateAdded");
                foreach (var score in scores)
                {
                    score.DisplaySubtitle = score.GetSubtitle(subtitlePref);
                    score.PropertyChanged -= OnScorePropertyChanged;
                    score.PropertyChanged += OnScorePropertyChanged;
                }

                _lastLoadedScores = scores;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (EmptyScoresLabel != null)
                    {
                        EmptyScoresLabel.Text = LocalizationService.Instance.GetString("Scores_Empty", "Aucune partition trouvée.");
                    }
                    ApplyCurrentViewModeData(scores);
                });

                // Calcul asynchrone des nombres de pages manquants en arrière-plan sans bloquer l'affichage initial
                var missingPageScores = scores.Where(s => s.PageCount <= 0).ToList();
                if (missingPageScores.Count > 0)
                {
                    foreach (var score in missingPageScores)
                    {
                        if (score.Type == ScoreType.Image)
                        {
                            score.PageCount = 1;
                            _ = _databaseService.SaveScoreAsync(score);
                        }
                        else if (score.Type == ScoreType.PDF)
                        {
                            string fullPath = _settingsService.GetAbsolutePath(score.FilePath);
                            if (File.Exists(fullPath))
                            {
                                int cnt = await _pdfService.GetPdfPageCountAsync(fullPath);
                                score.PageCount = cnt > 0 ? cnt : 1;
                                _ = _databaseService.SaveScoreAsync(score);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ScoresPage.OnAppearing: {ex.Message}");
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Désabonner des événements Wi-Fi Direct pour éviter les fuites de mémoire
        _wifiService.DeviceDiscovered -= OnWifiDeviceDiscovered;
        _wifiService.ScanFinished -= OnWifiScanFinished;
        _wifiService.TransferProgressChanged -= OnWifiTransferProgressChanged;

        // Arrêter l'écoute, le scan ou la diffusion de groupe
        _ = _wifiService.StopScanningAsync();
        _ = _wifiService.StopListeningAsync();
        _ = _wifiService.StopGroupBroadcastAsync();
    }

    private void UpdateActiveTagsChips()
    {
        ActiveTagsStack.Children.Clear();
        var activeTags = _allTags.Where(t => _selectedTagIds.Contains(t.Id)).ToList();
        
        if (activeTags.Any())
        {
            ActiveTagsScrollView.IsVisible = true;
            foreach (var tag in activeTags)
            {
                var border = new Border
                {
                    BackgroundColor = Color.FromArgb(tag.ColorHex),
                    StrokeThickness = 0,
                    Padding = new Thickness(12, 6, 8, 6),
                    VerticalOptions = LayoutOptions.Center,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }
                };

                var labelStack = new HorizontalStackLayout { Spacing = 6 };

                var label = new Label
                {
                    Text = tag.Name + "  ",
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.NoWrap
                };

                var closeLabel = new Label
                {
                    Text = "✕",
                    TextColor = Color.FromArgb("#DDDDDD"),
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 12
                };

                labelStack.Children.Add(label);
                labelStack.Children.Add(closeLabel);
                border.Content = labelStack;

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    _selectedTagIds.Remove(tag.Id);
                    UpdateActiveTagsChips();
                    await LoadScoresAsync(SearchScoreBar.Text);
                };
                border.GestureRecognizers.Add(tapGesture);

                ActiveTagsStack.Children.Add(border);
            }
        }
        else
        {
            ActiveTagsScrollView.IsVisible = false;
        }
    }

    private List<Models.Score> SortScores(IEnumerable<Models.Score> scores, string sortType)
    {
        bool emptyComposerFirst = Preferences.Default.Get("ComposerSortEmptyFirst", false);

        return sortType switch
        {
            "TitleAsc" => scores.OrderBy(s => s.Title).ToList(),
            "TitleDesc" => scores.OrderByDescending(s => s.Title).ToList(),
            "DateAsc" => scores.OrderBy(s => s.DateAdded).ToList(),
            "DateDesc" => scores.OrderByDescending(s => s.DateAdded).ToList(),
            "ModifiedDesc" => scores.OrderByDescending(s => s.DateModified).ToList(),
            "RatingDesc" => scores.OrderByDescending(s => s.Rating).ThenBy(s => s.Title).ToList(),
            "ComposerAsc" => emptyComposerFirst
                ? scores.OrderBy(s => string.IsNullOrWhiteSpace(s.Composer) ? 0 : 1).ThenBy(s => s.Composer).ThenBy(s => s.Title).ToList()
                : scores.OrderBy(s => string.IsNullOrWhiteSpace(s.Composer) ? 1 : 0).ThenBy(s => s.Composer).ThenBy(s => s.Title).ToList(),
            "NoTagsFirst" => scores.OrderBy(s => (s.AppliedTags != null && s.AppliedTags.Any()) ? 1 : 0).ThenBy(s => s.Title).ToList(),
            _ => scores.OrderByDescending(s => s.DateAdded).ToList()
        };
    }

    private async Task LoadScoresAsync(string query = "")
    {
        var scores = await _databaseService.SearchScoresAsync(query ?? string.Empty, _selectedTagIds);
        scores = SortScores(scores, _currentSort);

        string subtitlePref = Preferences.Default.Get("ScoreSubtitleDisplay", "DateAdded");
        foreach (var score in scores)
        {
            score.DisplaySubtitle = score.GetSubtitle(subtitlePref);
            score.PropertyChanged -= OnScorePropertyChanged;
            score.PropertyChanged += OnScorePropertyChanged;
        }

        _lastLoadedScores = scores;

        MainThread.BeginInvokeOnMainThread(() => {
            if (EmptyScoresLabel != null)
            {
                EmptyScoresLabel.Text = LocalizationService.Instance.GetString("Scores_Empty", "Aucune partition trouvée.");
            }
            ApplyCurrentViewModeData(scores);
        });

        // Calcul asynchrone des nombres de pages manquants en arrière-plan (temporisé pour libérer SQLite)
        var missingPageScores = scores.Where(s => s.PageCount <= 0).ToList();
        if (missingPageScores.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                foreach (var score in missingPageScores)
                {
                    try
                    {
                        if (score.Type == ScoreType.Image)
                        {
                            score.PageCount = 1;
                            await _databaseService.SaveScoreAsync(score);
                        }
                        else if (score.Type == ScoreType.PDF)
                        {
                            string fullPath = _settingsService.GetAbsolutePath(score.FilePath);
                            if (File.Exists(fullPath))
                            {
                                int cnt = await _pdfService.GetPdfPageCountAsync(fullPath);
                                score.PageCount = cnt > 0 ? cnt : 1;
                                await _databaseService.SaveScoreAsync(score);
                            }
                        }
                        await Task.Delay(40);
                    }
                    catch { }
                }
            });
        }
    }

    private void OnScorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Models.Score.IsSelected))
        {
            RecalculateSelectedCount();
        }
    }

    private void RecalculateSelectedCount()
    {
        int count = 0;
        if (ScoresCollectionView.ItemsSource is IEnumerable<Models.Score> scores)
        {
            count = scores.Count(s => s.IsSelected);
        }

        var loc = LocalizationService.Instance;
        SelectedCountLabel.Text = count == 1 
            ? loc.GetString("Scores_Bulk_Selected_One", "1 partition sélectionnée") 
            : string.Format(loc.GetString("Scores_Bulk_Selected_Multiple", "{0} partitions sélectionnées"), count);
    }

    private void UpdateMultiSelectUI()
    {
        if (IsMultiSelectActive)
        {
            MultiSelectActionBar.IsVisible = true;
            MultiSelectToggleButton.BackgroundColor = Color.FromArgb("#007ACC");
        }
        else
        {
            MultiSelectActionBar.IsVisible = false;
            MultiSelectToggleButton.BackgroundColor = Color.FromArgb("#333333");

            if (ScoresCollectionView.ItemsSource is IEnumerable<Models.Score> scores)
            {
                foreach (var score in scores)
                {
                    score.IsSelected = false;
                }
            }
            RecalculateSelectedCount();
        }
    }

    private void OnMultiSelectToggleClicked(object sender, EventArgs e)
    {
        IsMultiSelectActive = !IsMultiSelectActive;
    }

    private async void OnBulkDeleteClicked(object sender, EventArgs e)
    {
        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;

        var loc = LocalizationService.Instance;
        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any())
        {
            await DisplayAlertAsync(
                loc.GetString("Scores_Bulk_No_Selection_Title", "Aucune sélection"), 
                loc.GetString("Scores_Bulk_No_Selection_Msg", "Veuillez sélectionner au moins une partition."), 
                loc.GetString("Common_OK", "OK"));
            return;
        }

        bool confirm = await DisplayAlertAsync(
            loc.GetString("Scores_Bulk_Delete_Title", "Suppression groupée"), 
            string.Format(loc.GetString("Scores_Bulk_Delete_Confirm", "Voulez-vous vraiment supprimer les références de {0} partition(s) dans la base de données ?\n\nNote : Les fichiers physiques ne seront pas supprimés de votre appareil."), selectedScores.Count), 
            loc.GetString("Common_Yes", "Oui"), 
            loc.GetString("Common_No", "Non"));

        if (confirm)
        {
            foreach (var score in selectedScores)
            {
                await _databaseService.DeleteScoreAsync(score);
            }

            IsMultiSelectActive = false; // Quitter le mode sélection multiple
            await LoadScoresAsync(SearchScoreBar.Text);
            await DisplayAlertAsync(
                loc.GetString("Common_Success", "Succès"), 
                string.Format(loc.GetString("Scores_Bulk_Delete_Success", "{0} partition(s) ont été retirées de l'application."), selectedScores.Count), 
                loc.GetString("Common_OK", "OK"));
        }
    }

    private async void OnBulkCategoryClicked(object? sender, EventArgs e)
    {
        var selectedScores = _lastLoadedScores.Where(s => s.IsSelected).ToList();

        var loc = LocalizationService.Instance;
        if (!selectedScores.Any())
        {
            await DisplayAlertAsync(
                loc.GetString("Scores_Bulk_No_Selection_Title", "Aucune sélection"),
                loc.GetString("Scores_Bulk_No_Selection_Msg", "Veuillez sélectionner au moins une partition."),
                loc.GetString("Common_OK", "OK"));
            return;
        }

        string title = loc.GetString("Score_Category_Picker_Title", "Dossier de destination");
        string subtitle = $"{selectedScores.Count} partition(s) sélectionnée(s)";

        string? targetCategory = await ShowCategoryPickerModalAsync(title, subtitle, null);
        if (string.IsNullOrEmpty(targetCategory)) return;

        foreach (var score in selectedScores)
        {
            await _databaseService.MoveScoreToCategoryAsync(score, targetCategory);
        }

        IsMultiSelectActive = false;
        await LoadScoresAsync(SearchScoreBar?.Text ?? string.Empty);

        string successMsg = string.Format(loc.GetString("Scores_Bulk_Move_Success", "{0} partition(s) déplacée(s) dans '{1}'"), selectedScores.Count, targetCategory);
        await ShowModernAlertDialogAsync("Déplacement effectué", successMsg);
    }

    private readonly List<int> _bulkSelectedTagIds = new();
    private string _bulkTagSearchQuery = string.Empty;

    private void OnBulkTagsClicked(object sender, EventArgs e)
    {
        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;

        var loc = LocalizationService.Instance;
        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any())
        {
            DisplayAlertAsync(
                loc.GetString("Scores_Bulk_No_Selection_Title", "Aucune sélection"), 
                loc.GetString("Scores_Bulk_No_Selection_Msg", "Veuillez sélectionner au moins une partition."), 
                loc.GetString("Common_OK", "OK"));
            return;
        }

        BulkTagsHeaderLabel.Text = string.Format(LocalizationService.Instance.GetString("Scores_Bulk_Tags_Header", "🏷️ Étiqueter {0} partition(s)"), selectedScores.Count);
        _bulkSelectedTagIds.Clear();
        _bulkTagSearchQuery = string.Empty;
        BulkTagSearchEntry.Text = string.Empty;
        RenderBulkTags();
        BulkTagsOverlay.IsVisible = true;
    }

    private void OnBulkTagsCloseClicked(object sender, EventArgs e)
    {
        BulkTagsOverlay.IsVisible = false;
    }

    private void OnBulkTagSearchEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        _bulkTagSearchQuery = e.NewTextValue?.Trim() ?? string.Empty;
        RenderBulkTags();
    }

    private void RenderBulkTags()
    {
        BulkTagsContainer.Children.Clear();

        var filteredTags = _allTags.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_bulkTagSearchQuery))
        {
            filteredTags = filteredTags.Where(t => t.Name.Contains(_bulkTagSearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        var list = filteredTags.OrderBy(t => t.Name).ToList();

        foreach (var tag in list)
        {
            bool isSelected = _bulkSelectedTagIds.Contains(tag.Id);

            var border = new Border
            {
                BackgroundColor = isSelected ? Color.FromArgb(tag.ColorHex) : Color.FromArgb("#2A2A2A"),
                Stroke = isSelected ? Colors.White : Color.FromArgb(tag.ColorHex),
                StrokeThickness = isSelected ? 2 : 1,
                Padding = new Thickness(12, 6, 8, 6),
                Margin = new Thickness(0, 0, 8, 8),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Opacity = isSelected ? 1.0 : 0.75
            };

            var label = new Label
            {
                Text = isSelected ? $"✓ {tag.Name}  " : tag.Name + "  ",
                TextColor = isSelected ? Colors.White : Color.FromArgb("#E0E0E0"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.NoWrap
            };

            border.Content = label;

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                if (_bulkSelectedTagIds.Contains(tag.Id))
                {
                    _bulkSelectedTagIds.Remove(tag.Id);
                }
                else
                {
                    _bulkSelectedTagIds.Add(tag.Id);
                }
                RenderBulkTags();
            };
            border.GestureRecognizers.Add(tap);

            BulkTagsContainer.Children.Add(border);
        }

        if (!list.Any())
        {
            var noTags = new Label
            {
                Text = LocalizationService.Instance.GetString("Scores_Bulk_No_Tags_Available", "Aucune étiquette disponible. Créez-en dans le menu Outils."),
                TextColor = Color.FromArgb("#888888"),
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalOptions = LayoutOptions.Center
            };
            BulkTagsContainer.Children.Add(noTags);
        }
    }

    private async void OnApplyBulkTagsClicked(object sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        if (!_bulkSelectedTagIds.Any())
        {
            await DisplayAlertAsync(
                loc.GetString("Common_Warning", "Attention"), 
                loc.GetString("Scores_Bulk_Tags_None_Selected", "Veuillez sélectionner au moins une étiquette à appliquer."), 
                loc.GetString("Common_OK", "OK"));
            return;
        }

        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;

        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any()) return;

        foreach (var score in selectedScores)
        {
            foreach (var tagId in _bulkSelectedTagIds)
            {
                await _databaseService.AddTagToScoreAsync(score.Id, tagId);
            }
        }

        BulkTagsOverlay.IsVisible = false;
        IsMultiSelectActive = false; // Quitter le mode multi-sélection
        await LoadScoresAsync(SearchScoreBar.Text);
        await DisplayAlertAsync(
            loc.GetString("Common_Success", "Succès"), 
            string.Format(loc.GetString("Scores_Bulk_Tags_Success", "{0} étiquette(s) attribuée(s) avec succès à {1} partition(s)."), _bulkSelectedTagIds.Count, selectedScores.Count), 
            loc.GetString("Common_OK", "OK"));
    }

    private List<Models.Score>? _scoresToSendForExchange = null;
    private ExportOptions _currentExportOptions = new() { IncludeAnnotations = true, IncludeAudio = true };

    private async void OnBulkExportClicked(object sender, EventArgs e)
    {
        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;

        var loc = LocalizationService.Instance;
        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any())
        {
            await DisplayAlertAsync(
                loc.GetString("Scores_Bulk_No_Selection_Title", "Aucune sélection"), 
                loc.GetString("Scores_Bulk_No_Selection_Msg", "Veuillez sélectionner au moins une partition."), 
                loc.GetString("Common_OK", "OK"));
            return;
        }

        _scoresToSendForExchange = selectedScores;
        ScoreOptionsTitle.Text = string.Format(loc.GetString("Scores_Bulk_Export_Title", "Exporter {0} partition(s)"), selectedScores.Count);
        ScoreOptSendWifiActionButton.IsVisible = false;
        ScoreOptExportFileActionButton.IsVisible = true;
        ScoreOptionsOverlay.IsVisible = true;
    }

    private async void OnBulkExchangeClicked(object sender, EventArgs e)
    {
        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;

        var loc = LocalizationService.Instance;
        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any())
        {
            await DisplayAlertAsync(
                loc.GetString("Scores_Bulk_No_Selection_Title", "Aucune sélection"), 
                loc.GetString("Scores_Bulk_No_Selection_Msg", "Veuillez sélectionner au moins une partition."), 
                loc.GetString("Common_OK", "OK"));
            return;
        }

        _scoresToSendForExchange = selectedScores;
        ScoreOptionsTitle.Text = string.Format(loc.GetString("Scores_Bulk_Share_Title", "Partager {0} partition(s)"), selectedScores.Count);
        ScoreOptSendWifiActionButton.IsVisible = true;
        ScoreOptExportFileActionButton.IsVisible = false;
        ScoreOptionsOverlay.IsVisible = true;
    }

    private async Task StartExchangeFlowAsync(List<Models.Score> scoresToSend)
    {
        _scoresToSendForExchange = scoresToSend;
        WifiOverlayTitle.Text = "📡 Envoi Wi-Fi Direct";
        WifiOverlay.IsVisible = true;
        ShowWifiView("Sending");

        _discoveredDevices.Clear();
        WifiScanSpinner.IsRunning = true;
        WifiScanSpinner.IsVisible = true;

        bool initialized = await _wifiService.InitializeAsync();
        if (!initialized)
        {
            await DisplayAlertAsync("Wi-Fi indisponible", "Veuillez activer le Wi-Fi sur votre appareil et accorder les permissions.", "OK");
            WifiOverlay.IsVisible = false;
            return;
        }

        try
        {
            await _wifiService.StartScanningAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors du démarrage de la recherche : {ex.Message}", "OK");
            WifiOverlay.IsVisible = false;
        }
    }

    private async void OnWifiRescanClicked(object sender, EventArgs e)
    {
        _discoveredDevices.Clear();
        WifiScanSpinner.IsRunning = true;
        WifiScanSpinner.IsVisible = true;

        try
        {
            await _wifiService.StartScanningAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors de la recherche : {ex.Message}", "OK");
        }
    }

    private async void OnWifiCloseClicked(object sender, EventArgs e)
    {
        WifiOverlay.IsVisible = false;
        WifiScanSpinner.IsRunning = false;
        _scoresToSendForExchange = null;

        await _wifiService.StopScanningAsync();
        await _wifiService.StopListeningAsync();
        await _wifiService.StopGroupBroadcastAsync();
    }

    private async void OnWifiStartListeningClicked(object sender, EventArgs e)
    {
        WifiOverlayTitle.Text = "Mode Réception Wi-Fi";
        ShowWifiView("Listening");
        WifiListeningStatusLabel.Text = "En attente d'un émetteur...";

        bool initialized = await _wifiService.InitializeAsync();
        if (!initialized)
        {
            await DisplayAlertAsync("Wi-Fi indisponible", "Veuillez vérifier que le Wi-Fi est activé et que les permissions sont accordées.", "OK");
            ShowWifiView("Choice");
            return;
        }

        try
        {
            await _wifiService.StartListeningAsync(
                confirmCallback: OnWifiReceiveRequestAsync,
                onReceiveComplete: OnWifiDataReceivedAsync
            );
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors de l'écoute : {ex.Message}", "OK");
            ShowWifiView("Choice");
        }
    }

    private async void OnWifiStopListeningClicked(object sender, EventArgs e)
    {
        await _wifiService.StopListeningAsync();
        ShowWifiView("Choice");
    }

    private void OnWifiDeviceDiscovered(object? sender, WifiDeviceInfo device)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_discoveredDevices.Any(d => d.Address.Equals(device.Address, StringComparison.OrdinalIgnoreCase)))
            {
                _discoveredDevices.Add(device);
            }
        });
    }

    private void OnWifiScanFinished(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            WifiScanSpinner.IsRunning = false;
            WifiScanSpinner.IsVisible = false;
        });
    }

    private void OnWifiTransferProgressChanged(object? sender, WifiTransferProgressEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ShowWifiView("Progress");
            WifiTransferStatusLabel.Text = $"{e.Status} ({Math.Round(e.Progress * 100)}%)";
            WifiTransferProgressBar.Progress = e.Progress;
            if (e.SpeedMBps > 0)
            {
                WifiTransferSpeedLabel.Text = $"{e.SpeedMBps:F1} Mo/s";
            }
        });
    }

    private async void OnSendToDeviceButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is WifiDeviceInfo device)
        {
            await SendScoresToDeviceAsync(device);
        }
    }

    private async void OnWifiDeviceSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is not WifiDeviceInfo selectedDevice) return;
        WifiDevicesListView.SelectedItem = null;
        await SendScoresToDeviceAsync(selectedDevice);
    }

    private async Task SendScoresToDeviceAsync(WifiDeviceInfo targetDevice)
    {
        await _wifiService.StopScanningAsync();

        List<Models.Score> selectedScores = _scoresToSendForExchange ?? new List<Models.Score>();
        if (!selectedScores.Any() && ScoresCollectionView.ItemsSource is IEnumerable<Models.Score> scores)
        {
            selectedScores = scores.Where(s => s.IsSelected).ToList();
        }
        if (!selectedScores.Any()) return;

        try
        {
            ShowWifiView("Progress");
            WifiTransferStatusLabel.Text = "Préparation du paquet...";
            WifiTransferProgressBar.Progress = 0.05;

            var filesToSend = await _exportImportService.CreateWifiPayloadsForScoresAsync(selectedScores, _currentExportOptions);

            if (!filesToSend.Any())
            {
                await DisplayAlertAsync("Erreur", "Aucun fichier valide n'a pu être trouvé pour l'envoi.", "OK");
                WifiOverlay.IsVisible = false;
                return;
            }

            WifiTransferStatusLabel.Text = $"Connexion à {targetDevice.Name}...";
            WifiTransferProgressBar.Progress = 0.15;

            string senderName = DeviceInfo.Name ?? "Tablette Musique";
            bool success = await _wifiService.SendDataAsync(targetDevice, filesToSend, senderName);

            if (success)
            {
                await DisplayAlertAsync("Succès", $"{selectedScores.Count} partition(s) envoyée(s) avec succès via Wi-Fi Direct !", "OK");
                IsMultiSelectActive = false;
            }
            else
            {
                await DisplayAlertAsync("Échec", "Le transfert a échoué ou a été refusé par le destinataire.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors du transfert : {ex.Message}", "OK");
        }
        finally
        {
            WifiOverlay.IsVisible = false;
            _scoresToSendForExchange = null;
        }
    }

    private async void OnToggleGroupShareClicked(object sender, EventArgs e)
    {
        List<Models.Score> selectedScores = _scoresToSendForExchange ?? new List<Models.Score>();
        if (!selectedScores.Any() && ScoresCollectionView.ItemsSource is IEnumerable<Models.Score> scores)
        {
            selectedScores = scores.Where(s => s.IsSelected).ToList();
        }
        if (!selectedScores.Any()) return;

        var filesToSend = await _exportImportService.CreateWifiPayloadsForScoresAsync(selectedScores, _currentExportOptions);

        string title = $"{selectedScores.Count} partition(s)";
        var shareInfo = await _wifiService.StartGroupBroadcastAsync(filesToSend, title, (clientsCount) =>
        {
            QrGroupInfoLabel.Text = $"🟢 {clientsCount} musicien(s) ont téléchargé la sélection !";
        });

        if (shareInfo != null)
        {
            QrCodeImageView.Source = QrCodeGenerator.GenerateQrCodeImageSource(shareInfo.QrCodeContent, 220);
            QrGroupUrlLabel.Text = shareInfo.QrCodeContent;
            QrGroupInfoLabel.Text = "🟢 En attente de musiciens (0 connecté)...";
            ShowWifiView("GroupQr");
        }
        else
        {
            await DisplayAlertAsync("Erreur", "Impossible de démarrer le partage de groupe local.", "OK");
        }
    }

    private async void OnStopGroupShareClicked(object sender, EventArgs e)
    {
        await _wifiService.StopGroupBroadcastAsync();
        ShowWifiView("Sending");
    }

    private async Task<bool> OnWifiReceiveRequestAsync(string senderDeviceName, int scoreCount)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            bool accept = await DisplayAlertAsync(
                "📥 Réception Wi-Fi Direct", 
                $"L'appareil '{senderDeviceName}' souhaite vous envoyer {scoreCount} partition(s) / élément(s).\n\nAcceptez-vous la réception ?", 
                "Oui, accepter", 
                "Non, refuser");

            if (accept)
            {
                ShowWifiView("Progress");
                WifiTransferStatusLabel.Text = "Connexion acceptée. Réception haute vitesse...";
                WifiTransferProgressBar.Progress = 0;
            }

            return accept;
        });
    }

    private async Task OnWifiDataReceivedAsync(List<WifiFilePayload> receivedFiles, string senderName)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                WifiTransferStatusLabel.Text = "Intégration des données...";
                WifiTransferProgressBar.Progress = 0.95;
            });

            bool success = await _exportImportService.ImportPackageFromWifiPayloadsAsync(receivedFiles);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                WifiOverlay.IsVisible = false;
                await LoadScoresAsync(SearchScoreBar.Text);
                if (success)
                {
                    await DisplayAlertAsync("Réception réussie", $"Les partitions ont été reçues et intégrées avec succès depuis {senderName} !", "OK");
                }
                else
                {
                    await DisplayAlertAsync("Avertissement", "Les données ont été reçues mais une erreur s'est produite lors de l'intégration.", "OK");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing received files: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                WifiOverlay.IsVisible = false;
                await DisplayAlertAsync("Échec de l'intégration", $"Une erreur est survenue lors de l'intégration : {ex.Message}", "OK");
            });
        }
    }

    private void ShowWifiView(string viewName)
    {
        WifiChoiceView.IsVisible = (viewName == "Choice");
        WifiListeningView.IsVisible = (viewName == "Listening");
        WifiSendingView.IsVisible = (viewName == "Sending");
        WifiGroupQrView.IsVisible = (viewName == "GroupQr");
        WifiTransferProgressView.IsVisible = (viewName == "Progress");
    }

    private void OnTagsFilterButtonClicked(object sender, EventArgs e)
    {
        TagSearchEntry.Text = string.Empty;
        _tagSearchQuery = string.Empty;
        UpdateTagSortIndicator();
        TagsFilterOverlay.IsVisible = true;
        RenderOverlayTags();
    }

    private void UpdateTagSortIndicator()
    {
        var loc = LocalizationService.Instance;
        TagSortIndicatorLabel.Text = _tagSortType switch
        {
            "NameDesc" => loc.GetString("Scores_Sort_Tag_ZA", "Tri : Z-A"),
            "SelectedFirst" => loc.GetString("Scores_Sort_Tag_SelectedFirst", "Tri : Sélectionnés d'abord"),
            "Color" => loc.GetString("Scores_Sort_Tag_Color", "Tri : Couleur"),
            _ => loc.GetString("Scores_Sort_Tag_AZ", "Tri : A-Z")
        };
    }

    private void OnTagsFilterCloseClicked(object sender, EventArgs e)
    {
        TagsFilterOverlay.IsVisible = false;
    }

    private void OnTagSearchEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        _tagSearchQuery = e.NewTextValue?.Trim() ?? string.Empty;
        RenderOverlayTags();
    }

    private void OnTagSortClicked(object sender, EventArgs e)
    {
        if (_tagSortType == "NameAsc")
        {
            _tagSortType = "NameDesc";
        }
        else if (_tagSortType == "NameDesc")
        {
            _tagSortType = "SelectedFirst";
        }
        else if (_tagSortType == "SelectedFirst")
        {
            _tagSortType = "Color";
        }
        else
        {
            _tagSortType = "NameAsc";
        }

        UpdateTagSortIndicator();
        RenderOverlayTags();
    }

    private void RenderOverlayTags()
    {
        OverlayTagsContainer.Children.Clear();

        // 1. Filtrer
        var filteredTags = _allTags.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_tagSearchQuery))
        {
            filteredTags = filteredTags.Where(t => t.Name.Contains(_tagSearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        // 2. Trier
        if (_tagSortType == "NameAsc")
        {
            filteredTags = filteredTags.OrderBy(t => t.Name);
        }
        else if (_tagSortType == "NameDesc")
        {
            filteredTags = filteredTags.OrderByDescending(t => t.Name);
        }
        else if (_tagSortType == "SelectedFirst")
        {
            filteredTags = filteredTags.OrderByDescending(t => _selectedTagIds.Contains(t.Id)).ThenBy(t => t.Name);
        }
        else if (_tagSortType == "Color")
        {
            filteredTags = filteredTags.OrderBy(t => t.ColorHex).ThenBy(t => t.Name);
        }

        var tagsList = filteredTags.ToList();

        foreach (var tag in tagsList)
        {
            bool isSelected = _selectedTagIds.Contains(tag.Id);

            var border = new Border
            {
                BackgroundColor = isSelected ? Color.FromArgb(tag.ColorHex) : Color.FromArgb("#2A2A2A"),
                Stroke = isSelected ? Colors.White : Color.FromArgb(tag.ColorHex),
                StrokeThickness = isSelected ? 2 : 1,
                Padding = new Thickness(12, 6, 8, 6),
                Margin = new Thickness(0, 0, 8, 8),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Opacity = isSelected ? 1.0 : 0.7
            };

            var label = new Label
            {
                Text = isSelected ? $"✓ {tag.Name}  " : tag.Name + "  ",
                TextColor = isSelected ? Colors.White : Color.FromArgb("#E0E0E0"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.NoWrap
            };

            border.Content = label;

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                if (_selectedTagIds.Contains(tag.Id))
                {
                    _selectedTagIds.Remove(tag.Id);
                }
                else
                {
                    _selectedTagIds.Add(tag.Id);
                }
                RenderOverlayTags();
            };
            border.GestureRecognizers.Add(tap);

            OverlayTagsContainer.Children.Add(border);
        }

        if (!tagsList.Any())
        {
            var noTagsLabel = new Label
            {
                Text = LocalizationService.Instance.GetString("Scores_No_Tags_Found", "Aucune étiquette trouvée."),
                TextColor = Color.FromArgb("#888888"),
                FontSize = 13,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalOptions = LayoutOptions.Center
            };
            OverlayTagsContainer.Children.Add(noTagsLabel);
        }
    }

    private void OnClearAllTagsClicked(object sender, EventArgs e)
    {
        _selectedTagIds.Clear();
        RenderOverlayTags();
    }

    private async void OnApplyTagsFilterClicked(object sender, EventArgs e)
    {
        TagsFilterOverlay.IsVisible = false;
        UpdateActiveTagsChips();
        await LoadScoresAsync(SearchScoreBar.Text);
    }

    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(250, token);
            await LoadScoresAsync(e.NewTextValue);
        }
        catch (TaskCanceledException)
        {
            // Frappe suivante ignorée
        }
    }

    private void OnConversionStateChanged(bool isConverting, string? statusMessage)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConversionLoadingOverlay.IsVisible = isConverting;
            if (!string.IsNullOrEmpty(statusMessage))
            {
                ConversionStatusLabel.Text = statusMessage;
            }
        });
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        var scores = await _importService.ImportScoresAsync(_selectedCategory);
        if (scores != null && scores.Any())
        {
            if (!string.IsNullOrEmpty(SearchScoreBar?.Text))
            {
                SearchScoreBar.Text = string.Empty;
            }
            await LoadScoresAsync(string.Empty);

            await ShowModernImportSuccessAsync(scores);
        }
    }

    private void OnScoreLongPressed(Models.Score? score)
    {
        if (score == null) return;

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        catch { }

        if (!IsMultiSelectActive)
        {
            IsMultiSelectActive = true;
        }

        score.IsSelected = true;
    }

    public async Task HandleScoreTappedAsync(Models.Score selectedScore)
    {
        if (IsMultiSelectActive)
        {
            selectedScore.IsSelected = !selectedScore.IsSelected;
            return;
        }

        if (selectedScore.IsFileMissing)
        {
            await this.DisplayAlertAsync("Fichier manquant", "Le fichier de cette partition est introuvable. Veuillez vérifier votre répertoire de partitions dans les paramètres.", "OK");
            return;
        }

        // Naviguer vers le ViewerPage
        await Navigation.PushAsync(new ViewerPage(selectedScore));
    }

    private async void OnScoreSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Models.Score selectedScore)
        {
            // Toujours désélectionner l'élément
            ScoresCollectionView.SelectedItem = null;
            await HandleScoreTappedAsync(selectedScore);
        }
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        string optDateDesc = loc.GetString("Sort_DateDesc", "Date d'ajout (Récent)");
        string optDateAsc = loc.GetString("Sort_DateAsc", "Date d'ajout (Ancien)");
        string optTitleAsc = loc.GetString("Sort_TitleAsc", "Titre (A-Z)");
        string optTitleDesc = loc.GetString("Sort_TitleDesc", "Titre (Z-A)");
        string optModDesc = loc.GetString("Sort_ModifiedDesc", "Date de modification");
        string optRatingDesc = loc.GetString("Sort_RatingDesc", "Évaluation (Note)");
        string optCompAsc = loc.GetString("Sort_ComposerAsc", "Compositeur (A-Z)");
        string optNoTags = loc.GetString("Sort_NoTagsFirst", "Sans étiquette d'abord");
        string cancel = loc.GetString("Common_Cancel", "Annuler");

        string action = await DisplayActionSheetAsync(loc.GetString("Sort_Dialog_Title", "Trier par"), cancel, null, 
            optDateDesc, optDateAsc, optTitleAsc, optTitleDesc, optModDesc, optRatingDesc, optCompAsc, optNoTags);

        if (action == optDateDesc) { _currentSort = "DateDesc"; _hasUserCustomSort = true; }
        else if (action == optDateAsc) { _currentSort = "DateAsc"; _hasUserCustomSort = true; }
        else if (action == optTitleAsc) { _currentSort = "TitleAsc"; _hasUserCustomSort = true; }
        else if (action == optTitleDesc) { _currentSort = "TitleDesc"; _hasUserCustomSort = true; }
        else if (action == optModDesc) { _currentSort = "ModifiedDesc"; _hasUserCustomSort = true; }
        else if (action == optRatingDesc) { _currentSort = "RatingDesc"; _hasUserCustomSort = true; }
        else if (action == optCompAsc) { _currentSort = "ComposerAsc"; _hasUserCustomSort = true; }
        else if (action == optNoTags) { _currentSort = "NoTagsFirst"; _hasUserCustomSort = true; }

        if (!string.IsNullOrEmpty(action) && action != cancel)
            await LoadScoresAsync(SearchScoreBar.Text);
    }

    private void OnScoreOptionsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Models.Score score)
        {
            _selectedScoreForMenu = score;
            ScoreMenuTitleLabel.Text = score.Title;
            string subtitlePref = Preferences.Default.Get("ScoreSubtitleDisplay", "DateAdded");
            ScoreMenuSubtitleLabel.Text = score.GetSubtitle(subtitlePref);
            ScoreMenuModifyAssemblyButton.IsVisible = (score.Type == ScoreType.PDF && !score.IsFileMissing);
            ScoreMenuOverlay.IsVisible = true;
        }
    }

    private async void OnMenuOpenClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            if (_selectedScoreForMenu.IsFileMissing)
            {
                await DisplayAlertAsync("Fichier manquant", "Le fichier de cette partition est introuvable. Veuillez vérifier votre répertoire de partitions dans les paramètres.", "OK");
                return;
            }
            await Navigation.PushAsync(new ViewerPage(_selectedScoreForMenu));
        }
    }

    private async void OnMenuModifyAssemblyClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            var score = _selectedScoreForMenu;
            _selectedScoreForMenu = null;

            await ToolsPage.NavigateAndOpenScoreAsync(score);
        }
    }

    private async void OnMenuEditClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            await Navigation.PushAsync(new ScoreEditPage(_selectedScoreForMenu, _databaseService));
        }
    }

    private async void OnMenuAddToSetlistClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu == null)
            return;

        var score = _selectedScoreForMenu;
        _selectedScoreForMenu = null;

        var setlists = await _databaseService.GetSetlistsAsync();
        string ok = LocalizationService.Instance.GetString("Common_OK", "OK");
        string cancel = LocalizationService.Instance.GetString("Common_Cancel", "Annuler");
        string yes = LocalizationService.Instance.GetString("Common_Yes", "Oui");
        string no = LocalizationService.Instance.GetString("Common_No", "Non");

        if (setlists == null || setlists.Count == 0)
        {
            string emptyMsg = LocalizationService.Instance.GetString("Alert_No_Setlist", "Aucune setlist n'a été créée pour le moment.");
            await DisplayAlertAsync("Setlists", emptyMsg, ok);
            return;
        }

        setlists = setlists.OrderBy(s => s.Name).ToList();

        var displayMap = new Dictionary<string, Models.Setlist>();
        foreach (var s in setlists)
        {
            string displayName = string.IsNullOrWhiteSpace(s.Name) ? $"Setlist #{s.Id}" : s.Name;
            if (displayName.Equals(cancel, StringComparison.OrdinalIgnoreCase))
            {
                displayName = $"{displayName} (Setlist)";
            }
            string candidate = displayName;
            int counter = 2;
            while (displayMap.ContainsKey(candidate))
            {
                candidate = $"{displayName} ({counter++})";
            }
            displayMap[candidate] = s;
        }

        string chooseTitle = LocalizationService.Instance.GetString("Alert_Choose_Setlist", "Choisir une setlist");
        string action = await DisplayActionSheetAsync(chooseTitle, cancel, null, displayMap.Keys.ToArray());
        if (string.IsNullOrEmpty(action) || action == cancel || !displayMap.TryGetValue(action, out var targetSetlist))
        {
            return;
        }

        if (targetSetlist.IsLocked)
        {
            string lockedTitle = LocalizationService.Instance.GetString("Alert_Setlist_Locked_Title", "Setlist verrouillée");
            string lockedMsg = string.Format(LocalizationService.Instance.GetString("Alert_Setlist_Locked_Msg", "La setlist '{0}' est verrouillée. Voulez-vous tout de même y ajouter la partition '{1}' en première place ?"), targetSetlist.Name, score.Title);
            bool proceed = await DisplayAlertAsync(
                lockedTitle,
                lockedMsg,
                yes,
                no);
            if (!proceed)
                return;
        }

        await _databaseService.AddScoreToSetlistAtBeginningAsync(targetSetlist.Id, score.Id);

        string successTitle = LocalizationService.Instance.GetString("Common_Success", "Succès");
        string successMsg = string.Format(LocalizationService.Instance.GetString("Alert_Score_Added_To_Setlist", "La partition '{0}' a été ajoutée en première place dans la setlist '{1}'."), score.Title, targetSetlist.Name);
        await DisplayAlertAsync(successTitle, successMsg, ok);
    }

    #region View Mode & Categories (Vue Dossiers / Catégories)

    private void UpdateViewModeUI()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (ViewModeToggleButton != null)
            {
                ViewModeToggleButton.Text = _isFoldersView ? "☰" : "📁";
                ToolTipProperties.SetText(ViewModeToggleButton, LocalizationService.Instance.GetString("Scores_View_Mode_Tooltip", "Basculer entre vue liste et vue dossiers"));
            }

            if (_isFoldersView)
            {
                if (_selectedCategory == null)
                {
                    if (FoldersOverviewView != null) FoldersOverviewView.IsVisible = true;
                    if (CategoryBreadcrumbBar != null) CategoryBreadcrumbBar.IsVisible = false;
                    if (ScoresCollectionView != null) ScoresCollectionView.IsVisible = false;
                    if (AlphabeticalIndexBorder != null) AlphabeticalIndexBorder.IsVisible = false;
                }
                else
                {
                    if (FoldersOverviewView != null) FoldersOverviewView.IsVisible = false;
                    if (CategoryBreadcrumbBar != null) CategoryBreadcrumbBar.IsVisible = true;
                    if (ScoresCollectionView != null) ScoresCollectionView.IsVisible = true;
                    UpdateAlphabeticalIndexVisibility();
                }
            }
            else
            {
                if (FoldersOverviewView != null) FoldersOverviewView.IsVisible = false;
                if (CategoryBreadcrumbBar != null) CategoryBreadcrumbBar.IsVisible = false;
                if (ScoresCollectionView != null) ScoresCollectionView.IsVisible = true;
                UpdateAlphabeticalIndexVisibility();
            }
        });
    }

    private void OnToggleViewModeClicked(object? sender, EventArgs e)
    {
        _isFoldersView = !_isFoldersView;
        _selectedCategory = null;
        Preferences.Default.Set("ScoresViewMode", _isFoldersView ? "Folders" : "List");
        UpdateViewModeUI();
        ApplyCurrentViewModeData(_lastLoadedScores);
    }

    private void ApplyCurrentViewModeData(List<Models.Score> scores)
    {
        if (_isFoldersView && _selectedCategory == null)
        {
            if (FoldersOverviewView != null) FoldersOverviewView.IsVisible = true;
            if (CategoryBreadcrumbBar != null) CategoryBreadcrumbBar.IsVisible = false;
            if (ScoresCollectionView != null) ScoresCollectionView.IsVisible = false;
            if (AlphabeticalIndexBorder != null) AlphabeticalIndexBorder.IsVisible = false;

            PopulateFoldersOverview(scores);
        }
        else
        {
            if (FoldersOverviewView != null) FoldersOverviewView.IsVisible = false;

            List<Models.Score> displayScores;
            if (_isFoldersView && _selectedCategory != null)
            {
                if (CategoryBreadcrumbBar != null)
                {
                    CategoryBreadcrumbBar.IsVisible = true;
                    if (CurrentCategoryTitleLabel != null)
                    {
                        var inCatScores = scores.Where(s => string.Equals(s.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();
                        int cnt = inCatScores.Count;
                        string pWord = cnt > 1 
                            ? LocalizationService.Instance.GetString("Scores_Count_Plural", "partitions") 
                            : LocalizationService.Instance.GetString("Scores_Count_Single", "partition");
                        CurrentCategoryTitleLabel.Text = $"📁 {_selectedCategory} ({cnt} {pWord})";
                    }
                }
                displayScores = scores.Where(s => string.Equals(s.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                if (CategoryBreadcrumbBar != null) CategoryBreadcrumbBar.IsVisible = false;
                displayScores = scores;
            }

            if (ScoresCollectionView != null)
            {
                ScoresCollectionView.IsVisible = true;
                ScoresCollectionView.ItemsSource = displayScores;
            }
            RecalculateSelectedCount();
            UpdateAlphabeticalIndexMap(displayScores);
            UpdateAlphabeticalIndexVisibility();
        }
    }

    private void PopulateFoldersOverview(List<Models.Score> scores)
    {
        var grouped = scores.GroupBy(s => string.IsNullOrWhiteSpace(s.Category) ? "Général" : s.Category.Trim(), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        _ = Task.Run(async () =>
        {
            try
            {
                var allCategories = await _databaseService.GetCategoriesAsync();
                var groups = new List<ScoreCategoryGroup>();

                foreach (var cat in allCategories)
                {
                    grouped.TryGetValue(cat, out int count);
                    groups.Add(new ScoreCategoryGroup
                    {
                        Name = cat,
                        Count = count
                    });
                }

                int totalFolders = groups.Count;
                int totalScores = scores.Count;
                string fWord = totalFolders > 1 ? "dossiers" : "dossier";
                string sWord = totalScores > 1 ? "partitions" : "partition";

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (FoldersSummaryLabel != null)
                    {
                        FoldersSummaryLabel.Text = $"📁 {totalFolders} {fWord} • {totalScores} {sWord}";
                    }
                    if (FoldersCollectionView != null)
                    {
                        FoldersCollectionView.ItemsSource = groups;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScoresPage] PopulateFoldersOverview error: {ex.Message}");
            }
        });
    }

    private void OnCategoryCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is ScoreCategoryGroup group)
        {
            _selectedCategory = group.Name;
            UpdateViewModeUI();
            ApplyCurrentViewModeData(_lastLoadedScores);
        }
    }

    private void OnBackToFoldersClicked(object? sender, EventArgs e)
    {
        _selectedCategory = null;
        UpdateViewModeUI();
        ApplyCurrentViewModeData(_lastLoadedScores);
    }

    private async void OnCurrentCategoryOptionsClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedCategory)) return;
        var dummyGroup = new ScoreCategoryGroup { Name = _selectedCategory };
        await HandleCategoryOptionsAsync(dummyGroup);
    }

    private async void OnCategoryOptionsClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ScoreCategoryGroup group)
        {
            await HandleCategoryOptionsAsync(group);
        }
    }

    private async Task HandleCategoryOptionsAsync(ScoreCategoryGroup group)
    {
            var loc = LocalizationService.Instance;
            string cancel = loc.GetString("Common_Cancel", "Annuler");
            string rename = loc.GetString("Score_Category_Rename_Title", "Renommer le dossier");
            string delete = loc.GetString("Score_Category_Delete_Title", "Supprimer le dossier");

            string? action;
            if (string.Equals(group.Name, "Général", StringComparison.OrdinalIgnoreCase))
            {
                action = await DisplayActionSheetAsync(group.DisplayName, cancel, null, rename);
            }
            else
            {
                action = await DisplayActionSheetAsync(group.DisplayName, cancel, delete, rename);
            }

            if (action == rename)
            {
                string promptTitle = loc.GetString("Score_Category_Rename_Title", "Renommer le dossier");
                string promptMsg = loc.GetString("Score_Category_Rename_Prompt", "Entrez le nouveau nom du dossier :");
                string ok = loc.GetString("Common_Save", "Enregistrer");
                string? newName = await ShowModernPromptDialogAsync(promptTitle, promptMsg, group.Name, ok);
                if (!string.IsNullOrWhiteSpace(newName) && !string.Equals(newName.Trim(), group.Name, StringComparison.OrdinalIgnoreCase))
                {
                    string cleanNew = newName.Trim();
                    await _databaseService.RenameCategoryAsync(group.Name, cleanNew);
                    if (string.Equals(_selectedCategory, group.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        _selectedCategory = cleanNew;
                    }
                    await LoadScoresAsync(SearchScoreBar?.Text ?? string.Empty);
                }
            }
            else if (action == delete)
            {
                string confirmMsg = string.Format(
                    loc.GetString("Score_Category_Delete_Confirm", "Voulez-vous supprimer le dossier '{0}' ? Ses {1} partition(s) seront déplacées dans 'Général'."),
                    group.Name, group.Count);

                bool confirm = await DisplayAlertAsync(
                    loc.GetString("Score_Category_Delete_Title", "Supprimer le dossier"),
                    confirmMsg,
                    loc.GetString("Common_Delete", "Supprimer"),
                    cancel);

                if (confirm)
                {
                    await _databaseService.DeleteCategoryAsync(group.Name, "Général");
                    if (string.Equals(_selectedCategory, group.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        _selectedCategory = null;
                    }
                    await LoadScoresAsync(SearchScoreBar?.Text ?? string.Empty);
                }
            }
        }

    private async void OnAddNewCategoryFromScoresPageClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        string title = loc.GetString("Score_Category_New_Title", "Nouveau dossier");
        string prompt = loc.GetString("Score_Category_New_Prompt", "Entrez le nom du nouveau dossier / catégorie :");
        string ok = loc.GetString("Common_OK", "OK");

        string? result = await ShowModernPromptDialogAsync(title, prompt, string.Empty, ok);
        if (!string.IsNullOrWhiteSpace(result))
        {
            string clean = result.Trim();
            await _databaseService.CreateCategoryAsync(clean);
            await LoadScoresAsync(SearchScoreBar?.Text ?? string.Empty);
        }
    }

    private async void OnScoreCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Models.Score score)
        {
            if (IsMultiSelectActive)
            {
                score.IsSelected = !score.IsSelected;
                RecalculateSelectedCount();
                return;
            }

            if (score.IsFileMissing)
            {
                await this.DisplayAlertAsync("Fichier manquant", "Le fichier de cette partition est introuvable. Veuillez vérifier votre répertoire de partitions dans les paramètres.", "OK");
                return;
            }

            await Navigation.PushAsync(new ViewerPage(score));
        }
    }

    private async void OnMenuChangeCategoryClicked(object sender, EventArgs e)
    {
        if (_selectedScoreForMenu == null) return;
        var score = _selectedScoreForMenu;
        ScoreMenuOverlay.IsVisible = false;

        var loc = LocalizationService.Instance;
        string title = loc.GetString("Score_Category_Picker_Title", "Dossier de destination");
        string subtitle = $"{score.Title} ({score.Category})";

        string? targetCategory = await ShowCategoryPickerModalAsync(title, subtitle, score.Category);
        if (string.IsNullOrEmpty(targetCategory) || string.Equals(targetCategory, score.Category, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _databaseService.MoveScoreToCategoryAsync(score, targetCategory);
        await LoadScoresAsync(SearchScoreBar?.Text ?? string.Empty);
    }

    #endregion

    private void OnMenuExchangeClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            _scoresToSendForExchange = new List<Models.Score> { _selectedScoreForMenu };
            ScoreOptionsTitle.Text = $"Partager '{_selectedScoreForMenu.Title}'";
            ScoreOptSendWifiActionButton.IsVisible = true;
            ScoreOptExportFileActionButton.IsVisible = false;
            ScoreOptionsOverlay.IsVisible = true;
        }
    }

    private void OnMenuExportClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            _scoresToSendForExchange = new List<Models.Score> { _selectedScoreForMenu };
            ScoreOptionsTitle.Text = $"Exporter '{_selectedScoreForMenu.Title}'";
            ScoreOptSendWifiActionButton.IsVisible = false;
            ScoreOptExportFileActionButton.IsVisible = true;
            ScoreOptionsOverlay.IsVisible = true;
        }
    }

    private async void OnScoreOptionsSendWifiClicked(object sender, EventArgs e)
    {
        ScoreOptionsOverlay.IsVisible = false;
        _currentExportOptions = new ExportOptions
        {
            IncludeAnnotations = ScoreOptAnnotationsCheckBox.IsChecked,
            IncludeAudio = ScoreOptAudioCheckBox.IsChecked
        };

        if (_scoresToSendForExchange != null && _scoresToSendForExchange.Any())
        {
            await StartExchangeFlowAsync(_scoresToSendForExchange);
        }
    }

    private async void OnScoreOptionsExportFileClicked(object sender, EventArgs e)
    {
        ScoreOptionsOverlay.IsVisible = false;
        _currentExportOptions = new ExportOptions
        {
            IncludeAnnotations = ScoreOptAnnotationsCheckBox.IsChecked,
            IncludeAudio = ScoreOptAudioCheckBox.IsChecked
        };

        if (_scoresToSendForExchange != null && _scoresToSendForExchange.Any())
        {
            var scores = _scoresToSendForExchange;
            _scoresToSendForExchange = null;
            _selectedScoreForMenu = null;

            try
            {
                string path = await _exportImportService.ExportScoresToFileAsync(scores, _currentExportOptions);
                string message = scores.Count == 1
                    ? $"La partition '{scores[0].Title}' a été exportée avec succès dans :\n\n{path}"
                    : $"{scores.Count} partitions ont été exportées avec succès dans :\n\n{path}";

                await DisplayAlertAsync("Export réussi", message, "OK");
                IsMultiSelectActive = false;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erreur d'export", $"Impossible d'exporter la sélection : {ex.Message}", "OK");
            }
        }
    }

    private void OnScoreOptionsCancelClicked(object sender, EventArgs e)
    {
        ScoreOptionsOverlay.IsVisible = false;
        _scoresToSendForExchange = null;
    }

    private async void OnMenuRenameClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            string promptTitle = LocalizationService.Instance.GetString("Prompt_Rename_Title", "Renommer");
            string promptMsg = LocalizationService.Instance.GetString("Prompt_Rename_Score_Msg", "Nouveau titre :");
            string ok = LocalizationService.Instance.GetString("Common_OK", "OK");
            string cancel = LocalizationService.Instance.GetString("Common_Cancel", "Annuler");

            string newTitle = await DisplayPromptAsync(promptTitle, promptMsg, ok, cancel, initialValue: _selectedScoreForMenu.Title);
            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                string trimmed = newTitle.Trim();
                if (trimmed != _selectedScoreForMenu.Title)
                {
                    try
                    {
                        _selectedScoreForMenu.Title = trimmed;
                        await _databaseService.SaveScoreAsync(_selectedScoreForMenu);
                        
                        // Si une recherche est active et ne correspond plus au nouveau titre/compositeur, vider la recherche pour que la partition reste visible
                        string currentSearch = SearchScoreBar.Text?.Trim() ?? string.Empty;
                        if (!string.IsNullOrEmpty(currentSearch) && 
                            !_selectedScoreForMenu.Title.Contains(currentSearch, StringComparison.OrdinalIgnoreCase) && 
                            !_selectedScoreForMenu.Composer.Contains(currentSearch, StringComparison.OrdinalIgnoreCase))
                        {
                            SearchScoreBar.Text = string.Empty;
                        }
                        await LoadScoresAsync(SearchScoreBar.Text ?? string.Empty);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ScoresPage] Error renaming score: {ex.Message}");
                    }
                }
            }
        }
    }

    private async void OnMenuDeleteClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            string title = LocalizationService.Instance.GetString("Alert_Delete_Title", "Supprimer");
            string msgPattern = LocalizationService.Instance.GetString("Alert_Delete_Score_Msg", "Voulez-vous vraiment supprimer '{0}' ?");
            string msg = string.Format(msgPattern, _selectedScoreForMenu.Title);
            string yes = LocalizationService.Instance.GetString("Common_Yes", "Oui");
            string no = LocalizationService.Instance.GetString("Common_No", "Non");

            bool answer = await DisplayAlertAsync(title, msg, yes, no);
            if (answer)
            {
                await _databaseService.DeleteScoreAsync(_selectedScoreForMenu);
                await LoadScoresAsync(SearchScoreBar.Text);
            }
        }
    }

    private void OnMenuCancelClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        _selectedScoreForMenu = null;
    }

    private void BuildAlphabeticalIndex()
    {
        if (AlphabeticalIndexLayout == null) return;
        AlphabeticalIndexLayout.Children.Clear();

        string[] letters = ["#", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"];
        foreach (var letter in letters)
        {
            var cell = new Grid
            {
                HeightRequest = 14.5,
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = Colors.Transparent,
                InputTransparent = false
            };

            var lbl = new Label
            {
                Text = letter,
                FontSize = 9.0,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#BBBBBB"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                InputTransparent = true
            };

            cell.Children.Add(lbl);

            var tap = new TapGestureRecognizer();
            string capturedLetter = letter;
            var capturedCell = cell;
            var capturedLbl = lbl;

            tap.Tapped += (s, e) =>
            {
                try
                {
                    capturedCell.BackgroundColor = Color.FromArgb("#007ACC");
                    capturedLbl.TextColor = Colors.White;
                }
                catch { }

                OnAlphabeticalIndexTapped(capturedLetter);

                _ = Task.Delay(180).ContinueWith(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            capturedCell.BackgroundColor = Colors.Transparent;
                            capturedLbl.TextColor = Color.FromArgb("#BBBBBB");
                        }
                        catch { }
                    });
                });
            };

            cell.GestureRecognizers.Add(tap);
            AlphabeticalIndexLayout.Children.Add(cell);
        }
    }

    private void UpdateAlphabeticalIndexVisibility()
    {
        _cachedShowAlphabeticalIndex = Preferences.Default.Get("ShowAlphabeticalIndex", true);
        _cachedShowScrollTop = Preferences.Default.Get("ShowScrollTop", true);

        bool isAlphaSort = _currentSort is "TitleAsc" or "TitleDesc" or "ComposerAsc" or "ComposerDesc";
        bool hasScores = ScoresCollectionView?.ItemsSource is IEnumerable<Models.Score> list && list.Any();

        if (AlphabeticalIndexBorder != null)
        {
            AlphabeticalIndexBorder.IsVisible = _cachedShowAlphabeticalIndex && isAlphaSort && hasScores;
        }
    }

    private void UpdateAlphabeticalIndexMap(IList<Models.Score> scores)
    {
        _alphabeticalIndexMap.Clear();
        if (scores == null || scores.Count == 0) return;

        bool byComposer = _currentSort.StartsWith("Composer", StringComparison.OrdinalIgnoreCase);
        bool isDescending = _currentSort.EndsWith("Desc", StringComparison.OrdinalIgnoreCase);

        var scoreKeys = new string[scores.Count];
        for (int i = 0; i < scores.Count; i++)
        {
            scoreKeys[i] = GetScoreSortKey(scores[i], byComposer);
        }

        // Cible pour '#'
        if (!isDescending)
        {
            int hashIndex = -1;
            for (int i = 0; i < scoreKeys.Length; i++)
            {
                if (string.IsNullOrEmpty(scoreKeys[i]) || !char.IsLetter(scoreKeys[i][0]))
                {
                    hashIndex = i;
                    break;
                }
            }
            _alphabeticalIndexMap["#"] = hashIndex >= 0 ? hashIndex : 0;
        }
        else
        {
            int hashIndex = -1;
            for (int i = scoreKeys.Length - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(scoreKeys[i]) || !char.IsLetter(scoreKeys[i][0]))
                {
                    hashIndex = i;
                    break;
                }
            }
            _alphabeticalIndexMap["#"] = hashIndex >= 0 ? hashIndex : scoreKeys.Length - 1;
        }

        // Cibles pour chaque lettre A à Z
        for (char c = 'A'; c <= 'Z'; c++)
        {
            string letterStr = c.ToString();
            int targetIndex = -1;

            // 1. Recherche exacte
            for (int i = 0; i < scoreKeys.Length; i++)
            {
                if (!string.IsNullOrEmpty(scoreKeys[i]) && char.ToUpperInvariant(scoreKeys[i][0]) == c)
                {
                    targetIndex = i;
                    break;
                }
            }

            // 2. Repli vers la lettre la plus proche
            if (targetIndex == -1)
            {
                if (!isDescending)
                {
                    for (int i = 0; i < scoreKeys.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(scoreKeys[i]) && char.ToUpperInvariant(scoreKeys[i][0]) > c)
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                    if (targetIndex == -1) targetIndex = scoreKeys.Length - 1;
                }
                else
                {
                    for (int i = 0; i < scoreKeys.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(scoreKeys[i]) && char.ToUpperInvariant(scoreKeys[i][0]) < c)
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                    if (targetIndex == -1) targetIndex = scoreKeys.Length - 1;
                }
            }

            _alphabeticalIndexMap[letterStr] = targetIndex;
        }
    }

    private void OnAlphabeticalIndexTapped(string letter)
    {
        if (ScoresCollectionView?.ItemsSource is not IList<Models.Score> scores || scores.Count == 0)
            return;

        if (!_alphabeticalIndexMap.TryGetValue(letter, out int targetIndex) || targetIndex < 0 || targetIndex >= scores.Count)
            return;

        var targetItem = scores[targetIndex];

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                ScoresCollectionView.ScrollTo(targetItem, position: ScrollToPosition.Start, animate: false);
            }
            catch
            {
                try
                {
                    ScoresCollectionView.ScrollTo(targetIndex, position: ScrollToPosition.Start, animate: false);
                }
                catch { }
            }
        });
    }

    private static string GetScoreSortKey(Models.Score score, bool byComposer)
    {
        string raw = byComposer ? (score.Composer ?? string.Empty) : (score.Title ?? string.Empty);
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        return RemoveDiacritics(raw.Trim());
    }

    private static string RemoveDiacritics(string text)
    {
        string normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (char c in normalized)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private void OnScoresScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        if (ScrollToTopButton == null || !_cachedShowScrollTop)
        {
            if (ScrollToTopButton != null && ScrollToTopButton.IsVisible)
                ScrollToTopButton.IsVisible = false;
            return;
        }

        bool shouldShow = e.FirstVisibleItemIndex >= 5 || e.VerticalOffset > 250;
        if (ScrollToTopButton.IsVisible != shouldShow)
        {
            ScrollToTopButton.IsVisible = shouldShow;
        }
    }

    private void OnScrollToTopClicked(object? sender, EventArgs e)
    {
        if (ScoresCollectionView?.ItemsSource is IList<Models.Score> scores && scores.Count > 0)
        {
            var topItem = scores[0];
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    ScoresCollectionView.ScrollTo(topItem, position: ScrollToPosition.Start, animate: false);
                }
                catch
                {
                    try
                    {
                        ScoresCollectionView.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
                    }
                    catch { }
                }
            });
        }
        else if (ScoresCollectionView != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ScoresCollectionView.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
            });
        }
    }

    #region Modern Import Dialogs

    private TaskCompletionSource<bool>? _modalTcsBool;
    private TaskCompletionSource<string?>? _modalTcsString;
    private Models.Score? _lastImportedScore;

    private async Task AnimateModalOpenAsync()
    {
        ModernImportModalOverlay.IsVisible = true;
        ModernImportModalOverlay.Opacity = 0;
        ModernImportModalCard.Scale = 0.88;
        await Task.WhenAll(
            ModernImportModalOverlay.FadeTo(1, 180, Easing.CubicOut),
            ModernImportModalCard.ScaleTo(1.0, 180, Easing.CubicOut)
        );
    }

    private async Task AnimateModalCloseAsync()
    {
        await Task.WhenAll(
            ModernImportModalOverlay.FadeTo(0, 140, Easing.CubicIn),
            ModernImportModalCard.ScaleTo(0.9, 140, Easing.CubicIn)
        );
        ModernImportModalOverlay.IsVisible = false;
    }

    private async void OnModernModalBackgroundTapped(object? sender, EventArgs e)
    {
        _modalTcsBool?.TrySetResult(false);
        _modalTcsBool = null;
        _modalTcsString?.TrySetResult(null);
        _modalTcsString = null;

        await AnimateModalCloseAsync();
    }

    private Task ShowModernImportSuccessAsync(List<Models.Score> scores)
    {
        if (scores == null || !scores.Any()) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ModernModalIconLabel.Text = "✓";
            ModernModalIconLabel.TextColor = Color.FromArgb("#2ECC71");
            ModernModalIconHalo.Stroke = Color.FromArgb("#2ECC71");
            ModernModalIconHalo.BackgroundColor = Color.FromArgb("#182ECC71");
            ModernModalTitleLabel.Text = "Importation réussie !";

            ModernModalBadgeBorder.IsVisible = true;
            ModernModalInputWrapper.IsVisible = false;
            ModernModalButtonsLayout.Clear();

            if (scores.Count == 1)
            {
                var singleScore = scores[0];
                _lastImportedScore = singleScore;

                ModernModalBadgeLabel.Text = "1 partition ajoutée";
                ModernModalBadgeBorder.Stroke = Color.FromArgb("#1E88E5");
                ModernModalBadgeBorder.BackgroundColor = Color.FromArgb("#132B45");
                ModernModalBadgeLabel.TextColor = Color.FromArgb("#4FC3F7");

                ModernModalContentContainer.Clear();
                var titleLbl = new Label
                {
                    Text = singleScore.Title,
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 15,
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                ModernModalContentContainer.Add(titleLbl);

                string pageInfo = singleScore.PageCount > 0 ? $"📄 {singleScore.PageCount} page(s)" : "📄 Format PDF";
                var pageLbl = new Label
                {
                    Text = pageInfo,
                    TextColor = Color.FromArgb("#AAAAAA"),
                    FontSize = 12,
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                ModernModalContentContainer.Add(pageLbl);

                ModernModalHintLabel.Text = "Disponible immédiatement dans votre bibliothèque";

                var openBtn = new Button
                {
                    Text = "🎵 Ouvrir la partition",
                    BackgroundColor = Color.FromArgb("#007ACC"),
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    HeightRequest = 44,
                    CornerRadius = 12
                };
                openBtn.Clicked += async (s, ev) =>
                {
                    await AnimateModalCloseAsync();
                    tcs.TrySetResult(true);
                    if (_lastImportedScore != null)
                    {
                        await Navigation.PushAsync(new ViewerPage(_lastImportedScore));
                    }
                };
                ModernModalButtonsLayout.Add(openBtn);

                var closeBtn = new Button
                {
                    Text = "Fermer",
                    BackgroundColor = Color.FromArgb("#2D303B"),
                    TextColor = Color.FromArgb("#DDDDDD"),
                    FontSize = 13,
                    HeightRequest = 38,
                    CornerRadius = 10
                };
                closeBtn.Clicked += async (s, ev) =>
                {
                    await AnimateModalCloseAsync();
                    tcs.TrySetResult(true);
                };
                ModernModalButtonsLayout.Add(closeBtn);
            }
            else
            {
                _lastImportedScore = null;
                ModernModalBadgeLabel.Text = $"{scores.Count} partitions ajoutées";
                ModernModalBadgeBorder.Stroke = Color.FromArgb("#2ECC71");
                ModernModalBadgeBorder.BackgroundColor = Color.FromArgb("#142E1F");
                ModernModalBadgeLabel.TextColor = Color.FromArgb("#58D68D");

                ModernModalContentContainer.Clear();
                foreach (var sc in scores.Take(8))
                {
                    var row = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center };
                    row.Add(new Label { Text = "•", TextColor = Color.FromArgb("#00B0FF"), FontSize = 13 });
                    row.Add(new Label { Text = sc.Title, TextColor = Color.FromArgb("#E0E0E0"), FontSize = 13, LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 });
                    ModernModalContentContainer.Add(row);
                }
                if (scores.Count > 8)
                {
                    ModernModalContentContainer.Add(new Label
                    {
                        Text = $"... et {scores.Count - 8} autre(s)",
                        TextColor = Color.FromArgb("#888888"),
                        FontSize = 11,
                        FontAttributes = FontAttributes.Italic,
                        HorizontalOptions = LayoutOptions.Center
                    });
                }

                ModernModalHintLabel.Text = "Disponibles immédiatement dans votre bibliothèque";

                var okBtn = new Button
                {
                    Text = "✓ Voir la bibliothèque",
                    BackgroundColor = Color.FromArgb("#007ACC"),
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    HeightRequest = 44,
                    CornerRadius = 12
                };
                okBtn.Clicked += async (s, ev) =>
                {
                    await AnimateModalCloseAsync();
                    tcs.TrySetResult(true);
                };
                ModernModalButtonsLayout.Add(okBtn);
            }

            await AnimateModalOpenAsync();
        });

        return tcs.Task;
    }

    private Task<bool> ShowModernConfirmDialogAsync(string title, string message, string accept, string cancel)
    {
        var tcs = new TaskCompletionSource<bool>();
        _modalTcsBool = tcs;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ModernModalIconLabel.Text = "📑";
            ModernModalIconLabel.TextColor = Color.FromArgb("#00B0FF");
            ModernModalIconHalo.Stroke = Color.FromArgb("#00B0FF");
            ModernModalIconHalo.BackgroundColor = Color.FromArgb("#1500B0FF");
            ModernModalTitleLabel.Text = title;

            ModernModalBadgeBorder.IsVisible = false;
            ModernModalInputWrapper.IsVisible = false;
            ModernModalHintLabel.Text = string.Empty;

            ModernModalContentContainer.Clear();
            ModernModalContentContainer.Add(new Label
            {
                Text = message,
                TextColor = Color.FromArgb("#E2E8F0"),
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.WordWrap
            });

            ModernModalButtonsLayout.Clear();

            var acceptBtn = new Button
            {
                Text = accept,
                BackgroundColor = Color.FromArgb("#007ACC"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                MinimumHeightRequest = 44,
                Padding = new Thickness(14, 8),
                CornerRadius = 12
            };
            acceptBtn.Clicked += async (s, ev) =>
            {
                await AnimateModalCloseAsync();
                tcs.TrySetResult(true);
            };
            ModernModalButtonsLayout.Add(acceptBtn);

            var cancelBtn = new Button
            {
                Text = cancel,
                BackgroundColor = Color.FromArgb("#2D303B"),
                TextColor = Color.FromArgb("#DDDDDD"),
                FontSize = 13,
                MinimumHeightRequest = 38,
                Padding = new Thickness(14, 6),
                CornerRadius = 10
            };
            cancelBtn.Clicked += async (s, ev) =>
            {
                await AnimateModalCloseAsync();
                tcs.TrySetResult(false);
            };
            ModernModalButtonsLayout.Add(cancelBtn);

            await AnimateModalOpenAsync();
        });

        return tcs.Task;
    }

    private Task<string?> ShowModernChoiceDialogAsync(string title, string cancel, string[] options)
    {
        var tcs = new TaskCompletionSource<string?>();
        _modalTcsString = tcs;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ModernModalIconLabel.Text = "📁";
            ModernModalIconLabel.TextColor = Color.FromArgb("#00B0FF");
            ModernModalIconHalo.Stroke = Color.FromArgb("#00B0FF");
            ModernModalIconHalo.BackgroundColor = Color.FromArgb("#1500B0FF");
            ModernModalTitleLabel.Text = title;

            ModernModalBadgeBorder.IsVisible = false;
            ModernModalInputWrapper.IsVisible = false;
            ModernModalContentContainer.Clear();
            ModernModalHintLabel.Text = "Choisissez le mode de traitement :";

            ModernModalButtonsLayout.Clear();

            foreach (var opt in options)
            {
                var optBtn = new Button
                {
                    Text = opt,
                    BackgroundColor = opt.Contains("Conseillé") ? Color.FromArgb("#007ACC") : Color.FromArgb("#282B36"),
                    TextColor = Colors.White,
                    FontAttributes = opt.Contains("Conseillé") ? FontAttributes.Bold : FontAttributes.None,
                    FontSize = 13,
                    MinimumHeightRequest = 42,
                    Padding = new Thickness(14, 8),
                    CornerRadius = 10
                };
                optBtn.Clicked += async (s, ev) =>
                {
                    await AnimateModalCloseAsync();
                    tcs.TrySetResult(opt);
                };
                ModernModalButtonsLayout.Add(optBtn);
            }

            var cancelBtn = new Button
            {
                Text = cancel,
                BackgroundColor = Color.FromArgb("#1E2028"),
                TextColor = Color.FromArgb("#AAAAAA"),
                FontSize = 12,
                MinimumHeightRequest = 36,
                Padding = new Thickness(14, 6),
                CornerRadius = 8
            };
            cancelBtn.Clicked += async (s, ev) =>
            {
                await AnimateModalCloseAsync();
                tcs.TrySetResult(cancel);
            };
            ModernModalButtonsLayout.Add(cancelBtn);

            await AnimateModalOpenAsync();
        });

        return tcs.Task;
    }

    private Task<string?> ShowModernPromptDialogAsync(string title, string message, string defaultVal)
        => ShowModernPromptDialogAsync(title, message, defaultVal, "OK");

    private Task<string?> ShowModernPromptDialogAsync(string title, string message, string defaultVal, string confirmBtnText)
    {
        var tcs = new TaskCompletionSource<string?>();
        _modalTcsString = tcs;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ModernModalIconLabel.Text = "✎";
            ModernModalIconLabel.TextColor = Color.FromArgb("#00B0FF");
            ModernModalIconHalo.Stroke = Color.FromArgb("#00B0FF");
            ModernModalIconHalo.BackgroundColor = Color.FromArgb("#1500B0FF");
            ModernModalTitleLabel.Text = title;

            ModernModalBadgeBorder.IsVisible = false;
            ModernModalContentContainer.Clear();
            ModernModalContentContainer.Add(new Label
            {
                Text = message,
                TextColor = Color.FromArgb("#CCCCCC"),
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center
            });

            ModernModalHintLabel.Text = string.Empty;

            ModernModalInputWrapper.IsVisible = true;
            ModernModalEntry.Text = defaultVal;
            ModernModalEntry.Focus();

            ModernModalButtonsLayout.Clear();

            var okBtn = new Button
            {
                Text = confirmBtnText,
                BackgroundColor = Color.FromArgb("#007ACC"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                HeightRequest = 44,
                CornerRadius = 12
            };
            okBtn.Clicked += async (s, ev) =>
            {
                string text = ModernModalEntry.Text;
                await AnimateModalCloseAsync();
                tcs.TrySetResult(string.IsNullOrWhiteSpace(text) ? defaultVal : text.Trim());
            };
            ModernModalButtonsLayout.Add(okBtn);

            var cancelBtn = new Button
            {
                Text = "Annuler",
                BackgroundColor = Color.FromArgb("#2D303B"),
                TextColor = Color.FromArgb("#DDDDDD"),
                FontSize = 13,
                HeightRequest = 38,
                CornerRadius = 10
            };
            cancelBtn.Clicked += async (s, ev) =>
            {
                await AnimateModalCloseAsync();
                tcs.TrySetResult(null);
            };
            ModernModalButtonsLayout.Add(cancelBtn);

            await AnimateModalOpenAsync();
        });

        return tcs.Task;
    }

    private Task ShowModernAlertDialogAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool isError = title.Contains("Erreur", StringComparison.OrdinalIgnoreCase) || title.Contains("Impossible", StringComparison.OrdinalIgnoreCase);

            ModernModalIconLabel.Text = isError ? "✕" : "ℹ";
            ModernModalIconLabel.TextColor = isError ? Color.FromArgb("#E53935") : Color.FromArgb("#00B0FF");
            ModernModalIconHalo.Stroke = isError ? Color.FromArgb("#E53935") : Color.FromArgb("#00B0FF");
            ModernModalIconHalo.BackgroundColor = isError ? Color.FromArgb("#20E53935") : Color.FromArgb("#1500B0FF");
            ModernModalTitleLabel.Text = title;

            ModernModalBadgeBorder.IsVisible = false;
            ModernModalInputWrapper.IsVisible = false;
            ModernModalContentContainer.Clear();
            ModernModalContentContainer.Add(new Label
            {
                Text = message,
                TextColor = Color.FromArgb("#E2E8F0"),
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.WordWrap
            });

            ModernModalHintLabel.Text = string.Empty;

            ModernModalButtonsLayout.Clear();

            var okBtn = new Button
            {
                Text = "OK",
                BackgroundColor = isError ? Color.FromArgb("#C62828") : Color.FromArgb("#007ACC"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                HeightRequest = 42,
                CornerRadius = 10
            };
            okBtn.Clicked += async (s, ev) =>
            {
                await AnimateModalCloseAsync();
                tcs.TrySetResult(true);
            };
            ModernModalButtonsLayout.Add(okBtn);

            await AnimateModalOpenAsync();
        });

        return tcs.Task;
    }

    private TaskCompletionSource<string?>? _categoryModalTcs;

    private Task<string?> ShowCategoryPickerModalAsync(string title, string subtitle, string? currentCategory)
    {
        var tcs = new TaskCompletionSource<string?>();
        _categoryModalTcs = tcs;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            CategoryModalTitleLabel.Text = title;
            CategoryModalSubtitleLabel.Text = subtitle;
            CategoryModalListContainer.Clear();

            var categories = await _databaseService.GetCategoriesAsync();
            var loc = LocalizationService.Instance;
            string currentBadgeText = loc.GetString("Score_Category_Current_Badge", "Actuel");

            foreach (var cat in categories)
            {
                bool isCurrent = string.Equals(cat, currentCategory, StringComparison.OrdinalIgnoreCase);

                var itemBorder = new Border
                {
                    BackgroundColor = isCurrent ? Color.FromArgb("#19354F") : Color.FromArgb("#1E202B"),
                    Stroke = isCurrent ? Color.FromArgb("#007ACC") : Color.FromArgb("#2C303E"),
                    StrokeThickness = 1,
                    Padding = new Thickness(12, 10),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    HorizontalOptions = LayoutOptions.Fill
                };

                var itemGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    ColumnSpacing = 10,
                    VerticalOptions = LayoutOptions.Center
                };

                var iconLabel = new Label
                {
                    Text = "📁",
                    FontSize = 16,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(iconLabel, 0);

                var nameLabel = new Label
                {
                    Text = cat,
                    TextColor = isCurrent ? Colors.White : Color.FromArgb("#E0E0E0"),
                    FontAttributes = isCurrent ? FontAttributes.Bold : FontAttributes.None,
                    FontSize = 14,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                Grid.SetColumn(nameLabel, 1);

                itemGrid.Add(iconLabel);
                itemGrid.Add(nameLabel);

                if (isCurrent)
                {
                    var badgeBorder = new Border
                    {
                        BackgroundColor = Color.FromArgb("#25007ACC"),
                        Stroke = Color.FromArgb("#007ACC"),
                        StrokeThickness = 1,
                        Padding = new Thickness(6, 2),
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                        VerticalOptions = LayoutOptions.Center
                    };
                    badgeBorder.Content = new Label
                    {
                        Text = $"✓ {currentBadgeText}",
                        TextColor = Color.FromArgb("#00B0FF"),
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold
                    };
                    Grid.SetColumn(badgeBorder, 2);
                    itemGrid.Add(badgeBorder);
                }

                itemBorder.Content = itemGrid;

                var capturedCat = cat;
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, e) =>
                {
                    await HideCategoryModalAsync();
                    tcs.TrySetResult(capturedCat);
                };
                itemBorder.GestureRecognizers.Add(tap);

                CategoryModalListContainer.Add(itemBorder);
            }

            ModernCategoryModalOverlay.IsVisible = true;
            ModernCategoryModalOverlay.Opacity = 0;
            ModernCategoryModalCard.Scale = 0.9;
            await Task.WhenAll(
                ModernCategoryModalOverlay.FadeTo(1, 180, Easing.CubicOut),
                ModernCategoryModalCard.ScaleTo(1.0, 180, Easing.CubicOut)
            );
        });

        return tcs.Task;
    }

    private async Task HideCategoryModalAsync()
    {
        if (ModernCategoryModalOverlay == null || !ModernCategoryModalOverlay.IsVisible) return;
        await Task.WhenAll(
            ModernCategoryModalOverlay.FadeTo(0, 140, Easing.CubicIn),
            ModernCategoryModalCard.ScaleTo(0.9, 140, Easing.CubicIn)
        );
        ModernCategoryModalOverlay.IsVisible = false;
    }

    private async void OnCategoryModalCancelClicked(object? sender, EventArgs e)
    {
        await HideCategoryModalAsync();
        _categoryModalTcs?.TrySetResult(null);
    }

    private async void OnCategoryModalCreateNewClicked(object? sender, EventArgs e)
    {
        await HideCategoryModalAsync();
        var loc = LocalizationService.Instance;
        string title = loc.GetString("Score_Category_New_Title", "Nouveau dossier");
        string prompt = loc.GetString("Score_Category_New_Prompt", "Entrez le nom du nouveau dossier / catégorie :");
        string ok = loc.GetString("Common_OK", "OK");

        string? newCat = await ShowModernPromptDialogAsync(title, prompt, string.Empty, ok);
        if (!string.IsNullOrWhiteSpace(newCat))
        {
            string clean = newCat.Trim();
            await _databaseService.CreateCategoryAsync(clean);
            _categoryModalTcs?.TrySetResult(clean);
        }
        else
        {
            _categoryModalTcs?.TrySetResult(null);
        }
    }

    #endregion
}
