using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class SetlistsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly IWifiDirectTransferService _wifiService;
    private readonly ExportImportService _exportImportService;

    private string _currentSort = "NameAsc";
    private SetlistStatus? _selectedStatusFilter = null;
    private Setlist? _selectedSetlistForAction = null;
    private readonly ObservableCollection<WifiDeviceInfo> _discoveredDevices = new();

    public SetlistsPage(DatabaseService databaseService, IWifiDirectTransferService wifiService, ExportImportService exportImportService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _wifiService = wifiService;
        _exportImportService = exportImportService;

        _wifiService.DeviceDiscovered += OnWifiDeviceDiscovered;
        _wifiService.ScanFinished += OnWifiScanFinished;
        _wifiService.TransferProgressChanged += OnWifiTransferProgressChanged;
    }

    public SetlistsPage(DatabaseService databaseService) 
        : this(databaseService, new WifiDirectTransferService(), new ExportImportService(databaseService, new SettingsService()))
    {
    }

    public SetlistsPage() : this(new DatabaseService())
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadStatusFilters();
        await LoadSetlistsAsync();

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () =>
        {
            SearchSetlistBar?.Unfocus();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ = _wifiService.StopScanningAsync();
        _ = _wifiService.StopGroupBroadcastAsync();
    }

    private void LoadStatusFilters()
    {
        StatusFiltersStack.Children.Clear();
        StatusFiltersStack.Children.Add(CreateStatusFilterChip("Tous", null));

        foreach (SetlistStatus status in Enum.GetValues(typeof(SetlistStatus)))
        {
            StatusFiltersStack.Children.Add(CreateStatusFilterChip(status.ToString(), status));
        }
    }

    private View CreateStatusFilterChip(string text, SetlistStatus? status)
    {
        bool isSelected = _selectedStatusFilter == status;
        string colorHex = status switch
        {
            SetlistStatus.Upcoming => "#007ACC",
            SetlistStatus.Active => "#28A745",
            SetlistStatus.Done => "#6C757D",
            _ => "#333333"
        };

        var border = new Border
        {
            BackgroundColor = isSelected ? Color.FromArgb(colorHex) : Color.FromArgb("#1E1E1E"),
            Stroke = Color.FromArgb(colorHex),
            StrokeThickness = 1,
            Padding = new Thickness(15, 5),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 }
        };

        border.Content = new Label
        {
            Text = text,
            TextColor = isSelected ? Colors.White : Color.FromArgb(colorHex),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            _selectedStatusFilter = status;
            LoadStatusFilters();
            await LoadSetlistsAsync(SearchSetlistBar.Text);
        };
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private async Task LoadSetlistsAsync(string query = "")
    {
        var setlists = await _databaseService.GetSetlistsAsync();
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            setlists = setlists.Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (_selectedStatusFilter.HasValue)
        {
            setlists = setlists.Where(s => s.Status == _selectedStatusFilter.Value).ToList();
        }

        if (_currentSort == "NameAsc") setlists = setlists.OrderBy(s => s.Name).ToList();
        else if (_currentSort == "NameDesc") setlists = setlists.OrderByDescending(s => s.Name).ToList();
        else if (_currentSort == "DateAsc") setlists = setlists.OrderBy(s => s.DateCreated).ToList();
        else if (_currentSort == "DateDesc") setlists = setlists.OrderByDescending(s => s.DateCreated).ToList();
        else if (_currentSort == "Status") setlists = setlists.OrderBy(s => s.Status).ToList();

        SetlistsCollectionView.ItemsSource = setlists;
    }

    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadSetlistsAsync(e.NewTextValue);
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync("Trier par", "Annuler", null, 
            "Nom (A-Z)", "Nom (Z-A)", "Date de création (Récent)", "Date de création (Ancien)", "Statut");

        if (action == "Nom (A-Z)") _currentSort = "NameAsc";
        else if (action == "Nom (Z-A)") _currentSort = "NameDesc";
        else if (action == "Date de création (Récent)") _currentSort = "DateDesc";
        else if (action == "Date de création (Ancien)") _currentSort = "DateAsc";
        else if (action == "Statut") _currentSort = "Status";

        if (action != "Annuler")
            await LoadSetlistsAsync(SearchSetlistBar.Text);
    }

    private async void OnAddSetlistClicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("Nouvelle Setlist", "Entrez le nom de la Setlist :");
        if (!string.IsNullOrWhiteSpace(result))
        {
            var newSetlist = new Setlist { Name = result.Trim(), DateCreated = DateTime.Now };
            await _databaseService.SaveSetlistAsync(newSetlist);
            await Navigation.PushAsync(new SetlistEditPage(newSetlist, _databaseService));
        }
    }

    private void OnSetlistMenuTapped(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Setlist setlist)
        {
            _selectedSetlistForAction = setlist;
            SetlistMenuTitleLabel.Text = setlist.Name;
            string statusStr = setlist.StatusText;
            string date = setlist.DateCreated != default ? setlist.DateCreated.ToString("dd/MM/yyyy") : "";
            SetlistMenuSubtitleLabel.Text = $"{statusStr} • Créée le {date}";
            SetlistMenuOverlay.IsVisible = true;
        }
    }

    private void OnMenuCancelClicked(object sender, EventArgs e)
    {
        SetlistMenuOverlay.IsVisible = false;
        _selectedSetlistForAction = null;
    }

    private async void OnMenuOpenSetlistClicked(object sender, EventArgs e)
    {
        SetlistMenuOverlay.IsVisible = false;
        if (_selectedSetlistForAction != null)
        {
            await Navigation.PushAsync(new SetlistEditPage(_selectedSetlistForAction, _databaseService));
        }
    }

    private async void OnMenuEditSetlistClicked(object sender, EventArgs e)
    {
        SetlistMenuOverlay.IsVisible = false;
        if (_selectedSetlistForAction != null)
        {
            await Navigation.PushAsync(new SetlistEditPage(_selectedSetlistForAction, _databaseService));
        }
    }

    private void OnMenuSendWifiTriggerClicked(object sender, EventArgs e)
    {
        SetlistMenuOverlay.IsVisible = false;
        if (_selectedSetlistForAction != null)
        {
            SetlistOptionsTitle.Text = $"Envoyer '{_selectedSetlistForAction.Name}'";
            OptSendWifiActionButton.IsVisible = true;
            OptExportFileActionButton.IsVisible = false;
            SetlistOptionsOverlay.IsVisible = true;
        }
    }

    private void OnMenuExportSetlistTriggerClicked(object sender, EventArgs e)
    {
        SetlistMenuOverlay.IsVisible = false;
        if (_selectedSetlistForAction != null)
        {
            SetlistOptionsTitle.Text = $"Exporter '{_selectedSetlistForAction.Name}'";
            OptSendWifiActionButton.IsVisible = false;
            OptExportFileActionButton.IsVisible = true;
            SetlistOptionsOverlay.IsVisible = true;
        }
    }

    private async void OnMenuDuplicateSetlistClicked(object sender, EventArgs e)
    {
        SetlistMenuOverlay.IsVisible = false;
        if (_selectedSetlistForAction != null)
        {
            var setlist = _selectedSetlistForAction;
            string defaultName = $"{setlist.Name} (Copie)";
            string result = await DisplayPromptAsync("Dupliquer", "Entrez le nom de la nouvelle setlist :", initialValue: defaultName);
            if (!string.IsNullOrWhiteSpace(result))
            {
                var duplicated = await _databaseService.DuplicateSetlistAsync(setlist.Id, result.Trim());
                await LoadSetlistsAsync(SearchSetlistBar.Text);
                if (duplicated != null)
                {
                    await DisplayAlertAsync("Setlist dupliquée", $"La setlist '{duplicated.Name}' a été créée avec succès.", "OK");
                }
            }
        }
    }

    private async void OnMenuRenameSetlistClicked(object sender, EventArgs e)
    {
        SetlistMenuOverlay.IsVisible = false;
        if (_selectedSetlistForAction != null)
        {
            var setlist = _selectedSetlistForAction;
            await RenameSetlistAsync(setlist);
        }
    }

    private async void OnMenuDeleteSetlistClicked(object sender, EventArgs e)
    {
        SetlistMenuOverlay.IsVisible = false;
        if (_selectedSetlistForAction != null)
        {
            var setlist = _selectedSetlistForAction;
            await DeleteSetlistAsync(setlist);
        }
    }

    private void OnOptionsCancelClicked(object sender, EventArgs e)
    {
        SetlistOptionsOverlay.IsVisible = false;
        _selectedSetlistForAction = null;
    }

    private async void OnOptionsExportFileClicked(object sender, EventArgs e)
    {
        if (_selectedSetlistForAction == null) return;
        var setlist = _selectedSetlistForAction;
        SetlistOptionsOverlay.IsVisible = false;

        try
        {
            var options = new ExportOptions
            {
                IncludeAnnotations = OptAnnotationsCheckBox.IsChecked,
                IncludeAudio = OptAudioCheckBox.IsChecked
            };

            string exportedPath = await _exportImportService.ExportSetlistToFileAsync(setlist, options);
            await DisplayAlertAsync("Export réussi", $"La Setlist '{setlist.Name}' a été exportée avec succès dans :\n\n{exportedPath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur d'export", $"Impossible d'exporter la setlist : {ex.Message}", "OK");
        }
        finally
        {
            _selectedSetlistForAction = null;
        }
    }

    private async void OnOptionsSendWifiClicked(object sender, EventArgs e)
    {
        if (_selectedSetlistForAction == null) return;
        SetlistOptionsOverlay.IsVisible = false;

        _discoveredDevices.Clear();
        WifiDevicesListView.ItemsSource = _discoveredDevices;

        ShowWifiView("Sending");
        WifiScanSpinner.IsRunning = true;
        WifiScanSpinner.IsVisible = true;
        WifiOverlay.IsVisible = true;

        bool initialized = await _wifiService.InitializeAsync();
        if (!initialized)
        {
            await DisplayAlertAsync("Wi-Fi indisponible", "Veuillez activer le Wi-Fi sur votre appareil et accorder les permissions.", "OK");
            WifiOverlay.IsVisible = false;
            _selectedSetlistForAction = null;
            return;
        }

        await _wifiService.StartScanningAsync();
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

    private async void OnSendSetlistToDeviceButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is WifiDeviceInfo device)
        {
            await SendSetlistToDeviceAsync(device);
        }
    }

    private async void OnWifiDeviceSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is not WifiDeviceInfo selectedDevice) return;
        WifiDevicesListView.SelectedItem = null;
        await SendSetlistToDeviceAsync(selectedDevice);
    }

    private async Task SendSetlistToDeviceAsync(WifiDeviceInfo selectedDevice)
    {
        await _wifiService.StopScanningAsync();

        if (_selectedSetlistForAction == null) return;
        var setlist = _selectedSetlistForAction;

        try
        {
            ShowWifiView("Progress");
            WifiTransferStatusLabel.Text = "Préparation du paquet Setlist...";
            WifiTransferProgressBar.Progress = 0.05;

            var options = new ExportOptions
            {
                IncludeAnnotations = OptAnnotationsCheckBox.IsChecked,
                IncludeAudio = OptAudioCheckBox.IsChecked
            };

            var payloads = await _exportImportService.CreateWifiPayloadsForSetlistAsync(setlist, options);

            WifiTransferStatusLabel.Text = $"Connexion à {selectedDevice.Name}...";
            WifiTransferProgressBar.Progress = 0.15;

            string senderName = DeviceInfo.Name ?? "Tablette Musique";
            bool success = await _wifiService.SendDataAsync(selectedDevice, payloads, senderName);

            if (success)
            {
                await DisplayAlertAsync("Succès", $"La Setlist '{setlist.Name}' ({payloads.Count - 1} partition(s)/fichiers) a été envoyée avec succès via Wi-Fi Direct !", "OK");
            }
            else
            {
                await DisplayAlertAsync("Échec", "Le transfert de la Setlist a échoué ou a été refusé par le destinataire.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Erreur lors du transfert : {ex.Message}", "OK");
        }
        finally
        {
            WifiOverlay.IsVisible = false;
            _selectedSetlistForAction = null;
        }
    }

    private async void OnToggleGroupShareClicked(object sender, EventArgs e)
    {
        if (_selectedSetlistForAction == null) return;
        var setlist = _selectedSetlistForAction;

        var options = new ExportOptions
        {
            IncludeAnnotations = OptAnnotationsCheckBox.IsChecked,
            IncludeAudio = OptAudioCheckBox.IsChecked
        };

        var payloads = await _exportImportService.CreateWifiPayloadsForSetlistAsync(setlist, options);
        string title = $"Setlist : {setlist.Name}";

        var shareInfo = await _wifiService.StartGroupBroadcastAsync(payloads, title, (clientsCount) =>
        {
            QrGroupInfoLabel.Text = $"🟢 {clientsCount} musicien(s) ont téléchargé la setlist !";
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
            await DisplayAlertAsync("Erreur", "Impossible de démarrer la diffusion de groupe.", "OK");
        }
    }

    private async void OnStopGroupShareClicked(object sender, EventArgs e)
    {
        await _wifiService.StopGroupBroadcastAsync();
        ShowWifiView("Sending");
    }

    private void ShowWifiView(string viewName)
    {
        WifiSendingView.IsVisible = (viewName == "Sending");
        WifiGroupQrView.IsVisible = (viewName == "GroupQr");
        WifiTransferProgressView.IsVisible = (viewName == "Progress");
    }

    private async void OnWifiRescanClicked(object sender, EventArgs e)
    {
        _discoveredDevices.Clear();
        WifiScanSpinner.IsRunning = true;
        WifiScanSpinner.IsVisible = true;
        await _wifiService.StartScanningAsync();
    }

    private async void OnWifiCloseOverlayClicked(object sender, EventArgs e)
    {
        await _wifiService.StopScanningAsync();
        await _wifiService.StopGroupBroadcastAsync();
        WifiOverlay.IsVisible = false;
        _selectedSetlistForAction = null;
    }

    private async Task RenameSetlistAsync(Setlist setlist)
    {
        string result = await DisplayPromptAsync("Renommer", "Entrez le nouveau nom :", initialValue: setlist.Name);
        if (!string.IsNullOrWhiteSpace(result))
        {
            setlist.Name = result.Trim();
            await _databaseService.SaveSetlistAsync(setlist);
            await LoadSetlistsAsync(SearchSetlistBar.Text);
        }
    }

    private async Task DeleteSetlistAsync(Setlist setlist)
    {
        bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer '{setlist.Name}' ?", "Oui", "Non");
        if (answer)
        {
            await _databaseService.DeleteSetlistAsync(setlist);
            await LoadSetlistsAsync(SearchSetlistBar.Text);
        }
    }

    private async void OnRenameSetlistInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Setlist setlist)
        {
            await RenameSetlistAsync(setlist);
        }
    }

    private async void OnDeleteSetlistInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Setlist setlist)
        {
            await DeleteSetlistAsync(setlist);
        }
    }

    private async void OnSetlistTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers[0] is TapGestureRecognizer tap && tap.CommandParameter is Setlist selectedSetlist)
        {
            await Navigation.PushAsync(new SetlistEditPage(selectedSetlist, _databaseService));
        }
    }
}
