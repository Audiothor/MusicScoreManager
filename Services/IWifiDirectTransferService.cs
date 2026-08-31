using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusicScoreManager.Models;

namespace MusicScoreManager.Services
{
    public class WifiDeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 8765;
        public bool IsGroupOwner { get; set; }
        public object? NativeDevice { get; set; }
    }

    public class WifiTransferProgressEventArgs : EventArgs
    {
        public string Status { get; set; } = string.Empty;
        public double Progress { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public double SpeedMBps { get; set; }
    }

    public class WifiFilePayload
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public class WifiGroupShareInfo
    {
        public string Title { get; set; } = string.Empty;
        public string HostIp { get; set; } = string.Empty;
        public int Port { get; set; } = 8765;
        public string Ssid { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
        public int ItemCount { get; set; }
        public string QrCodeContent { get; set; } = string.Empty;
    }

    public class AnnotationMetadata
    {
        public int PageNumber { get; set; }
        public AnnotationType Type { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Scale { get; set; } = 1.0;
        public string Color { get; set; } = "#FF0000";
        public string BackgroundColor { get; set; } = "Transparent";
    }

    public class ScoreTransferMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public ScoreType Type { get; set; }
        public int Rotation { get; set; }
        public bool IsRotationSaved { get; set; }
        public bool ShowMetronome { get; set; }
        public int BPM { get; set; }
        public bool HasMetronomeSound { get; set; }
        public bool ShowAudioPlayer { get; set; }
        public int PreCountMeasures { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<string> TagColors { get; set; } = new();
        public List<AnnotationMetadata> Annotations { get; set; } = new();
        public List<string> AudioFiles { get; set; } = new();
    }

    public class SetlistTransferMetadata
    {
        public string Name { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public DateTime? ConcertDate { get; set; }
        public TimeSpan? ConcertTime { get; set; }
        public SetlistStatus Status { get; set; }
        public bool IsLocked { get; set; }
        public bool IsContinuousReading { get; set; }
        public List<ScoreTransferMetadata> Scores { get; set; } = new();
    }

    public interface IWifiDirectTransferService
    {
        event EventHandler<WifiDeviceInfo> DeviceDiscovered;
        event EventHandler ScanFinished;
        event EventHandler<WifiTransferProgressEventArgs> TransferProgressChanged;

        Task<bool> InitializeAsync();
        Task StartScanningAsync();
        Task StopScanningAsync();
        
        Task StartListeningAsync(Func<string, int, Task<bool>> confirmCallback, Func<List<WifiFilePayload>, string, Task> onReceiveComplete);
        Task StopListeningAsync();
        
        Task<bool> SendDataAsync(WifiDeviceInfo targetDevice, List<WifiFilePayload> files, string senderName);

        Task<WifiGroupShareInfo?> StartGroupBroadcastAsync(List<WifiFilePayload> files, string title, Action<int>? onClientCountChanged = null);
        Task StopGroupBroadcastAsync();

        Task<bool> DownloadFromQrCodeAsync(string qrCodeContent, Func<List<WifiFilePayload>, string, Task> onReceiveComplete);
    }
}
