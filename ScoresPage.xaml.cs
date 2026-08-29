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
    private readonly IBluetoothTransferService _bluetoothService;
    private readonly ExportImportService _exportImportService;
    private readonly SettingsService _settingsService = new();
    private string _currentSort = "TitleAsc";
    private readonly List<int> _selectedTagIds = new();
    private List<Models.Tag> _allTags = new();
    private string _tagSearchQuery = string.Empty;
    private string _tagSortType = "NameAsc"; // "NameAsc", "NameDesc", "SelectedFirst", "Color"
    private Models.Score? _selectedScoreForMenu;

    private readonly ObservableCollection<BluetoothDeviceInfo> _discoveredDevices = new();

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

    public ScoresPage(DatabaseService databaseService, ImportService importService, IBluetoothTransferService bluetoothService, ExportImportService exportImportService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _importService = importService;
        _bluetoothService = bluetoothService;
        _exportImportService = exportImportService;

        BluetoothDevicesListView.ItemsSource = _discoveredDevices;
    }

    public ScoresPage() : this(
        new DatabaseService(), 
        new ImportService(new DatabaseService()), 
        new BluetoothTransferService(), 
        new ExportImportService(new DatabaseService(), new SettingsService()))
    {
    }

    private bool _isInitialLoaded = false;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // S'abonner aux événements Bluetooth
        _bluetoothService.DeviceDiscovered += OnBluetoothDeviceDiscovered;
        _bluetoothService.ScanFinished += OnBluetoothScanFinished;
        _bluetoothService.TransferProgressChanged += OnBluetoothTransferProgressChanged;

        if (!_isInitialLoaded)
        {
            _isInitialLoaded = true;
            _ = Task.Run(async () =>
            {
                try 
                {
                    _allTags = await _databaseService.GetTagsAsync();
                    
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        UpdateActiveTagsChips();
                        await LoadScoresAsync(SearchScoreBar.Text);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ScoresPage.OnAppearing: {ex.Message}");
                }
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Désabonner des événements Bluetooth pour éviter les fuites de mémoire
        _bluetoothService.DeviceDiscovered -= OnBluetoothDeviceDiscovered;
        _bluetoothService.ScanFinished -= OnBluetoothScanFinished;
        _bluetoothService.TransferProgressChanged -= OnBluetoothTransferProgressChanged;

        // Arrêter l'écoute ou le scan si actif
        _ = _bluetoothService.StopScanningAsync();
        _ = _bluetoothService.StopListeningAsync();
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

    private async Task LoadScoresAsync(string query = "")
    {
        var scores = await _databaseService.SearchScoresAsync(query ?? string.Empty, _selectedTagIds);

        if (_currentSort == "TitleAsc") scores = scores.OrderBy(s => s.Title).ToList();
        else if (_currentSort == "TitleDesc") scores = scores.OrderByDescending(s => s.Title).ToList();
        else if (_currentSort == "DateAsc") scores = scores.OrderBy(s => s.DateAdded).ToList();
        else if (_currentSort == "DateDesc") scores = scores.OrderByDescending(s => s.DateAdded).ToList();

        foreach (var score in scores)
        {
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
        BluetoothOverlayTitle.Text = "Envoi de partitions";
        BluetoothOverlay.IsVisible = true;
        ShowBluetoothView("Sending");

        _discoveredDevices.Clear();
        BluetoothScanSpinner.IsRunning = true;
        BluetoothScanSpinner.IsVisible = true;

        bool initialized = await _bluetoothService.InitializeAsync();
        if (!initialized)
        {
            await DisplayAlertAsync("Bluetooth indisponible", "Impossible d'initialiser le Bluetooth. Veuillez vérifier que le Bluetooth est activé et que les permissions sont accordées.", "OK");
            BluetoothOverlay.IsVisible = false;
            return;
        }

        try
        {
            await _bluetoothService.StartScanningAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors du démarrage du scan: {ex.Message}", "OK");
            BluetoothOverlay.IsVisible = false;
        }
    }

    private async void OnBluetoothRescanClicked(object sender, EventArgs e)
    {
        _discoveredDevices.Clear();
        BluetoothScanSpinner.IsRunning = true;
        BluetoothScanSpinner.IsVisible = true;

        try
        {
            await _bluetoothService.StartScanningAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors du démarrage du scan: {ex.Message}", "OK");
        }
    }

    private async void OnBluetoothCloseClicked(object sender, EventArgs e)
    {
        BluetoothOverlay.IsVisible = false;
        BluetoothScanSpinner.IsRunning = false;
        _scoresToSendForExchange = null;

        await _bluetoothService.StopScanningAsync();
        await _bluetoothService.StopListeningAsync();
    }

    private async void OnBluetoothStartListeningClicked(object sender, EventArgs e)
    {
        BluetoothOverlayTitle.Text = "Mode Écoute";
        ShowBluetoothView("Listening");
        BluetoothListeningStatusLabel.Text = "En attente d'un émetteur...";

        bool initialized = await _bluetoothService.InitializeAsync();
        if (!initialized)
        {
            await DisplayAlertAsync("Bluetooth indisponible", "Impossible d'initialiser le Bluetooth. Veuillez activer le Bluetooth et accorder les permissions.", "OK");
            ShowBluetoothView("Choice");
            return;
        }

        try
        {
            await _bluetoothService.StartListeningAsync(
                confirmCallback: OnBluetoothReceiveRequestAsync,
                onReceiveComplete: OnBluetoothDataReceivedAsync
            );
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors du démarrage de l'écoute: {ex.Message}", "OK");
            ShowBluetoothView("Choice");
        }
    }

    private async void OnBluetoothStopListeningClicked(object sender, EventArgs e)
    {
        await _bluetoothService.StopListeningAsync();
        ShowBluetoothView("Choice");
    }

    private void OnBluetoothDeviceDiscovered(object? sender, BluetoothDeviceInfo device)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_discoveredDevices.Any(d => d.Address.Equals(device.Address, StringComparison.OrdinalIgnoreCase)))
            {
                _discoveredDevices.Add(device);
            }
        });
    }

    private void OnBluetoothScanFinished(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BluetoothScanSpinner.IsRunning = false;
            BluetoothScanSpinner.IsVisible = false;
        });
    }

    private void OnBluetoothTransferProgressChanged(object? sender, BluetoothTransferProgressEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ShowBluetoothView("Progress");
            BluetoothTransferStatusLabel.Text = $"{e.Status} ({Math.Round(e.Progress * 100)}%)";
            BluetoothTransferProgressBar.Progress = e.Progress;
        });
    }

    private async void OnBluetoothDeviceSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is not BluetoothDeviceInfo selectedDevice) return;

        BluetoothDevicesListView.SelectedItem = null;

        await _bluetoothService.StopScanningAsync();

        List<Models.Score> selectedScores = _scoresToSendForExchange ?? new List<Models.Score>();
        if (!selectedScores.Any() && ScoresCollectionView.ItemsSource is IEnumerable<Models.Score> scores)
        {
            selectedScores = scores.Where(s => s.IsSelected).ToList();
        }
        if (!selectedScores.Any()) return;

        try
        {
            ShowBluetoothView("Progress");
            BluetoothTransferStatusLabel.Text = "Préparation du package...";
            BluetoothTransferProgressBar.Progress = 0.05;

            var options = new ExportOptions { IncludeAnnotations = true, IncludeAudio = true };
            var filesToSend = await _exportImportService.BuildBluetoothPayloadForScoresAsync(selectedScores, options);

            if (!filesToSend.Any())
            {
                await DisplayAlertAsync("Erreur", "Aucun fichier valide n'a pu être trouvé pour l'envoi.", "OK");
                BluetoothOverlay.IsVisible = false;
                return;
            }

            BluetoothTransferStatusLabel.Text = "Connexion et envoi...";
            BluetoothTransferProgressBar.Progress = 0.15;

            string senderName = DeviceInfo.Name ?? "Appareil Mobile";

            bool success = await _bluetoothService.SendDataAsync(selectedDevice, filesToSend, senderName);

            if (success)
            {
                await DisplayAlertAsync("Succès", $"{selectedScores.Count} partition(s) envoyée(s) avec succès et reçue(s) par le destinataire !", "OK");
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
            BluetoothOverlay.IsVisible = false;
            _scoresToSendForExchange = null;
        }
    }

    private async Task<bool> OnBluetoothReceiveRequestAsync(string senderDeviceName, int scoreCount)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            bool accept = await DisplayAlertAsync(
                "Demande d'envoi", 
                $"L'appareil '{senderDeviceName}' souhaite vous envoyer {scoreCount} partition(s) / élément(s).\n\nAcceptez-vous la réception ?", 
                "Oui, accepter", 
                "Non, refuser");

            if (accept)
            {
                ShowBluetoothView("Progress");
                BluetoothTransferStatusLabel.Text = "Connexion acceptée. Réception...";
                BluetoothTransferProgressBar.Progress = 0;
            }

            return accept;
        });
    }

    private async Task OnBluetoothDataReceivedAsync(List<BluetoothFilePayload> receivedFiles, string senderName)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                BluetoothTransferStatusLabel.Text = "Intégration des données...";
                BluetoothTransferProgressBar.Progress = 0.9;
            });

            bool success = await _exportImportService.ImportPackageFromPayloadsAsync(receivedFiles);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                BluetoothOverlay.IsVisible = false;
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
                BluetoothOverlay.IsVisible = false;
                await DisplayAlertAsync("Échec de l'intégration", $"Une erreur est survenue lors de l'intégration des partitions : {ex.Message}", "OK");
            });
        }
    }

    private void ShowBluetoothView(string viewName)
    {
        BluetoothChoiceView.IsVisible = (viewName == "Choice");
        BluetoothListeningView.IsVisible = (viewName == "Listening");
        BluetoothSendingView.IsVisible = (viewName == "Sending");
        BluetoothTransferProgressView.IsVisible = (viewName == "Progress");
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
            "Titre (A-Z)", "Titre (Z-A)", "Date d'ajout (Récent)", "Date d'ajout (Ancien)");

        if (action == "Titre (A-Z)") _currentSort = "TitleAsc";
        else if (action == "Titre (Z-A)") _currentSort = "TitleDesc";
        else if (action == "Date d'ajout (Récent)") _currentSort = "DateDesc";
        else if (action == "Date d'ajout (Ancien)") _currentSort = "DateAsc";

        if (action != "Annuler")
            await LoadScoresAsync(SearchScoreBar.Text);
    }

    private void OnScoreOptionsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Models.Score score)
        {
            _selectedScoreForMenu = score;
            ScoreMenuTitleLabel.Text = score.Title;
            ScoreMenuOverlay.IsVisible = true;
        }
    }

    private async void OnMenuEditClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            _isInitialLoaded = false;
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
