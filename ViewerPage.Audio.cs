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
    private bool _isAudioInitialized = false;
    private bool _isAudioEventsSubscribed = false;
    private double _audioPlayerXBase = 0;
    private double _audioPlayerYBase = 0;

    private void EnsureAudioEventsSubscribed()
    {
        if (_isAudioEventsSubscribed) return;
        _isAudioEventsSubscribed = true;

        AudioPlayer.PositionChanged += OnAudioPositionChanged;
        AudioPlayer.MediaEnded += OnAudioMediaEnded;
        AudioPlayer.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CommunityToolkit.Maui.Views.MediaElement.Duration))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (AudioPlayer.Duration > TimeSpan.Zero)
                    {
                        AudioTotalTimeLabel.Text = AudioPlayer.Duration.ToString(@"m\:ss");
                        AudioSlider.Maximum = AudioPlayer.Duration.TotalSeconds;
                    }
                });
            }
        };
    }

    private void OnAudioMediaEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isAudioPlaying = false;
            if (AudioPlayBtn != null) AudioPlayBtn.Text = "▶";
            if (AudioSlider != null) AudioSlider.Value = 0;
            if (AudioCurrentTimeLabel != null) AudioCurrentTimeLabel.Text = "0:00";
        });
    }

    private void InitializeAudio(bool forceReload = false)
    {
        if (_isAudioInitialized && !forceReload) return;
        _isAudioInitialized = true;

        EnsureAudioEventsSubscribed();

        _selectedAudioFile = _score.AudioFiles?.FirstOrDefault(af => af.IsSelected) 
                             ?? _score.AudioFiles?.FirstOrDefault();

        bool hasAudio = _selectedAudioFile != null;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateToolsDrawerUI();

            AudioPlayerOverlay.IsVisible = _score.ShowAudioPlayer && hasAudio;

            if (hasAudio && _selectedAudioFile != null)
            {
                if (AudioTrackNameLabel != null)
                {
                    AudioTrackNameLabel.Text = _selectedAudioFile.FileName;
                }

                string fullPath = _settingsService.GetAbsolutePath(_selectedAudioFile.FilePath, isAudio: true);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        AudioPlayer.Source = MediaSource.FromFile(fullPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur source audio: {ex.Message}");
                    }
                }
            }
            else
            {
                try
                {
                    AudioPlayer.Stop();
                    AudioPlayer.Source = null;
                }
                catch { }
                _isAudioPlaying = false;
                if (AudioPlayBtn != null) AudioPlayBtn.Text = "▶";
            }
        });
    }

    private void OnAudioPositionChanged(object? sender, MediaPositionChangedEventArgs e)
    {
        if (!_isDraggingAudioSlider)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AudioCurrentTimeLabel.Text = e.Position.ToString(@"m\:ss");
                AudioSlider.Value = e.Position.TotalSeconds;
            });
        }
    }

    private CancellationTokenSource? _preCountCts;
    private bool _isPreCounting = false;

    private void CancelPreCount()
    {
        _isPreCounting = false;
        try
        {
            _preCountCts?.Cancel();
        }
        catch { }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (MetronomeBpmLabel != null) MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
            if (MetronomeLight != null) MetronomeLight.Color = Color.FromArgb("#333333");
            if (AudioPlayBtn != null)
            {
                AudioPlayBtn.Text = "▶";
                AudioPlayBtn.IsEnabled = true;
            }
        });
    }

    private async void OnAudioPlayPauseClicked(object sender, EventArgs e)
    {
        // 1. Si un pré-compte est en cours, un nouvel appui annule le pré-compte et stoppe
        if (_isPreCounting)
        {
            CancelPreCount();
            _isAudioPlaying = false;
            return;
        }

        // 2. Si l'audio joue déjà, on met en pause
        if (_isAudioPlaying)
        {
            try
            {
                AudioPlayer.Pause();
            }
            catch { }
            AudioPlayBtn.Text = "▶";
            _isAudioPlaying = false;

            // Arrêter également le métronome synchronisé en pause
            if (_isMetronomePlaying)
            {
                StopMetronome();
            }
        }
        else
        {
            // 3. Démarrage de la lecture : au début OU lors de la reprise après une pause
            // Si le pré-compte est configuré (> 0), on le joue SYSTÉMATIQUEMENT
            if (_score.PreCountMeasures > 0)
            {
                await RunPreCountAndPlayAsync();
            }
            else
            {
                try
                {
                    AudioPlayer.Play();
                }
                catch { }

                if (_score.ShowMetronome || _score.HasMetronomeSound)
                {
                    StartMetronome();
                }

                AudioPlayBtn.Text = "⏸";
                _isAudioPlaying = true;
            }
        }
    }

    private async Task RunPreCountAndPlayAsync()
    {
        _isPreCounting = true;
        _preCountCts = new CancellationTokenSource();
        var token = _preCountCts.Token;

        AudioPlayBtn.Text = "⏸"; // Permet à l'utilisateur de cliquer pour annuler le pré-compte

        bool wasMetronomeRunning = _isMetronomePlaying;
        StopMetronome();

        int totalBeeps = _score.PreCountMeasures;
        double bpm = _score.BPM > 0 ? _score.BPM : 120.0;
        double intervalMs = 60000.0 / bpm;

        bool completed = await Task.Run(() =>
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < totalBeeps; i++)
            {
                if (token.IsCancellationRequested) return false;

                // 1. Déclencher le tick sonore et visuel synchro métronome
                MetronomeTick(isPreCount: true, preCountBeat: i + 1, preCountTotal: totalBeeps);

                // 2. Attente ultra-précise sans dérive temporelle cumulée
                double targetMs = (i + 1) * intervalMs;
                while (sw.Elapsed.TotalMilliseconds < targetMs)
                {
                    if (token.IsCancellationRequested) return false;

                    double remainingMs = targetMs - sw.Elapsed.TotalMilliseconds;
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
                        Thread.SpinWait(30);
                    }
                }
            }

            return !token.IsCancellationRequested;
        });

        _isPreCounting = false;

        if (completed && !token.IsCancellationRequested)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (MetronomeBpmLabel != null) MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
                if (MetronomeLight != null) MetronomeLight.Color = Color.FromArgb("#333333");
            });

            // Lancement synchronisé de l'audio
            try
            {
                AudioPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur Play audio: {ex.Message}");
            }

            if (wasMetronomeRunning || _score.ShowMetronome || _score.HasMetronomeSound)
            {
                StartMetronome();
            }

            AudioPlayBtn.Text = "⏸";
            _isAudioPlaying = true;
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (MetronomeBpmLabel != null) MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
                if (MetronomeLight != null) MetronomeLight.Color = Color.FromArgb("#333333");
                AudioPlayBtn.Text = "▶";
            });
            _isAudioPlaying = false;
        }
    }

    private void OnAudioToStartClicked(object sender, EventArgs e)
    {
        CancelPreCount();
        try
        {
            AudioPlayer.Pause();
            AudioPlayer.SeekTo(TimeSpan.Zero);
        }
        catch { }
        _isAudioPlaying = false;
        AudioPlayBtn.Text = "▶";
        AudioCurrentTimeLabel.Text = "0:00";
        AudioSlider.Value = 0;
        if (_isMetronomePlaying)
        {
            StopMetronome();
        }
    }

    private void OnAudioRewindClicked(object sender, EventArgs e)
    {
        CancelPreCount();
        try
        {
            var newPos = AudioPlayer.Position - TimeSpan.FromSeconds(5);
            if (newPos < TimeSpan.Zero) newPos = TimeSpan.Zero;
            AudioPlayer.SeekTo(newPos);
            AudioSlider.Value = newPos.TotalSeconds;
            AudioCurrentTimeLabel.Text = newPos.ToString(@"m\:ss");
        }
        catch { }
    }

    private void OnAudioForwardClicked(object sender, EventArgs e)
    {
        CancelPreCount();
        try
        {
            var newPos = AudioPlayer.Position + TimeSpan.FromSeconds(5);
            if (AudioPlayer.Duration > TimeSpan.Zero && newPos > AudioPlayer.Duration)
                newPos = AudioPlayer.Duration;
            AudioPlayer.SeekTo(newPos);
            AudioSlider.Value = newPos.TotalSeconds;
            AudioCurrentTimeLabel.Text = newPos.ToString(@"m\:ss");
        }
        catch { }
    }

    private void OnAudioCloseClicked(object sender, EventArgs e)
    {
        CancelPreCount();
        AudioPlayerOverlay.IsVisible = false;
        UpdateToolsDrawerUI();
        _score.ShowAudioPlayer = false;
        _ = _databaseService.SaveScoreAsync(_score);
    }

    private void OnAudioPlayerPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_isDraggingAudioSlider) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _audioPlayerXBase = AudioPlayerOverlay.TranslationX;
                _audioPlayerYBase = AudioPlayerOverlay.TranslationY;
                break;
            case GestureStatus.Running:
                double newX = _audioPlayerXBase + e.TotalX;
                double newY = _audioPlayerYBase + e.TotalY;
                if (this.Width > 0 && this.Height > 0)
                {
                    double w = AudioPlayerOverlay.Width > 0 ? AudioPlayerOverlay.Width : 265;
                    double h = AudioPlayerOverlay.Height > 0 ? AudioPlayerOverlay.Height : 90;
                    double minX = -5;
                    double maxX = Math.Max(minX, this.Width - w - 10);
                    double minY = -5;
                    double maxY = Math.Max(minY, this.Height - h - 10);
                    newX = Math.Clamp(newX, minX, maxX);
                    newY = Math.Clamp(newY, minY, maxY);
                }
                AudioPlayerOverlay.TranslationX = newX;
                AudioPlayerOverlay.TranslationY = newY;
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                break;
        }
    }

    private void OnAudioSliderDragStarted(object sender, EventArgs e)
    {
        _isDraggingAudioSlider = true;
    }

    private void OnAudioSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (_isDraggingAudioSlider)
        {
            // Mettre à jour l'étiquette pendant le déplacement manuel du slider
            AudioCurrentTimeLabel.Text = TimeSpan.FromSeconds(e.NewValue).ToString(@"m\:ss");
        }
    }

    private void OnAudioSliderDragCompleted(object sender, EventArgs e)
    {
        _isDraggingAudioSlider = false;
        AudioPlayer.SeekTo(TimeSpan.FromSeconds(AudioSlider.Value));
    }

}
