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

    private string _currentSort = "DateCreatedDesc";
    private bool _hasUserCustomSort = false;
    private SetlistStatus? _selectedStatusFilter = null;
    private Setlist? _selectedSetlistForAction = null;
    private readonly ObservableCollection<WifiDeviceInfo> _discoveredDevices = new();
    private bool _isOpeningSetlist = false;

    public SetlistsPage(DatabaseService databaseService, IWifiDirectTransferService wifiService, ExportImportService exportImportService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _wifiService = wifiService;
        _exportImportService = exportImportService;

        _currentSort = Preferences.Default.Get("DefaultSetlistSort", "DateCreatedDesc");

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
        SetlistLoadingOverlay.IsVisible = false;
        _isOpeningSetlist = false;

        if (!_hasUserCustomSort)
        {
            _currentSort = Preferences.Default.Get("DefaultSetlistSort", "DateCreatedDesc");
        }
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
        var loc = LocalizationService.Instance;
        StatusFiltersStack.Children.Add(CreateStatusFilterChip(loc.GetString("Common_All", "Tous"), null));

        foreach (SetlistStatus status in Enum.GetValues(typeof(SetlistStatus)))
        {
            string label = status switch
            {
                SetlistStatus.Upcoming => loc.GetString("Setlist_Status_Upcoming", "À venir"),
                SetlistStatus.Active => loc.GetString("Setlist_Status_Active", "En cours"),
                SetlistStatus.Done => loc.GetString("Setlist_Status_Done", "Terminée"),
                _ => status.ToString()
            };
            StatusFiltersStack.Children.Add(CreateStatusFilterChip(label, status));
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

        bool doneAtBottom = Preferences.Default.Get("SetlistsDoneAtBottom", true);

        IOrderedEnumerable<Setlist> ordered;
        if (doneAtBottom)
        {
            ordered = setlists.OrderBy(s => s.Status == SetlistStatus.Done ? 1 : 0);
            ordered = _currentSort switch
            {
                "DateCreatedAsc" or "DateAsc" => ordered.ThenBy(s => s.DateCreated),
                "DateCreatedDesc" or "DateDesc" => ordered.ThenByDescending(s => s.DateCreated),
                "ConcertDateAsc" => ordered.ThenBy(s => s.ConcertDate.HasValue ? 0 : 1).ThenBy(s => s.ConcertDate).ThenBy(s => s.ConcertTime),
                "ConcertDateDesc" => ordered.ThenBy(s => s.ConcertDate.HasValue ? 0 : 1).ThenByDescending(s => s.ConcertDate).ThenByDescending(s => s.ConcertTime),
                "NameDesc" => ordered.ThenByDescending(s => s.Name, StringComparer.CurrentCultureIgnoreCase),
                "Status" => ordered.ThenBy(s => s.Status).ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase),
                _ => ordered.ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase) // "NameAsc"
            };
        }
        else
        {
            ordered = _currentSort switch
            {
                "DateCreatedAsc" or "DateAsc" => setlists.OrderBy(s => s.DateCreated),
                "DateCreatedDesc" or "DateDesc" => setlists.OrderByDescending(s => s.DateCreated),
                "ConcertDateAsc" => setlists.OrderBy(s => s.ConcertDate.HasValue ? 0 : 1).ThenBy(s => s.ConcertDate).ThenBy(s => s.ConcertTime),
                "ConcertDateDesc" => setlists.OrderBy(s => s.ConcertDate.HasValue ? 0 : 1).ThenByDescending(s => s.ConcertDate).ThenByDescending(s => s.ConcertTime),
                "NameDesc" => setlists.OrderByDescending(s => s.Name, StringComparer.CurrentCultureIgnoreCase),
                "Status" => setlists.OrderBy(s => s.Status).ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase),
                _ => setlists.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase) // "NameAsc"
            };
        }

        SetlistsCollectionView.ItemsSource = ordered.ToList();
    }

    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadSetlistsAsync(e.NewTextValue);
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        string cancel = loc.GetString("Common_Cancel", "Annuler");
        string title = loc.GetString("Sort_Dialog_Title", "Trier par");

        string optDateDesc = loc.GetString("Setlist_Sort_DateCreatedDesc", "Date de création (Récent)");
        string optDateAsc = loc.GetString("Setlist_Sort_DateCreatedAsc", "Date de création (Ancien)");
        string optConcertAsc = loc.GetString("Setlist_Sort_ConcertDateAsc", "Date du concert (Prochain)");
        string optConcertDesc = loc.GetString("Setlist_Sort_ConcertDateDesc", "Date du concert (Lointain)");
        string optNameAsc = loc.GetString("Setlist_Sort_NameAsc", "Nom (A-Z)");
        string optNameDesc = loc.GetString("Setlist_Sort_NameDesc", "Nom (Z-A)");
        string optStatus = loc.GetString("Setlist_Sort_Status", "Statut");

        string action = await DisplayActionSheetAsync(title, cancel, null, 
            optDateDesc, optDateAsc, optConcertAsc, optConcertDesc, optNameAsc, optNameDesc, optStatus);

        if (action == optDateDesc) { _currentSort = "DateCreatedDesc"; _hasUserCustomSort = true; }
        else if (action == optDateAsc) { _currentSort = "DateCreatedAsc"; _hasUserCustomSort = true; }
        else if (action == optConcertAsc) { _currentSort = "ConcertDateAsc"; _hasUserCustomSort = true; }
        else if (action == optConcertDesc) { _currentSort = "ConcertDateDesc"; _hasUserCustomSort = true; }
        else if (action == optNameAsc) { _currentSort = "NameAsc"; _hasUserCustomSort = true; }
        else if (action == optNameDesc) { _currentSort = "NameDesc"; _hasUserCustomSort = true; }
        else if (action == optStatus) { _currentSort = "Status"; _hasUserCustomSort = true; }

        if (!string.IsNullOrEmpty(action) && action != cancel)
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
            SetlistMenuSubtitleLabel.Text = $"{statusStr} • {setlist.SubtitleInfo}";
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
            await StartSetlistAsync(_selectedSetlistForAction);
        }
    }

    private async Task StartSetlistAsync(Setlist setlist)
    {
        if (_isOpeningSetlist) return;
        _isOpeningSetlist = true;

        try
        {
            SetlistLoadingOverlay.IsVisible = true;
            await Task.Yield();

            var scores = await _databaseService.GetScoresForSetlistAsync(setlist.Id);
            if (scores == null || !scores.Any())
            {
                SetlistLoadingOverlay.IsVisible = false;
                await DisplayAlertAsync("Setlist vide", $"Aucune partition n'est actuellement assignée à la setlist '{setlist.Name}'.", "OK");
                return;
            }

            var firstScore = scores[0];
            if (firstScore.IsFileMissing)
            {
                SetlistLoadingOverlay.IsVisible = false;
                await DisplayAlertAsync("Fichier manquant", $"Le fichier de la première partition '{firstScore.Title}' est introuvable. Veuillez vérifier son emplacement.", "OK");
                return;
            }

            await Navigation.PushAsync(new ViewerPage(firstScore, scores, 0, setlist.IsContinuousReading, setlist));
        }
        catch (Exception ex)
        {
            SetlistLoadingOverlay.IsVisible = false;
            System.Diagnostics.Debug.WriteLine($"[SetlistsPage] Erreur ouverture setlist : {ex.Message}");
        }
        finally
        {
            _isOpeningSetlist = false;
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
            string template = LocalizationService.Instance.GetString("Setlist_Options_Title_Send");
            SetlistOptionsTitle.Text = string.Format(template, _selectedSetlistForAction.Name);
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
            string template = LocalizationService.Instance.GetString("Setlist_Options_Title_Export");
            SetlistOptionsTitle.Text = string.Format(template, _selectedSetlistForAction.Name);
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
            string copySuffix = LocalizationService.Instance.GetString("Setlist_Duplicate_Copy_Suffix");
            string defaultName = $"{setlist.Name}{copySuffix}";
            string promptTitle = LocalizationService.Instance.GetString("Setlist_Duplicate_Title");
            string promptMsg = LocalizationService.Instance.GetString("Setlist_Duplicate_Prompt");
            string okText = LocalizationService.Instance.GetString("Common_OK");
            string cancelText = LocalizationService.Instance.GetString("Common_Cancel");
            string result = await DisplayPromptAsync(promptTitle, promptMsg, okText, cancelText, initialValue: defaultName);
            if (!string.IsNullOrWhiteSpace(result))
            {
                var duplicated = await _databaseService.DuplicateSetlistAsync(setlist.Id, result.Trim());
                await LoadSetlistsAsync(SearchSetlistBar.Text);
                if (duplicated != null)
                {
                    string successTitle = LocalizationService.Instance.GetString("Setlist_Duplicate_Success_Title");
                    string successMsg = string.Format(LocalizationService.Instance.GetString("Setlist_Duplicate_Success_Msg"), duplicated.Name);
                    await DisplayAlertAsync(successTitle, successMsg, okText);
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
            string successTitle = LocalizationService.Instance.GetString("Setlist_Export_Success_Title");
            string successMsg = $"{LocalizationService.Instance.GetString("Setlist_Export_Success_Msg")}\n\n{exportedPath}";
            await DisplayAlertAsync(successTitle, successMsg, LocalizationService.Instance.GetString("Common_OK"));
        }
        catch (Exception ex)
        {
            string errorTitle = LocalizationService.Instance.GetString("Setlist_Export_Error_Title");
            string errorMsg = string.Format(LocalizationService.Instance.GetString("Setlist_Export_Error_Msg"), ex.Message);
            await DisplayAlertAsync(errorTitle, errorMsg, LocalizationService.Instance.GetString("Common_OK"));
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
            string unavailTitle = LocalizationService.Instance.GetString("Wifi_Setlist_Unavailable_Title");
            string unavailMsg = LocalizationService.Instance.GetString("Wifi_Setlist_Unavailable_Msg");
            await DisplayAlertAsync(unavailTitle, unavailMsg, LocalizationService.Instance.GetString("Common_OK"));
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
            WifiTransferStatusLabel.Text = LocalizationService.Instance.GetString("Wifi_Setlist_Preparing");
            WifiTransferProgressBar.Progress = 0.05;

            var options = new ExportOptions
            {
                IncludeAnnotations = OptAnnotationsCheckBox.IsChecked,
                IncludeAudio = OptAudioCheckBox.IsChecked
            };

            var payloads = await _exportImportService.CreateWifiPayloadsForSetlistAsync(setlist, options);

            string connTemplate = LocalizationService.Instance.GetString("Wifi_Setlist_Connecting");
            WifiTransferStatusLabel.Text = string.Format(connTemplate, selectedDevice.Name);
            WifiTransferProgressBar.Progress = 0.15;

            string senderName = DeviceInfo.Name ?? "Tablette Musique";
            bool success = await _wifiService.SendDataAsync(selectedDevice, payloads, senderName);

            if (success)
            {
                string successTitle = LocalizationService.Instance.GetString("Wifi_Setlist_Success_Title");
                string successMsg = LocalizationService.Instance.GetString("Wifi_Setlist_Success_Msg");
                await DisplayAlertAsync(successTitle, successMsg, LocalizationService.Instance.GetString("Common_OK"));
            }
            else
            {
                string failedTitle = LocalizationService.Instance.GetString("Wifi_Setlist_Failed_Title");
                string failedMsg = string.Format(LocalizationService.Instance.GetString("Wifi_Setlist_Failed_Msg"), selectedDevice.Name);
                await DisplayAlertAsync(failedTitle, failedMsg, LocalizationService.Instance.GetString("Common_OK"));
            }
        }
        catch (Exception ex)
        {
            string errorTitle = LocalizationService.Instance.GetString("Wifi_Setlist_Error_Title");
            string errorMsg = string.Format(LocalizationService.Instance.GetString("Wifi_Setlist_Error_Msg"), ex.Message);
            await DisplayAlertAsync(errorTitle, errorMsg, LocalizationService.Instance.GetString("Common_OK"));
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
            string connectedTemplate = LocalizationService.Instance.GetString("Wifi_Setlist_Qr_Connected");
            QrGroupInfoLabel.Text = $"🟢 {string.Format(connectedTemplate, clientsCount)}";
        });

        if (shareInfo != null)
        {
            QrCodeImageView.Source = QrCodeGenerator.GenerateQrCodeImageSource(shareInfo.QrCodeContent, 220);
            QrGroupUrlLabel.Text = shareInfo.QrCodeContent;
            QrGroupInfoLabel.Text = $"🟢 {LocalizationService.Instance.GetString("Wifi_Setlist_Qr_Waiting_Zero")}";
            ShowWifiView("GroupQr");
        }
        else
        {
            string errorTitle = LocalizationService.Instance.GetString("Wifi_Setlist_Error_Title");
            string errorMsg = string.Format(LocalizationService.Instance.GetString("Wifi_Setlist_Qr_Start_Error"), "");
            await DisplayAlertAsync(errorTitle, errorMsg, LocalizationService.Instance.GetString("Common_OK"));
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
        string promptTitle = LocalizationService.Instance.GetString("Common_Rename");
        string promptMsg = LocalizationService.Instance.GetString("Setlist_Rename_Prompt");
        string okText = LocalizationService.Instance.GetString("Common_OK");
        string cancelText = LocalizationService.Instance.GetString("Common_Cancel");
        string result = await DisplayPromptAsync(promptTitle, promptMsg, okText, cancelText, initialValue: setlist.Name);
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
            await StartSetlistAsync(selectedSetlist);
        }
    }
}
