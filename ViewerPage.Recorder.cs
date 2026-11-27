using MusicScoreManager.Models;
using MusicScoreManager.Services;
using System.Diagnostics;

namespace MusicScoreManager;

public partial class ViewerPage
{
    private double _recorderXBase = 0;
    private double _recorderYBase = 0;
    private IDispatcherTimer? _recorderTimer;
    private DateTime _recordingStartUtc;
    private bool _recorderPulseState = false;
    private bool _isRecorderEventsSubscribed = false;

    private async void OnDrawerRecorderTapped(object? sender, EventArgs e)
    {
        if (RecorderOverlay == null || _score == null) return;

        bool newState = !RecorderOverlay.IsVisible;
        if (newState)
        {
            // Exclusion mutuelle fluide : fermer l'accordeur s'il est ouvert
            if (TunerOverlay != null && TunerOverlay.IsVisible)
            {
                StopTunerListening();
                TunerOverlay.IsVisible = false;
            }

            if (!_isRecorderEventsSubscribed)
            {
                AudioRecorderService.Instance.PreviewPlaybackEnded += OnRecorderPreviewEnded;
                _isRecorderEventsSubscribed = true;
            }

            RecorderOverlay.IsVisible = true;
            RefreshRecorderTakesUI();
            UpdateRecorderControlsUI(AudioRecorderService.Instance.IsRecording);
        }
        else
        {
            await CloseRecorderOverlayAsync();
        }

        UpdateToolsDrawerUI();
    }

    private async void OnCloseRecorderClicked(object? sender, EventArgs e)
    {
        await CloseRecorderOverlayAsync();
        UpdateToolsDrawerUI();
    }

    private async Task CloseRecorderOverlayAsync()
    {
        if (AudioRecorderService.Instance.IsRecording)
        {
            await StopAndSaveRecordingAsync();
        }
        AudioRecorderService.Instance.StopPreview();
        StopRecorderTimer();
        if (RecorderOverlay != null)
        {
            RecorderOverlay.IsVisible = false;
        }
    }

    private void OnRecorderBoxTapped(object? sender, EventArgs e)
    {
        // Absorbe les clics pour éviter de fermer ou tourner la page derrière l'overlay
    }

