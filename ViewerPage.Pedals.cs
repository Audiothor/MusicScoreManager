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
    private async void OnPedalActionTriggered(PedalAction action)
    {
        try
        {
            switch (action)
            {
                case PedalAction.NextPage:
                    OnNextTapped(this, EventArgs.Empty);
                    break;

                case PedalAction.PreviousPage:
                    OnPrevTapped(this, EventArgs.Empty);
                    break;

                case PedalAction.ScrollDown:
                    if (_score.Type == ScoreType.PDF && PdfWebView != null && PdfWebView.IsVisible)
                    {
                        _ = PdfWebView.EvaluateJavaScriptAsync("window.scrollBy({ top: window.innerHeight * 0.5, behavior: 'smooth' });");
                    }
                    break;

                case PedalAction.ScrollUp:
                    if (_score.Type == ScoreType.PDF && PdfWebView != null && PdfWebView.IsVisible)
                    {
                        _ = PdfWebView.EvaluateJavaScriptAsync("window.scrollBy({ top: -window.innerHeight * 0.5, behavior: 'smooth' });");
                    }
                    break;

                case PedalAction.FirstPage:
                    await GoToPage(1);
                    break;

                case PedalAction.LastPage:
                    await GoToPage(_maxPages);
                    break;

                case PedalAction.NextScore:
                    if (_setlistScores != null && _currentIndex < _setlistScores.Count - 1 && !_isSwitchingScore)
                    {
                        _isSwitchingScore = true;
                        try
                        {
                            int nextIndex = _currentIndex + 1;
                            var nextScore = _setlistScores[nextIndex];
                            await SwitchToScoreAsync(nextScore, nextIndex, toLastPage: false);
                        }
                        finally
                        {
                            _isSwitchingScore = false;
                        }
                    }
                    break;

                case PedalAction.PreviousScore:
                    if (_setlistScores != null && _currentIndex > 0 && !_isSwitchingScore)
                    {
                        _isSwitchingScore = true;
                        try
                        {
                            int prevIndex = _currentIndex - 1;
                            var prevScore = _setlistScores[prevIndex];
                            await SwitchToScoreAsync(prevScore, prevIndex, toLastPage: false);
                        }
                        finally
                        {
                            _isSwitchingScore = false;
                        }
                    }
                    break;

                case PedalAction.ToggleMetronome:
                    if (_isMetronomePlaying)
                    {
                        StopMetronome();
                    }
                    else
                    {
                        StartMetronome();
                    }
                    break;

                case PedalAction.ToggleMetronomeSound:
                    _score.HasMetronomeSound = !_score.HasMetronomeSound;
                    if ((_score.HasMetronomeSound || _score.ShowMetronome) && !_isMetronomePlaying)
                    {
                        StartMetronome();
                    }
                    else if (!_score.HasMetronomeSound && !_score.ShowMetronome)
                    {
                        StopMetronome();
                    }
                    await _databaseService.SaveScoreAsync(_score);
                    break;

                case PedalAction.ToggleAudio:
                    OnAudioPlayPauseClicked(this, EventArgs.Empty);
                    break;

                case PedalAction.RestartAudio:
                    try
                    {
                        await AudioPlayer.SeekTo(TimeSpan.Zero);
                        if (!_isAudioPlaying)
                        {
                            OnAudioPlayPauseClicked(this, EventArgs.Empty);
                        }
                    }
                    catch { }
                    break;

                case PedalAction.ResetZoom:
                    OnResetZoomClicked(this, EventArgs.Empty);
                    break;

                case PedalAction.OpenPageJump:
                    OnGoToPageClicked(this, EventArgs.Empty);
                    break;

                case PedalAction.ToggleAnnotationsLock:
                    if (_isAnnotationsLocked)
                    {
                        UnlockAnnotations();
                    }
                    else
                    {
                        LockAnnotations();
                    }
                    break;

                case PedalAction.UndoAnnotation:
                    OnUndoAnnotationClicked(this, EventArgs.Empty);
                    break;

                case PedalAction.RedoAnnotation:
                    OnRedoAnnotationClicked(this, EventArgs.Empty);
                    break;

                case PedalAction.OpenQuickMenu:
                    ShowMenu();
                    break;

                case PedalAction.CloseViewer:
                    await Navigation.PopAsync();
                    break;

                case PedalAction.NextBookmark:
                    await GoToNextBookmarkAsync();
                    break;

                case PedalAction.PreviousBookmark:
                    await GoToPreviousBookmarkAsync();
                    break;

                case PedalAction.OpenSetlistDrawer:
                    ToggleSetlistProgressOverlay();
                    break;

                case PedalAction.ToggleNightMode:
                    var loc = LocalizationService.Instance;
        await DisplayAlertAsync(loc.GetString("Viewer_NightMode_Title", "Mode Nuit"), loc.GetString("Viewer_NightMode_Future", "L'activation du mode nuit sera disponible dans une prochaine mise à jour."), loc.GetString("Common_OK", "OK"));
                    break;

                case PedalAction.ZoomIn:
                    await ZoomInAsync();
                    break;

                case PedalAction.ZoomOut:
                    await ZoomOutAsync();
                    break;

                case PedalAction.ToggleAnnotationsVisibility:
                    await ToggleAnnotationsVisibilityAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur action pédale/MIDI ({action}): {ex.Message}");
        }
    }

    private async Task ZoomInAsync()
    {
        try
        {
            double targetScale = Math.Min(3.5, ZoomLayout.Scale + 0.25);
            currentScale = targetScale;
            _isZoomed = targetScale > 1.05;
            await ZoomLayout.ScaleToAsync(targetScale, 180, Easing.CubicOut);
        }
        catch { }
    }

    private async Task ZoomOutAsync()
    {
        try
        {
            double targetScale = Math.Max(1.0, ZoomLayout.Scale - 0.25);
            currentScale = targetScale;
            _isZoomed = targetScale > 1.05;
            if (targetScale <= 1.05)
            {
                _ = ZoomLayout.TranslateToAsync(0, 0, 180, Easing.CubicOut);
            }
            await ZoomLayout.ScaleToAsync(targetScale, 180, Easing.CubicOut);
        }
        catch { }
    }

    private async Task ToggleAnnotationsVisibilityAsync()
    {
        try
        {
            _score.ShowAnnotations = !_score.ShowAnnotations;
            UpdateToolsDrawerUI();
            ApplyAnnotationsVisibility(_score.ShowAnnotations);
            await _databaseService.SaveScoreAsync(_score);
        }
        catch { }
    }

    private void ToggleSetlistProgressOverlay()
    {
        try
        {
            if (SetlistProgressModal != null && SetlistProgressModal.IsVisible)
            {
                HideSetlistProgressOverlay();
            }
            else if (_setlistScores != null && _setlistScores.Count > 0)
            {
                ShowSetlistProgressOverlay();
            }
        }
        catch { }
    }

}
