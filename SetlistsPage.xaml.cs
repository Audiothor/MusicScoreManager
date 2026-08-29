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
    private readonly IBluetoothTransferService _bluetoothService;
    private readonly ExportImportService _exportImportService;

    private string _currentSort = "NameAsc";
    private SetlistStatus? _selectedStatusFilter = null;
    private Setlist? _selectedSetlistForAction = null;
    private readonly ObservableCollection<BluetoothDeviceInfo> _discoveredDevices = new();

    public SetlistsPage(DatabaseService databaseService, IBluetoothTransferService bluetoothService, ExportImportService exportImportService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _bluetoothService = bluetoothService;
        _exportImportService = exportImportService;

        _bluetoothService.DeviceDiscovered += OnBluetoothDeviceDiscovered;
        _bluetoothService.ScanFinished += OnBluetoothScanFinished;
        _bluetoothService.TransferProgressChanged += OnBluetoothTransferProgressChanged;
    }

    public SetlistsPage(DatabaseService databaseService) 
        : this(databaseService, new BluetoothTransferService(), new ExportImportService(databaseService, new SettingsService()))
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
        _ = _bluetoothService.StopScanningAsync();
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

    private async void OnSetlistMenuTapped(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Setlist setlist)
        {
            string action = await DisplayActionSheetAsync(setlist.Name, "Annuler", null, 
                "Éditer", "📤 Envoyer (Bluetooth)", "📦 Exporter (.msmsetlist)", "Renommer", "Supprimer");

            if (action == "Éditer")
            {
                await Navigation.PushAsync(new SetlistEditPage(setlist, _databaseService));
            }
            else if (action == "📤 Envoyer (Bluetooth)")
            {
                _selectedSetlistForAction = setlist;
                SetlistOptionsTitle.Text = $"Envoyer '{setlist.Name}'";
                SetlistOptionsOverlay.IsVisible = true;
            }
            else if (action == "📦 Exporter (.msmsetlist)")
            {
                _selectedSetlistForAction = setlist;
                SetlistOptionsTitle.Text = $"Exporter '{setlist.Name}'";
                SetlistOptionsOverlay.IsVisible = true;
            }
            else if (action == "Renommer")
            {
                await RenameSetlistAsync(setlist);
            }
            else if (action == "Supprimer")
            {
                await DeleteSetlistAsync(setlist);
            }
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

    private async void OnOptionsSendBluetoothClicked(object sender, EventArgs e)
    {
        if (_selectedSetlistForAction == null) return;
        SetlistOptionsOverlay.IsVisible = false;

        bool initialized = await _bluetoothService.InitializeAsync();
        if (!initialized)
        {
            await DisplayAlertAsync("Bluetooth", "Veuillez activer le Bluetooth et autoriser les permissions de proximité pour envoyer.", "OK");
            _selectedSetlistForAction = null;
            return;
        }

        _discoveredDevices.Clear();
        BluetoothDevicesListView.ItemsSource = null;
        BluetoothDevicesListView.ItemsSource = _discoveredDevices;

        BluetoothSendingView.IsVisible = true;
        BluetoothTransferProgressView.IsVisible = false;
        BluetoothScanSpinner.IsRunning = true;
        BluetoothOverlay.IsVisible = true;

        await _bluetoothService.StartScanningAsync();
    }

    private void OnBluetoothDeviceDiscovered(object? sender, BluetoothDeviceInfo device)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_discoveredDevices.Any(d => d.Address == device.Address))
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
            BluetoothSendingView.IsVisible = false;
            BluetoothTransferProgressView.IsVisible = true;
            BluetoothTransferStatusLabel.Text = $"{e.Status} ({Math.Round(e.Progress * 100)}%)";
            BluetoothTransferProgressBar.Progress = e.Progress;
        });
    }

    private async void OnBluetoothDeviceSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is not BluetoothDeviceInfo selectedDevice) return;
        BluetoothDevicesListView.SelectedItem = null;

        await _bluetoothService.StopScanningAsync();

        if (_selectedSetlistForAction == null) return;
        var setlist = _selectedSetlistForAction;

        try
        {
            BluetoothSendingView.IsVisible = false;
            BluetoothTransferProgressView.IsVisible = true;
            BluetoothTransferStatusLabel.Text = "Préparation du package Setlist...";
            BluetoothTransferProgressBar.Progress = 0.05;

            var options = new ExportOptions
            {
                IncludeAnnotations = OptAnnotationsCheckBox.IsChecked,
                IncludeAudio = OptAudioCheckBox.IsChecked
            };

            var payloads = await _exportImportService.BuildBluetoothPayloadForSetlistAsync(setlist, options);

            BluetoothTransferStatusLabel.Text = "Connexion et envoi...";
            BluetoothTransferProgressBar.Progress = 0.15;

            string senderName = DeviceInfo.Name ?? "Appareil Mobile";
            bool success = await _bluetoothService.SendDataAsync(selectedDevice, payloads, senderName);

            if (success)
            {
                await DisplayAlertAsync("Succès", $"La Setlist '{setlist.Name}' ({payloads.Count - 1} partition(s)/fichiers) a été envoyée et reçue avec succès !", "OK");
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
            BluetoothOverlay.IsVisible = false;
            _selectedSetlistForAction = null;
        }
    }

    private async void OnBluetoothRescanClicked(object sender, EventArgs e)
    {
        _discoveredDevices.Clear();
        BluetoothScanSpinner.IsRunning = true;
        BluetoothScanSpinner.IsVisible = true;
        await _bluetoothService.StartScanningAsync();
    }

    private async void OnBluetoothCloseOverlayClicked(object sender, EventArgs e)
    {
        await _bluetoothService.StopScanningAsync();
        BluetoothOverlay.IsVisible = false;
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