    private void OnRecorderPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (RecorderOverlay == null) return;
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _recorderXBase = RecorderOverlay.TranslationX;
                _recorderYBase = RecorderOverlay.TranslationY;
                break;
            case GestureStatus.Running:
                RecorderOverlay.TranslationX = _recorderXBase + e.TotalX;
                RecorderOverlay.TranslationY = _recorderYBase + e.TotalY;
                break;
        }
    }

    private async void OnRecorderToggleClicked(object? sender, EventArgs e)
    {
        if (_score == null) return;
        var loc = LocalizationService.Instance;

        if (!AudioRecorderService.Instance.IsRecording)
        {
            // S'assurer que l'accordeur ne monopolise pas le micro
            StopTunerListening();

            string recordingsDir = Path.Combine(_settingsService.AudioRootDirectory, "Recordings");
            string safeTitle = string.Concat((_score.Title ?? "Score").Split(Path.GetInvalidFileNameChars())).Trim();
            if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = "Score";
            if (safeTitle.Length > 32) safeTitle = safeTitle.Substring(0, 32).Trim();
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{safeTitle}_{stamp}.wav";
            string targetPath = Path.Combine(recordingsDir, fileName);

            bool started = await AudioRecorderService.Instance.StartRecordingAsync(targetPath);
            if (!started)
            {
                await DisplayAlertAsync(
                    loc.GetString("Common_Error", "Erreur"),
                    loc.GetString("Recorder_Permission_Error", "Impossible d'accéder au microphone. Vérifiez les permissions de l'application."),
                    loc.GetString("Common_OK", "OK"));
                UpdateRecorderControlsUI(false);
                return;
            }

            _recordingStartUtc = DateTime.UtcNow;
            StartRecorderTimer();
            UpdateRecorderControlsUI(true);
            RefreshRecorderTakesUI();
        }
        else
        {
            await StopAndSaveRecordingAsync();
        }
    }

    private async Task StopAndSaveRecordingAsync()
    {
        StopRecorderTimer();
        string? savedPath = await AudioRecorderService.Instance.StopRecordingAsync();
        UpdateRecorderControlsUI(false);

        if (_score != null && !string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
        {
            var loc = LocalizationService.Instance;
            int existingCount = _score.AudioFiles.Count(a => a.IsRecording);
            int nextOrder = _score.AudioFiles.Count > 0 ? _score.AudioFiles.Max(a => a.SortOrder) + 1 : 0;
            string takePrefix = loc.GetString("Recorder_Take_Prefix", "Prise");
            string displayName = $"{takePrefix} {existingCount + 1} ({DateTime.Now:dd/MM HH:mm})";

            var audioFile = new ScoreAudioFile
            {
                ScoreId = _score.Id,
                FileName = displayName,
                FilePath = _settingsService.GetRelativePath(savedPath, isAudio: true),
                IsSelected = false,
                IsRecording = true,
                SortOrder = nextOrder
            };

            await _databaseService.SaveAudioFileAsync(audioFile);
            _score.AudioFiles.Add(audioFile);

            if (RecorderStatusLabel != null)
            {
                RecorderStatusLabel.Text = loc.GetString("Recorder_Status_Saved", "Prise enregistrée !");
            }
        }

        RefreshRecorderTakesUI();
        UpdateAudioTrackNavigationUI();
        UpdateToolsDrawerUI();
    }

    private void StartRecorderTimer()
    {
        StopRecorderTimer();
        _recorderTimer = Dispatcher.CreateTimer();
        _recorderTimer.Interval = TimeSpan.FromMilliseconds(250);
        _recorderTimer.Tick += (s, e) =>
        {
            var elapsed = DateTime.UtcNow - _recordingStartUtc;
            if (RecorderTimerLabel != null)
            {
                RecorderTimerLabel.Text = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
            }
            if (RecorderPulseDot != null)
            {
                _recorderPulseState = !_recorderPulseState;
                RecorderPulseDot.Color = _recorderPulseState ? Color.FromArgb("#FF3B30") : Color.FromArgb("#661818");
            }
        };
        _recorderTimer.Start();
    }

    private void StopRecorderTimer()
    {
        if (_recorderTimer != null)
        {
            _recorderTimer.Stop();
            _recorderTimer = null;
        }
    }

    private void UpdateRecorderControlsUI(bool isRecording)
    {
        var loc = LocalizationService.Instance;
        if (RecorderToggleBtn != null)
        {
            RecorderToggleBtn.Text = isRecording ? "⏹" : "⏺";
            RecorderToggleBtn.BackgroundColor = isRecording ? Color.FromArgb("#E74C3C") : Color.FromArgb("#C0392B");
        }
        if (RecorderPulseDot != null && !isRecording)
        {
            RecorderPulseDot.Color = Color.FromArgb("#666677");
        }
        if (RecorderTimerLabel != null && !isRecording)
        {
            RecorderTimerLabel.Text = "00:00";
        }
        if (RecorderStatusLabel != null)
        {
            RecorderStatusLabel.Text = isRecording
                ? loc.GetString("Recorder_Status_Recording", "Enregistrement en cours...")
                : loc.GetString("Recorder_Status_Ready", "Prêt à enregistrer");
        }
    }

    private void OnRecorderPreviewEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(RefreshRecorderTakesUI);
    }

    private void RefreshRecorderTakesUI()
    {
        if (RecorderTakesList == null || _score == null) return;
        RecorderTakesList.Children.Clear();

        var loc = LocalizationService.Instance;
        var takes = _score.AudioFiles
            .Where(a => a.IsRecording)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Id)
            .ToList();

        if (RecorderTakesCountLabel != null)
        {
            RecorderTakesCountLabel.Text = takes.Count > 0
                ? string.Format(loc.GetString("Recorder_Takes_Count", "{0} prise(s)"), takes.Count)
                : string.Empty;
        }

        if (takes.Count == 0)
        {
            RecorderTakesList.Children.Add(new Label
            {
                Text = loc.GetString("Recorder_No_Takes", "Aucune prise enregistrée pour cette partition."),
                TextColor = Color.FromArgb("#888899"),
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4)
            });
            return;
        }

        foreach (var take in takes)
        {
            bool isPlayingThis = AudioRecorderService.Instance.IsPlayingPreview
                && AudioRecorderService.Instance.PlayingAudioFileId == take.Id;

            var rowBorder = new Border
            {
                BackgroundColor = isPlayingThis ? Color.FromArgb("#2E3A55") : Color.FromArgb("#232436"),
                Stroke = isPlayingThis ? Color.FromArgb("#007ACC") : Color.FromArgb("#35364A"),
                StrokeThickness = 1,
                Padding = new Thickness(8, 6),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }
            };

            var rowGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 6
            };

            var playBtn = new Button
            {
                Text = isPlayingThis ? "⏹" : "▶",
                BackgroundColor = isPlayingThis ? Color.FromArgb("#E67E22") : Color.FromArgb("#007ACC"),
                TextColor = Colors.White,
                FontSize = 12,
                WidthRequest = 32,
                HeightRequest = 30,
                CornerRadius = 6,
                Padding = 0
            };
            playBtn.Clicked += async (s, ev) =>
            {
                if (AudioRecorderService.Instance.IsRecording) return;
                if (AudioRecorderService.Instance.IsPlayingPreview && AudioRecorderService.Instance.PlayingAudioFileId == take.Id)
                {
                    AudioRecorderService.Instance.StopPreview();
                }
                else
                {
                    string absPath = _settingsService.GetAbsolutePath(take.FilePath, isAudio: true);
                    bool ok = AudioRecorderService.Instance.PlayPreview(absPath, take.Id);
                    if (!ok)
                    {
                        await DisplayAlertAsync(
                            loc.GetString("Common_Error", "Erreur"),
                            loc.GetString("Recorder_Play_Error", "Fichier d'enregistrement introuvable ou illisible."),
                            loc.GetString("Common_OK", "OK"));
                    }
                }
                RefreshRecorderTakesUI();
            };

            var titleLabel = new Label
            {
                Text = $"🎙️ {take.FileName}",
                TextColor = Colors.White,
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            var renameBtn = new Button
            {
                Text = "✏️",
                BackgroundColor = Color.FromArgb("#33344A"),
                TextColor = Colors.White,
                FontSize = 12,
                WidthRequest = 30,
                HeightRequest = 30,
                CornerRadius = 6,
                Padding = 0
            };
            renameBtn.Clicked += async (s, ev) =>
            {
                string? newName = await DisplayPromptAsync(
                    loc.GetString("Recorder_Rename_Title", "Renommer la prise"),
                    loc.GetString("Recorder_Rename_Prompt", "Nouveau nom :"),
                    loc.GetString("Common_OK", "OK"),
                    loc.GetString("Common_Cancel", "Annuler"),
                    initialValue: take.FileName);
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    take.FileName = newName.Trim();
                    await _databaseService.SaveAudioFileAsync(take);
                    RefreshRecorderTakesUI();
                }
            };

            var deleteBtn = new Button
            {
                Text = "🗑️",
                BackgroundColor = Color.FromArgb("#4A2328"),
                TextColor = Color.FromArgb("#FF6B6B"),
                FontSize = 12,
                WidthRequest = 30,
                HeightRequest = 30,
                CornerRadius = 6,
                Padding = 0
            };
            deleteBtn.Clicked += async (s, ev) =>
            {
                bool confirm = await DisplayAlertAsync(
                    loc.GetString("Recorder_Delete_Title", "Supprimer la prise"),
                    string.Format(loc.GetString("Recorder_Delete_Confirm", "Supprimer définitivement '{0}' ?"), take.FileName),
                    loc.GetString("Common_Delete", "Supprimer"),
                    loc.GetString("Common_Cancel", "Annuler"));
                if (!confirm) return;

                if (AudioRecorderService.Instance.PlayingAudioFileId == take.Id)
                {
                    AudioRecorderService.Instance.StopPreview();
                }

                string absPath = _settingsService.GetAbsolutePath(take.FilePath, isAudio: true);
                await _databaseService.DeleteAudioFileAsync(take);
                _score.AudioFiles.Remove(take);
                try { if (File.Exists(absPath)) File.Delete(absPath); } catch { }

                RefreshRecorderTakesUI();
                UpdateToolsDrawerUI();
            };

            rowGrid.Add(playBtn, 0, 0);
            rowGrid.Add(titleLabel, 1, 0);
            rowGrid.Add(renameBtn, 2, 0);
            rowGrid.Add(deleteBtn, 3, 0);

            rowBorder.Content = rowGrid;
            RecorderTakesList.Children.Add(rowBorder);
        }
    }

    private void CleanupRecorderOnExit()
    {
        try
        {
            if (_isRecorderEventsSubscribed)
            {
                AudioRecorderService.Instance.PreviewPlaybackEnded -= OnRecorderPreviewEnded;
                _isRecorderEventsSubscribed = false;
            }
            StopRecorderTimer();
            AudioRecorderService.Instance.StopPreview();
            if (AudioRecorderService.Instance.IsRecording)
            {
                _ = AudioRecorderService.Instance.CancelRecordingAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Recorder] Cleanup error: {ex.Message}");
        }
    }
}
