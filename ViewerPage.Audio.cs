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
    private double _lastNonZeroAudioVolume = 100.0;
    private bool _isUpdatingAudioVolumeUI = false;

    private List<ScoreAudioFile> GetOrderedAudioFiles()
    {
        if (_score?.AudioFiles == null) return new List<ScoreAudioFile>();
        return _score.AudioFiles
            .OrderBy(af => af.SortOrder)
            .ThenBy(af => af.Id)
            .ToList();
    }

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
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _isAudioPlaying = false;
            if (_isMetronomePlaying)
            {
                StopMetronome();
            }
            try
            {
                AudioPlayer.Pause();
                await AudioPlayer.SeekTo(TimeSpan.Zero);
            }
            catch { }
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

        var ordered = GetOrderedAudioFiles();
        _selectedAudioFile = ordered.FirstOrDefault(af => af.IsSelected)
                             ?? ordered.FirstOrDefault();

        bool hasAudio = _selectedAudioFile != null;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ApplySavedAudioVolume();
            UpdateToolsDrawerUI();

            AudioPlayerOverlay.IsVisible = _score.ShowAudioPlayer && hasAudio;
            if (AudioTrackListContainer != null) AudioTrackListContainer.IsVisible = false;

            if (hasAudio && _selectedAudioFile != null)
            {
                LoadSelectedAudioTrack(_selectedAudioFile);
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
                UpdateAudioTrackNavigationUI();
            }
        });
    }

    private void LoadSelectedAudioTrack(ScoreAudioFile track)
    {
        CancelPreCount();
        _isAudioPlaying = false;
        if (_isMetronomePlaying) StopMetronome();

        _selectedAudioFile = track;

        if (AudioTrackIconLabel != null)
        {
            AudioTrackIconLabel.Text = track.IsRecording ? "🎙️" : "🎵";
        }
        if (AudioTrackNameLabel != null)
        {
            AudioTrackNameLabel.Text = track.FileName;
        }
        if (AudioPlayBtn != null) AudioPlayBtn.Text = "▶";
        if (AudioCurrentTimeLabel != null) AudioCurrentTimeLabel.Text = "0:00";
        if (AudioTotalTimeLabel != null) AudioTotalTimeLabel.Text = "0:00";
        if (AudioSlider != null) AudioSlider.Value = 0;

        string fullPath = _settingsService.GetAbsolutePath(track.FilePath, isAudio: true);
        if (File.Exists(fullPath))
        {
            try
            {
                AudioPlayer.Stop();
                AudioPlayer.Source = MediaSource.FromFile(fullPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur source audio: {ex.Message}");
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
        }

        UpdateAudioTrackNavigationUI();
        if (AudioTrackListContainer != null && AudioTrackListContainer.IsVisible)
        {
            RefreshAudioTrackListUI();
        }
    }

    private void UpdateAudioTrackNavigationUI()
    {
        var ordered = GetOrderedAudioFiles();
        bool hasMultiple = ordered.Count > 1;
        int idx = _selectedAudioFile != null ? ordered.IndexOf(_selectedAudioFile) : -1;

        if (AudioTrackListToggleBtn != null)
        {
            AudioTrackListToggleBtn.IsVisible = hasMultiple;
            bool isOpen = AudioTrackListContainer?.IsVisible == true;
            AudioTrackListToggleBtn.Text = isOpen ? "☰ ▴" : "☰ ▾";
        }

        if (AudioPrevTrackBtn != null)
        {
            AudioPrevTrackBtn.IsVisible = hasMultiple;
            bool canPrev = hasMultiple && idx > 0;
            AudioPrevTrackBtn.IsEnabled = canPrev;
            AudioPrevTrackBtn.Opacity = canPrev ? 1.0 : 0.35;
        }

        if (AudioNextTrackBtn != null)
        {
            AudioNextTrackBtn.IsVisible = hasMultiple;
            bool canNext = hasMultiple && idx >= 0 && idx < ordered.Count - 1;
            AudioNextTrackBtn.IsEnabled = canNext;
            AudioNextTrackBtn.Opacity = canNext ? 1.0 : 0.35;
        }

        if (!hasMultiple && AudioTrackListContainer != null)
        {
            AudioTrackListContainer.IsVisible = false;
        }
    }

    private void RefreshAudioTrackListUI()
    {
        if (AudioTrackListStack == null) return;
        AudioTrackListStack.Children.Clear();

        var ordered = GetOrderedAudioFiles();
        foreach (var track in ordered)
        {
            bool isCurrent = ReferenceEquals(track, _selectedAudioFile)
                || (_selectedAudioFile != null && track.Id > 0 && track.Id == _selectedAudioFile.Id);

            var row = new Border
            {
                BackgroundColor = isCurrent ? Color.FromArgb("#1E3A5F") : Color.FromArgb("#1F2030"),
                Stroke = isCurrent ? Color.FromArgb("#007ACC") : Color.FromArgb("#33354A"),
                StrokeThickness = 1,
                Padding = new Thickness(8, 6),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 }
            };

            var rowGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 6
            };

            rowGrid.Add(new Label
            {
                Text = track.IsRecording ? "🎙️" : "🎵",
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            }, 0, 0);

            rowGrid.Add(new Label
            {
                Text = track.FileName,
                TextColor = isCurrent ? Color.FromArgb("#88DDFF") : Colors.White,
                FontSize = 11,
                FontAttributes = isCurrent ? FontAttributes.Bold : FontAttributes.None,
                LineBreakMode = LineBreakMode.TailTruncation,
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);

            if (isCurrent)
            {
                rowGrid.Add(new Label
                {
                    Text = "✓",
                    TextColor = Color.FromArgb("#00E676"),
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                }, 2, 0);
            }

            row.Content = rowGrid;

            var captured = track;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                LoadSelectedAudioTrack(captured);
                if (AudioTrackListContainer != null)
                {
                    AudioTrackListContainer.IsVisible = false;
                }
                UpdateAudioTrackNavigationUI();
            };
            row.GestureRecognizers.Add(tap);

            AudioTrackListStack.Children.Add(row);
        }
    }

    private void OnAudioTrackListToggleClicked(object? sender, EventArgs e)
    {
        var ordered = GetOrderedAudioFiles();
        if (ordered.Count <= 1 || AudioTrackListContainer == null) return;

        bool newVisible = !AudioTrackListContainer.IsVisible;
        AudioTrackListContainer.IsVisible = newVisible;
        if (newVisible)
        {
            RefreshAudioTrackListUI();
        }
        UpdateAudioTrackNavigationUI();
    }

    private void OnAudioPrevTrackClicked(object? sender, EventArgs e)
    {
        var ordered = GetOrderedAudioFiles();
        if (ordered.Count <= 1 || _selectedAudioFile == null) return;
        int idx = ordered.IndexOf(_selectedAudioFile);
        if (idx > 0)
        {
            LoadSelectedAudioTrack(ordered[idx - 1]);
        }
    }

    private void OnAudioNextTrackClicked(object? sender, EventArgs e)
    {
        var ordered = GetOrderedAudioFiles();
        if (ordered.Count <= 1 || _selectedAudioFile == null) return;
        int idx = ordered.IndexOf(_selectedAudioFile);
        if (idx >= 0 && idx < ordered.Count - 1)
        {
            LoadSelectedAudioTrack(ordered[idx + 1]);
        }
    }

    private void ApplySavedAudioVolume()
    {
        double savedPercent = Math.Clamp(Preferences.Default.Get("AudioPlayerVolume", 100.0), 0.0, 100.0);
        if (savedPercent > 0) _lastNonZeroAudioVolume = savedPercent;
        _isUpdatingAudioVolumeUI = true;
        try
        {
            if (AudioVolumeSlider != null) AudioVolumeSlider.Value = savedPercent;
            if (AudioVolumeLabel != null) AudioVolumeLabel.Text = $"{(int)Math.Round(savedPercent)}%";
            if (AudioVolumeMuteBtn != null) AudioVolumeMuteBtn.Text = savedPercent <= 0.5 ? "🔇" : (savedPercent < 50 ? "🔉" : "🔊");
            AudioPlayer.Volume = savedPercent / 100.0;
        }
        catch { }
        finally
        {
            _isUpdatingAudioVolumeUI = false;
        }
    }

    private void OnAudioVolumeSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isUpdatingAudioVolumeUI) return;
        double percent = Math.Clamp(e.NewValue, 0.0, 100.0);
        if (percent > 1.0) _lastNonZeroAudioVolume = percent;
        Preferences.Default.Set("AudioPlayerVolume", percent);
        try { AudioPlayer.Volume = percent / 100.0; } catch { }
        if (AudioVolumeLabel != null) AudioVolumeLabel.Text = $"{(int)Math.Round(percent)}%";
        if (AudioVolumeMuteBtn != null) AudioVolumeMuteBtn.Text = percent <= 0.5 ? "🔇" : (percent < 50 ? "🔉" : "🔊");
    }

    private void OnAudioVolumeMuteClicked(object? sender, EventArgs e)
    {
        if (AudioVolumeSlider == null) return;
        if (AudioVolumeSlider.Value > 0.5)
        {
            _lastNonZeroAudioVolume = AudioVolumeSlider.Value;
            AudioVolumeSlider.Value = 0;
        }
        else
        {
            AudioVolumeSlider.Value = _lastNonZeroAudioVolume > 1.0 ? _lastNonZeroAudioVolume : 100.0;
        }
    }

    private void OnAudioBoxTapped(object? sender, EventArgs e)
    {
        // Absorbe les taps pour éviter de tourner la page ou d'ouvrir le menu central derrière le lecteur
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
                    double w = AudioPlayerOverlay.Width > 0 ? AudioPlayerOverlay.Width : 310;
                    double h = AudioPlayerOverlay.Height > 0 ? AudioPlayerOverlay.Height : 145;
                    // Ancrage HorizontalOptions="Center", VerticalOptions="Start" (Margin top = 52)
                    double halfSpaceX = Math.Max(0, (this.Width - w) / 2.0 - 6);
                    double minX = -halfSpaceX;
                    double maxX = halfSpaceX;
                    double baseY = AudioPlayerOverlay.Y > 0 ? AudioPlayerOverlay.Y : 52;
                    double minY = -(baseY - 6);
                    double maxY = Math.Max(minY, this.Height - baseY - h - 6);
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
