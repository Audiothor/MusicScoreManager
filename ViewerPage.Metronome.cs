using MusicScoreManager.Models;
using MusicScoreManager.Services;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using System.Linq;
using System.Diagnostics;
using Plugin.Maui.Audio;

namespace MusicScoreManager;

public partial class ViewerPage
{
    private readonly object _metronomeLock = new();
    private CancellationTokenSource? _metronomeCts = null;
    private static bool _isMetronomeAssetsPrepared = false;

    private async void InitializeMetronome()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
            MetronomeOverlay.IsVisible = _score.ShowMetronome;
        });

        try
        {
            string clickPath = Path.Combine(FileSystem.CacheDirectory, "click.wav");
            string preCountPath = Path.Combine(FileSystem.CacheDirectory, "precount.wav");

            if (!_isMetronomeAssetsPrepared || !File.Exists(clickPath) || !File.Exists(preCountPath))
            {
                using (var src = await FileSystem.OpenAppPackageFileAsync("click.wav"))
                using (var dst = File.Create(clickPath))
                {
                    await src.CopyToAsync(dst);
                }

                // Générer le wav du pré-compte (880Hz, 100ms)
                GenerateBeepWav(preCountPath, 880, 0.1);
                _isMetronomeAssetsPrepared = true;
            }

#if ANDROID
            var audioAttributes = new Android.Media.AudioAttributes.Builder()!
                .SetUsage(Android.Media.AudioUsageKind.Media)!
                .SetContentType(Android.Media.AudioContentType.Music)!
                .Build();

            if (audioAttributes != null)
            {
                _soundPool = new Android.Media.SoundPool.Builder()!
                    .SetMaxStreams(16)!
                    .SetAudioAttributes(audioAttributes)!
                    .Build();

                if (_soundPool != null)
                {
                    _metronomeSoundId = _soundPool.Load(clickPath, 1);
                    _preCountSoundId = _soundPool.Load(preCountPath, 1);
                }
            }
#else
            using var stream = File.OpenRead(clickPath);
            _metronomeAudioPlayer = AudioManager.Current.CreatePlayer(stream);

            using var preStream = File.OpenRead(preCountPath);
            _preCountAudioPlayer = AudioManager.Current.CreatePlayer(preStream);
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur initialisation audio métronome: {ex.Message}");
        }

        if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();
    }

    private void GenerateBeepWav(string filePath, double frequency, double durationSeconds)
    {
        int sampleRate = 44100;
        int samples = (int)(sampleRate * durationSeconds);
        short[] wave = new short[samples];

        for (int i = 0; i < samples; i++)
        {
            wave[i] = (short)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * short.MaxValue * 0.5);
        }

        using var fs = new FileStream(filePath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        bw.Write(new char[] { 'R', 'I', 'F', 'F' });
        bw.Write(36 + samples * 2);
        bw.Write(new char[] { 'W', 'A', 'V', 'E' });
        bw.Write(new char[] { 'f', 'm', 't', ' ' });
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(new char[] { 'd', 'a', 't', 'a' });
        bw.Write(samples * 2);
        for (int i = 0; i < samples; i++) bw.Write(wave[i]);
    }

    private void StartMetronome()
    {
        lock (_metronomeLock)
        {
            StopMetronome();
            if (_score.BPM <= 0 || !_isScoreReady) return;
            if (!_score.ShowMetronome && !_score.HasMetronomeSound) return;

            _isMetronomePlaying = true;
            _isMetronomeRunning = true;
            _metronomeCts = new CancellationTokenSource();
            var token = _metronomeCts.Token;

            double intervalMs = 60000.0 / _score.BPM;

            // Horloge absolue à haute précision et priorité maximale sur Thread dédié
            _metronomeThread = new Thread(() =>
            {
                var sw = Stopwatch.StartNew();
                double nextTickMs = 0;

                while (!token.IsCancellationRequested && _isMetronomeRunning)
                {
                    double elapsed = sw.Elapsed.TotalMilliseconds;
                    if (elapsed >= nextTickMs)
                    {
                        MetronomeTick();

                        nextTickMs += intervalMs;
                        // Rattrapage sécurisé en cas de retard exceptionnel
                        if (elapsed > nextTickMs + intervalMs)
                        {
                            nextTickMs = elapsed + intervalMs;
                        }
                    }

                    double remainingMs = nextTickMs - sw.Elapsed.TotalMilliseconds;
                    if (remainingMs > 15.0)
                    {
                        Thread.Sleep((int)(remainingMs - 10.0));
                    }
                    else if (remainingMs > 1.0)
                    {
                        Thread.Sleep(1);
                    }
                    else if (remainingMs > 0)
                    {
                        Thread.SpinWait(40);
                    }
                }
            })
            {
                Priority = ThreadPriority.Highest,
                IsBackground = true,
                Name = "HighPrecisionMetronomeThread"
            };

            _metronomeThread.Start();
        }
    }

    private void MetronomeTick(bool isPreCount = false, int preCountBeat = 0, int preCountTotal = 0)
    {
        if (!_isMetronomeRunning && !isPreCount) return;

        // 1. Son à priorité absolue (latence instantanée < 2ms via SoundPool sous Android)
        if (_score.HasMetronomeSound || isPreCount)
        {
#if ANDROID
            if (_soundPool != null)
            {
                int soundId = isPreCount ? _preCountSoundId : _metronomeSoundId;
                if (soundId != 0)
                {
                    float vol = (float)Math.Clamp(_score.MetronomeVolume, 0.05, 1.0);
                    _soundPool.Play(soundId, vol, vol, 1, 0, 1.0f);
                }
            }
#else
            try
            {
                float vol = (float)Math.Clamp(_score.MetronomeVolume, 0.05, 1.0);
                if (isPreCount && _preCountAudioPlayer != null)
                {
                    _preCountAudioPlayer.Volume = vol;
                    _preCountAudioPlayer.Seek(0);
                    _preCountAudioPlayer.Play();
                }
                else if (!isPreCount && _metronomeAudioPlayer != null)
                {
                    _metronomeAudioPlayer.Volume = vol;
                    _metronomeAudioPlayer.Seek(0);
                    _metronomeAudioPlayer.Play();
                }
            }
            catch { }
#endif
        }

        // 2. Flash visuel fluide synchro avec le métronome (en continu et pendant le pré-compte)
        if (_score.ShowMetronome || (MetronomeOverlay != null && MetronomeOverlay.IsVisible))
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (isPreCount && preCountTotal > 0 && MetronomeBpmLabel != null)
                    {
                        MetronomeBpmLabel.Text = $"● {preCountBeat} / {preCountTotal}";
                    }
                    else if (!isPreCount && MetronomeBpmLabel != null)
                    {
                        MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
                    }

                    if (MetronomeLight != null)
                    {
                        // Couleur orange vif pour le pré-compte, bleu standard pour le métronome
                        MetronomeLight.Color = isPreCount ? Color.FromArgb("#FF9800") : Color.FromArgb("#007ACC");
                    }

                    await Task.Delay(50);

                    if (MetronomeLight != null)
                    {
                        MetronomeLight.Color = Color.FromArgb("#333333");
                    }
                }
                catch { }
            });
        }
    }

    private void StopMetronome()
    {
        lock (_metronomeLock)
        {
            _isMetronomePlaying = false;
            _isMetronomeRunning = false;
            _metronomeCts?.Cancel();

            if (_metronomeThread != null)
            {
                try
                {
                    if (_metronomeThread.IsAlive)
                    {
                        _metronomeThread.Join(50);
                    }
                }
                catch { }
                _metronomeThread = null;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MetronomeLight.Color = Color.FromArgb("#333333");
            });
        }
    }

    private double _metronomeXBase = 0;
    private double _metronomeYBase = 0;
    private bool _isDraggingMetronome = false;
    private DateTime _lastMetronomeDragTime = DateTime.MinValue;

    private void OnMetronomePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _metronomeXBase = MetronomeOverlay.TranslationX;
                _metronomeYBase = MetronomeOverlay.TranslationY;
                _isDraggingMetronome = false;
                break;
            case GestureStatus.Running:
                if (Math.Abs(e.TotalX) > 5 || Math.Abs(e.TotalY) > 5)
                {
                    _isDraggingMetronome = true;
                    _lastMetronomeDragTime = DateTime.UtcNow;
                }
                double newX = _metronomeXBase + e.TotalX;
                double newY = _metronomeYBase + e.TotalY;
                if (this.Width > 0 && this.Height > 0)
                {
                    double w = MetronomeOverlay.Width > 0 ? MetronomeOverlay.Width : 70;
                    double h = MetronomeOverlay.Height > 0 ? MetronomeOverlay.Height : 50;
                    double minX = -10;
                    double maxX = Math.Max(minX, this.Width - w - 10);
                    double minY = -5;
                    double maxY = Math.Max(minY, this.Height - h - 10);
                    newX = Math.Clamp(newX, minX, maxX);
                    newY = Math.Clamp(newY, minY, maxY);
                }
                MetronomeOverlay.TranslationX = newX;
                MetronomeOverlay.TranslationY = newY;
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_isDraggingMetronome)
                {
                    _lastMetronomeDragTime = DateTime.UtcNow;
                }
                _isDraggingMetronome = false;
                break;
        }
    }

    private void OnMetronomeOverlayTapped(object sender, EventArgs e)
    {
        if ((DateTime.UtcNow - _lastMetronomeDragTime).TotalMilliseconds < 350)
            return;
        OpenMetronomeSettingsModal();
    }

    private void OnMetronomeSettingsButtonClicked(object sender, EventArgs e)
    {
        OpenMetronomeSettingsModal();
    }

    private readonly List<DateTime> _tapTempoHistory = new();

    private void OpenMetronomeSettingsModal()
    {
        _tapTempoHistory.Clear();
        ModalMetronomeBpmLabel.Text = $"{_score.BPM} BPM";
        ModalMetronomeBpmSlider.Value = Math.Clamp(_score.BPM, 40, 250);
        ModalMetronomeSoundSwitch.IsToggled = _score.HasMetronomeSound;
        ModalMetronomeVisualSwitch.IsToggled = _score.ShowMetronome;

        int volPercent = (int)Math.Round(_score.MetronomeVolume * 100);
        ModalMetronomeVolumeSlider.Value = Math.Clamp(volPercent, 0, 100);
        ModalVolumeLabel.Text = $"{(int)ModalMetronomeVolumeSlider.Value}%";
        ModalVolumeLayout.Opacity = _score.HasMetronomeSound ? 1.0 : 0.4;
        ModalMetronomeVolumeSlider.IsEnabled = _score.HasMetronomeSound;

        MetronomeSettingsModal.Opacity = 0;
        MetronomeSettingsModal.IsVisible = true;
        _ = MetronomeSettingsModal.FadeToAsync(1.0, 180, Easing.CubicOut);
    }

    private double _metroSettingsXBase = 0;
    private double _metroSettingsYBase = 0;

    private void OnMetronomeSettingsPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (MetronomeSettingsBorder == null) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _metroSettingsXBase = MetronomeSettingsBorder.TranslationX;
                _metroSettingsYBase = MetronomeSettingsBorder.TranslationY;
                break;
            case GestureStatus.Running:
                double newX = _metroSettingsXBase + e.TotalX;
                double newY = _metroSettingsYBase + e.TotalY;
                if (this.Width > 0 && this.Height > 0)
                {
                    double w = MetronomeSettingsBorder.Width > 0 ? MetronomeSettingsBorder.Width : 390;
                    double h = MetronomeSettingsBorder.Height > 0 ? MetronomeSettingsBorder.Height : 500;
                    double halfW = Math.Max(0, (this.Width - w) / 2.0);
                    double halfH = Math.Max(0, (this.Height - h) / 2.0);
                    newX = Math.Clamp(newX, -halfW, halfW);
                    newY = Math.Clamp(newY, -halfH, halfH);
                }
                MetronomeSettingsBorder.TranslationX = newX;
                MetronomeSettingsBorder.TranslationY = newY;
                break;
        }
    }

    private async void OnCloseMetronomeSettingsModalClicked(object sender, EventArgs e)
    {
        await MetronomeSettingsModal.FadeToAsync(0, 150, Easing.CubicIn);
        MetronomeSettingsModal.IsVisible = false;
        await _databaseService.SaveScoreAsync(_score);
    }

    private void OnModalTapTempoClicked(object sender, EventArgs e)
    {
        var now = DateTime.Now;
        if (_tapTempoHistory.Count > 0 && (now - _tapTempoHistory.Last()).TotalSeconds > 2.5)
        {
            _tapTempoHistory.Clear();
        }

        _tapTempoHistory.Add(now);
        if (_tapTempoHistory.Count > 4)
        {
            _tapTempoHistory.RemoveAt(0);
        }

        if (_tapTempoHistory.Count >= 2)
        {
            double totalMs = (_tapTempoHistory.Last() - _tapTempoHistory.First()).TotalMilliseconds;
            double avgIntervalMs = totalMs / (_tapTempoHistory.Count - 1);
            if (avgIntervalMs > 0)
            {
                int calculatedBpm = (int)Math.Round(60000.0 / avgIntervalMs);
                SetMetronomeBpm(Math.Clamp(calculatedBpm, 40, 250));
            }
        }
    }

    private void OnModalBpmMinus5Clicked(object sender, EventArgs e) => SetMetronomeBpm(_score.BPM - 5);
    private void OnModalBpmMinus1Clicked(object sender, EventArgs e) => SetMetronomeBpm(_score.BPM - 1);
    private void OnModalBpmPlus1Clicked(object sender, EventArgs e) => SetMetronomeBpm(_score.BPM + 1);
    private void OnModalBpmPlus5Clicked(object sender, EventArgs e) => SetMetronomeBpm(_score.BPM + 5);

    private void OnModalBpmSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        int newBpm = (int)Math.Round(e.NewValue);
        if (newBpm != _score.BPM)
        {
            SetMetronomeBpm(newBpm);
        }
    }

    private void SetMetronomeBpm(int bpm)
    {
        int clampedBpm = Math.Clamp(bpm, 40, 250);
        _score.BPM = clampedBpm;
        ModalMetronomeBpmLabel.Text = $"{_score.BPM} BPM";
        if (Math.Abs(ModalMetronomeBpmSlider.Value - clampedBpm) > 0.5)
        {
            ModalMetronomeBpmSlider.Value = clampedBpm;
        }
        if (MetronomeBpmLabel != null)
        {
            MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
        }
        if (_isMetronomePlaying)
        {
            StartMetronome();
        }
    }

    private void OnModalMetronomeSoundToggled(object sender, ToggledEventArgs e)
    {
        _score.HasMetronomeSound = e.Value;
        ModalVolumeLayout.Opacity = e.Value ? 1.0 : 0.4;
        ModalMetronomeVolumeSlider.IsEnabled = e.Value;

        if ((_score.HasMetronomeSound || _score.ShowMetronome) && !_isMetronomePlaying)
        {
            StartMetronome();
        }
        else if (!_score.HasMetronomeSound && !_score.ShowMetronome)
        {
            StopMetronome();
        }
    }

    private void OnModalMetronomeVisualToggled(object sender, ToggledEventArgs e)
    {
        _score.ShowMetronome = e.Value;
        MetronomeOverlay.IsVisible = e.Value;
        UpdateToolsDrawerUI();

        if (e.Value && !_isMetronomePlaying)
        {
            StartMetronome();
        }
        else if (!e.Value && !_score.HasMetronomeSound)
        {
            StopMetronome();
        }
    }

    private void OnModalVolumeSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        int percent = (int)Math.Round(e.NewValue);
        ModalVolumeLabel.Text = $"{percent}%";
        _score.MetronomeVolume = percent / 100.0;
    }

    private void OnModalVolumeSliderDragCompleted(object sender, EventArgs e)
    {
        PlayTestMetronomeClick();
    }

    private void PlayTestMetronomeClick()
    {
        try
        {
            float vol = (float)Math.Clamp(_score.MetronomeVolume, 0.05, 1.0);
#if ANDROID
            if (_soundPool != null && _metronomeSoundId != 0)
            {
                _soundPool.Play(_metronomeSoundId, vol, vol, 1, 0, 1.0f);
            }
#else
            if (_metronomeAudioPlayer != null)
            {
                _metronomeAudioPlayer.Volume = vol;
                _metronomeAudioPlayer.Seek(0);
                _metronomeAudioPlayer.Play();
            }
#endif
        }
        catch { }
    }

}
