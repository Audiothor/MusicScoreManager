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
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new[]
        {
            (Android.Manifest.Permission.BluetoothScan, true),
            (Android.Manifest.Permission.BluetoothConnect, true),
            (Android.Manifest.Permission.BluetoothAdvertise, true),
            (Android.Manifest.Permission.AccessFineLocation, true)
        };
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

        public async Task StartListeningAsync(Func<string, int, Task<bool>> confirmCallback, Func<byte[], string, Task> onReceiveComplete)
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

                                // 1. Lire Magic Bytes ("MSMTRANS")
                                byte[] magicBytes = new byte[8];
                                await ReadExactAsync(inputStream, magicBytes, 8);
                                string magic = Encoding.UTF8.GetString(magicBytes);
                                if (magic != "MSMTRANS")
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

                                // 3. Nombre de partitions
                                byte[] scoreCountBytes = new byte[4];
                                await ReadExactAsync(inputStream, scoreCountBytes, 4);
                                int scoreCount = BitConverter.ToInt32(scoreCountBytes, 0);

                                // 4. Taille du ZIP
                                byte[] zipLenBytes = new byte[8];
                                await ReadExactAsync(inputStream, zipLenBytes, 8);
                                long zipLength = BitConverter.ToInt64(zipLenBytes, 0);

                                // 5. Demander confirmation à l'utilisateur
                                bool accept = await confirmCallback(senderName, scoreCount);

                                // 6. Renvoyer la décision
                                outputStream.WriteByte(accept ? (byte)1 : (byte)0);
                                await outputStream.FlushAsync();

                                if (!accept)
                                {
                                    socket.Close();
                                    return;
                                }

                                // 7. Recevoir le fichier ZIP
                                byte[] buffer = new byte[8192];
                                long totalBytesRead = 0;
                                using var ms = new MemoryStream();

                                while (totalBytesRead < zipLength)
                                {
                                    int bytesToRead = (int)Math.Min(buffer.Length, zipLength - totalBytesRead);
                                    int read = await inputStream.ReadAsync(buffer, 0, bytesToRead);
                                    if (read <= 0) break;

                                    await ms.WriteAsync(buffer, 0, read);
                                    totalBytesRead += read;

                                    double progress = (double)totalBytesRead / zipLength;
                                    TransferProgressChanged?.Invoke(this, new BluetoothTransferProgressEventArgs
                                    {
                                        Status = $"Réception de {scoreCount} partition(s)...",
                                        Progress = progress
                                    });
                                }

                                if (totalBytesRead == zipLength)
                                {
                                    TransferProgressChanged?.Invoke(this, new BluetoothTransferProgressEventArgs
                                    {
                                        Status = "Enregistrement en cours...",
                                        Progress = 1.0
                                    });
                                    await onReceiveComplete(ms.ToArray(), senderName);
                                }
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

        public async Task<bool> SendDataAsync(BluetoothDeviceInfo targetDevice, byte[] data, string senderName, int scoreCount)
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

                // 1. Envoyer MSMTRANS
                byte[] magicBytes = Encoding.UTF8.GetBytes("MSMTRANS");
                await outputStream.WriteAsync(magicBytes, 0, magicBytes.Length);

                // 2. Envoyer Nom de l'émetteur
                byte[] nameBytes = Encoding.UTF8.GetBytes(senderName);
                byte[] lenBytes = BitConverter.GetBytes(nameBytes.Length);
                await outputStream.WriteAsync(lenBytes, 0, lenBytes.Length);
                await outputStream.WriteAsync(nameBytes, 0, nameBytes.Length);

                // 3. Envoyer Nombre de partitions
                byte[] scoreCountBytes = BitConverter.GetBytes(scoreCount);
                await outputStream.WriteAsync(scoreCountBytes, 0, scoreCountBytes.Length);

                // 4. Envoyer Taille ZIP
                byte[] zipLenBytes = BitConverter.GetBytes((long)data.Length);
                await outputStream.WriteAsync(zipLenBytes, 0, zipLenBytes.Length);
                await outputStream.FlushAsync();

                // 5. Lire la réponse de confirmation
                int response = inputStream.ReadByte();
                if (response != 1)
                {
                    return false; // Refusé par le destinataire
                }

                // 6. Envoyer le fichier ZIP
                int bufferSize = 8192;
                int offset = 0;
                while (offset < data.Length)
                {
                    int bytesToSend = Math.Min(bufferSize, data.Length - offset);
                    await outputStream.WriteAsync(data, offset, bytesToSend);
                    offset += bytesToSend;

                    double progress = (double)offset / data.Length;
                    TransferProgressChanged?.Invoke(this, new BluetoothTransferProgressEventArgs
                    {
                        Status = $"Envoi de {scoreCount} partition(s)...",
                        Progress = progress
                    });
                }

                await outputStream.FlushAsync();
                return true;
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

        public async Task StartListeningAsync(Func<string, int, Task<bool>> confirmCallback, Func<byte[], string, Task> onReceiveComplete)
        {
            await Task.CompletedTask;
        }

        public async Task StopListeningAsync()
        {
            await Task.CompletedTask;
        }

        public async Task<bool> SendDataAsync(BluetoothDeviceInfo targetDevice, byte[] data, string senderName, int scoreCount)
        {
            await Task.CompletedTask;
            return false;
        }
#endif
    }
}
