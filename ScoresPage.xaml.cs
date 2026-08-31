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
    private string _currentSort = "DateDesc";
    private bool _hasUserCustomSort = false;
    private readonly List<int> _selectedTagIds = new();
    private List<Models.Tag> _allTags = new();
    private string _tagSearchQuery = string.Empty;
    private string _tagSortType = "NameAsc"; // "NameAsc", "NameDesc", "SelectedFirst", "Color"
    private Models.Score? _selectedScoreForMenu;

    private readonly ObservableCollection<WifiDeviceInfo> _discoveredDevices = new();

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

    public ScoresPage(DatabaseService databaseService, ImportService importService, IWifiDirectTransferService wifiService, ExportImportService exportImportService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _importService = importService;
        _wifiService = wifiService;
        _exportImportService = exportImportService;

        _currentSort = Preferences.Default.Get("DefaultScoreSort", "DateDesc");

        WifiDevicesListView.ItemsSource = _discoveredDevices;
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

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (EmptyScoresLabel != null)
                    {
                        EmptyScoresLabel.Text = "Aucune partition trouvée.";
                    }
                    ScoresCollectionView.ItemsSource = scores;
                    RecalculateSelectedCount();
                });
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
        return sortType switch
        {
            "TitleAsc" => scores.OrderBy(s => s.Title).ToList(),
            "TitleDesc" => scores.OrderByDescending(s => s.Title).ToList(),
            "DateAsc" => scores.OrderBy(s => s.DateAdded).ToList(),
            "DateDesc" => scores.OrderByDescending(s => s.DateAdded).ToList(),
            "ModifiedDesc" => scores.OrderByDescending(s => s.DateModified).ToList(),
            "RatingDesc" => scores.OrderByDescending(s => s.Rating).ThenBy(s => s.Title).ToList(),
            "ComposerAsc" => scores.OrderBy(s => s.Composer).ThenBy(s => s.Title).ToList(),
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

        MainThread.BeginInvokeOnMainThread(() => {
            if (EmptyScoresLabel != null)
            {
                EmptyScoresLabel.Text = "Aucune partition trouvée.";
            }
            ScoresCollectionView.ItemsSource = scores;
            RecalculateSelectedCount();
        });
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

        SelectedCountLabel.Text = count == 1 ? "1 partition sélectionnée" : $"{count} partitions sélectionnées";
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

        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any())
        {
            await DisplayAlertAsync("Aucune sélection", "Veuillez sélectionner au moins une partition à supprimer.", "OK");
            return;
        }

        bool confirm = await DisplayAlertAsync(
            "Suppression groupée", 
            $"Voulez-vous vraiment supprimer les références de {selectedScores.Count} partition(s) dans la base de données ?\n\nNote : Les fichiers physiques ne seront pas supprimés de votre appareil.", 
            "Oui", 
            "Non");

        if (confirm)
        {
            foreach (var score in selectedScores)
            {
                await _databaseService.DeleteScoreAsync(score);
            }

            IsMultiSelectActive = false; // Quitter le mode sélection multiple
            await LoadScoresAsync(SearchScoreBar.Text);
            await DisplayAlertAsync("Suppression réussie", $"{selectedScores.Count} partition(s) ont été retirées de l'application.", "OK");
        }
    }

    private readonly List<int> _bulkSelectedTagIds = new();
    private string _bulkTagSearchQuery = string.Empty;

    private void OnBulkTagsClicked(object sender, EventArgs e)
    {
        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;

        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any())
        {
            DisplayAlertAsync("Aucune sélection", "Veuillez sélectionner au moins une partition à étiqueter.", "OK");
            return;
        }

        BulkTagsHeaderLabel.Text = $"🏷️ Étiqueter {selectedScores.Count} partition(s)";
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
                Text = "Aucune étiquette disponible. Créez-en dans le menu Outils.",
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
        if (!_bulkSelectedTagIds.Any())
        {
            await DisplayAlertAsync("Aucune étiquette", "Veuillez sélectionner au moins une étiquette à appliquer.", "OK");
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
        await DisplayAlertAsync("Étiquettes appliquées", $"{_bulkSelectedTagIds.Count} étiquette(s) attribuée(s) avec succès à {selectedScores.Count} partition(s).", "OK");
    }

    private List<Models.Score>? _scoresToSendForExchange = null;

    private async void OnBulkExportClicked(object sender, EventArgs e)
    {
        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;

        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any())
        {
            await DisplayAlertAsync("Aucune sélection", "Veuillez sélectionner au moins une partition à exporter.", "OK");
            return;
        }

        try
        {
            var options = new ExportOptions { IncludeAnnotations = true, IncludeAudio = true };
            string path = await _exportImportService.ExportScoresToFileAsync(selectedScores, options);
            await DisplayAlertAsync("Export réussi", $"{selectedScores.Count} partition(s) exportée(s) avec succès dans :\n\n{path}", "OK");
            IsMultiSelectActive = false;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur d'export", $"Impossible d'exporter les partitions : {ex.Message}", "OK");
        }
    }

    private async void OnBulkExchangeClicked(object sender, EventArgs e)
    {
        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;

        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any())
        {
            await DisplayAlertAsync("Aucune sélection", "Veuillez sélectionner au moins une partition à envoyer.", "OK");
            return;
        }

        await StartExchangeFlowAsync(selectedScores);
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

            var options = new ExportOptions { IncludeAnnotations = true, IncludeAudio = true };
            var filesToSend = await _exportImportService.CreateWifiPayloadsForScoresAsync(selectedScores, options);

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

        var options = new ExportOptions { IncludeAnnotations = true, IncludeAudio = true };
        var filesToSend = await _exportImportService.CreateWifiPayloadsForScoresAsync(selectedScores, options);

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
        TagsFilterOverlay.IsVisible = true;
        RenderOverlayTags();
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
            TagSortIndicatorLabel.Text = "Tri : Z-A";
        }
        else if (_tagSortType == "NameDesc")
        {
            _tagSortType = "SelectedFirst";
            TagSortIndicatorLabel.Text = "Tri : Sélectionnés d'abord";
        }
        else if (_tagSortType == "SelectedFirst")
        {
            _tagSortType = "Color";
            TagSortIndicatorLabel.Text = "Tri : Couleur";
        }
        else
        {
            _tagSortType = "NameAsc";
            TagSortIndicatorLabel.Text = "Tri : A-Z";
        }

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
                Text = "Aucune étiquette trouvée.",
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
        await LoadScoresAsync(e.NewTextValue);
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        var scores = await _importService.ImportScoresAsync();
        if (scores != null && scores.Any())
        {
            // Recharger la liste après l'import réussi
            await LoadScoresAsync(SearchScoreBar.Text);
        }
    }

    private async void OnScoreSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Models.Score selectedScore)
        {
            // Toujours désélectionner l'élément
            ScoresCollectionView.SelectedItem = null;

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
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync("Trier par", "Annuler", null, 
            "Date d'ajout (Récent)", "Date d'ajout (Ancien)", "Titre (A-Z)", "Titre (Z-A)", "Date de modification", "Évaluation (Note)", "Compositeur (A-Z)");

        if (action == "Date d'ajout (Récent)") { _currentSort = "DateDesc"; _hasUserCustomSort = true; }
        else if (action == "Date d'ajout (Ancien)") { _currentSort = "DateAsc"; _hasUserCustomSort = true; }
        else if (action == "Titre (A-Z)") { _currentSort = "TitleAsc"; _hasUserCustomSort = true; }
        else if (action == "Titre (Z-A)") { _currentSort = "TitleDesc"; _hasUserCustomSort = true; }
        else if (action == "Date de modification") { _currentSort = "ModifiedDesc"; _hasUserCustomSort = true; }
        else if (action == "Évaluation (Note)") { _currentSort = "RatingDesc"; _hasUserCustomSort = true; }
        else if (action == "Compositeur (A-Z)") { _currentSort = "ComposerAsc"; _hasUserCustomSort = true; }

        if (action != "Annuler")
            await LoadScoresAsync(SearchScoreBar.Text);
    }

    private void OnScoreOptionsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Models.Score score)
        {
            _selectedScoreForMenu = score;
            ScoreMenuTitleLabel.Text = score.Title;
            string comp = !string.IsNullOrWhiteSpace(score.Composer) ? score.Composer : "Compositeur non renseigné";
            string date = score.DateAdded != default ? score.DateAdded.ToString("dd/MM/yyyy") : "";
            ScoreMenuSubtitleLabel.Text = $"{comp} • {date}";
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

            await Shell.Current.GoToAsync("//ToolsPage");
            var toolsPage = Shell.Current.CurrentPage as ToolsPage 
                ?? Handler?.MauiContext?.Services.GetService<ToolsPage>();
            if (toolsPage != null)
            {
                await toolsPage.OpenScoreInAssemblerAsync(score);
            }
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

    private async void OnMenuExchangeClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            await StartExchangeFlowAsync(new List<Models.Score> { _selectedScoreForMenu });
        }
    }

    private async void OnMenuExportClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            var score = _selectedScoreForMenu;
            _selectedScoreForMenu = null;
            try
            {
                var options = new ExportOptions { IncludeAnnotations = true, IncludeAudio = true };
                string path = await _exportImportService.ExportScoresToFileAsync(new List<Models.Score> { score }, options);
                await DisplayAlertAsync("Export réussi", $"La partition '{score.Title}' a été exportée avec succès dans :\n\n{path}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erreur d'export", $"Impossible d'exporter la partition : {ex.Message}", "OK");
            }
        }
    }

    private async void OnMenuRenameClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            string newTitle = await DisplayPromptAsync("Renommer", "Nouveau titre :", "OK", "Annuler", initialValue: _selectedScoreForMenu.Title);
            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                _selectedScoreForMenu.Title = newTitle.Trim();
                await _databaseService.SaveScoreAsync(_selectedScoreForMenu);
                await LoadScoresAsync(SearchScoreBar.Text);
            }
        }
    }

    private async void OnMenuDeleteClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer '{_selectedScoreForMenu.Title}' ?", "Oui", "Non");
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
}
