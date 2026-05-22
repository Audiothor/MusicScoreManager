#pragma warning disable CA1416
#pragma warning disable CA1422

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

#if ANDROID
using Android.Bluetooth;
using Android.Content;
#endif

namespace MusicScoreManager.Services
{
#if ANDROID
    public class BluetoothPermissions : Microsoft.Maui.ApplicationModel.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        {
            get
            {
                var list = new List<(string androidPermission, bool isRuntime)>();

                // Si Android 12 (API 31) ou supérieur, les permissions de proximité sont requises
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
                {
                    list.Add((Android.Manifest.Permission.BluetoothScan, true));
                    list.Add((Android.Manifest.Permission.BluetoothConnect, true));
                    list.Add((Android.Manifest.Permission.BluetoothAdvertise, true));
                }

                // Pour toutes les versions d'Android, la position fine est requise pour le scan classique
                list.Add((Android.Manifest.Permission.AccessFineLocation, true));

                return list.ToArray();
            }
        }
    }
#endif

    public class BluetoothTransferService : IBluetoothTransferService
    {
        public event EventHandler<BluetoothDeviceInfo>? DeviceDiscovered;
        public event EventHandler? ScanFinished;
        public event EventHandler<BluetoothTransferProgressEventArgs>? TransferProgressChanged;

        private const string ServiceName = "MusicScoreManager";
        private static readonly Guid ServiceGuid = Guid.Parse("f3079b76-47b2-4d2d-bebf-5c4a5c0bfcb1");

#if ANDROID
        private static readonly Java.Util.UUID RfcommUuid = Java.Util.UUID.FromString(ServiceGuid.ToString());
        private BluetoothAdapter? _bluetoothAdapter;
        private BluetoothDiscoveryReceiver? _receiver;
        private BluetoothServerSocket? _serverSocket;
        private bool _isListening;

        public BluetoothTransferService()
        {
            var bluetoothManager = Android.App.Application.Context.GetSystemService(Context.BluetoothService) as BluetoothManager;
            _bluetoothAdapter = bluetoothManager?.Adapter;
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var status = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<BluetoothPermissions>();
                if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                {
                    status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<BluetoothPermissions>();
                    if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
                    {
                        return false;
                    }
                }

                if (_bluetoothAdapter == null)
                {
                    return false;
                }

                if (!_bluetoothAdapter.IsEnabled)
                {
                    // Lancer un appel pour activer Bluetooth n'est plus aussi simple sans Activity
                    // On demande à l'utilisateur de l'activer s'il ne l'est pas
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bluetooth init error: {ex.Message}");
                return false;
            }
        }

        public async Task StartScanningAsync()
        {
            if (_bluetoothAdapter == null || !_bluetoothAdapter.IsEnabled)
            {
                throw new InvalidOperationException("Bluetooth n'est pas activé ou indisponible.");
            }

            if (_bluetoothAdapter.IsDiscovering)
            {
                _bluetoothAdapter.CancelDiscovery();
            }

            // Enregistrer le récepteur
            _receiver = new BluetoothDiscoveryReceiver(this);
            var filter = new IntentFilter();
            filter.AddAction(BluetoothDevice.ActionFound);
            filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
            Android.App.Application.Context.RegisterReceiver(_receiver, filter);

            _bluetoothAdapter.StartDiscovery();
        }

        public async Task StopScanningAsync()
        {
            if (_bluetoothAdapter != null && _bluetoothAdapter.IsDiscovering)
            {
                _bluetoothAdapter.CancelDiscovery();
            }

            if (_receiver != null)
            {
                try
                {
                    Android.App.Application.Context.UnregisterReceiver(_receiver);
                }
                catch { }
                _receiver = null;
            }
            await Task.CompletedTask;
        }

        public void OnDeviceDiscovered(BluetoothDevice device)
        {
            string name = device.Name ?? "Appareil Inconnu";
            string address = device.Address ?? string.Empty;

            DeviceDiscovered?.Invoke(this, new BluetoothDeviceInfo
            {
                Name = name,
                Address = address,
                NativeDevice = device
            });
        }

        public void OnScanFinished()
        {
            ScanFinished?.Invoke(this, EventArgs.Empty);
        }

        public async Task StartListeningAsync(Func<string, int, Task<bool>> confirmCallback, Func<List<BluetoothFilePayload>, string, Task> onReceiveComplete)
        {
            if (_bluetoothAdapter == null || !_bluetoothAdapter.IsEnabled)
            {
                throw new InvalidOperationException("Bluetooth n'est pas activé.");
            }

            _isListening = true;
            _serverSocket = _bluetoothAdapter.ListenUsingRfcommWithServiceRecord(ServiceName, RfcommUuid);

            _ = Task.Run(async () =>
            {
                while (_isListening)
                {
                    BluetoothSocket? socket = null;
                    try
                    {
                        if (_serverSocket == null) break;
                        socket = await _serverSocket.AcceptAsync();
                        if (socket == null) continue;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var inputStream = socket.InputStream;
                                var outputStream = socket.OutputStream;
                                if (inputStream == null || outputStream == null) return;

                                // 1. Lire Magic Bytes ("MSMFILES")
                                byte[] magicBytes = new byte[8];
                                await ReadExactAsync(inputStream, magicBytes, 8);
                                string magic = Encoding.UTF8.GetString(magicBytes);
                                if (magic != "MSMFILES")
                                {
                                    socket.Close();
                                    return;
                                }

                                // 2. Nom de l'émetteur
                                byte[] lenBytes = new byte[4];
                                await ReadExactAsync(inputStream, lenBytes, 4);
                                int nameLen = BitConverter.ToInt32(lenBytes, 0);

                                byte[] nameBytes = new byte[nameLen];
                                await ReadExactAsync(inputStream, nameBytes, nameLen);
                                string senderName = Encoding.UTF8.GetString(nameBytes);

                                // 3. Nombre de fichiers
                                byte[] fileCountBytes = new byte[4];
                                await ReadExactAsync(inputStream, fileCountBytes, 4);
                                int fileCount = BitConverter.ToInt32(fileCountBytes, 0);

                                // 4. Demander confirmation à l'utilisateur
                                bool accept = await confirmCallback(senderName, fileCount);

                                // 5. Renvoyer la décision
                                outputStream.WriteByte(accept ? (byte)1 : (byte)0);
                                await outputStream.FlushAsync();

                                if (!accept)
                                {
                                    socket.Close();
                                    return;
                                }

                                // 6. Recevoir les fichiers un par un
                                var receivedFiles = new List<BluetoothFilePayload>();
                                byte[] buffer = new byte[8192];

                                for (int i = 0; i < fileCount; i++)
                                {
                                    // 6a. Lire la taille du nom de fichier
                                    byte[] fNameLenBytes = new byte[4];
                                    await ReadExactAsync(inputStream, fNameLenBytes, 4);
                                    int fNameLen = BitConverter.ToInt32(fNameLenBytes, 0);

                                    // 6b. Lire le nom de fichier
                                    byte[] fNameBytes = new byte[fNameLen];
                                    await ReadExactAsync(inputStream, fNameBytes, fNameLen);
                                    string fileName = Encoding.UTF8.GetString(fNameBytes);

                                    // 6c. Lire la taille du fichier
                                    byte[] fLenBytes = new byte[8];
                                    await ReadExactAsync(inputStream, fLenBytes, 8);
                                    long fileLength = BitConverter.ToInt64(fLenBytes, 0);

                                    // 6d. Recevoir le contenu du fichier
                                    long totalBytesRead = 0;
                                    using var ms = new MemoryStream();

                                    while (totalBytesRead < fileLength)
                                    {
                                        int bytesToRead = (int)Math.Min(buffer.Length, fileLength - totalBytesRead);
                                        int read = await inputStream.ReadAsync(buffer, 0, bytesToRead);
                                        if (read <= 0) break;

                                        await ms.WriteAsync(buffer, 0, read);
                                        totalBytesRead += read;

                                        // Progression globale
                                        double fileProgress = (double)totalBytesRead / fileLength;
                                        double overallProgress = ((double)i + fileProgress) / fileCount;

                                        TransferProgressChanged?.Invoke(this, new BluetoothTransferProgressEventArgs
                                        {
                                            Status = $"Réception de {fileName} ({i + 1}/{fileCount})...",
                                            Progress = overallProgress
                                        });
                                    }

                                    if (totalBytesRead != fileLength)
                                    {
                                        throw new IOException($"Erreur de réception de fichier {fileName} : taille attendue {fileLength}, reçue {totalBytesRead}.");
                                    }

                                    receivedFiles.Add(new BluetoothFilePayload
                                    {
                                        FileName = fileName,
                                        Data = ms.ToArray()
                                    });
                                }

                                // 7. Envoyer l'accusé de réception global (ACK)
                                outputStream.WriteByte((byte)1);
                                await outputStream.FlushAsync();

                                TransferProgressChanged?.Invoke(this, new BluetoothTransferProgressEventArgs
                                {
                                    Status = "Enregistrement...",
                                    Progress = 1.0
                                });

                                await onReceiveComplete(receivedFiles, senderName);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Bluetooth receive thread error: {ex.Message}");
                            }
                            finally
                            {
                                socket?.Close();
                            }
                        });
                    }
                    catch (Exception)
                    {
                        // Socket fermé
                        break;
                    }
                }
            });
            await Task.CompletedTask;
        }

        public async Task StopListeningAsync()
        {
            _isListening = false;
            if (_serverSocket != null)
            {
                try
                {
                    _serverSocket.Close();
                }
                catch { }
                _serverSocket = null;
            }
            await Task.CompletedTask;
        }

        public async Task<bool> SendDataAsync(BluetoothDeviceInfo targetDevice, List<BluetoothFilePayload> files, string senderName)
        {
            if (_bluetoothAdapter == null || targetDevice.NativeDevice is not BluetoothDevice device)
            {
                return false;
            }

            BluetoothSocket? socket = null;
            try
            {
                socket = device.CreateRfcommSocketToServiceRecord(RfcommUuid);
                await socket.ConnectAsync();

                var inputStream = socket.InputStream;
                var outputStream = socket.OutputStream;
                if (inputStream == null || outputStream == null) return false;

                // 1. Envoyer MSMFILES
                byte[] magicBytes = Encoding.UTF8.GetBytes("MSMFILES");
                await outputStream.WriteAsync(magicBytes, 0, magicBytes.Length);

                // 2. Envoyer Nom de l'émetteur
                byte[] nameBytes = Encoding.UTF8.GetBytes(senderName);
                byte[] lenBytes = BitConverter.GetBytes(nameBytes.Length);
                await outputStream.WriteAsync(lenBytes, 0, lenBytes.Length);
                await outputStream.WriteAsync(nameBytes, 0, nameBytes.Length);

                // 3. Envoyer Nombre de fichiers
                byte[] fileCountBytes = BitConverter.GetBytes(files.Count);
                await outputStream.WriteAsync(fileCountBytes, 0, fileCountBytes.Length);
                await outputStream.FlushAsync();

                // 4. Lire la réponse de confirmation
                int response = inputStream.ReadByte();
                if (response != 1)
                {
                    return false; // Refusé par le destinataire
                }

                // 5. Envoyer les fichiers un par un
                int bufferSize = 8192;
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];

                    // 5a. Envoyer Taille du nom de fichier et le Nom
                    byte[] fNameBytes = Encoding.UTF8.GetBytes(file.FileName);
                    byte[] fNameLenBytes = BitConverter.GetBytes(fNameBytes.Length);
                    await outputStream.WriteAsync(fNameLenBytes, 0, fNameLenBytes.Length);
                    await outputStream.WriteAsync(fNameBytes, 0, fNameBytes.Length);

                    // 5b. Envoyer Taille du contenu du fichier
                    byte[] fLenBytes = BitConverter.GetBytes((long)file.Data.Length);
                    await outputStream.WriteAsync(fLenBytes, 0, fLenBytes.Length);
                    await outputStream.FlushAsync();

                    // 5c. Envoyer le contenu
                    int offset = 0;
                    while (offset < file.Data.Length)
                    {
                        int bytesToSend = Math.Min(bufferSize, file.Data.Length - offset);
                        await outputStream.WriteAsync(file.Data, offset, bytesToSend);
                        offset += bytesToSend;

                        // Progression
                        double fileProgress = (double)offset / file.Data.Length;
                        double overallProgress = ((double)i + fileProgress) / files.Count;

                        TransferProgressChanged?.Invoke(this, new BluetoothTransferProgressEventArgs
                        {
                            Status = $"Envoi de {file.FileName} ({i + 1}/{files.Count})...",
                            Progress = overallProgress
                        });
                    }
                    await outputStream.FlushAsync();
                }

                // 6. Attendre l'accusé de réception global (ACK) de la part du récepteur
                TransferProgressChanged?.Invoke(this, new BluetoothTransferProgressEventArgs
                {
                    Status = "En attente de l'accusé de réception...",
                    Progress = 0.99
                });

                int ack = inputStream.ReadByte();
                if (ack == 1)
                {
                    TransferProgressChanged?.Invoke(this, new BluetoothTransferProgressEventArgs
                    {
                        Status = "Terminé !",
                        Progress = 1.0
                    });
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bluetooth send error: {ex.Message}");
                return false;
            }
            finally
            {
                socket?.Close();
            }
        }

        private async Task ReadExactAsync(Stream stream, byte[] buffer, int length)
        {
            int offset = 0;
            while (offset < length)
            {
                int read = await stream.ReadAsync(buffer, offset, length - offset);
                if (read <= 0)
                {
                    throw new EndOfStreamException("Fin de flux inattendue.");
                }
                offset += read;
            }
        }

        private class BluetoothDiscoveryReceiver : BroadcastReceiver
        {
            private readonly BluetoothTransferService _service;

            public BluetoothDiscoveryReceiver(BluetoothTransferService service)
            {
                _service = service;
            }

            public override void OnReceive(Context? context, Intent? intent)
            {
                if (intent == null) return;
                string action = intent.Action ?? string.Empty;

                if (action == BluetoothDevice.ActionFound)
                {
                    var device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
                    if (device != null)
                    {
                        _service.OnDeviceDiscovered(device);
                    }
                }
                else if (action == BluetoothAdapter.ActionDiscoveryFinished)
                {
                    _service.OnScanFinished();
                }
            }
        }
#else
        // Implémentation Stub/Simulation pour Windows, iOS, macOS
        public async Task<bool> InitializeAsync()
        {
            await Task.CompletedTask;
            return false; // Pas supporté hors d'Android pour le moment
        }

        public async Task StartScanningAsync()
        {
            await Task.CompletedTask;
        }

        public async Task StopScanningAsync()
        {
            await Task.CompletedTask;
        }

        public async Task StartListeningAsync(Func<string, int, Task<bool>> confirmCallback, Func<List<BluetoothFilePayload>, string, Task> onReceiveComplete)
        {
            await Task.CompletedTask;
        }

        public async Task StopListeningAsync()
        {
            await Task.CompletedTask;
        }

        public async Task<bool> SendDataAsync(BluetoothDeviceInfo targetDevice, List<BluetoothFilePayload> files, string senderName)
        {
            await Task.CompletedTask;
            return false;
        }
#endif
    }
}
