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
    private (double x, double y) PageToScreen(double rx, double ry, int pageNumber, double containerW, double containerH)
    {
        if (containerW > 0 && containerH > 0 && _currentRenderedWidth > 0 && _currentRenderedHeight > 0)
        {
            UpdateCanvasBounds(containerW, containerH, _currentRenderedWidth, _currentRenderedHeight);
        }

        var bounds = new AnnotationGeometryHelper.CanvasBounds(_canvasXRel, _canvasYRel, _canvasWRel, _canvasHRel);
        return AnnotationGeometryHelper.PageToScreen(
            rx, ry, pageNumber,
            _score.Type == ScoreType.PDF && _isTwoPagesMode,
            _rightPageNumber,
            bounds, containerW, containerH);
    }

    private (int pageNumber, double rx, double ry) ScreenToPage(double absX, double absY, double containerW, double containerH)
    {
        if (containerW <= 0 || containerH <= 0) return (_currentPage, 0.5, 0.5);

        if (_currentRenderedWidth > 0 && _currentRenderedHeight > 0)
        {
            UpdateCanvasBounds(containerW, containerH, _currentRenderedWidth, _currentRenderedHeight);
        }

        var bounds = new AnnotationGeometryHelper.CanvasBounds(_canvasXRel, _canvasYRel, _canvasWRel, _canvasHRel);
        return AnnotationGeometryHelper.ScreenToPage(
            absX, absY,
            _score.Type == ScoreType.PDF && _isTwoPagesMode,
            _currentPage, _leftPageNumber, _rightPageNumber, _hasRightPage,
            bounds, containerW, containerH);
    }

    private (double rx, double ry) ScreenToPageCoords(double absX, double absY, int targetPage, double containerW, double containerH)
    {
        if (containerW <= 0 || containerH <= 0) return (0.5, 0.5);

        if (_currentRenderedWidth > 0 && _currentRenderedHeight > 0)
        {
            UpdateCanvasBounds(containerW, containerH, _currentRenderedWidth, _currentRenderedHeight);
        }

        var bounds = new AnnotationGeometryHelper.CanvasBounds(_canvasXRel, _canvasYRel, _canvasWRel, _canvasHRel);
        return AnnotationGeometryHelper.ScreenToPageCoords(
            absX, absY, targetPage,
            _score.Type == ScoreType.PDF && _isTwoPagesMode,
            _rightPageNumber,
            bounds, containerW, containerH);
    }

    private double GetPageDisplayScale()
    {
        if (_score.Type == ScoreType.PDF && _isTwoPagesMode)
        {
            if (ActiveAnnotationsContainer != null && ActiveAnnotationsContainer.Width > 0 && ActiveAnnotationsContainer.Height > 0)
            {
                double containerW = ActiveAnnotationsContainer.Width;
                double containerH = ActiveAnnotationsContainer.Height;
                double singlePageW = (_canvasWRel / 2.0) * containerW;
                double portraitRefW = Math.Min(containerW, containerH) * _canvasWRel;
                if (portraitRefW > 0 && singlePageW > 0)
                {
                    return Math.Clamp(singlePageW / portraitRefW, 0.50, 0.85);
                }
            }
            return 0.707;
        }
        return 1.0;
    }

    private void UnlockAnnotations()
    {
        _isAnnotationsLocked = false;
        if (ActiveAnnotationsContainer != null)
        {
            ActiveAnnotationsContainer.IsVisible = true;
            ActiveAnnotationsContainer.InputTransparent = false;
        }
        if (AnnotationsContainer != null)
        {
            AnnotationsContainer.IsVisible = true;
            AnnotationsContainer.InputTransparent = false;
        }
    }

    private void LockAnnotations()
    {
        _isAnnotationsLocked = true;
        if (ActiveAnnotationsContainer != null)
        {
            ActiveAnnotationsContainer.IsVisible = true;
#if ANDROID
            ActiveAnnotationsContainer.InputTransparent = (PdfWebView != null && PdfWebView.IsVisible);
#else
            ActiveAnnotationsContainer.InputTransparent = true;
#endif
        }
    }



    private double _barYBase = 0;
    private void OnAnnotationBarPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _barYBase = AnnotationBar.TranslationY;
                break;
            case GestureStatus.Running:
                double newY = _barYBase + e.TotalY;
                
                // Limiter la translation pour éviter que la barre sorte de l'écran
                // Ne pas dépasser le bas (newY = 0) ni le haut (margin de sécurité de 80px)
                double limitTop = -500; // Par défaut si la hauteur n'est pas encore lue
                if (this.Height > 0)
                {
                    limitTop = -this.Height + 80;
                }
                
                newY = Math.Max(limitTop, Math.Min(0, newY));
                
                AnnotationBar.TranslationY = newY;
                StickerPickerOverlay.TranslationY = newY;
                HighlightOptionsOverlay.TranslationY = newY;
                DrawOptionsOverlay.TranslationY = newY;
                TextOptionsOverlay.TranslationY = newY;
                break;
        }
    }

    private void OnAnnotationToggleTapped(object sender, EventArgs e)
    {
        AnnotationBar.IsVisible = !AnnotationBar.IsVisible;
        BottomTouchBar.IsVisible = !AnnotationBar.IsVisible; // Masque la zone tactile quand la vraie barre d'annotations est ouverte, et inversement

        if (AnnotationBar.IsVisible && !_score.ShowAnnotations)
        {
            _score.ShowAnnotations = true;
            UpdateToolsDrawerUI();
            ApplyAnnotationsVisibility(true);
            _ = _databaseService.SaveScoreAsync(_score);
        }

        UpdateToolsDrawerUI();

        if (!AnnotationBar.IsVisible)
        {
            StickerPickerOverlay.IsVisible = false;
            HighlightOptionsOverlay.IsVisible = false;
            DrawOptionsOverlay.IsVisible = false;
            TextOptionsOverlay.IsVisible = false;
            _isHighlightMode = false;
            _isDrawMode = false;
            _isTextMode = false;
            HighlightBtn.BackgroundColor = Colors.Transparent;
            DrawBtn.BackgroundColor = Colors.Transparent;
            TextAnnotationBtn.BackgroundColor = Colors.Transparent;
            AnnotationBar.TranslationY = 0;
            StickerPickerOverlay.TranslationY = 0;
            HighlightOptionsOverlay.TranslationY = 0;
            DrawOptionsOverlay.TranslationY = 0;
            TextOptionsOverlay.TranslationY = 0;
            _selectedAnnotation = null;
            _pendingSticker = null;
            _isAnnotationMode = false;
            LockAnnotations();

            if (StickersCollection != null)
            {
                StickersCollection.SelectedItem = null;
            }

            RenderAnnotations();
        }
        else
        {
            UnlockAnnotations();
            AnnotationsContainer.InputTransparent = false;
            AnnotationsContainer.Opacity = 1;
            AnnotationsContainer.IsVisible = true;
            if (ActiveAnnotationsContainer != null)
            {
                ActiveAnnotationsContainer.InputTransparent = false;
                ActiveAnnotationsContainer.Opacity = 1;
                ActiveAnnotationsContainer.IsVisible = true;
            }
#if ANDROID
            SetupNativeTouchHandling();
#endif
        }

        // Ajuster la position de l'indicateur de page
        PageIndicator.Margin = new Thickness(0, 0, 10, AnnotationBar.IsVisible ? 65 : 2);
    }

    private bool _isHighlightMode = false;
    private string _currentHighlightColor = "#FFFF00";
    private double _currentHighlightThickness = 30.0;
    private double _currentHighlightOpacity = Preferences.Default.Get("HighlightOpacity", 0.28);
    private Microsoft.Maui.Controls.Shapes.Polyline? _activeLivePolyline = null;
    private List<Point> _activeLivePoints = new();
    private bool _isFinishingStroke = false;

    private void OnAnnotationHighlightClicked(object sender, EventArgs e)
    {
        UnlockAnnotations();

        _isHighlightMode = !_isHighlightMode;
        HighlightOptionsOverlay.IsVisible = _isHighlightMode;
        if (_isHighlightMode)
        {
            _score.ShowAnnotations = true;
            if (AnnotationsContainer != null)
            {
                AnnotationsContainer.Opacity = 1;
                AnnotationsContainer.IsVisible = true;
                AnnotationsContainer.InputTransparent = false;
            }
            if (ActiveAnnotationsContainer != null)
            {
                ActiveAnnotationsContainer.Opacity = 1;
                ActiveAnnotationsContainer.IsVisible = true;
                ActiveAnnotationsContainer.InputTransparent = false;
            }
            RenderAnnotations();

            UpdateAnnotationOverlaysLayout(this.Width, this.Height);
            HighlightOptionsOverlay.TranslationY = AnnotationBar.TranslationY;
            _isDrawMode = false;
            DrawOptionsOverlay.IsVisible = false;
            DrawBtn.BackgroundColor = Colors.Transparent;

            _isTextMode = false;
            TextOptionsOverlay.IsVisible = false;
            TextAnnotationBtn.BackgroundColor = Colors.Transparent;

            _isAnnotationMode = false;
            _pendingSticker = null;
            StickerPickerOverlay.IsVisible = false;
            DeactivateEraserMode();

            UpdateHighlightOpacityButtons();
            HighlightBtn.BackgroundColor = Color.FromArgb("#40007ACC");
        }
        else
        {
            HighlightBtn.BackgroundColor = Colors.Transparent;
        }
    }

    private void OnHighlightColorSelected(object? sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is string color)
        {
            _currentHighlightColor = color;
            UpdateHighlightColorBorders();
        }
    }

    private void UpdateHighlightColorBorders()
    {
        HighlightYellowBtn.Stroke = _currentHighlightColor == "#FFFF00" ? Colors.White : Colors.Transparent;
        HighlightYellowBtn.StrokeThickness = _currentHighlightColor == "#FFFF00" ? 3 : 2;

        HighlightGreenBtn.Stroke = _currentHighlightColor == "#00E676" ? Colors.White : Colors.Transparent;
        HighlightGreenBtn.StrokeThickness = _currentHighlightColor == "#00E676" ? 3 : 2;

        HighlightBlueBtn.Stroke = _currentHighlightColor == "#00B0FF" ? Colors.White : Colors.Transparent;
        HighlightBlueBtn.StrokeThickness = _currentHighlightColor == "#00B0FF" ? 3 : 2;

        HighlightRedBtn.Stroke = _currentHighlightColor == "#FF4081" ? Colors.White : Colors.Transparent;
        HighlightRedBtn.StrokeThickness = _currentHighlightColor == "#FF4081" ? 3 : 2;
    }

    private void OnHighlightSizeSelected(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string sizeStr && double.TryParse(sizeStr, out double size))
        {
            _currentHighlightThickness = size;
            HighlightSizeSmallBtn.BackgroundColor = (size == 15) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
            HighlightSizeSmallBtn.FontAttributes = (size == 15) ? FontAttributes.Bold : FontAttributes.None;

            HighlightSizeMediumBtn.BackgroundColor = (size == 30) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
            HighlightSizeMediumBtn.FontAttributes = (size == 30) ? FontAttributes.Bold : FontAttributes.None;

            HighlightSizeLargeBtn.BackgroundColor = (size == 50) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
            HighlightSizeLargeBtn.FontAttributes = (size == 50) ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    private async void OnHighlightOpacitySelected(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string opStr && double.TryParse(opStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double opacity))
        {
            _currentHighlightOpacity = opacity;
            Preferences.Default.Set("HighlightOpacity", opacity);
            UpdateHighlightOpacityButtons();

            if (_selectedAnnotation != null && _selectedAnnotation.Category == "Highlight")
            {
                _selectedAnnotation.Opacity = opacity;
                await _databaseService.SaveAnnotationAsync(_selectedAnnotation);
                RenderAnnotations();
            }
        }
    }

    private void UpdateHighlightOpacityButtons()
    {
        if (HighlightOpacityLightBtn == null || HighlightOpacityMediumBtn == null || HighlightOpacityDarkBtn == null) return;

        HighlightOpacityLightBtn.BackgroundColor = (Math.Abs(_currentHighlightOpacity - 0.28) < 0.05) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        HighlightOpacityLightBtn.FontAttributes = (Math.Abs(_currentHighlightOpacity - 0.28) < 0.05) ? FontAttributes.Bold : FontAttributes.None;

        HighlightOpacityMediumBtn.BackgroundColor = (Math.Abs(_currentHighlightOpacity - 0.50) < 0.05) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        HighlightOpacityMediumBtn.FontAttributes = (Math.Abs(_currentHighlightOpacity - 0.50) < 0.05) ? FontAttributes.Bold : FontAttributes.None;

        HighlightOpacityDarkBtn.BackgroundColor = (Math.Abs(_currentHighlightOpacity - 0.75) < 0.05) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        HighlightOpacityDarkBtn.FontAttributes = (Math.Abs(_currentHighlightOpacity - 0.75) < 0.05) ? FontAttributes.Bold : FontAttributes.None;
    }

    private void OnCloseHighlightOptionsClicked(object sender, EventArgs e)
    {
        HighlightOptionsOverlay.IsVisible = false;
        _isHighlightMode = false;
        HighlightBtn.BackgroundColor = Colors.Transparent;
    }

    private void OnHighlightPointerPressed(object? sender, PointerEventArgs e)
    {
#if !ANDROID
        if (_isAnnotationsLocked) return;
        var pos = e.GetPosition(ActiveAnnotationsContainer);
        if (pos == null) return;

        if (_isEraserMode)
        {
            _isPointerEraserPressed = true;
            EraseAnnotationsAtPoint(pos.Value.X, pos.Value.Y);
            return;
        }

        if ((!_isHighlightMode && !_isDrawMode)) return;

        if (_isHighlightMode) StartLiveHighlightStroke(pos.Value);
        else if (_isDrawMode) StartLiveDrawStroke(pos.Value);
#endif
    }

    private void OnHighlightPointerMoved(object? sender, PointerEventArgs e)
    {
#if !ANDROID
        if (_isAnnotationsLocked) return;
        var pos = e.GetPosition(ActiveAnnotationsContainer);
        if (pos == null) return;

        if (_isEraserMode)
        {
            if (_isPointerEraserPressed)
            {
                EraseAnnotationsAtPoint(pos.Value.X, pos.Value.Y);
            }
            return;
        }

        if ((!_isHighlightMode && !_isDrawMode) || _activeLivePolyline == null) return;

        if (_isHighlightMode) AddLiveHighlightPoint(pos.Value);
        else if (_isDrawMode) AddLiveDrawPoint(pos.Value);
#endif
    }

    private async void OnHighlightPointerReleased(object? sender, PointerEventArgs e)
    {
#if !ANDROID
        _isPointerEraserPressed = false;
        if ((!_isHighlightMode && !_isDrawMode) || _isAnnotationsLocked || _activeLivePolyline == null) return;
        if (_isHighlightMode) await FinishLiveHighlightStrokeAsync();
        else if (_isDrawMode) await FinishLiveDrawStrokeAsync();
#endif
    }

    private void OnHighlightPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        // Désactivé pour éviter les déclenchements concurrents en double avec Pointer et Native Touch
    }

    private void StartLiveHighlightStroke(Point pt)
    {
        _activeLivePoints.Clear();
        _activeLivePoints.Add(pt);

        double displayScale = GetPageDisplayScale();
        _activeLivePolyline = new Microsoft.Maui.Controls.Shapes.Polyline
        {
            Stroke = ParseColor(_currentHighlightColor),
            StrokeThickness = _currentHighlightThickness * displayScale,
            StrokeLineCap = Microsoft.Maui.Controls.Shapes.PenLineCap.Flat,
            StrokeLineJoin = Microsoft.Maui.Controls.Shapes.PenLineJoin.Round,
            Opacity = _currentHighlightOpacity,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            ZIndex = 50,
            Points = new PointCollection { pt, new Point(pt.X + 0.5, pt.Y) }
        };

        AbsoluteLayout.SetLayoutFlags(_activeLivePolyline, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(_activeLivePolyline, new Rect(0, 0, 1, 1));
        ActiveAnnotationsContainer.Children.Add(_activeLivePolyline);
    }

    private void AddLiveHighlightPoint(Point pt)
    {
        if (_activeLivePoints.Count > 0)
        {
            var last = _activeLivePoints[_activeLivePoints.Count - 1];
            double dist = Math.Sqrt(Math.Pow(pt.X - last.X, 2) + Math.Pow(pt.Y - last.Y, 2));
            if (dist < 3.0) return;
        }

        _activeLivePoints.Add(pt);
        if (_activeLivePolyline != null)
        {
            _activeLivePolyline.Points = new PointCollection(_activeLivePoints.ToArray());
#if ANDROID
            if (_activeLivePolyline.Handler?.PlatformView is Android.Views.View nv)
            {
                nv.Invalidate();
            }
#endif
        }
    }

    private async Task FinishLiveHighlightStrokeAsync()
    {
        if (_isFinishingStroke) return;
        _isFinishingStroke = true;
        try
        {
            if (_activeLivePoints.Count == 1)
            {
                var p0 = _activeLivePoints[0];
                _activeLivePoints.Add(new Point(p0.X + 1.0, p0.Y));
            }

            double containerW = ActiveAnnotationsContainer.Width > 0 ? ActiveAnnotationsContainer.Width : (AnnotationsContainer.Width > 0 ? AnnotationsContainer.Width : this.Width);
            double containerH = ActiveAnnotationsContainer.Height > 0 ? ActiveAnnotationsContainer.Height : (AnnotationsContainer.Height > 0 ? AnnotationsContainer.Height : this.Height);

            if (_activeLivePoints.Count < 2 || containerW <= 0 || containerH <= 0)
            {
                if (_activeLivePolyline != null)
                {
                    ActiveAnnotationsContainer.Children.Remove(_activeLivePolyline);
                    _activeLivePolyline = null;
                }
                _activeLivePoints.Clear();
                return;
            }

            var (targetPage, _, _) = ScreenToPage(_activeLivePoints[0].X, _activeLivePoints[0].Y, containerW, containerH);

            string content = string.Join(";", _activeLivePoints.Select(p => 
            {
                var (rx, ry) = ScreenToPageCoords(p.X, p.Y, targetPage, containerW, containerH);
                return $"{rx.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)},{ry.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}";
            }));

            var annotation = new Annotation
            {
                ScoreId = _score.Id,
                Type = AnnotationType.Drawing,
                Category = "Highlight",
                Content = content,
                Color = _currentHighlightColor,
                Scale = _currentHighlightThickness / 30.0,
                Opacity = _currentHighlightOpacity,
                PageNumber = targetPage
            };

            _lastSinglePageNumber = targetPage;
            await _databaseService.SaveAnnotationAsync(annotation);
            _annotations.Add(annotation);

            _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Add, Annotation = annotation });
            _redoStack.Clear();
            UpdateUndoRedoButtons();

            if (_activeLivePolyline != null)
            {
                ActiveAnnotationsContainer.Children.Remove(_activeLivePolyline);
                _activeLivePolyline = null;
            }
            _activeLivePoints.Clear();

            RenderAnnotations();
        }
        finally
        {
            _isFinishingStroke = false;
        }
    }

    #region Drawing Mode (Pencil)

    private bool _isDrawMode = false;
    private string _currentDrawColor = "#000000";
    private double _currentDrawThickness = 9.0; // 3mm par défaut (3px/mm : 1mm=3, 2mm=6, 3mm=9, 4mm=12, 5mm=15)
    private double _currentDrawOpacity = Preferences.Default.Get("DrawOpacity", 1.0);

    private void OnAnnotationDrawClicked(object sender, EventArgs e)
    {
        UnlockAnnotations();

        _isDrawMode = !_isDrawMode;
        DrawOptionsOverlay.IsVisible = _isDrawMode;
        if (_isDrawMode)
        {
            _score.ShowAnnotations = true;
            if (AnnotationsContainer != null)
            {
                AnnotationsContainer.Opacity = 1;
                AnnotationsContainer.IsVisible = true;
                AnnotationsContainer.InputTransparent = false;
            }
            if (ActiveAnnotationsContainer != null)
            {
                ActiveAnnotationsContainer.Opacity = 1;
                ActiveAnnotationsContainer.IsVisible = true;
                ActiveAnnotationsContainer.InputTransparent = false;
            }
            RenderAnnotations();

            UpdateAnnotationOverlaysLayout(this.Width, this.Height);
            DrawOptionsOverlay.TranslationY = AnnotationBar.TranslationY;
            _isHighlightMode = false;
            HighlightOptionsOverlay.IsVisible = false;
            HighlightBtn.BackgroundColor = Colors.Transparent;

            _isTextMode = false;
            TextOptionsOverlay.IsVisible = false;
            TextAnnotationBtn.BackgroundColor = Colors.Transparent;

            _isAnnotationMode = false;
            _pendingSticker = null;
            StickerPickerOverlay.IsVisible = false;
            DeactivateEraserMode();

            UpdateDrawOpacityButtons();
            DrawBtn.BackgroundColor = Color.FromArgb("#40007ACC");
        }
        else
        {
            DrawBtn.BackgroundColor = Colors.Transparent;
        }
    }

    private void OnDrawColorSelected(object? sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is string color)
        {
            _currentDrawColor = color;
            UpdateDrawColorBorders();
        }
    }

    private void UpdateDrawColorBorders()
    {
        DrawBlackBtn.Stroke = _currentDrawColor == "#000000" ? Colors.White : Colors.Transparent;
        DrawBlackBtn.StrokeThickness = _currentDrawColor == "#000000" ? 2 : 1;

        DrawRedBtn.Stroke = _currentDrawColor == "#E53935" ? Colors.White : Colors.Transparent;
        DrawRedBtn.StrokeThickness = _currentDrawColor == "#E53935" ? 2 : 1;

        DrawBlueBtn.Stroke = _currentDrawColor == "#1E88E5" ? Colors.White : Colors.Transparent;
        DrawBlueBtn.StrokeThickness = _currentDrawColor == "#1E88E5" ? 2 : 1;

        DrawGreenBtn.Stroke = _currentDrawColor == "#43A047" ? Colors.White : Colors.Transparent;
        DrawGreenBtn.StrokeThickness = _currentDrawColor == "#43A047" ? 2 : 1;

        DrawYellowBtn.Stroke = _currentDrawColor == "#FFD600" ? Colors.White : Colors.Transparent;
        DrawYellowBtn.StrokeThickness = _currentDrawColor == "#FFD600" ? 2 : 1;

        DrawWhiteBtn.Stroke = _currentDrawColor == "#FFFFFF" ? Colors.Gray : Colors.Transparent;
        DrawWhiteBtn.StrokeThickness = _currentDrawColor == "#FFFFFF" ? 2 : 1;
    }

    private void OnDrawSizeSelected(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string sizeStr && double.TryParse(sizeStr, out double size))
        {
            _currentDrawThickness = size;
            DrawSize1Btn.BackgroundColor = (size == 3) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
            DrawSize1Btn.FontAttributes = (size == 3) ? FontAttributes.Bold : FontAttributes.None;

            DrawSize2Btn.BackgroundColor = (size == 6) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
            DrawSize2Btn.FontAttributes = (size == 6) ? FontAttributes.Bold : FontAttributes.None;

            DrawSize3Btn.BackgroundColor = (size == 9) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
            DrawSize3Btn.FontAttributes = (size == 9) ? FontAttributes.Bold : FontAttributes.None;

            DrawSize4Btn.BackgroundColor = (size == 12) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
            DrawSize4Btn.FontAttributes = (size == 12) ? FontAttributes.Bold : FontAttributes.None;

            DrawSize5Btn.BackgroundColor = (size == 15) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
            DrawSize5Btn.FontAttributes = (size == 15) ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    private async void OnDrawOpacitySelected(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string opStr && double.TryParse(opStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double opacity))
        {
            _currentDrawOpacity = opacity;
            Preferences.Default.Set("DrawOpacity", opacity);
            UpdateDrawOpacityButtons();

            if (_selectedAnnotation != null && _selectedAnnotation.Category == "Pencil")
            {
                _selectedAnnotation.Opacity = opacity;
                await _databaseService.SaveAnnotationAsync(_selectedAnnotation);
                RenderAnnotations();
            }
        }
    }

    private void UpdateDrawOpacityButtons()
    {
        if (DrawOpacityLightBtn == null || DrawOpacityMediumBtn == null || DrawOpacityDarkBtn == null) return;

        DrawOpacityLightBtn.BackgroundColor = (Math.Abs(_currentDrawOpacity - 0.40) < 0.05) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        DrawOpacityLightBtn.FontAttributes = (Math.Abs(_currentDrawOpacity - 0.40) < 0.05) ? FontAttributes.Bold : FontAttributes.None;

        DrawOpacityMediumBtn.BackgroundColor = (Math.Abs(_currentDrawOpacity - 0.70) < 0.05) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        DrawOpacityMediumBtn.FontAttributes = (Math.Abs(_currentDrawOpacity - 0.70) < 0.05) ? FontAttributes.Bold : FontAttributes.None;

        DrawOpacityDarkBtn.BackgroundColor = (Math.Abs(_currentDrawOpacity - 1.0) < 0.05) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        DrawOpacityDarkBtn.FontAttributes = (Math.Abs(_currentDrawOpacity - 1.0) < 0.05) ? FontAttributes.Bold : FontAttributes.None;
    }

    private void OnCloseDrawOptionsClicked(object sender, EventArgs e)
    {
        DrawOptionsOverlay.IsVisible = false;
        _isDrawMode = false;
        DrawBtn.BackgroundColor = Colors.Transparent;
    }

    private void StartLiveDrawStroke(Point pt)
    {
        _activeLivePoints.Clear();
        _activeLivePoints.Add(pt);

        double displayScale = GetPageDisplayScale();
        _activeLivePolyline = new Microsoft.Maui.Controls.Shapes.Polyline
        {
            Stroke = ParseColor(_currentDrawColor),
            StrokeThickness = _currentDrawThickness * displayScale,
            StrokeLineCap = Microsoft.Maui.Controls.Shapes.PenLineCap.Round,
            StrokeLineJoin = Microsoft.Maui.Controls.Shapes.PenLineJoin.Round,
            Opacity = _currentDrawOpacity,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            ZIndex = 50,
            Points = new PointCollection { pt, new Point(pt.X + 0.5, pt.Y) }
        };

        AbsoluteLayout.SetLayoutFlags(_activeLivePolyline, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(_activeLivePolyline, new Rect(0, 0, 1, 1));
        ActiveAnnotationsContainer.Children.Add(_activeLivePolyline);
    }

    private void AddLiveDrawPoint(Point pt)
    {
        if (_activeLivePoints.Count > 0)
        {
            var last = _activeLivePoints[_activeLivePoints.Count - 1];
            double dist = Math.Sqrt(Math.Pow(pt.X - last.X, 2) + Math.Pow(pt.Y - last.Y, 2));
            if (dist < 2.0) return;
        }

        _activeLivePoints.Add(pt);
        if (_activeLivePolyline != null)
        {
            _activeLivePolyline.Points = new PointCollection(_activeLivePoints.ToArray());
#if ANDROID
            if (_activeLivePolyline.Handler?.PlatformView is Android.Views.View nv)
            {
                nv.Invalidate();
            }
#endif
        }
    }

    private async Task FinishLiveDrawStrokeAsync()
    {
        if (_isFinishingStroke) return;
        _isFinishingStroke = true;
        try
        {
            if (_activeLivePoints.Count == 1)
            {
                var p0 = _activeLivePoints[0];
                _activeLivePoints.Add(new Point(p0.X + 1.0, p0.Y));
            }

            double containerW = ActiveAnnotationsContainer.Width > 0 ? ActiveAnnotationsContainer.Width : (AnnotationsContainer.Width > 0 ? AnnotationsContainer.Width : this.Width);
            double containerH = ActiveAnnotationsContainer.Height > 0 ? ActiveAnnotationsContainer.Height : (AnnotationsContainer.Height > 0 ? AnnotationsContainer.Height : this.Height);

            if (_activeLivePoints.Count < 2 || containerW <= 0 || containerH <= 0)
            {
                if (_activeLivePolyline != null)
                {
                    ActiveAnnotationsContainer.Children.Remove(_activeLivePolyline);
                    _activeLivePolyline = null;
                }
                _activeLivePoints.Clear();
                return;
            }

            var (targetPage, _, _) = ScreenToPage(_activeLivePoints[0].X, _activeLivePoints[0].Y, containerW, containerH);

            string content = string.Join(";", _activeLivePoints.Select(p => 
            {
                var (rx, ry) = ScreenToPageCoords(p.X, p.Y, targetPage, containerW, containerH);
                return $"{rx.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)},{ry.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}";
            }));

            var annotation = new Annotation
            {
                ScoreId = _score.Id,
                Type = AnnotationType.Drawing,
                Category = "Pencil",
                Content = content,
                Color = _currentDrawColor,
                Scale = _currentDrawThickness / 9.0,
                Opacity = _currentDrawOpacity,
                PageNumber = targetPage
            };

            _lastSinglePageNumber = targetPage;
            await _databaseService.SaveAnnotationAsync(annotation);
            _annotations.Add(annotation);

            _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Add, Annotation = annotation });
            _redoStack.Clear();
            UpdateUndoRedoButtons();

            if (_activeLivePolyline != null)
            {
                ActiveAnnotationsContainer.Children.Remove(_activeLivePolyline);
                _activeLivePolyline = null;
            }
            _activeLivePoints.Clear();

            RenderAnnotations();
        }
        finally
        {
            _isFinishingStroke = false;
        }
    }

    #endregion

    #region Text Mode (T)

    private bool _isTextMode = false;
    private string _currentTextColor = "#000000";
    private string _currentTextBgColor = "Transparent";
    private string _currentTextBorderColor = "Transparent";
    private double _currentTextSize = 14.0; // 8, 10, 12, 14, 16, 18 (14 par défaut)

    private void OnAnnotationTextClicked(object sender, EventArgs e)
    {
        UnlockAnnotations();

        _isTextMode = !_isTextMode;
        TextOptionsOverlay.IsVisible = _isTextMode;
        if (_isTextMode)
        {
            UpdateTextColorBorders();
            UpdateTextSizeButtons();
            UpdateTextBgBorders();
            UpdateTextBorderBorders();
            UpdateAnnotationOverlaysLayout(this.Width, this.Height);
            TextOptionsOverlay.TranslationY = AnnotationBar.TranslationY;
            _isHighlightMode = false;
            HighlightOptionsOverlay.IsVisible = false;
            HighlightBtn.BackgroundColor = Colors.Transparent;

            _isDrawMode = false;
            DrawOptionsOverlay.IsVisible = false;
            DrawBtn.BackgroundColor = Colors.Transparent;

            _isAnnotationMode = false;
            _pendingSticker = null;
            StickerPickerOverlay.IsVisible = false;
            DeactivateEraserMode();

            TextAnnotationBtn.BackgroundColor = Color.FromArgb("#40007ACC");
            ActiveAnnotationsContainer.InputTransparent = false;
        }
        else
        {
            TextAnnotationBtn.BackgroundColor = Colors.Transparent;
        }
    }

    private async void OnTextColorSelected(object? sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is string color)
        {
            _currentTextColor = color;
            UpdateTextColorBorders();

            // Modification dynamique du texte sélectionné en temps réel
            if (!_isAnnotationsLocked && _selectedAnnotation != null && _selectedAnnotation.Type == AnnotationType.Text)
            {
                _selectedAnnotation.Color = _currentTextColor;
                await _databaseService.SaveAnnotationAsync(_selectedAnnotation);
                RenderAnnotations();
            }
        }
    }

    private void UpdateTextColorBorders()
    {
        TextBlackBtn.Stroke = _currentTextColor == "#000000" ? Colors.White : Colors.Transparent;
        TextBlackBtn.StrokeThickness = _currentTextColor == "#000000" ? 2 : 1;

        TextRedBtn.Stroke = _currentTextColor == "#E53935" ? Colors.White : Colors.Transparent;
        TextRedBtn.StrokeThickness = _currentTextColor == "#E53935" ? 2 : 1;

        TextBlueBtn.Stroke = _currentTextColor == "#1E88E5" ? Colors.White : Colors.Transparent;
        TextBlueBtn.StrokeThickness = _currentTextColor == "#1E88E5" ? 2 : 1;

        TextGreenBtn.Stroke = _currentTextColor == "#43A047" ? Colors.White : Colors.Transparent;
        TextGreenBtn.StrokeThickness = _currentTextColor == "#43A047" ? 2 : 1;

        TextYellowBtn.Stroke = _currentTextColor == "#FFD600" ? Colors.White : Colors.Transparent;
        TextYellowBtn.StrokeThickness = _currentTextColor == "#FFD600" ? 2 : 1;

        TextWhiteBtn.Stroke = _currentTextColor == "#FFFFFF" ? Colors.Gray : Colors.Transparent;
        TextWhiteBtn.StrokeThickness = _currentTextColor == "#FFFFFF" ? 2 : 1;
    }

    private async void OnTextBgColorSelected(object? sender, EventArgs e)
    {
        string? color = (e as TappedEventArgs)?.Parameter as string;
        if (string.IsNullOrEmpty(color))
        {
            if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer bTap && bTap.CommandParameter is string bColor)
            {
                color = bColor;
            }
            else if (sender is Grid grid && grid.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer gTap && gTap.CommandParameter is string gColor)
            {
                color = gColor;
            }
        }

        if (!string.IsNullOrEmpty(color))
        {
            _currentTextBgColor = color;
            UpdateTextBgBorders();

            // Modification dynamique du fond du texte sélectionné en temps réel
            if (!_isAnnotationsLocked && _selectedAnnotation != null && _selectedAnnotation.Type == AnnotationType.Text)
            {
                _selectedAnnotation.BackgroundColor = _currentTextBgColor;
                await _databaseService.SaveAnnotationAsync(_selectedAnnotation);
                RenderAnnotations();
            }
        }
    }

    private void UpdateTextBgBorders()
    {
        if (TextBgNoneBtn != null)
        {
            TextBgNoneBtn.Stroke = (_currentTextBgColor == "Transparent") ? Colors.White : Color.FromArgb("#888888");
            TextBgNoneBtn.StrokeThickness = (_currentTextBgColor == "Transparent") ? 2 : 1;
        }

        if (TextBgBlackBtn != null)
        {
            TextBgBlackBtn.Stroke = _currentTextBgColor == "#000000" ? Colors.White : Color.FromArgb("#555555");
            TextBgBlackBtn.StrokeThickness = _currentTextBgColor == "#000000" ? 2.5 : 1.5;
        }

        if (TextBgRedBtn != null)
        {
            TextBgRedBtn.Stroke = _currentTextBgColor == "#E53935" ? Colors.White : Colors.Transparent;
            TextBgRedBtn.StrokeThickness = _currentTextBgColor == "#E53935" ? 2.5 : 1.5;
        }

        if (TextBgBlueBtn != null)
        {
            TextBgBlueBtn.Stroke = _currentTextBgColor == "#1E88E5" ? Colors.White : Colors.Transparent;
            TextBgBlueBtn.StrokeThickness = _currentTextBgColor == "#1E88E5" ? 2.5 : 1.5;
        }

        if (TextBgGreenBtn != null)
        {
            TextBgGreenBtn.Stroke = _currentTextBgColor == "#43A047" ? Colors.White : Colors.Transparent;
            TextBgGreenBtn.StrokeThickness = _currentTextBgColor == "#43A047" ? 2.5 : 1.5;
        }

        if (TextBgYellowBtn != null)
        {
            TextBgYellowBtn.Stroke = _currentTextBgColor == "#FFD600" ? Colors.White : Colors.Transparent;
            TextBgYellowBtn.StrokeThickness = _currentTextBgColor == "#FFD600" ? 2.5 : 1.5;
        }

        if (TextBgWhiteBtn != null)
        {
            TextBgWhiteBtn.Stroke = _currentTextBgColor == "#FFFFFF" ? Color.FromArgb("#007ACC") : Colors.Transparent;
            TextBgWhiteBtn.StrokeThickness = _currentTextBgColor == "#FFFFFF" ? 2.5 : 1.5;
        }
    }

    private async void OnTextBorderColorSelected(object? sender, EventArgs e)
    {
        string? color = (e as TappedEventArgs)?.Parameter as string;
        if (string.IsNullOrEmpty(color))
        {
            if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer bTap && bTap.CommandParameter is string bColor)
            {
                color = bColor;
            }
            else if (sender is Grid grid && grid.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer gTap && gTap.CommandParameter is string gColor)
            {
                color = gColor;
            }
        }

        if (!string.IsNullOrEmpty(color))
        {
            _currentTextBorderColor = color;
            UpdateTextBorderBorders();

            // Modification dynamique du cadre du texte sélectionné en temps réel
            if (!_isAnnotationsLocked && _selectedAnnotation != null && _selectedAnnotation.Type == AnnotationType.Text)
            {
                _selectedAnnotation.BorderColor = _currentTextBorderColor;
                await _databaseService.SaveAnnotationAsync(_selectedAnnotation);
                RenderAnnotations();
            }
        }
    }


    private void UpdateTextBorderBorders()
    {
        if (TextBorderNoneBtn != null)
        {
            TextBorderNoneBtn.Stroke = (_currentTextBorderColor == "Transparent") ? Colors.White : Color.FromArgb("#888888");
            TextBorderNoneBtn.StrokeThickness = (_currentTextBorderColor == "Transparent") ? 2 : 1;
        }

        if (TextBorderBlackBtn != null)
        {
            TextBorderBlackBtn.Stroke = _currentTextBorderColor == "#000000" ? Colors.White : Color.FromArgb("#000000");
            TextBorderBlackBtn.StrokeThickness = _currentTextBorderColor == "#000000" ? 2.5 : 1.5;
        }

        if (TextBorderRedBtn != null)
        {
            TextBorderRedBtn.Stroke = _currentTextBorderColor == "#E53935" ? Colors.White : Color.FromArgb("#E53935");
            TextBorderRedBtn.StrokeThickness = _currentTextBorderColor == "#E53935" ? 2.5 : 1.5;
        }

        if (TextBorderBlueBtn != null)
        {
            TextBorderBlueBtn.Stroke = _currentTextBorderColor == "#1E88E5" ? Colors.White : Color.FromArgb("#1E88E5");
            TextBorderBlueBtn.StrokeThickness = _currentTextBorderColor == "#1E88E5" ? 2.5 : 1.5;
        }

        if (TextBorderGreenBtn != null)
        {
            TextBorderGreenBtn.Stroke = _currentTextBorderColor == "#43A047" ? Colors.White : Color.FromArgb("#43A047");
            TextBorderGreenBtn.StrokeThickness = _currentTextBorderColor == "#43A047" ? 2.5 : 1.5;
        }

        if (TextBorderYellowBtn != null)
        {
            TextBorderYellowBtn.Stroke = _currentTextBorderColor == "#FFD600" ? Colors.White : Color.FromArgb("#FFD600");
            TextBorderYellowBtn.StrokeThickness = _currentTextBorderColor == "#FFD600" ? 2.5 : 1.5;
        }

        if (TextBorderWhiteBtn != null)
        {
            TextBorderWhiteBtn.Stroke = _currentTextBorderColor == "#FFFFFF" ? Color.FromArgb("#007ACC") : Colors.White;
            TextBorderWhiteBtn.StrokeThickness = _currentTextBorderColor == "#FFFFFF" ? 2.5 : 1.5;
        }
    }

    private async void OnTextSizeSelected(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string sizeStr && double.TryParse(sizeStr, out double size))
        {
            _currentTextSize = size;
            UpdateTextSizeButtons();

            // Modification dynamique du texte sélectionné en temps réel
            if (!_isAnnotationsLocked && _selectedAnnotation != null && _selectedAnnotation.Type == AnnotationType.Text)
            {
                _selectedAnnotation.Scale = _currentTextSize;
                await _databaseService.SaveAnnotationAsync(_selectedAnnotation);
                RenderAnnotations();
            }
        }
    }

    private void UpdateTextSizeButtons()
    {
        TextSize8Btn.BackgroundColor = (_currentTextSize == 8) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        TextSize8Btn.FontAttributes = (_currentTextSize == 8) ? FontAttributes.Bold : FontAttributes.None;

        TextSize10Btn.BackgroundColor = (_currentTextSize == 10) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        TextSize10Btn.FontAttributes = (_currentTextSize == 10) ? FontAttributes.Bold : FontAttributes.None;

        TextSize12Btn.BackgroundColor = (_currentTextSize == 12) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        TextSize12Btn.FontAttributes = (_currentTextSize == 12) ? FontAttributes.Bold : FontAttributes.None;

        TextSize14Btn.BackgroundColor = (_currentTextSize == 14) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        TextSize14Btn.FontAttributes = (_currentTextSize == 14) ? FontAttributes.Bold : FontAttributes.None;

        TextSize16Btn.BackgroundColor = (_currentTextSize == 16) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        TextSize16Btn.FontAttributes = (_currentTextSize == 16) ? FontAttributes.Bold : FontAttributes.None;

        TextSize18Btn.BackgroundColor = (_currentTextSize == 18) ? Color.FromArgb("#007ACC") : Color.FromArgb("#333333");
        TextSize18Btn.FontAttributes = (_currentTextSize == 18) ? FontAttributes.Bold : FontAttributes.None;
    }

    private void OnCloseTextOptionsClicked(object sender, EventArgs e)
    {
        TextOptionsOverlay.IsVisible = false;
        _isTextMode = false;
        TextAnnotationBtn.BackgroundColor = Colors.Transparent;
        _selectedAnnotation = null;
        RenderAnnotations();
    }

    private bool _isPromptingText = false;
    private async Task HandleTextPlacementAsync(double absX, double absY)
    {
        if (_isPromptingText || !_isTextMode || _isAnnotationsLocked || ActiveAnnotationsContainer.Width <= 0 || ActiveAnnotationsContainer.Height <= 0) return;

        try
        {
            _isPromptingText = true;
            double containerW = ActiveAnnotationsContainer.Width;
            double containerH = ActiveAnnotationsContainer.Height;
            var (targetPage, relX, relY) = ScreenToPage(absX, absY, containerW, containerH);

            var loc = LocalizationService.Instance;
            string promptTitle = loc.GetString("Viewer_Annotations_Text_Prompt_Title");
            string promptMsg = loc.GetString("Viewer_Annotations_Text_Prompt_Msg");
            string cancelText = loc.GetString("Common_Cancel");
            string text = await DisplayPromptAsync(promptTitle, promptMsg, "OK", cancelText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var annotation = new Annotation
                {
                    ScoreId = _score.Id,
                    Type = AnnotationType.Text,
                    Category = "Text",
                    Content = text.Trim(),
                    X = relX,
                    Y = relY,
                    Scale = _currentTextSize, // 8, 10, 12, 14, 16, 18
                    Color = _currentTextColor,
                    BackgroundColor = _currentTextBgColor,
                    BorderColor = _currentTextBorderColor,
                    PageNumber = targetPage
                };

                _lastSinglePageNumber = targetPage;
                await _databaseService.SaveAnnotationAsync(annotation);
                _annotations.Add(annotation);
                _selectedAnnotation = annotation;

                _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Add, Annotation = annotation });
                _redoStack.Clear();
                UpdateUndoRedoButtons();
                RenderAnnotations();
            }
        }
        finally
        {
            // Petit délai pour laisser le temps aux événements résiduels de se dissiper
            await Task.Delay(250);
            _isPromptingText = false;
        }
    }

    #endregion

    private async void OnAnnotationStickersClicked(object sender, EventArgs e)
    {
        UnlockAnnotations();

        _isHighlightMode = false;
        HighlightOptionsOverlay.IsVisible = false;
        HighlightBtn.BackgroundColor = Colors.Transparent;

        _isDrawMode = false;
        DrawOptionsOverlay.IsVisible = false;
        DrawBtn.BackgroundColor = Colors.Transparent;

        _isTextMode = false;
        TextOptionsOverlay.IsVisible = false;
        TextAnnotationBtn.BackgroundColor = Colors.Transparent;
        DeactivateEraserMode();

        ActiveAnnotationsContainer.InputTransparent = false;
        StickerPickerOverlay.IsVisible = !StickerPickerOverlay.IsVisible;
        if (StickerPickerOverlay.IsVisible)
        {
            UpdateAnnotationOverlaysLayout(this.Width, this.Height);
            StickerPickerOverlay.TranslationY = AnnotationBar.TranslationY;
            if (StickerCategoriesCollection.ItemsSource == null || StickerCategoriesCollection.SelectedItem == null || StickersCollection?.ItemsSource == null)
            {
                await InitializeAnnotationUI(force: true);
            }
        }
    }

    private void OnCloseStickerPickerClicked(object sender, EventArgs e)
    {
        StickerPickerOverlay.IsVisible = false;
        StickerPickerOverlay.TranslationY = 0;

        _pendingSticker = null;
        _isAnnotationMode = false;

        if (StickersCollection != null)
        {
            if (StickersCollection.ItemsSource is IEnumerable<StickerItem> items)
            {
                foreach (var item in items) item.IsSelected = false;
            }
            StickersCollection.SelectedItem = null;
        }
    }

    private void OnCloseAnnotationBarClicked(object sender, EventArgs e)
    {
        AnnotationBar.IsVisible = false;
        BottomTouchBar.IsVisible = true;
        StickerPickerOverlay.IsVisible = false;
        HighlightOptionsOverlay.IsVisible = false;
        DrawOptionsOverlay.IsVisible = false;
        TextOptionsOverlay.IsVisible = false;
        _isHighlightMode = false;
        _isDrawMode = false;
        _isTextMode = false;
        DeactivateEraserMode();
        HighlightBtn.BackgroundColor = Colors.Transparent;
        DrawBtn.BackgroundColor = Colors.Transparent;
        TextAnnotationBtn.BackgroundColor = Colors.Transparent;
        
        // Réinitialiser la translation pour éviter qu'elle reste décalée lors de la prochaine ouverture
        AnnotationBar.TranslationY = 0;
        StickerPickerOverlay.TranslationY = 0;
        HighlightOptionsOverlay.TranslationY = 0;
        DrawOptionsOverlay.TranslationY = 0;
        TextOptionsOverlay.TranslationY = 0;
        
        _selectedAnnotation = null;
        _pendingSticker = null;
        _isAnnotationMode = false;
        LockAnnotations();

        if (StickersCollection != null)
        {
            if (StickersCollection.ItemsSource is IEnumerable<StickerItem> items)
            {
                foreach (var item in items) item.IsSelected = false;
            }
            StickersCollection.SelectedItem = null;
        }

        RenderAnnotations();
        
        PageIndicator.Margin = new Thickness(0, 0, 10, 2);
    }

    private void OnStickerCategoryChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection.FirstOrDefault() is StickerCategory category)
            {
                _pendingSticker = null;
                _isAnnotationMode = false;

                MainThread.BeginInvokeOnMainThread(() => {
                    if (StickersCollection != null)
                    {
                        // Appliquer les réglages actuels à TOUS les stickers de cette catégorie pour l'aperçu
                        foreach (var s in category.Stickers)
                        {
                            s.Color = _currentStickerColor;
                            s.BackgroundColor = _currentStickerBgColor;
                            s.Scale = StickerSizeSlider.Value;
                            s.IsSelected = false;
                        }
                        
                        StickersCollection.ItemsSource = category.Stickers;
                        StickersCollection.SelectedItem = null; // Reset selection
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur changement catégorie: {ex.Message}");
        }
    }

    private void OnStickerDragStarting(object? sender, DragStartingEventArgs e)
    {
        if (sender is View view && view.BindingContext is StickerItem sticker)
        {
            e.Data.Properties.Add("Sticker", sticker.Text);
            // On désélectionne l'annotation placée lors d'un nouveau glisser-déposer
            _selectedAnnotation = null;
            RenderAnnotations();
        }
    }

    private DateTime _lastStickerTapTime = DateTime.MinValue;
    private StickerItem? _lastTappedSticker = null;

    private async Task PlaceStickerDirectlyAsync(StickerItem sticker)
    {
        UnlockAnnotations();

        // S'assurer que les annotations sont actives et visibles
        _score.ShowAnnotations = true;
        if (AnnotationsContainer != null)
        {
            AnnotationsContainer.Opacity = 1;
            AnnotationsContainer.IsVisible = true;
            AnnotationsContainer.InputTransparent = false;
        }
        if (ActiveAnnotationsContainer != null)
        {
            ActiveAnnotationsContainer.Opacity = 1;
            ActiveAnnotationsContainer.IsVisible = true;
            ActiveAnnotationsContainer.InputTransparent = false;
        }

        int targetPage = _currentPage;
        if (_score.Type == ScoreType.PDF && _isTwoPagesMode)
        {
            targetPage = _leftPageNumber;
        }

        string color = _currentStickerColor;
        string bgColor = _currentStickerBgColor;
        double scale = StickerSizeSlider?.Value ?? 1.0;

        // Positionnement en haut au centre de la partition (X: 0.5, Y: 0.08)
        var annotation = new Annotation
        {
            ScoreId = _score.Id,
            Type = AnnotationType.Sticker,
            Content = sticker.Text,
            X = 0.5,
            Y = 0.08,
            Scale = scale,
            Color = color,
            BackgroundColor = bgColor,
            PageNumber = targetPage
        };

        _lastSinglePageNumber = targetPage;
        _lastSinglePageNumber = targetPage;
        await _databaseService.SaveAnnotationAsync(annotation);
        _annotations.Add(annotation);
        _selectedAnnotation = annotation;

        _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Add, Annotation = annotation });
        _redoStack.Clear();
        UpdateUndoRedoButtons();

        RenderAnnotations();
        HighlightPlacedAnnotation(annotation);
    }

    private async void HighlightPlacedAnnotation(Annotation annotation)
    {
        try
        {
            if (ActiveAnnotationsContainer == null) return;
            await Task.Delay(60);
            var border = ActiveAnnotationsContainer.Children.OfType<Border>().FirstOrDefault(b => b.BindingContext == annotation);
            if (border != null)
            {
                border.Stroke = Colors.Gold;
                border.StrokeThickness = 3;
                await border.ScaleToAsync(1.4, 120);
                await border.ScaleToAsync(1.0, 120);
                border.Stroke = Colors.Red;
                border.StrokeThickness = 2;
            }
        }
        catch { }
    }

    private string _currentStickerColor = "#FF0000";
    private string _currentStickerBgColor = "#000000";

    private Color ParseColor(string colorStr)
    {
        if (string.IsNullOrWhiteSpace(colorStr))
            return Colors.Transparent;

        string trimmed = colorStr.Trim();
        if (trimmed.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
            return Colors.Transparent;
        if (trimmed.Equals("Black", StringComparison.OrdinalIgnoreCase))
            return Colors.Black;
        if (trimmed.Equals("White", StringComparison.OrdinalIgnoreCase))
            return Colors.White;
        if (trimmed.Equals("Red", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#FF0000");
        if (trimmed.Equals("Yellow", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#FFD600");
        if (trimmed.Equals("Green", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#2ECC71");
        if (trimmed.Equals("Blue", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#007ACC");
        if (trimmed.Equals("Orange", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#FF9800");
        if (trimmed.Equals("DarkGray", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#333333");
        if (trimmed.Equals("Ivory", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#FFFFE0");

        try
        {
            return Color.FromArgb(trimmed);
        }
        catch
        {
            return Colors.Transparent;
        }
    }

    private void UpdateStickersPickerPreview()
    {
        if (StickersCollection?.ItemsSource is IEnumerable<StickerItem> items)
        {
            foreach (var item in items)
            {
                item.Color = _currentStickerColor;
                item.BackgroundColor = _currentStickerBgColor;
                item.Scale = StickerSizeSlider.Value;
            }
        }
    }

    private async void OnStickerColorTapped(object sender, TappedEventArgs e)
    {
        string? colorStr = e.Parameter as string;
        if (colorStr == null && sender is TapGestureRecognizer tgr)
        {
            colorStr = tgr.CommandParameter as string;
        }

        if (colorStr != null)
        {
            _currentStickerColor = colorStr switch
            {
                "Red" => "#FF0000",
                "White" => "#FFFFFF",
                "Black" => "#000000",
                "Blue" => "#007ACC",
                "Yellow" => "#FFD600",
                "Green" => "#2ECC71",
                "Orange" => "#FF9800",
                _ => "#FF0000"
            };
            
            // APERÇU TEMPS RÉEL : Mettre à jour TOUS les stickers visibles dans le tiroir
            UpdateStickersPickerPreview();

            // MODIFICATION DE L'ANNOTATION SÉLECTIONNÉE
            if (!_isAnnotationsLocked && _selectedAnnotation != null)
            {
                _selectedAnnotation.Color = _currentStickerColor;
                await _databaseService.SaveAnnotationAsync(_selectedAnnotation);
                RenderAnnotations();
            }
        }
    }

    private async void OnStickerBgColorTapped(object sender, TappedEventArgs e)
    {
        string? colorStr = e.Parameter as string;
        if (colorStr == null && sender is TapGestureRecognizer tgr)
        {
            colorStr = tgr.CommandParameter as string;
        }

        if (colorStr != null)
        {
            _currentStickerBgColor = colorStr switch
            {
                "Black" => "#000000",
                "Transparent" => "Transparent",
                "DarkGray" => "#333333",
                "Ivory" => "#FFFFE0",
                "Red" => "#FF0000",
                "Yellow" => "#FFD600",
                "Green" => "#2ECC71",
                "Blue" => "#007ACC",
                _ => "#000000"
            };
            
            // APERÇU TEMPS RÉEL : Mettre à jour TOUS les stickers visibles dans le tiroir
            UpdateStickersPickerPreview();

            // MODIFICATION DE L'ANNOTATION SÉLECTIONNÉE
            if (!_isAnnotationsLocked && _selectedAnnotation != null)
            {
                _selectedAnnotation.BackgroundColor = _currentStickerBgColor;
                await _databaseService.SaveAnnotationAsync(_selectedAnnotation);
                RenderAnnotations();
            }
        }
    }

    private CancellationTokenSource? _sliderCts;
    private async void OnStickerSizeChanged(object? sender, ValueChangedEventArgs e)
    {
        _sliderCts?.Cancel();
        _sliderCts = new CancellationTokenSource();
        var token = _sliderCts.Token;

        try 
        {
            // Debouncing pour éviter de saturer le thread UI pendant que le slider bouge
            await Task.Delay(20, token); 
            
            // APERÇU TEMPS RÉEL : Mettre à jour TOUS les stickers visibles dans le tiroir
            UpdateStickersPickerPreview();

            // MODIFICATION DE L'ANNOTATION SÉLECTIONNÉE
            if (!_isAnnotationsLocked && _selectedAnnotation != null)
            {
                if (Math.Abs(_selectedAnnotation.Scale - e.NewValue) > 0.01)
                {
                    _selectedAnnotation.Scale = e.NewValue;
                    await _databaseService.SaveAnnotationAsync(_selectedAnnotation);
                    RenderAnnotations();
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async void OnStickerDrop(object? sender, DropEventArgs e)
    {
        if (_isAnnotationsLocked)
        {
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(loc.GetString("Viewer_Annotations_Locked_Title", "Verrouillé"), loc.GetString("Viewer_Annotations_Locked_Stickers", "Les annotations sont verrouillées. Déverrouillez-les (🔓) pour ajouter des stickers."), loc.GetString("Common_OK", "OK"));
            return;
        }

        if (e.Data.Properties.TryGetValue("Sticker", out var stickerObj) && stickerObj is string sticker)
        {
            var position = e.GetPosition(ActiveAnnotationsContainer);
            if (position == null) return;

            // Ignorer le drop s'il se trouve dans la zone du sélecteur ou de la barre d'annotations.
            // Cela évite de poser accidentellement un sticker lors d'une simple sélection ou d'un réglage.
            double absY = position.Value.Y;
            double containerHeight = ActiveAnnotationsContainer.Height;

            if (StickerPickerOverlay.IsVisible)
            {
                // Vérifier si le drop tombe précisément À L'INTÉRIEUR du sélecteur de stickers
                double pickerHeight = StickerPickerOverlay.Height > 0 ? StickerPickerOverlay.Height : 270;
                double pickerTop = containerHeight - pickerHeight - 45 + StickerPickerOverlay.TranslationY;
                double pickerBottom = pickerTop + pickerHeight;

                if (absY >= pickerTop && absY <= pickerBottom)
                {
                    System.Diagnostics.Debug.WriteLine("[Viewer] Drop ignoré car situé dans la surface du sélecteur de stickers.");
                    return;
                }
            }
            
            if (AnnotationBar.IsVisible)
            {
                // Vérifier si le drop tombe précisément À L'INTÉRIEUR de la barre d'annotations (45px de haut)
                double barHeight = AnnotationBar.Height > 0 ? AnnotationBar.Height : 45;
                double barTop = containerHeight - barHeight + AnnotationBar.TranslationY;
                double barBottom = barTop + barHeight;

                if (absY >= barTop && absY <= barBottom)
                {
                    System.Diagnostics.Debug.WriteLine("[Viewer] Drop ignoré car situé dans la surface de la barre d'annotations.");
                    return;
                }
            }

            // On prend DIRECTEMENT les valeurs actuelles des réglettes pour être sûr
            string color = _currentStickerColor;
            string bgColor = _currentStickerBgColor;
            double scale = StickerSizeSlider.Value;

            double containerW = ActiveAnnotationsContainer.Width;
            double containerH = ActiveAnnotationsContainer.Height;
            var (targetPage, relX, relY) = ScreenToPage(position.Value.X, position.Value.Y, containerW, containerH);

            var annotation = new Annotation
            {
                ScoreId = _score.Id,
                Type = AnnotationType.Sticker,
                Content = sticker,
                X = relX,
                Y = relY,
                Scale = scale,
                Color = color,
                BackgroundColor = bgColor,
                PageNumber = targetPage
            };

            await _databaseService.SaveAnnotationAsync(annotation);
            _annotations.Add(annotation);
            _selectedAnnotation = annotation;
            _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Add, Annotation = annotation });
            _redoStack.Clear();
            UpdateUndoRedoButtons();
            RenderAnnotations();
            HighlightPlacedAnnotation(annotation);
        }
    }

    private void SelectStickerItem(StickerItem sticker, View? view)
    {
        UnlockAnnotations();

        _pendingSticker = sticker.Text;
        _isAnnotationMode = true;
        
        if (ActiveAnnotationsContainer != null)
        {
            ActiveAnnotationsContainer.InputTransparent = false;
        }
        
        // Appliquer les réglages actuels pour l'aperçu immédiat
        sticker.Color = _currentStickerColor;
        sticker.BackgroundColor = _currentStickerBgColor;
        sticker.Scale = StickerSizeSlider?.Value ?? 1.0;

        if (StickersCollection?.ItemsSource is IEnumerable<StickerItem> currentItems)
        {
            foreach (var item in currentItems)
            {
                item.IsSelected = (item == sticker);
            }
        }

        if (StickersCollection != null && StickersCollection.SelectedItem != sticker)
        {
            StickersCollection.SelectedItem = sticker;
        }

        // Effet visuel immédiat sur le sticker / mot cliqué dans le tiroir
        if (view != null)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await view.ScaleToAsync(1.15, 60);
                    await view.ScaleToAsync(1.0, 60);
                });
            }
            catch { }
        }

        // Nouvelle sélection de sticker ➔ on désélectionne l'annotation placée
        _selectedAnnotation = null;
        RenderAnnotations();
    }

    private async void OnStickerSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is StickerItem sticker)
        {
            var now = DateTime.UtcNow;
            if (_lastTappedSticker == sticker && (now - _lastStickerTapTime).TotalMilliseconds < 500)
            {
                // Double tap rapide détecté sur le mot / sticker !
                _lastStickerTapTime = DateTime.MinValue;
                _lastTappedSticker = null;

                if (sender is CollectionView cvDouble)
                {
                    cvDouble.SelectedItem = null;
                }

                await PlaceStickerDirectlyAsync(sticker);
                return;
            }

            _lastStickerTapTime = now;
            _lastTappedSticker = sticker;

            SelectStickerItem(sticker, null);

            if (sender is CollectionView cv)
            {
                // Permet de re-sélectionner le même élément pour détecter un éventuel 2e clic (double-tap)
                cv.SelectedItem = null;
            }
        }
    }

    private async Task PlaceStickerAtAsync(double absX, double absY)
    {
        if (ActiveAnnotationsContainer == null || ActiveAnnotationsContainer.Width <= 0 || ActiveAnnotationsContainer.Height <= 0) return;
        if (string.IsNullOrEmpty(_pendingSticker)) return;

        UnlockAnnotations();

        double containerW = ActiveAnnotationsContainer.Width;
        double containerH = ActiveAnnotationsContainer.Height;
        var (targetPage, relX, relY) = ScreenToPage(absX, absY, containerW, containerH);

        // Récupérer les styles actuellement configurés dans le bandeau
        string color = _currentStickerColor;
        string bgColor = _currentStickerBgColor;
        double scale = StickerSizeSlider?.Value ?? 1.0;

        var annotation = new Annotation
        {
            ScoreId = _score.Id,
            Type = AnnotationType.Sticker,
            Content = _pendingSticker,
            X = relX,
            Y = relY,
            Scale = scale,
            Color = color,
            BackgroundColor = bgColor,
            PageNumber = targetPage
        };

        _lastSinglePageNumber = targetPage;
        _lastSinglePageNumber = targetPage;
        await _databaseService.SaveAnnotationAsync(annotation);
        _annotations.Add(annotation);
        _selectedAnnotation = annotation;

        _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Add, Annotation = annotation });
        _redoStack.Clear();
        UpdateUndoRedoButtons();

        // Sortir du mode placement et désélectionner l'item dans la collection
        _isAnnotationMode = false;
        _pendingSticker = null;

        if (StickersCollection?.ItemsSource is IEnumerable<StickerItem> placedItems)
        {
            foreach (var item in placedItems)
            {
                item.IsSelected = false;
            }
        }

        if (StickersCollection != null)
        {
            StickersCollection.SelectedItem = null;
        }

        RenderAnnotations();
        HighlightPlacedAnnotation(annotation);
    }

    private async void OnPlacementTapped(object? sender, TappedEventArgs e)
    {
        if (_isAnnotationsLocked || (!_isTextMode && (!_isAnnotationMode || string.IsNullOrEmpty(_pendingSticker))))
            return;

        UnlockAnnotations();

        var position = e.GetPosition(ActiveAnnotationsContainer);
        if (position == null || ActiveAnnotationsContainer.Width <= 0 || ActiveAnnotationsContainer.Height <= 0) return;

        if (_isTextMode)
        {
            await HandleTextPlacementAsync(position.Value.X, position.Value.Y);
            return;
        }

        if (_isAnnotationMode && !string.IsNullOrEmpty(_pendingSticker))
        {
            await PlaceStickerAtAsync(position.Value.X, position.Value.Y);
        }
    }

    private Annotation? _selectedAnnotation = null;

    private readonly Stack<AnnotationHistoryEntry> _undoStack = new();
    private readonly Stack<AnnotationHistoryEntry> _redoStack = new();

    private void UpdateUndoRedoButtons()
    {
        if (UndoBtn != null)
        {
            UndoBtn.Opacity = _undoStack.Count > 0 ? 1.0 : 0.4;
        }
        if (RedoBtn != null)
        {
            RedoBtn.Opacity = _redoStack.Count > 0 ? 1.0 : 0.4;
        }
    }

    private async void OnUndoAnnotationClicked(object? sender, EventArgs e)
    {
        if (_isAnnotationsLocked)
        {
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(loc.GetString("Viewer_Annotations_Locked_Title", "Verrouillé"), loc.GetString("Viewer_Annotations_Locked_Edit", "Les annotations sont verrouillées. Déverrouillez-les (🔓) pour pouvoir modifier."), loc.GetString("Common_OK", "OK"));
            return;
        }

        if (_undoStack.Count == 0)
        {
            return;
        }

        var entry = _undoStack.Pop();

        switch (entry.ActionType)
        {
            case AnnotationActionType.Add:
                if (entry.Annotation != null)
                {
                    await _databaseService.DeleteAnnotationAsync(entry.Annotation);
                    _annotations.Remove(entry.Annotation);
                    if (_selectedAnnotation == entry.Annotation) _selectedAnnotation = null;
                    _redoStack.Push(entry);
                }
                break;

            case AnnotationActionType.Delete:
                if (entry.Annotation != null)
                {
                    await _databaseService.SaveAnnotationAsync(entry.Annotation);
                    _annotations.Add(entry.Annotation);
                    _redoStack.Push(entry);
                }
                break;

            case AnnotationActionType.ClearAll:
                if (entry.AnnotationList != null)
                {
                    foreach (var ann in entry.AnnotationList)
                    {
                        await _databaseService.SaveAnnotationAsync(ann);
                        _annotations.Add(ann);
                    }
                    _redoStack.Push(entry);
                }
                break;
        }

        UpdateUndoRedoButtons();
        RenderAnnotations();
    }

    private async void OnRedoAnnotationClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        if (_isAnnotationsLocked)
        {
            await DisplayAlertAsync(loc.GetString("Viewer_Annotations_Locked_Title"), loc.GetString("Viewer_Annotations_Locked_Msg"), "OK");
            return;
        }

        if (_redoStack.Count == 0)
        {
            return;
        }

        var entry = _redoStack.Pop();

        switch (entry.ActionType)
        {
            case AnnotationActionType.Add:
                if (entry.Annotation != null)
                {
                    await _databaseService.SaveAnnotationAsync(entry.Annotation);
                    _annotations.Add(entry.Annotation);
                    _undoStack.Push(entry);
                }
                break;

            case AnnotationActionType.Delete:
                if (entry.Annotation != null)
                {
                    await _databaseService.DeleteAnnotationAsync(entry.Annotation);
                    _annotations.Remove(entry.Annotation);
                    if (_selectedAnnotation == entry.Annotation) _selectedAnnotation = null;
                    _undoStack.Push(entry);
                }
                break;

            case AnnotationActionType.ClearAll:
                if (entry.AnnotationList != null)
                {
                    await _databaseService.DeleteAllAnnotationsForScoreAsync(_score.Id);
                    _annotations.Clear();
                    _selectedAnnotation = null;
                    _undoStack.Push(entry);
                }
                break;
        }

        UpdateUndoRedoButtons();
        RenderAnnotations();
    }

    private void DeactivateEraserMode()
    {
        bool wasActive = _isEraserMode;
        _isEraserMode = false;
        _isPointerEraserPressed = false;
        if (EraserBtn != null)
        {
            EraserBtn.BackgroundColor = Colors.Transparent;
        }
        if (wasActive)
        {
            RenderAnnotations();
        }
    }

    private async void OnAnnotationEraserClicked(object? sender, EventArgs e)
    {
        if (_isAnnotationsLocked)
        {
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(loc.GetString("Viewer_Annotations_Locked_Title"), loc.GetString("Viewer_Annotations_Locked_Msg"), "OK");
            return;
        }

        // Si une annotation est déjà sélectionnée, la gommer/supprimer immédiatement
        if (_selectedAnnotation != null)
        {
            var annToDelete = _selectedAnnotation;
            _selectedAnnotation = null;
            await DeleteAnnotationWithUndoAsync(annToDelete);
            return;
        }

        _isEraserMode = !_isEraserMode;
        _isPointerEraserPressed = false;

        if (_isEraserMode)
        {
            UnlockAnnotations();
            _score.ShowAnnotations = true;
            if (AnnotationsContainer != null)
            {
                AnnotationsContainer.Opacity = 1;
                AnnotationsContainer.IsVisible = true;
                AnnotationsContainer.InputTransparent = false;
            }
            if (ActiveAnnotationsContainer != null)
            {
                ActiveAnnotationsContainer.Opacity = 1;
                ActiveAnnotationsContainer.IsVisible = true;
                ActiveAnnotationsContainer.InputTransparent = false;
            }

            _isHighlightMode = false;
            _isDrawMode = false;
            _isTextMode = false;
            _isAnnotationMode = false;
            _pendingSticker = string.Empty;

            HighlightOptionsOverlay.IsVisible = false;
            DrawOptionsOverlay.IsVisible = false;
            TextOptionsOverlay.IsVisible = false;
            StickerPickerOverlay.IsVisible = false;

            HighlightBtn.BackgroundColor = Colors.Transparent;
            DrawBtn.BackgroundColor = Colors.Transparent;
            TextAnnotationBtn.BackgroundColor = Colors.Transparent;
            StickersBtn.BackgroundColor = Colors.Transparent;

            EraserBtn.BackgroundColor = Color.FromArgb("#45FF5555");
        }
        else
        {
            EraserBtn.BackgroundColor = Colors.Transparent;
        }

        // Re-rendre immédiatement pour basculer InputTransparent sur tous les éléments existants
        RenderAnnotations();
    }

    private async Task DeleteAnnotationWithUndoAsync(Annotation annToDelete)
    {
        if (!_erasingAnnotationIds.Add(annToDelete.Id)) return;
        try
        {
            _annotations.Remove(annToDelete);
            _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Delete, Annotation = annToDelete });
            _redoStack.Clear();
            UpdateUndoRedoButtons();

            if (_selectedAnnotation == annToDelete) _selectedAnnotation = null;
            RenderAnnotations();

            await _databaseService.DeleteAnnotationAsync(annToDelete);
        }
        finally
        {
            _erasingAnnotationIds.Remove(annToDelete.Id);
        }
    }

    private async void EraseAnnotationsAtPoint(double touchX, double touchY)
    {
        if (!_isEraserMode || _isAnnotationsLocked || ActiveAnnotationsContainer == null) return;
        double containerW = ActiveAnnotationsContainer.Width;
        double containerH = ActiveAnnotationsContainer.Height;
        if (containerW <= 0 || containerH <= 0) return;

        var hit = FindAnnotationAtPoint(touchX, touchY, containerW, containerH);
        if (hit != null)
        {
            await DeleteAnnotationWithUndoAsync(hit);
        }
    }

    private static double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
        => AnnotationGeometryHelper.DistanceToSegment(px, py, x1, y1, x2, y2);

    private void OnDeleteSelectedAnnotationClicked(object? sender, EventArgs e)
    {
        OnAnnotationEraserClicked(sender, e);
    }

    private async void OnResetAnnotationsClicked(object? sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        if (_isAnnotationsLocked)
        {
            await DisplayAlertAsync(loc.GetString("Viewer_Annotations_Locked_Title"), loc.GetString("Viewer_Annotations_Locked_Msg"), "OK");
            return;
        }

        if (_annotations.Count == 0)
        {
            await DisplayAlertAsync("Reset", loc.GetString("Viewer_Annotations_None_To_Delete"), "OK");
            return;
        }

        bool confirm = await DisplayAlertAsync("Reset", loc.GetString("Viewer_Annotations_Confirm_Clear_All"), loc.GetString("Common_Yes"), loc.GetString("Common_No"));
        if (confirm)
        {
            var previousList = new List<Annotation>(_annotations);
            await _databaseService.DeleteAllAnnotationsForScoreAsync(_score.Id);
            _annotations.Clear();
            _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.ClearAll, AnnotationList = previousList });
            _redoStack.Clear();
            UpdateUndoRedoButtons();

            _selectedAnnotation = null;
            RenderAnnotations();
        }
    }


    private Annotation? FindAnnotationAtPoint(double x, double y, double containerW, double containerH)
    {
        if (containerW <= 0 || containerH <= 0) return null;

        List<Annotation> visibleAnnotations;
        if (_score.Type == ScoreType.PDF && _isTwoPagesMode)
        {
            visibleAnnotations = _annotations
                .Where(a => a.PageNumber == _leftPageNumber || (_rightPageNumber <= _maxPages && a.PageNumber == _rightPageNumber))
                .Reverse()
                .ToList();
        }
        else
        {
            visibleAnnotations = _annotations
                .Where(a => a.PageNumber == _currentPage)
                .Reverse()
                .ToList();
        }

        double displayScale = GetPageDisplayScale();

        foreach (var ann in visibleAnnotations)
        {
            var (ax, ay) = PageToScreen(ann.X, ann.Y, ann.PageNumber, containerW, containerH);
            if (ann.Type == AnnotationType.Text)
            {
                double fontSize = (ann.Scale <= 0 ? 14.0 : ann.Scale) * displayScale;
                double charCount = string.IsNullOrEmpty(ann.Content) ? 1 : ann.Content.Length;
                double halfW = Math.Max(28.0, (charCount * fontSize * 0.65) / 2.0 + 18.0);
                double halfH = Math.Max(24.0, (fontSize * 1.4) / 2.0 + 14.0);
                if (Math.Abs(x - ax) <= halfW && Math.Abs(y - ay) <= halfH)
                {
                    return ann;
                }
            }
            else if (ann.Type == AnnotationType.Sticker)
            {
                double effScale = Math.Max(0.5, ann.Scale <= 0 ? 1.0 : ann.Scale) * displayScale;
                double fontSize = 15.0 * effScale;
                double charCount = string.IsNullOrEmpty(ann.Content) ? 1 : ann.Content.Length;
                double halfW = Math.Max(28.0, (charCount * fontSize * 0.75 + 22.0 * effScale) / 2.0 + 12.0);
                double halfH = Math.Max(24.0, (fontSize * 1.5 + 10.0 * effScale) / 2.0 + 12.0);
                if (Math.Abs(x - ax) <= halfW && Math.Abs(y - ay) <= halfH)
                {
                    return ann;
                }
            }
            else if (ann.Type == AnnotationType.Drawing)
            {
                if (!string.IsNullOrEmpty(ann.Content))
                {
                    double baseThick = (ann.Category == "Pencil" ? ann.Scale * 9.0 : ann.Scale * 30.0) * displayScale;
                    double hitRadius = Math.Max(22.0, (baseThick / 2.0) + 14.0);
                    var pairs = ann.Content.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    double? prevX = null;
                    double? prevY = null;
                    foreach (var pair in pairs)
                    {
                        var coords = pair.Split(',');
                        if (coords.Length == 2 &&
                            double.TryParse(coords[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double rx) &&
                            double.TryParse(coords[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double ry))
                        {
                            var (px, py) = PageToScreen(rx, ry, ann.PageNumber, containerW, containerH);
                            if (prevX.HasValue && prevY.HasValue)
                            {
                                if (DistanceToSegment(x, y, prevX.Value, prevY.Value, px, py) <= hitRadius)
                                {
                                    return ann;
                                }
                            }
                            else
                            {
                                if (Math.Sqrt((x - px) * (x - px) + (y - py) * (y - py)) <= hitRadius)
                                {
                                    return ann;
                                }
                            }
                            prevX = px;
                            prevY = py;
                        }
                    }
                }
            }
        }
        return null;
    }

    private void RenderAnnotations()
    {
        if (ActiveAnnotationsContainer == null) return;

        ActiveAnnotationsContainer.IsVisible = true;
        ActiveAnnotationsContainer.Opacity = (_isScoreReady && _score.ShowAnnotations) ? 1 : 0;

        var elementsToRemove = ActiveAnnotationsContainer.Children
            .Where(c => c != _activeLivePolyline)
            .ToList();

        foreach (var el in elementsToRemove)
        {
            ActiveAnnotationsContainer.Children.Remove(el);
        }

        if (!_score.ShowAnnotations)
        {
            return;
        }

        if (!_isScoreReady || ActiveAnnotationsContainer.Width <= 0 || ActiveAnnotationsContainer.Height <= 0)
        {
            return;
        }

        double containerW = ActiveAnnotationsContainer.Width;
        double containerH = ActiveAnnotationsContainer.Height;
        if (_currentRenderedWidth > 0 && _currentRenderedHeight > 0)
        {
            UpdateCanvasBounds(containerW, containerH, _currentRenderedWidth, _currentRenderedHeight);
        }

        // Option C : repère pointillé très discret des bords réels de la feuille uniquement en mode édition d'annotations
        if (AnnotationBar != null && AnnotationBar.IsVisible && !_isAnnotationsLocked && (_canvasYRel > 0.015 || _canvasXRel > 0.015))
        {
            var pageGuide = new Border
            {
                BackgroundColor = Colors.Transparent,
                Stroke = Color.FromArgb("#38007ACC"),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                InputTransparent = true,
                ZIndex = -1
            };
            AbsoluteLayout.SetLayoutFlags(pageGuide, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
            AbsoluteLayout.SetLayoutBounds(pageGuide, new Rect(_canvasXRel * containerW, _canvasYRel * containerH, _canvasWRel * containerW, _canvasHRel * containerH));
            ActiveAnnotationsContainer.Children.Add(pageGuide);
        }

        List<Annotation> visibleAnnotations;
        if (_score.Type == ScoreType.PDF && _isTwoPagesMode)
        {
            visibleAnnotations = _annotations
                .Where(a => a.PageNumber == _leftPageNumber || (_rightPageNumber <= _maxPages && a.PageNumber == _rightPageNumber))
                .ToList();
        }
        else
        {
            visibleAnnotations = _annotations
                .Where(a => a.PageNumber == _currentPage)
                .ToList();
        }

        foreach (var ann in visibleAnnotations)
        {
            if (ann.Type == AnnotationType.Drawing)
            {
                if (ann.Category == "Pencil")
                {
                    var polyline = CreatePencilPolyline(ann, containerW, containerH);
                    ActiveAnnotationsContainer.Children.Add(polyline);
                }
                else
                {
                    var polyline = CreateHighlightPolyline(ann, containerW, containerH);
                    ActiveAnnotationsContainer.Children.Add(polyline);
                }
            }
            else if (ann.Type == AnnotationType.Text)
            {
                var border = CreateTextBorder(ann, containerW, containerH);
                ActiveAnnotationsContainer.Children.Add(border);
                UpdateTextBorderBounds(border, ann, containerW, containerH);
            }
            else
            {
                var border = CreateStickerBorder(ann, containerW, containerH);
                ActiveAnnotationsContainer.Children.Add(border);
            }
        }
    }

    private void UpdateTextBorderBounds(Border border, Annotation ann, double containerW, double containerH)
    {
        double displayScale = GetPageDisplayScale();
        double fontSize = (ann.Scale <= 0 ? 14.0 : ann.Scale) * displayScale;
        bool isSelected = (ann == _selectedAnnotation);
        bool hasBorder = !string.IsNullOrWhiteSpace(ann.BorderColor) && !ann.BorderColor.Equals("Transparent", StringComparison.OrdinalIgnoreCase);
        bool hasBg = !string.IsNullOrWhiteSpace(ann.BackgroundColor) && !ann.BackgroundColor.Equals("Transparent", StringComparison.OrdinalIgnoreCase);

        double strokeThick = hasBorder
            ? (isSelected ? (2.5 * displayScale) : (1.5 * displayScale))
            : (isSelected ? (1.5 * displayScale) : 0);

        Label? label = border.Content as Label ?? (border.Content as Grid)?.Children.OfType<Label>().FirstOrDefault();
        if (label != null)
        {
            label.FontSize = fontSize;
        }
        Size size = label != null ? label.Measure(double.PositiveInfinity, double.PositiveInfinity) : Size.Zero;

        double charCount = string.IsNullOrEmpty(ann.Content) ? 1 : ann.Content.Length;
        double minTextW = charCount * (fontSize * 0.60);
        double measuredW = (size.Width > 0) ? Math.Max(size.Width, minTextW) : minTextW;
        double measuredH = (size.Height > 0) ? Math.Max(size.Height, fontSize * 1.25) : (fontSize * 1.25);

        double padX = ((hasBorder || hasBg) ? 8 : 4) * displayScale;
        double padY = ((hasBorder || hasBg) ? 4 : 2) * displayScale;
        double w = Math.Max(measuredW + (padX * 2) + (strokeThick * 2), 24 * displayScale);
        double h = Math.Max(measuredH + (padY * 2) + (strokeThick * 2), 20 * displayScale);

        var (absX, absY) = PageToScreen(ann.X, ann.Y, ann.PageNumber, containerW, containerH);
        double left = Math.Clamp(absX - (w / 2.0), 2.0, Math.Max(2.0, containerW - w - 2.0));
        double top = Math.Clamp(absY - (h / 2.0), 2.0, Math.Max(2.0, containerH - h - 2.0));
        AbsoluteLayout.SetLayoutBounds(border, new Rect(left, top, w, h));
    }

    private Border CreateTextBorder(Annotation ann, double containerW, double containerH)
    {
        double displayScale = GetPageDisplayScale();
        double fontSize = (ann.Scale <= 0 ? 14.0 : ann.Scale) * displayScale;

        bool isSelected = (ann == _selectedAnnotation);
        bool hasBorder = !string.IsNullOrWhiteSpace(ann.BorderColor) && !ann.BorderColor.Equals("Transparent", StringComparison.OrdinalIgnoreCase);
        bool hasBg = !string.IsNullOrWhiteSpace(ann.BackgroundColor) && !ann.BackgroundColor.Equals("Transparent", StringComparison.OrdinalIgnoreCase);
        Color borderStroke = hasBorder ? ParseColor(ann.BorderColor) : Colors.Transparent;
        Color bgColor = hasBg ? ParseColor(ann.BackgroundColor) : Colors.Transparent;

        Color strokeColor;
        double strokeThick;
        DoubleCollection? dashArray = null;

        if (hasBorder)
        {
            strokeColor = borderStroke;
            strokeThick = isSelected ? (2.5 * displayScale) : (1.5 * displayScale);
        }
        else if (isSelected)
        {
            // Cadre de sélection discret en pointillés quand la bordure du texte est transparente
            strokeColor = Color.FromArgb("#007ACC");
            strokeThick = 1.0 * displayScale;
            dashArray = new DoubleCollection { 3, 2 };
        }
        else
        {
            strokeColor = Colors.Transparent;
            strokeThick = 0;
        }

        var label = new Label
        {
            BackgroundColor = Colors.Transparent,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = Thickness.Zero,
            Padding = Thickness.Zero,
            LineBreakMode = LineBreakMode.NoWrap,
            InputTransparent = true,
            Text = ann.Content,
            TextColor = ParseColor(ann.Color),
            FontSize = fontSize,
            FontAttributes = FontAttributes.Bold
        };

#if ANDROID
        void ApplyAndroidTextCentering(Label lbl)
        {
            if (lbl.Handler?.PlatformView is Android.Widget.TextView tv)
            {
                tv.SetIncludeFontPadding(false);
                tv.Gravity = Android.Views.GravityFlags.Center;
                tv.SetPadding(0, 0, 0, 0);
            }
        }
        label.HandlerChanged += (s, e) =>
        {
            if (s is Label lbl) ApplyAndroidTextCentering(lbl);
        };
#endif

        var contentGrid = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Margin = Thickness.Zero,
            Padding = Thickness.Zero,
            InputTransparent = true
        };
        contentGrid.Children.Add(label);

        var border = new Border
        {
            BindingContext = ann,
            Padding = Thickness.Zero,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = (float)(4 * displayScale) },
            StrokeThickness = strokeThick,
            Stroke = strokeColor,
            StrokeDashArray = dashArray,
            BackgroundColor = bgColor,
            Content = contentGrid,
            InputTransparent = _isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode
        };

        AbsoluteLayout.SetLayoutFlags(border, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        UpdateTextBorderBounds(border, ann, containerW, containerH);


        var selectTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        selectTap.Tapped += (s, e) => {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Border b && b.BindingContext is Annotation a)
            {
                _selectedAnnotation = a;
                _currentTextColor = a.Color;
                _currentTextSize = a.Scale <= 0 ? 14 : a.Scale;
                _currentTextBgColor = string.IsNullOrEmpty(a.BackgroundColor) ? "Transparent" : a.BackgroundColor;
                _currentTextBorderColor = string.IsNullOrEmpty(a.BorderColor) ? "Transparent" : a.BorderColor;
                UpdateTextColorBorders();
                UpdateTextSizeButtons();
                UpdateTextBgBorders();
                UpdateTextBorderBorders();
                RenderAnnotations();
            }
        };
        border.GestureRecognizers.Add(selectTap);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += async (s, e) => {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Border b && b.BindingContext is Annotation a)
            {
                var loc = LocalizationService.Instance;
                string newText = await DisplayPromptAsync(
                    loc.GetString("Viewer_Edit_Text_Title", "Modifier le texte"),
                    loc.GetString("Viewer_Edit_Text_Prompt", "Texte :"),
                    loc.GetString("Common_OK", "OK"),
                    loc.GetString("Common_Cancel", "Annuler"),
                    initialValue: a.Content);
                if (newText != null)
                {
                    if (string.IsNullOrWhiteSpace(newText))
                    {
                        await _databaseService.DeleteAnnotationAsync(a);
                        _annotations.Remove(a);
                        _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Delete, Annotation = a });
                        _redoStack.Clear();
                        UpdateUndoRedoButtons();
                        if (_selectedAnnotation == a) _selectedAnnotation = null;
                    }
                    else
                    {
                        a.Content = newText.Trim();
                        await _databaseService.SaveAnnotationAsync(a);
                    }
                    RenderAnnotations();
                }
            }
        };
        border.GestureRecognizers.Add(doubleTap);

        var pan = new PanGestureRecognizer();
        double startX = 0, startY = 0;
        pan.PanUpdated += async (s, e) => {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Border b && b.BindingContext is Annotation a)
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        startX = b.TranslationX;
                        startY = b.TranslationY;
                        _selectedAnnotation = a;
                        _currentTextColor = a.Color;
                        _currentTextSize = a.Scale <= 0 ? 14 : a.Scale;
                        UpdateTextColorBorders();
                        UpdateTextSizeButtons();
                        b.Stroke = Color.FromArgb("#007ACC");
                        b.StrokeThickness = 1.5;
                        b.BackgroundColor = Color.FromArgb("#33007ACC");
                        break;
                    case GestureStatus.Running:
                        b.TranslationX = startX + e.TotalX;
                        b.TranslationY = startY + e.TotalY;
                        break;
                    case GestureStatus.Completed:
                        if (containerW > 0 && containerH > 0)
                        {
                            var (currSx, currSy) = PageToScreen(a.X, a.Y, a.PageNumber, containerW, containerH);
                            double finalAbsX = currSx + b.TranslationX;
                            double finalAbsY = currSy + b.TranslationY;
                            var (newPage, newRx, newRy) = ScreenToPage(finalAbsX, finalAbsY, containerW, containerH);
                            a.PageNumber = newPage;
                            a.X = newRx;
                            a.Y = newRy;
                            _lastSinglePageNumber = newPage;
                        }
                        b.TranslationX = 0;
                        b.TranslationY = 0;
                        RenderAnnotations();
                        await _databaseService.SaveAnnotationAsync(a);
                        break;
                    case GestureStatus.Canceled:
                        b.TranslationX = 0;
                        b.TranslationY = 0;
                        RenderAnnotations();
                        break;
                }
            }
        };
        border.GestureRecognizers.Add(pan);

        return border;
    }

    private Microsoft.Maui.Controls.Shapes.Polyline CreatePencilPolyline(Annotation ann, double containerW, double containerH)
    {
        double displayScale = GetPageDisplayScale();
        var pointCollection = new PointCollection();
        if (!string.IsNullOrEmpty(ann.Content))
        {
            var pairs = ann.Content.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var coords = pair.Split(',');
                if (coords.Length == 2 && 
                    double.TryParse(coords[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double rx) && 
                    double.TryParse(coords[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double ry))
                {
                    var (px, py) = PageToScreen(rx, ry, ann.PageNumber, containerW, containerH);
                    pointCollection.Add(new Point(px, py));
                }
            }
        }

        double effectiveOpacity = ann.Opacity > 0 ? ann.Opacity : 1.0;
        var polyline = new Microsoft.Maui.Controls.Shapes.Polyline
        {
            BindingContext = ann,
            Points = pointCollection,
            Stroke = ParseColor(ann.Color),
            StrokeThickness = ann.Scale * 9.0 * displayScale, // 3px par mm (1mm=3, 2mm=6, 3mm=9, 4mm=12, 5mm=15)
            StrokeLineCap = Microsoft.Maui.Controls.Shapes.PenLineCap.Round,
            StrokeLineJoin = Microsoft.Maui.Controls.Shapes.PenLineJoin.Round,
            Opacity = (ann == _selectedAnnotation) ? Math.Min(1.0, effectiveOpacity * 0.7) : effectiveOpacity,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = _isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode
        };
        AbsoluteLayout.SetLayoutFlags(polyline, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(polyline, new Rect(0, 0, 1, 1));

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) =>
        {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Microsoft.Maui.Controls.Shapes.Polyline p && p.BindingContext is Annotation a)
            {
                _selectedAnnotation = a;
                RenderAnnotations();
            }
        };
        polyline.GestureRecognizers.Add(tap);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += async (s, e) =>
        {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Microsoft.Maui.Controls.Shapes.Polyline p && p.BindingContext is Annotation a)
            {
                await _databaseService.DeleteAnnotationAsync(a);
                _annotations.Remove(a);
                _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Delete, Annotation = a });
                _redoStack.Clear();
                UpdateUndoRedoButtons();

                if (_selectedAnnotation == a) _selectedAnnotation = null;
                RenderAnnotations();
            }
        };
        polyline.GestureRecognizers.Add(doubleTap);

        return polyline;
    }

    private Microsoft.Maui.Controls.Shapes.Polyline CreateHighlightPolyline(Annotation ann, double containerW, double containerH)
    {
        double displayScale = GetPageDisplayScale();
        var pointCollection = new PointCollection();
        if (!string.IsNullOrEmpty(ann.Content))
        {
            var pairs = ann.Content.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var coords = pair.Split(',');
                if (coords.Length == 2 && 
                    double.TryParse(coords[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double rx) && 
                    double.TryParse(coords[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double ry))
                {
                    var (px, py) = PageToScreen(rx, ry, ann.PageNumber, containerW, containerH);
                    pointCollection.Add(new Point(px, py));
                }
            }
        }

        double effectiveOpacity = ann.Opacity > 0 ? ann.Opacity : 0.28;
        var polyline = new Microsoft.Maui.Controls.Shapes.Polyline
        {
            BindingContext = ann,
            Points = pointCollection,
            Stroke = ParseColor(ann.Color),
            StrokeThickness = ann.Scale * 30.0 * displayScale,
            StrokeLineCap = Microsoft.Maui.Controls.Shapes.PenLineCap.Flat,
            StrokeLineJoin = Microsoft.Maui.Controls.Shapes.PenLineJoin.Round,
            Opacity = (ann == _selectedAnnotation) ? Math.Min(1.0, effectiveOpacity + 0.3) : effectiveOpacity,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = _isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode
        };
        AbsoluteLayout.SetLayoutFlags(polyline, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(polyline, new Rect(0, 0, 1, 1));

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) =>
        {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Microsoft.Maui.Controls.Shapes.Polyline p && p.BindingContext is Annotation a)
            {
                _selectedAnnotation = a;
                RenderAnnotations();
            }
        };
        polyline.GestureRecognizers.Add(tap);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += async (s, e) =>
        {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Microsoft.Maui.Controls.Shapes.Polyline p && p.BindingContext is Annotation a)
            {
                await _databaseService.DeleteAnnotationAsync(a);
                _annotations.Remove(a);
                _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Delete, Annotation = a });
                _redoStack.Clear();
                UpdateUndoRedoButtons();

                if (_selectedAnnotation == a) _selectedAnnotation = null;
                RenderAnnotations();
            }
        };
        polyline.GestureRecognizers.Add(doubleTap);

        return polyline;
    }

    private Border CreateStickerBorder(Annotation ann, double containerW, double containerH)
    {
        double displayScale = GetPageDisplayScale();
        double effectiveScale = Math.Max(0.5, ann.Scale <= 0 ? 1.0 : ann.Scale) * displayScale;
        double fontSize = 15.0 * effectiveScale;

        var label = new Label
        {
            BackgroundColor = Colors.Transparent,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.NoWrap,
            InputTransparent = true,
            Text = ann.Content,
            TextColor = ParseColor(ann.Color),
            FontSize = fontSize,
            FontAttributes = FontAttributes.Bold
        };

        var border = new Border
        {
            BindingContext = ann,
            Padding = new Thickness(6 * effectiveScale, 3 * effectiveScale),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = (float)(6 * effectiveScale) },
            StrokeThickness = (ann == _selectedAnnotation) ? 2 : 0,
            Stroke = (ann == _selectedAnnotation) ? Colors.Red : Colors.Transparent,
            BackgroundColor = ParseColor(ann.BackgroundColor),
            Content = label,
            InputTransparent = _isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode
        };

        AbsoluteLayout.SetLayoutFlags(border, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);

        var size = border.Measure(double.PositiveInfinity, double.PositiveInfinity);
        double charCount = string.IsNullOrEmpty(ann.Content) ? 1 : ann.Content.Length;
        double estimatedTextWidth = charCount * (fontSize * 0.75);

        // Garantir une largeur et hauteur amplement suffisantes pour ne JAMAIS tronquer le mot sur PDF ou Image
        double w = Math.Max(Math.Max(size.Width + (18 * displayScale), estimatedTextWidth + (22 * effectiveScale)), 28 * effectiveScale);
        double h = Math.Max(Math.Max(size.Height + (8 * displayScale), (fontSize * 1.5) + (10 * effectiveScale)), 22 * effectiveScale);
        var (absX, absY) = PageToScreen(ann.X, ann.Y, ann.PageNumber, containerW, containerH);
        double sLeft = Math.Clamp(absX - (w / 2.0), 2.0, Math.Max(2.0, containerW - w - 2.0));
        double sTop = Math.Clamp(absY - (h / 2.0), 2.0, Math.Max(2.0, containerH - h - 2.0));
        AbsoluteLayout.SetLayoutBounds(border, new Rect(sLeft, sTop, w, h));

        var selectTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        selectTap.Tapped += (s, e) => {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Border b && b.BindingContext is Annotation a)
            {
                _selectedAnnotation = a;
                _currentStickerColor = a.Color;
                _currentStickerBgColor = a.BackgroundColor;
                StickerSizeSlider.Value = a.Scale;
                RenderAnnotations();
            }
        };
        border.GestureRecognizers.Add(selectTap);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += async (s, e) => {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Border b && b.BindingContext is Annotation a)
            {
                await _databaseService.DeleteAnnotationAsync(a);
                _annotations.Remove(a);
                _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Delete, Annotation = a });
                _redoStack.Clear();
                UpdateUndoRedoButtons();

                if (_selectedAnnotation == a) _selectedAnnotation = null;
                RenderAnnotations();
            }
        };
        border.GestureRecognizers.Add(doubleTap);

        var pan = new PanGestureRecognizer();
        double startX = 0, startY = 0;
        pan.PanUpdated += async (s, e) => {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode || _isEraserMode) return;
            if (s is Border b && b.BindingContext is Annotation a)
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        startX = b.TranslationX;
                        startY = b.TranslationY;
                        _selectedAnnotation = a;
                        _currentStickerColor = a.Color;
                        _currentStickerBgColor = a.BackgroundColor;
                        StickerSizeSlider.Value = a.Scale;
                        b.Stroke = Colors.Red;
                        b.StrokeThickness = 2;
                        break;
                    case GestureStatus.Running:
                        b.TranslationX = startX + e.TotalX;
                        b.TranslationY = startY + e.TotalY;
                        break;
                    case GestureStatus.Completed:
                        if (containerW > 0 && containerH > 0)
                        {
                            var (currSx, currSy) = PageToScreen(a.X, a.Y, a.PageNumber, containerW, containerH);
                            double finalAbsX = currSx + b.TranslationX;
                            double finalAbsY = currSy + b.TranslationY;
                            var (newPage, newRx, newRy) = ScreenToPage(finalAbsX, finalAbsY, containerW, containerH);
                            a.PageNumber = newPage;
                            a.X = newRx;
                            a.Y = newRy;
                            _lastSinglePageNumber = newPage;
                        }
                        b.TranslationX = 0;
                        b.TranslationY = 0;
                        RenderAnnotations();
                        await _databaseService.SaveAnnotationAsync(a);
                        break;
                    case GestureStatus.Canceled:
                        b.TranslationX = 0;
                        b.TranslationY = 0;
                        RenderAnnotations();
                        break;
                }
            }
        };
        border.GestureRecognizers.Add(pan);

        return border;
    }

}

public enum AnnotationActionType
{
    Add,
    Delete,
    ClearAll
}

public class AnnotationHistoryEntry
{
    public AnnotationActionType ActionType { get; set; }
    public Annotation? Annotation { get; set; }
    public List<Annotation>? AnnotationList { get; set; }
}
