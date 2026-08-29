using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicScoreManager.Services
{
    public class BluetoothDeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public object? NativeDevice { get; set; }
    }

    public class BluetoothTransferProgressEventArgs : EventArgs
    {
        public string Status { get; set; } = string.Empty;
        public double Progress { get; set; }
    }

    public class BluetoothFilePayload
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public interface IBluetoothTransferService
    {
        event EventHandler<BluetoothDeviceInfo> DeviceDiscovered;
        event EventHandler ScanFinished;
        event EventHandler<BluetoothTransferProgressEventArgs> TransferProgressChanged;

        Task<bool> InitializeAsync();
        Task StartScanningAsync();
        Task StopScanningAsync();
        
        Task StartListeningAsync(Func<string, int, Task<bool>> confirmCallback, Func<List<BluetoothFilePayload>, string, Task> onReceiveComplete);
        Task StopListeningAsync();
        
        Task<bool> SendDataAsync(BluetoothDeviceInfo targetDevice, List<BluetoothFilePayload> files, string senderName);
    }

    public class ScoreTransferMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public Models.ScoreType Type { get; set; }
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

    public class AnnotationMetadata
    {
        public Models.AnnotationType Type { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Scale { get; set; }
        public string Color { get; set; } = "#FFFFFF";
        public string BackgroundColor { get; set; } = "Transparent";
        public int PageNumber { get; set; }
    }

    public class SetlistTransferMetadata
    {
        public string Name { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public DateTime? ConcertDate { get; set; }
        public TimeSpan? ConcertTime { get; set; }
        public Models.SetlistStatus Status { get; set; }
        public bool IsLocked { get; set; }
        public bool IsContinuousReading { get; set; }
        public List<ScoreTransferMetadata> Scores { get; set; } = new();
    }
}
