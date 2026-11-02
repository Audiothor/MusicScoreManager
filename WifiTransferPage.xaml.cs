using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using MusicScoreManager.Services;

namespace MusicScoreManager
{
    public partial class WifiTransferPage : ContentPage
    {
        private readonly IWifiDirectTransferService _wifiService;
        private readonly ExportImportService _exportImportService;
        private readonly DatabaseService _databaseService;
        private bool _isListening = false;

        public WifiTransferPage(IWifiDirectTransferService wifiService, ExportImportService exportImportService, DatabaseService databaseService)
        {
            InitializeComponent();
            _wifiService = wifiService;
            _exportImportService = exportImportService;
            _databaseService = databaseService;

            var loc = LocalizationService.Instance;
            DeviceNameLabel.Text = DeviceInfo.Name ?? loc.GetString("Wifi_Transfer_Device_Default", "Tablette Musique");
            _wifiService.TransferProgressChanged += OnTransferProgressChanged;
        }

        public WifiTransferPage() : this(
            new WifiDirectTransferService(),
            new ExportImportService(new DatabaseService(), new SettingsService()),
            new DatabaseService())
        {
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var loc = LocalizationService.Instance;
            DeviceNameLabel.Text = DeviceInfo.Name ?? loc.GetString("Wifi_Transfer_Device_Default", "Tablette Musique");
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            if (_isListening)
            {
                await _wifiService.StopListeningAsync();
                _isListening = false;
            }
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            if (_isListening)
            {
                await _wifiService.StopListeningAsync();
                _isListening = false;
            }
            await Navigation.PopAsync();
        }

        private async void OnToggleReceiveClicked(object? sender, EventArgs e)
        {
            if (!_isListening)
            {
                await _wifiService.InitializeAsync();
                await _wifiService.StartListeningAsync(ConfirmIncomingTransferAsync, ProcessReceivedFilesAsync);
                _isListening = true;

                var loc = LocalizationService.Instance;
                ToggleReceiveButton.Text = loc.GetString("Wifi_Transfer_Disable_Receive", "🔴 Désactiver la Réception");
                ToggleReceiveButton.BackgroundColor = Color.FromArgb("#E74C3C");
                StatusBadgeLabel.Text = loc.GetString("Wifi_Listening_Visible", "🟢 En écoute (Visible)");
                StatusBadgeLabel.TextColor = Color.FromArgb("#2ECC71");

                await DisplayAlertAsync(
                    loc.GetString("Wifi_Transfer_Receive_Active_Title", "Réception Activée"), 
                    loc.GetString("Wifi_Transfer_Receive_Active_Msg", "Votre tablette est maintenant visible par les autres musiciens en Wi-Fi Direct.\n\nVous recevrez une notification d'acceptation dès qu'un envoi vous parviendra."), 
                    loc.GetString("Common_OK", "OK"));
            }
            else
            {
                await _wifiService.StopListeningAsync();
                _isListening = false;

                var loc = LocalizationService.Instance;
                ToggleReceiveButton.Text = loc.GetString("Wifi_Transfer_Enable_Receive", "🟢 Activer la Réception (Se rendre visible)");
                ToggleReceiveButton.BackgroundColor = Color.FromArgb("#2ECC71");
                StatusBadgeLabel.Text = loc.GetString("Wifi_Idle_Status", "⚫ En veille");
                StatusBadgeLabel.TextColor = Color.FromArgb("#AAAAAA");
            }
        }

