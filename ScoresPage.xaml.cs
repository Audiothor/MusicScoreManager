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
    private readonly SettingsService _settingsService = new();
    private string _currentSort = "TitleAsc";
    private readonly List<int> _selectedTagIds = new();
    private List<Models.Tag> _allTags = new();
    private string _tagSearchQuery = string.Empty;
    private string _tagSortType = "NameAsc"; // "NameAsc", "NameDesc", "SelectedFirst", "Color"
    private Models.Score? _selectedScoreForMenu;

    private readonly ObservableCollection<BluetoothDeviceInfo> _discoveredDevices = new();
    private byte[]? _pendingSendData;

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

    public ScoresPage(DatabaseService databaseService, ImportService importService, IBluetoothTransferService bluetoothService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _importService = importService;
        _bluetoothService = bluetoothService;

        BluetoothDevicesListView.ItemsSource = _discoveredDevices;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // S'abonner aux événements Bluetooth
        _bluetoothService.DeviceDiscovered += OnBluetoothDeviceDiscovered;
        _bluetoothService.ScanFinished += OnBluetoothScanFinished;
        _bluetoothService.TransferProgressChanged += OnBluetoothTransferProgressChanged;

        _ = Task.Run(async () =>
        {
            try 
            {
                // On laisse un peu de temps pour l'animation de transition
                await Task.Delay(50);
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
                    Padding = new Thickness(12, 6),
                    VerticalOptions = LayoutOptions.Center,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }
                };

                var labelStack = new HorizontalStackLayout { Spacing = 6 };

                var label = new Label
                {
                    Text = tag.Name,
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 12
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

    private async void OnBulkExchangeClicked(object sender, EventArgs e)
    {
        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;

        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any())
        {
            await DisplayAlertAsync("Aucune sélection", "Veuillez sélectionner au moins une partition à échanger.", "OK");
            return;
        }

        BluetoothOverlayTitle.Text = "Échange de partitions";
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

        if (ScoresCollectionView.ItemsSource is not IEnumerable<Models.Score> scores) return;
        var selectedScores = scores.Where(s => s.IsSelected).ToList();
        if (!selectedScores.Any()) return;

        try
        {
            ShowBluetoothView("Progress");
            BluetoothTransferStatusLabel.Text = "Préparation des données...";
            BluetoothTransferProgressBar.Progress = 0.05;

            byte[] zipData = await PackageScoresAsync(selectedScores);

            BluetoothTransferStatusLabel.Text = "Connexion et envoi...";
            BluetoothTransferProgressBar.Progress = 0.15;

            string senderName = DeviceInfo.Name ?? "Appareil Mobile";

            bool success = await _bluetoothService.SendDataAsync(selectedDevice, zipData, senderName, selectedScores.Count);

            if (success)
            {
                await DisplayAlertAsync("Succès", $"{selectedScores.Count} partition(s) envoyées avec succès !", "OK");
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
        }
    }

    private async Task<bool> OnBluetoothReceiveRequestAsync(string senderDeviceName, int scoreCount)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            bool accept = await DisplayAlertAsync(
                "Demande d'échange", 
                $"L'appareil '{senderDeviceName}' souhaite vous envoyer {scoreCount} partition(s).\n\nAcceptez-vous la réception ?", 
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

    private async Task OnBluetoothDataReceivedAsync(byte[] zipData, string senderName)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                BluetoothTransferStatusLabel.Text = "Extraction et intégration...";
                BluetoothTransferProgressBar.Progress = 0.9;
            });

            string tempZipPath = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid().ToString() + ".zip");
            await File.WriteAllBytesAsync(tempZipPath, zipData);

            string extractDir = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid().ToString());
            Directory.CreateDirectory(extractDir);

            ZipFile.ExtractToDirectory(tempZipPath, extractDir);

            string metadataPath = Path.Combine(extractDir, "metadata.json");
            if (!File.Exists(metadataPath))
            {
                throw new FileNotFoundException("Fichier metadata.json manquant dans l'archive reçue.");
            }

            string jsonMetadata = await File.ReadAllTextAsync(metadataPath);
            var transferList = JsonSerializer.Deserialize<List<ScoreTransferMetadata>>(jsonMetadata);

            if (transferList != null && transferList.Any())
            {
                var scoresRootDir = _settingsService.ScoresRootDirectory;
                var audioRootDir = _settingsService.AudioRootDirectory;

                if (!Directory.Exists(scoresRootDir)) Directory.CreateDirectory(scoresRootDir);
                if (!Directory.Exists(audioRootDir)) Directory.CreateDirectory(audioRootDir);

                var existingTags = await _databaseService.GetTagsAsync();

                foreach (var item in transferList)
                {
                    string srcScorePath = Path.Combine(extractDir, item.FileName);
                    if (!File.Exists(srcScorePath)) continue;

                    string originalFileName = item.FileName;
                    var sanitizedFileName = item.Title.Replace(" ", "_") + Path.GetExtension(originalFileName);
                    var localScorePath = Path.Combine(scoresRootDir, sanitizedFileName);

                    int counter = 1;
                    while (File.Exists(localScorePath))
                    {
                        var fileNameOnly = Path.GetFileNameWithoutExtension(sanitizedFileName);
                        var extension = Path.GetExtension(sanitizedFileName);
                        localScorePath = Path.Combine(scoresRootDir, $"{fileNameOnly}_{counter}{extension}");
                        counter++;
                    }

                    File.Copy(srcScorePath, localScorePath);

                    var score = new Score
                    {
                        Title = item.Title,
                        FilePath = _settingsService.GetRelativePath(localScorePath),
                        Type = item.Type,
                        Rotation = item.Rotation,
                        IsRotationSaved = item.IsRotationSaved,
                        ShowMetronome = item.ShowMetronome,
                        BPM = item.BPM,
                        HasMetronomeSound = item.HasMetronomeSound,
                        ShowAudioPlayer = item.ShowAudioPlayer,
                        PreCountMeasures = item.PreCountMeasures,
                        DateAdded = DateTime.Now
                    };

                    await _databaseService.SaveScoreAsync(score);

                    var scoreTagIds = new List<int>();
                    for (int i = 0; i < item.Tags.Count; i++)
                    {
                        string tagName = item.Tags[i];
                        string tagColor = item.TagColors[i];

                        var tag = existingTags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
                        if (tag == null)
                        {
                            tag = new Models.Tag
                            {
                                Name = tagName,
                                ColorHex = tagColor
                            };
                            await _databaseService.SaveTagAsync(tag);
                            existingTags.Add(tag);
                        }
                        scoreTagIds.Add(tag.Id);
                    }
                    if (scoreTagIds.Any())
                    {
                        await _databaseService.UpdateScoreTagsAsync(score.Id, scoreTagIds);
                    }

                    if (item.AudioFiles != null)
                    {
                        foreach (var audioFileName in item.AudioFiles)
                        {
                            string srcAudioPath = Path.Combine(extractDir, audioFileName);
                            if (File.Exists(srcAudioPath))
                            {
                                string sanitizedAudioName = item.Title.Replace(" ", "_") + "_track" + Path.GetExtension(audioFileName);
                                string localAudioPath = Path.Combine(audioRootDir, sanitizedAudioName);

                                int aCounter = 1;
                                while (File.Exists(localAudioPath))
                                {
                                    var audioNameOnly = Path.GetFileNameWithoutExtension(sanitizedAudioName);
                                    var extension = Path.GetExtension(sanitizedAudioName);
                                    localAudioPath = Path.Combine(audioRootDir, $"{audioNameOnly}_{aCounter}{extension}");
                                    aCounter++;
                                }

                                File.Copy(srcAudioPath, localAudioPath);

                                var scoreAudio = new ScoreAudioFile
                                {
                                    ScoreId = score.Id,
                                    FileName = Path.GetFileNameWithoutExtension(localAudioPath),
                                    FilePath = _settingsService.GetRelativePath(localAudioPath, isAudio: true),
                                    IsSelected = true
                                };
                                await _databaseService.SaveAudioFileAsync(scoreAudio);
                            }
                        }
                    }

                    if (item.Annotations != null)
                    {
                        foreach (var annMeta in item.Annotations)
                        {
                            var ann = new Annotation
                            {
                                ScoreId = score.Id,
                                Type = annMeta.Type,
                                Category = annMeta.Category,
                                Content = annMeta.Content,
                                X = annMeta.X,
                                Y = annMeta.Y,
                                Scale = annMeta.Scale,
                                Color = annMeta.Color,
                                BackgroundColor = annMeta.BackgroundColor,
                                PageNumber = annMeta.PageNumber,
                                DateCreated = DateTime.Now
                            };
                            await _databaseService.SaveAnnotationAsync(ann);
                        }
                    }
                }
            }

            try
            {
                File.Delete(tempZipPath);
                Directory.Delete(extractDir, true);
            }
            catch { }

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                BluetoothOverlay.IsVisible = false;
                await LoadScoresAsync(SearchScoreBar.Text);
                await DisplayAlertAsync("Échange réussi", $"{transferList?.Count ?? 0} partition(s) ont été importées avec succès dans votre bibliothèque.", "OK");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing received zip: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                BluetoothOverlay.IsVisible = false;
                await DisplayAlertAsync("Échec de l'intégration", $"Une erreur est survenue lors de l'intégration des partitions : {ex.Message}", "OK");
            });
        }
    }

    private async Task<byte[]> PackageScoresAsync(List<Models.Score> selectedScores)
    {
        string tempDir = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var transferList = new List<ScoreTransferMetadata>();

            foreach (var score in selectedScores)
            {
                var metadata = new ScoreTransferMetadata
                {
                    Title = score.Title,
                    Type = score.Type,
                    Rotation = score.Rotation,
                    IsRotationSaved = score.IsRotationSaved,
                    ShowMetronome = score.ShowMetronome,
                    BPM = score.BPM,
                    HasMetronomeSound = score.HasMetronomeSound,
                    ShowAudioPlayer = score.ShowAudioPlayer,
                    PreCountMeasures = score.PreCountMeasures
                };

                string absoluteScorePath = _settingsService.GetAbsolutePath(score.FilePath);
                if (File.Exists(absoluteScorePath))
                {
                    string uniqueScoreFileName = Guid.NewGuid().ToString() + Path.GetExtension(absoluteScorePath);
                    string destScorePath = Path.Combine(tempDir, uniqueScoreFileName);
                    File.Copy(absoluteScorePath, destScorePath);
                    metadata.FileName = uniqueScoreFileName;
                }
                else
                {
                    continue;
                }

                if (score.AppliedTags != null)
                {
                    foreach (var tag in score.AppliedTags)
                    {
                        metadata.Tags.Add(tag.Name);
                        metadata.TagColors.Add(tag.ColorHex);
                    }
                }

                if (score.AudioFiles != null)
                {
                    foreach (var af in score.AudioFiles)
                    {
                        string absoluteAudioPath = _settingsService.GetAbsolutePath(af.FilePath, isAudio: true);
                        if (File.Exists(absoluteAudioPath))
                        {
                            string uniqueAudioFileName = Guid.NewGuid().ToString() + Path.GetExtension(absoluteAudioPath);
                            string destAudioPath = Path.Combine(tempDir, uniqueAudioFileName);
                            File.Copy(absoluteAudioPath, destAudioPath);
                            metadata.AudioFiles.Add(uniqueAudioFileName);
                        }
                    }
                }

                var annotations = await _databaseService.GetAnnotationsForScoreAsync(score.Id);
                if (annotations != null)
                {
                    foreach (var ann in annotations)
                    {
                        metadata.Annotations.Add(new AnnotationMetadata
                        {
                            Type = ann.Type,
                            Category = ann.Category,
                            Content = ann.Content,
                            X = ann.X,
                            Y = ann.Y,
                            Scale = ann.Scale,
                            Color = ann.Color,
                            BackgroundColor = ann.BackgroundColor,
                            PageNumber = ann.PageNumber
                        });
                    }
                }

                transferList.Add(metadata);
            }

            string jsonMetadata = JsonSerializer.Serialize(transferList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(tempDir, "metadata.json"), jsonMetadata);

            string zipPath = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid().ToString() + ".zip");
            ZipFile.CreateFromDirectory(tempDir, zipPath);

            byte[] zipData = await File.ReadAllBytesAsync(zipPath);

            try
            {
                File.Delete(zipPath);
                Directory.Delete(tempDir, true);
            }
            catch { }

            return zipData;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error packaging scores: {ex.Message}");
            try { Directory.Delete(tempDir, true); } catch { }
            throw;
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
                Padding = new Thickness(12, 6),
                Margin = new Thickness(0, 0, 8, 8),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Opacity = isSelected ? 1.0 : 0.7
            };

            var label = new Label
            {
                Text = isSelected ? $"✓ {tag.Name}" : tag.Name,
                TextColor = isSelected ? Colors.White : Color.FromArgb("#E0E0E0"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
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
            await Navigation.PushAsync(new ScoreEditPage(_selectedScoreForMenu, _databaseService));
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
    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}
