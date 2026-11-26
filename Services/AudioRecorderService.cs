using System.Diagnostics;
using Plugin.Maui.Audio;

namespace MusicScoreManager.Services;

/// <summary>
/// Service d'enregistrement audio multi-plateforme (Android / Windows / iOS) basé sur Plugin.Maui.Audio.
/// Gère les permissions micro, l'enregistrement d'une prise en WAV et la réécoute rapide.
/// </summary>
public sealed class AudioRecorderService
{
    public static AudioRecorderService Instance { get; } = new();

    private IAudioRecorder? _recorder;
    private IAudioPlayer? _previewPlayer;
    private Stream? _previewStream;
    private string? _currentTargetPath;

    public bool IsRecording => _recorder?.IsRecording == true;
    public bool IsPlayingPreview => _previewPlayer?.IsPlaying == true;
    public int? PlayingAudioFileId { get; private set; }

    public event EventHandler? PreviewPlaybackEnded;

    private AudioRecorderService() { }

    public async Task<bool> EnsureMicrophonePermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
            }
            return status == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioRecorder] Permission check error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StartRecordingAsync(string targetFilePath)
    {
        try
        {
            StopPreview();

            if (IsRecording)
            {
                await CancelRecordingAsync();
            }

            bool granted = await EnsureMicrophonePermissionAsync();
            if (!granted) return false;

            string? dir = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _currentTargetPath = targetFilePath;
            _recorder = AudioManager.Current.CreateRecorder();
            await _recorder.StartAsync(targetFilePath);
            return _recorder.IsRecording;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioRecorder] StartRecordingAsync error: {ex.Message}");
            _recorder = null;
            _currentTargetPath = null;
            return false;
        }
    }

    public async Task<string?> StopRecordingAsync()
    {
        if (_recorder == null) return null;

        string? targetPath = _currentTargetPath;
        try
        {
            var audioSource = await _recorder.StopAsync();
            if (string.IsNullOrEmpty(targetPath)) return null;

            // Si Plugin.Maui.Audio n'a pas déjà écrit directement dans targetPath, copier le flux capturé
            if (!File.Exists(targetPath) || new FileInfo(targetPath).Length == 0)
            {
                if (audioSource != null)
                {
                    using var srcStream = audioSource.GetAudioStream();
                    if (srcStream != null)
                    {
                        using var dstStream = File.Create(targetPath);
                        await srcStream.CopyToAsync(dstStream);
                    }
                }
            }

            return (File.Exists(targetPath) && new FileInfo(targetPath).Length > 0) ? targetPath : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioRecorder] StopRecordingAsync error: {ex.Message}");
            return null;
        }
        finally
        {
            _recorder = null;
            _currentTargetPath = null;
        }
    }

    public async Task CancelRecordingAsync()
    {
        string? targetPath = _currentTargetPath;
        try
        {
            if (_recorder != null && _recorder.IsRecording)
            {
                await _recorder.StopAsync();
            }
        }
        catch { }
        finally
        {
            _recorder = null;
            _currentTargetPath = null;
            if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
            {
                try { File.Delete(targetPath); } catch { }
            }
        }
    }

    public bool PlayPreview(string filePath, int audioFileId)
    {
        try
        {
            StopPreview();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

            _previewStream = File.OpenRead(filePath);
            _previewPlayer = AudioManager.Current.CreatePlayer(_previewStream);
            PlayingAudioFileId = audioFileId;
            _previewPlayer.PlaybackEnded += OnInternalPlaybackEnded;
            _previewPlayer.Play();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioRecorder] PlayPreview error: {ex.Message}");
            StopPreview();
            return false;
        }
    }

    private void OnInternalPlaybackEnded(object? sender, EventArgs e)
    {
        StopPreview();
        PreviewPlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    public void StopPreview()
    {
        try
        {
            if (_previewPlayer != null)
            {
                _previewPlayer.PlaybackEnded -= OnInternalPlaybackEnded;
                if (_previewPlayer.IsPlaying) _previewPlayer.Stop();
                _previewPlayer.Dispose();
                _previewPlayer = null;
            }
        }
        catch { }

        try
        {
            _previewStream?.Dispose();
            _previewStream = null;
        }
        catch { }

        PlayingAudioFileId = null;
    }
}