        private async Task<bool> ConfirmIncomingTransferAsync(string senderName, int fileCount)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var loc = LocalizationService.Instance;
                return await DisplayAlertAsync(
                    loc.GetString("Wifi_Transfer_Prompt_Title", "📥 Réception Wi-Fi Direct"),
                    string.Format(loc.GetString("Wifi_Transfer_Prompt_Msg", "{0} souhaite vous envoyer {1} élément(s) (partitions, annotations, audios).\n\nAccepter le transfert ?"), senderName, fileCount),
                    loc.GetString("Wifi_Transfer_Accept_Btn", "Accepter"),
                    loc.GetString("Wifi_Transfer_Refuse_Btn", "Refuser")
                );
            });
        }

        private async Task ProcessReceivedFilesAsync(List<WifiFilePayload> files, string senderName)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    bool success = false;
                    var zipFile = files.FirstOrDefault(f => f.FileName.EndsWith(".msmsetlist", StringComparison.OrdinalIgnoreCase) 
                                                         || f.FileName.EndsWith(".msmscore", StringComparison.OrdinalIgnoreCase) 
                                                         || f.FileName.EndsWith(".msmscores", StringComparison.OrdinalIgnoreCase));
                    if (zipFile != null)
                    {
                        string tempPath = Path.Combine(FileSystem.CacheDirectory, zipFile.FileName);
                        await File.WriteAllBytesAsync(tempPath, zipFile.Data);
                        success = await _exportImportService.ImportPackageFromFileAsync(tempPath);
                        try { File.Delete(tempPath); } catch { }
                    }
                    else
                    {
                        success = await _exportImportService.ImportPackageFromWifiPayloadsAsync(files);
                    }

                    var loc = LocalizationService.Instance;
                    if (success)
                    {
                        await DisplayAlertAsync(
                            loc.GetString("Wifi_Transfer_Success_Title", "Succès"), 
                            string.Format(loc.GetString("Wifi_Transfer_Success_Msg", "Réception terminée avec succès depuis {0} !\n\nLes éléments ont été intégrés à votre bibliothèque."), senderName), 
                            loc.GetString("Common_OK", "OK"));
                    }
                    else
                    {
                        await DisplayAlertAsync(
                            loc.GetString("Common_Warning", "Avertissement"), 
                            loc.GetString("Wifi_Transfer_Integration_Warning", "Les données ont été reçues mais une anomalie s'est produite lors de l'intégration."), 
                            loc.GetString("Common_OK", "OK"));
                    }
                }
                catch (Exception ex)
                {
                    var loc = LocalizationService.Instance;
                    await DisplayAlertAsync(
                        loc.GetString("Common_Error", "Erreur"), 
                        $"{loc.GetString("Wifi_Transfer_Import_Error", "Erreur lors de l'importation")} : {ex.Message}", 
                        loc.GetString("Common_OK", "OK"));
                }
            });
        }

        private async void OnDownloadQrClicked(object? sender, EventArgs e)
        {
            var loc = LocalizationService.Instance;
            string url = QrAddressEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                await DisplayAlertAsync(
                    loc.GetString("Wifi_Transfer_Address_Required_Title", "Adresse requise"), 
                    loc.GetString("Wifi_Transfer_Address_Required", "Veuillez saisir ou coller l'adresse affichée sur l'écran émetteur (ex: http://192.168.49.1:8765/msm_package/)."), 
                    loc.GetString("Common_OK", "OK"));
                return;
            }

            ProgressBorder.IsVisible = true;
            bool success = await _wifiService.DownloadFromQrCodeAsync(url, ProcessReceivedFilesAsync);

            if (!success)
            {
                await DisplayAlertAsync(
                    loc.GetString("Wifi_Transfer_Failed", "Échec"), 
                    loc.GetString("Wifi_Transfer_Join_Failed", "Impossible de récupérer le partage. Vérifiez que vous êtes connecté au même réseau ou au point d'accès de l'émetteur."), 
                    loc.GetString("Common_OK", "OK"));
            }
        }

        private void OnTransferProgressChanged(object? sender, WifiTransferProgressEventArgs e)
        {
            ProgressBorder.IsVisible = true;
            ProgressStatusLabel.Text = e.Status;
            TransferProgressBar.Progress = e.Progress;

            if (e.SpeedMBps > 0)
            {
                ProgressSpeedLabel.Text = $"{e.SpeedMBps:F1} Mo/s";
            }

            if (e.TotalBytes > 0)
            {
                double currentMb = e.BytesTransferred / 1024.0 / 1024.0;
                double totalMb = e.TotalBytes / 1024.0 / 1024.0;
                ProgressDetailsLabel.Text = $"{currentMb:F1} / {totalMb:F1} Mo";
            }
        }
    }
}
