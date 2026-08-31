using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace MusicScoreManager.Services
{
    public class WifiDirectTransferService : IWifiDirectTransferService
    {
        private const int DiscoveryPort = 8764;
        private const int DataPort = 8765;
        private static readonly byte[] HeaderMagic = new byte[] { 0x4D, 0x53, 0x4D, 0x57 }; // "MSMW" (Music Score Manager Wifi)

        public event EventHandler<WifiDeviceInfo>? DeviceDiscovered;
        public event EventHandler? ScanFinished;
        public event EventHandler<WifiTransferProgressEventArgs>? TransferProgressChanged;

        private UdpClient? _udpDiscoveryClient;
        private UdpClient? _udpBeaconListener;
        private TcpListener? _tcpDataListener;
        private CancellationTokenSource? _scanCts;
        private CancellationTokenSource? _listenCts;
        private CancellationTokenSource? _groupBroadcastCts;

        private readonly Dictionary<string, WifiDeviceInfo> _discoveredDevices = new();
        private byte[]? _groupBroadcastData;
        private string _groupBroadcastTitle = string.Empty;
        private int _groupClientsServed = 0;

        public async Task<bool> InitializeAsync()
        {
            try
            {
#if ANDROID
                // Vérification et demande des permissions nécessaires sur Android
                var locStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (locStatus != PermissionStatus.Granted)
                {
                    locStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }
#endif
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task StartScanningAsync()
        {
            await StopScanningAsync();
            _discoveredDevices.Clear();
            _scanCts = new CancellationTokenSource();

            try
            {
                _udpDiscoveryClient = new UdpClient();
                _udpDiscoveryClient.EnableBroadcast = true;
                _udpDiscoveryClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

                // 1. Écouter les réponses en arrière-plan
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!_scanCts.Token.IsCancellationRequested)
                        {
                            var result = await _udpDiscoveryClient.ReceiveAsync(_scanCts.Token);
                            string message = Encoding.UTF8.GetString(result.Buffer);

                            if (message.StartsWith("MSM_BEACON_REPLY:") || message.StartsWith("MSM_BEACON:"))
                            {
                                var parts = message.Split(':');
                                if (parts.Length >= 3)
                                {
                                    string deviceName = parts[1];
                                    int port = int.TryParse(parts[2], out int p) ? p : DataPort;
                                    string senderIp = result.RemoteEndPoint.Address.ToString();

                                    // Ne pas se découvrir soi-même
                                    if (!IsLocalAddress(result.RemoteEndPoint.Address))
                                    {
                                        var device = new WifiDeviceInfo
                                        {
                                            Name = deviceName,
                                            Address = senderIp,
                                            IpAddress = senderIp,
                                            Port = port
                                        };

                                        lock (_discoveredDevices)
                                        {
                                            if (!_discoveredDevices.ContainsKey(senderIp))
                                            {
                                                _discoveredDevices[senderIp] = device;
                                                MainThread.BeginInvokeOnMainThread(() =>
                                                {
                                                    DeviceDiscovered?.Invoke(this, device);
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                });

                // 2. Diffuser le message de découverte périodiquement
                _ = Task.Run(async () =>
                {
                    string myName = DeviceInfo.Name ?? "Tablette Musicien";
                    byte[] discoveryBytes = Encoding.UTF8.GetBytes($"MSM_DISCOVERY:{myName}:{DataPort}");
                    var broadcastEp = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);

                    for (int i = 0; i < 6 && !_scanCts.Token.IsCancellationRequested; i++)
                    {
                        try
                        {
                            await _udpDiscoveryClient.SendAsync(discoveryBytes, discoveryBytes.Length, broadcastEp);
                        }
                        catch { }
                        await Task.Delay(1000, _scanCts.Token);
                    }

                    MainThread.BeginInvokeOnMainThread(() => ScanFinished?.Invoke(this, EventArgs.Empty));
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WifiTransfer] Scan error: {ex.Message}");
                ScanFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        public Task StopScanningAsync()
        {
            try
            {
                _scanCts?.Cancel();
                _udpDiscoveryClient?.Close();
                _udpDiscoveryClient?.Dispose();
            }
            catch { }
            finally
            {
                _udpDiscoveryClient = null;
                _scanCts = null;
            }
            return Task.CompletedTask;
        }

        public async Task StartListeningAsync(Func<string, int, Task<bool>> confirmCallback, Func<List<WifiFilePayload>, string, Task> onReceiveComplete)
        {
            await StopListeningAsync();
            _listenCts = new CancellationTokenSource();

            // 1. Répondre aux requêtes de découverte UDP
            try
            {
                _udpBeaconListener = new UdpClient(new IPEndPoint(IPAddress.Any, DiscoveryPort));
                _udpBeaconListener.EnableBroadcast = true;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!_listenCts.Token.IsCancellationRequested)
                        {
                            var result = await _udpBeaconListener.ReceiveAsync(_listenCts.Token);
                            string msg = Encoding.UTF8.GetString(result.Buffer);

                            if (msg.StartsWith("MSM_DISCOVERY:"))
                            {
                                string myName = DeviceInfo.Name ?? "Tablette Musique";
                                byte[] reply = Encoding.UTF8.GetBytes($"MSM_BEACON_REPLY:{myName}:{DataPort}");
                                await _udpBeaconListener.SendAsync(reply, reply.Length, result.RemoteEndPoint);
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WifiTransfer] Beacon listener error: {ex.Message}");
            }

            // 2. Écouter les connexions TCP entrantes de transfert
            try
            {
                _tcpDataListener = new TcpListener(IPAddress.Any, DataPort);
                _tcpDataListener.Start();

                _ = Task.Run(async () =>
                {
                    while (!_listenCts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var client = await _tcpDataListener.AcceptTcpClientAsync(_listenCts.Token);
                            _ = HandleIncomingConnectionAsync(client, confirmCallback, onReceiveComplete);
                        }
                        catch (OperationCanceledException) { break; }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WifiTransfer] TCP listener error: {ex.Message}");
            }
        }

        private async Task HandleIncomingConnectionAsync(TcpClient client, Func<string, int, Task<bool>> confirmCallback, Func<List<WifiFilePayload>, string, Task> onReceiveComplete)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                try
                {
                    // Vérification de l'en-tête Magic
                    byte[] magic = reader.ReadBytes(4);
                    if (!magic.SequenceEqual(HeaderMagic))
                    {
                        writer.Write((byte)0x00); // Rejet
                        return;
                    }

                    string senderName = reader.ReadString();
                    int fileCount = reader.ReadInt32();
                    long totalBytes = reader.ReadInt64();

                    // Demander confirmation à l'utilisateur
                    bool accepted = await confirmCallback(senderName, fileCount);
                    if (!accepted)
                    {
                        writer.Write((byte)0x00); // Rejet
                        return;
                    }

                    // Envoi de l'accord (ACK)
                    writer.Write((byte)0x01);
                    writer.Flush();

                    var files = new List<WifiFilePayload>();
                    long receivedBytes = 0;
                    var stopwatch = Stopwatch.StartNew();

                    for (int i = 0; i < fileCount; i++)
                    {
                        string fileName = reader.ReadString();
                        long fileSize = reader.ReadInt64();
                        byte[] fileData = new byte[fileSize];

                        long fileBytesRead = 0;
                        while (fileBytesRead < fileSize)
                        {
                            int toRead = (int)Math.Min(65536, fileSize - fileBytesRead);
                            int read = await stream.ReadAsync(fileData.AsMemory((int)fileBytesRead, toRead));
                            if (read <= 0) break;

                            fileBytesRead += read;
                            receivedBytes += read;

                            double speed = (receivedBytes / 1024.0 / 1024.0) / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
                            double progress = totalBytes > 0 ? (double)receivedBytes / totalBytes : 0;

                            NotifyProgress($"Réception de {fileName}...", progress, receivedBytes, totalBytes, speed);
                        }

                        files.Add(new WifiFilePayload { FileName = fileName, Data = fileData });
                    }

                    NotifyProgress("Réception terminée !", 1.0, totalBytes, totalBytes, 0);

                    // Traitement de fin
                    await onReceiveComplete(files, senderName);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WifiTransfer] Receive error: {ex.Message}");
                }
            }
        }

        public async Task StopListeningAsync()
        {
            try
            {
                _listenCts?.Cancel();
                _udpBeaconListener?.Close();
                _udpBeaconListener?.Dispose();
                _tcpDataListener?.Stop();
            }
            catch { }
            finally
            {
                _udpBeaconListener = null;
                _tcpDataListener = null;
                _listenCts = null;
            }
            await Task.CompletedTask;
        }

        public async Task<bool> SendDataAsync(WifiDeviceInfo targetDevice, List<WifiFilePayload> files, string senderName)
        {
            if (files == null || files.Count == 0) return false;

            try
            {
                using var client = new TcpClient();
                client.SendTimeout = 10000;
                client.ReceiveTimeout = 10000;

                NotifyProgress($"Connexion à {targetDevice.Name}...", 0.0, 0, 0, 0);
                await client.ConnectAsync(targetDevice.IpAddress, targetDevice.Port);

                using var stream = client.GetStream();
                using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

                // Calcul de la taille totale
                long totalBytes = files.Sum(f => (long)f.Data.Length);

                // En-tête de proposition
                writer.Write(HeaderMagic);
                writer.Write(senderName);
                writer.Write(files.Count);
                writer.Write(totalBytes);
                writer.Flush();

                NotifyProgress("En attente d'acceptation du destinataire...", 0.05, 0, totalBytes, 0);

                // Attente de l'ACK du récepteur
                byte response = reader.ReadByte();
                if (response != 0x01)
                {
                    NotifyProgress("Le destinataire a refusé le transfert.", 0, 0, totalBytes, 0);
                    return false;
                }

                // Streaming des fichiers
                long bytesSent = 0;
                var stopwatch = Stopwatch.StartNew();

                foreach (var file in files)
                {
                    writer.Write(file.FileName);
                    writer.Write((long)file.Data.Length);
                    writer.Flush();

                    int chunkSize = 65536; // 64 Ko
                    int offset = 0;

                    while (offset < file.Data.Length)
                    {
                        int currentChunk = Math.Min(chunkSize, file.Data.Length - offset);
                        await stream.WriteAsync(file.Data.AsMemory(offset, currentChunk));
                        offset += currentChunk;
                        bytesSent += currentChunk;

                        double speed = (bytesSent / 1024.0 / 1024.0) / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
                        double progress = (double)bytesSent / totalBytes;

                        NotifyProgress($"Envoi de {file.FileName}...", progress, bytesSent, totalBytes, speed);
                    }
                    await stream.FlushAsync();
                }

                NotifyProgress("Transfert terminé avec succès !", 1.0, totalBytes, totalBytes, 0);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WifiTransfer] Send error: {ex.Message}");
                NotifyProgress($"Échec du transfert : {ex.Message}", 0, 0, 0, 0);
                return false;
            }
        }

        private TcpListener? _groupTcpServer;

        public Task<WifiGroupShareInfo?> StartGroupBroadcastAsync(List<WifiFilePayload> files, string title, Action<int>? onClientCountChanged = null)
        {
            StopGroupBroadcastAsync();

            if (files == null || files.Count == 0) return Task.FromResult<WifiGroupShareInfo?>(null);

            _groupBroadcastCts = new CancellationTokenSource();
            _groupBroadcastTitle = title;
            _groupClientsServed = 0;

            // Création d'une archive zip / paquet binaire en mémoire
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                bw.Write(HeaderMagic);
                bw.Write(DeviceInfo.Name ?? "Tablette Émettrice");
                bw.Write(files.Count);
                long totalBytes = files.Sum(f => (long)f.Data.Length);
                bw.Write(totalBytes);

                foreach (var file in files)
                {
                    bw.Write(file.FileName);
                    bw.Write((long)file.Data.Length);
                    bw.Write(file.Data);
                }
            }
            _groupBroadcastData = ms.ToArray();

            string localIp = GetLocalIpAddress();
            int port = DataPort;

            try
            {
                _groupTcpServer = new TcpListener(IPAddress.Any, port);
                _groupTcpServer.Start();

                var token = _groupBroadcastCts.Token;

                _ = Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested && _groupTcpServer != null)
                    {
                        try
                        {
                            var client = await _groupTcpServer.AcceptTcpClientAsync(token);
                            _ = Task.Run(async () =>
                            {
                                using (client)
                                {
                                    try
                                    {
                                        var stream = client.GetStream();
                                        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                                        
                                        // Lire la requête HTTP GET
                                        string? requestLine = await reader.ReadLineAsync();
                                        if (string.IsNullOrEmpty(requestLine)) return;

                                        // Consommer les en-têtes HTTP restants
                                        string? header;
                                        while (!string.IsNullOrEmpty(header = await reader.ReadLineAsync())) { }

                                        if (_groupBroadcastData != null)
                                        {
                                            string httpHeader = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {_groupBroadcastData.Length}\r\nConnection: close\r\nAccess-Control-Allow-Origin: *\r\n\r\n";
                                            byte[] headerBytes = Encoding.ASCII.GetBytes(httpHeader);
                                            await stream.WriteAsync(headerBytes, 0, headerBytes.Length, token);
                                            await stream.WriteAsync(_groupBroadcastData, 0, _groupBroadcastData.Length, token);
                                            await stream.FlushAsync(token);

                                            Interlocked.Increment(ref _groupClientsServed);
                                            MainThread.BeginInvokeOnMainThread(() => onClientCountChanged?.Invoke(_groupClientsServed));
                                        }
                                    }
                                    catch { }
                                }
                            });
                        }
                        catch (OperationCanceledException) { break; }
                        catch { break; }
                    }
                });

                var shareInfo = new WifiGroupShareInfo
                {
                    Title = title,
                    HostIp = localIp,
                    Port = port,
                    TotalBytes = _groupBroadcastData.Length,
                    ItemCount = files.Count,
                    QrCodeContent = $"http://{localIp}:{port}/msm_package/"
                };

                return Task.FromResult<WifiGroupShareInfo?>(shareInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WifiTransfer] Group broadcast error: {ex.Message}");
                return Task.FromResult<WifiGroupShareInfo?>(null);
            }
        }

        public Task StopGroupBroadcastAsync()
        {
            try
            {
                _groupBroadcastCts?.Cancel();
                _groupTcpServer?.Stop();
            }
            catch { }
            finally
            {
                _groupTcpServer = null;
                _groupBroadcastCts = null;
                _groupBroadcastData = null;
            }
            return Task.CompletedTask;
        }

        public async Task<bool> DownloadFromQrCodeAsync(string qrCodeContent, Func<List<WifiFilePayload>, string, Task> onReceiveComplete)
        {
            if (string.IsNullOrWhiteSpace(qrCodeContent)) return false;

            try
            {
                NotifyProgress("Connexion au partage de groupe...", 0.1, 0, 0, 0);

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var response = await httpClient.GetAsync(qrCodeContent, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    NotifyProgress("Impossible de joindre le partage.", 0, 0, 0, 0);
                    return false;
                }

                long totalBytes = response.Content.Headers.ContentLength ?? 0;
                using var stream = await response.Content.ReadAsStreamAsync();
                using var ms = new MemoryStream();

                byte[] buffer = new byte[65536];
                long received = 0;
                var stopwatch = Stopwatch.StartNew();

                int read;
                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    ms.Write(buffer, 0, read);
                    received += read;
                    double speed = (received / 1024.0 / 1024.0) / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
                    double progress = totalBytes > 0 ? (double)received / totalBytes : 0.5;
                    NotifyProgress("Téléchargement du paquet...", progress, received, totalBytes, speed);
                }

                ms.Position = 0;
                using var reader = new BinaryReader(ms, Encoding.UTF8);

                byte[] magic = reader.ReadBytes(4);
                if (!magic.SequenceEqual(HeaderMagic))
                {
                    NotifyProgress("Format de paquet invalide.", 0, 0, 0, 0);
                    return false;
                }

                string senderName = reader.ReadString();
                int fileCount = reader.ReadInt32();
                long dataSize = reader.ReadInt64();

                var files = new List<WifiFilePayload>();
                for (int i = 0; i < fileCount; i++)
                {
                    string fileName = reader.ReadString();
                    long fileSize = reader.ReadInt64();
                    byte[] fileData = reader.ReadBytes((int)fileSize);
                    files.Add(new WifiFilePayload { FileName = fileName, Data = fileData });
                }

                NotifyProgress("Importation du paquet...", 0.95, totalBytes, totalBytes, 0);
                await onReceiveComplete(files, senderName);
                NotifyProgress("Téléchargement et importation réussis !", 1.0, totalBytes, totalBytes, 0);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WifiTransfer] QR Download error: {ex.Message}");
                NotifyProgress($"Erreur : {ex.Message}", 0, 0, 0, 0);
                return false;
            }
        }

        private void NotifyProgress(string status, double progress, long bytesTransferred, long totalBytes, double speedMBps)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TransferProgressChanged?.Invoke(this, new WifiTransferProgressEventArgs
                {
                    Status = status,
                    Progress = progress,
                    BytesTransferred = bytesTransferred,
                    TotalBytes = totalBytes,
                    SpeedMBps = speedMBps
                });
            });
        }

        private string GetLocalIpAddress()
        {
            try
            {
                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                
                // 1. Chercher d'abord sur les interfaces Wi-Fi / Hotspot / P2P
                foreach (var iface in interfaces.Where(i => i.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up))
                {
                    string name = iface.Name.ToLowerInvariant();
                    string desc = iface.Description.ToLowerInvariant();
                    if (name.Contains("wlan") || name.Contains("ap") || name.Contains("p2p") || name.Contains("wi-fi") || 
                        desc.Contains("wlan") || desc.Contains("wireless") || desc.Contains("wi-fi"))
                    {
                        var props = iface.GetIPProperties();
                        foreach (var addr in props.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr.Address))
                            {
                                return addr.Address.ToString();
                            }
                        }
                    }
                }

                // 2. Chercher sur toute autre interface active non-loopback
                foreach (var iface in interfaces.Where(i => i.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up))
                {
                    var props = iface.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr.Address))
                        {
                            return addr.Address.ToString();
                        }
                    }
                }

                // 3. Repli DNS
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "192.168.49.1"; // IP par défaut Wi-Fi Direct Group Owner
        }

        private bool IsLocalAddress(IPAddress address)
        {
            try
            {
                if (IPAddress.IsLoopback(address)) return true;

                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                foreach (var iface in interfaces)
                {
                    var props = iface.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.Equals(address)) return true;
                    }
                }

                var host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList.Contains(address);
            }
            catch
            {
                return false;
            }
        }
    }
}
