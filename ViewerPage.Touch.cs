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
    private double _startScale = 1;
    private double _startTranslationX = 0;
    private double _startTranslationY = 0;
    private double _panStartX = 0;
    private double _panStartY = 0;
    private bool _isPinching = false;
    private bool _isZoomed = false;

    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _isPinching = true;
            _startScale = ZoomLayout.Scale;
            _startTranslationX = ZoomLayout.TranslationX;
            _startTranslationY = ZoomLayout.TranslationY;
            ZoomLayout.AnchorX = 0.5;
            ZoomLayout.AnchorY = 0.5;
        }
        else if (e.Status == GestureStatus.Running)
        {
            _isPinching = true;

            // Calcul du nouveau facteur d'échelle absolu basé sur startScale
            double targetScale = _startScale * e.Scale;
            targetScale = Math.Max(1.0, Math.Min(5.0, targetScale));
            currentScale = targetScale;
            ZoomLayout.Scale = targetScale;

            if (targetScale <= 1.0)
            {
                ZoomLayout.TranslationX = 0;
                ZoomLayout.TranslationY = 0;
            }
            else
            {
                // Zoom centré sur le point focal du pincement (ScaleOrigin)
                double ox = e.ScaleOrigin.X;
                double oy = e.ScaleOrigin.Y;
                double width = ZoomLayout.Width > 0 ? ZoomLayout.Width : this.Width;
                double height = ZoomLayout.Height > 0 ? ZoomLayout.Height : this.Height;

                double deltaScale = targetScale - _startScale;
                double targetTx = _startTranslationX - (deltaScale * (ox - 0.5) * width);
                double targetTy = _startTranslationY - (deltaScale * (oy - 0.5) * height);

                double maxTx = Math.Max(0, (width * (targetScale - 1)) / 2);
                double maxTy = Math.Max(0, (height * (targetScale - 1)) / 2);

                ZoomLayout.TranslationX = Math.Max(-maxTx, Math.Min(maxTx, targetTx));
                ZoomLayout.TranslationY = Math.Max(-maxTy, Math.Min(maxTy, targetTy));
            }
        }
        else if (e.Status == GestureStatus.Completed || e.Status == GestureStatus.Canceled)
        {
            _isPinching = false;
            if (ZoomLayout.Scale <= 1.05)
            {
                ZoomLayout.Scale = 1;
                ZoomLayout.TranslationX = 0;
                ZoomLayout.TranslationY = 0;
                currentScale = 1;
            }
            else
            {
                double width = ZoomLayout.Width > 0 ? ZoomLayout.Width : this.Width;
                double height = ZoomLayout.Height > 0 ? ZoomLayout.Height : this.Height;
                double maxTx = Math.Max(0, (width * (ZoomLayout.Scale - 1)) / 2);
                double maxTy = Math.Max(0, (height * (ZoomLayout.Scale - 1)) / 2);

                ZoomLayout.TranslationX = Math.Max(-maxTx, Math.Min(maxTx, ZoomLayout.TranslationX));
                ZoomLayout.TranslationY = Math.Max(-maxTy, Math.Min(maxTy, ZoomLayout.TranslationY));
            }
        }
    }

    private double _panLastTotalX = 0;
    private double _panLastTotalY = 0;

    private bool TriggerPageTurnSwipe(string detectedGesture)
    {
        if (string.IsNullOrEmpty(detectedGesture)) return false;

        var now = DateTime.UtcNow;
        if ((now - _lastSwipeTurnUtc).TotalMilliseconds < 180)
        {
            return true;
        }

        string nextG = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
        string prevG = Preferences.Default.Get("PrevPageGesture", "SwipeRight");

        if (detectedGesture == nextG)
        {
            _lastSwipeTurnUtc = now;
            MainThread.BeginInvokeOnMainThread(() => OnNextTapped(this, EventArgs.Empty));
            return true;
        }
        if (detectedGesture == prevG)
        {
            _lastSwipeTurnUtc = now;
            MainThread.BeginInvokeOnMainThread(() => OnPrevTapped(this, EventArgs.Empty));
            return true;
        }
        return false;
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (AnnotationBar.IsVisible || _isHighlightMode || _isDrawMode || _isEraserMode) return;

        if (e.StatusType == GestureStatus.Started)
        {
            _panStartX = ZoomLayout.TranslationX;
            _panStartY = ZoomLayout.TranslationY;
            _panLastTotalX = 0;
            _panLastTotalY = 0;
            return;
        }

        if (e.StatusType == GestureStatus.Running)
        {
            if (_isPinching) return;

            _panLastTotalX = e.TotalX;
            _panLastTotalY = e.TotalY;

            if (ZoomLayout.Scale > 1.05)
            {
                double targetX = _panStartX + e.TotalX;
                double targetY = _panStartY + e.TotalY;

                double width = ZoomLayout.Width > 0 ? ZoomLayout.Width : this.Width;
                double height = ZoomLayout.Height > 0 ? ZoomLayout.Height : this.Height;

                double maxTx = Math.Max(0, (width * (ZoomLayout.Scale - 1)) / 2);
                double maxTy = Math.Max(0, (height * (ZoomLayout.Scale - 1)) / 2);

                ZoomLayout.TranslationX = Math.Max(-maxTx, Math.Min(maxTx, targetX));
                ZoomLayout.TranslationY = Math.Max(-maxTy, Math.Min(maxTy, targetY));
            }
        }
        else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
        {
            if (!_isPinching && ZoomLayout.Scale <= 1.05)
            {
                double dist = Math.Sqrt(_panLastTotalX * _panLastTotalX + _panLastTotalY * _panLastTotalY);
                if (dist >= 35)
                {
                    string detectedG = Math.Abs(_panLastTotalX) > Math.Abs(_panLastTotalY)
                        ? (_panLastTotalX < 0 ? "SwipeLeft" : "SwipeRight")
                        : (_panLastTotalY < 0 ? "SwipeUp" : "SwipeDown");
                    TriggerPageTurnSwipe(detectedG);
                }
            }
            _panLastTotalX = 0;
            _panLastTotalY = 0;
            _panStartX = ZoomLayout.TranslationX;
            _panStartY = ZoomLayout.TranslationY;
        }
    }


    private void OnCenterTapped(object sender, EventArgs e) => ShowMenu();

    private void OnCenterSingleTapped(object sender, EventArgs e)
    {
        if (AnnotationBar.IsVisible)
        {
            if (_isAnnotationMode || _isTextMode || _isHighlightMode || _isDrawMode || _isEraserMode) return;

            AnnotationBar.IsVisible = false;
            BottomTouchBar.IsVisible = true; // Réactive la zone tactile du bas !
            StickerPickerOverlay.IsVisible = false;
            _selectedAnnotation = null;
            _pendingSticker = null;
            _isAnnotationMode = false;
            LockAnnotations();

            if (StickersCollection != null)
            {
                StickersCollection.SelectedItem = null;
            }

            RenderAnnotations();
            PageIndicator.Margin = new Thickness(0, 0, 10, 2);
        }
    }

    private void OnImageSingleTapped(object sender, TappedEventArgs e)
    {
        var position = e.GetPosition(this);
        if (position == null) return;

        double x = position.Value.X;
        double y = position.Value.Y;
        double width = this.Width;
        double height = this.Height;

        // Si la barre d'annotation est visible
        if (AnnotationBar.IsVisible)
        {
            if (_isAnnotationMode && !string.IsNullOrEmpty(_pendingSticker))
            {
                _ = PlaceStickerAtAsync(x, y);
                return;
            }
            if (_isTextMode)
            {
                _ = HandleTextPlacementAsync(x, y);
                return;
            }
            if (_isHighlightMode || _isDrawMode || _isEraserMode)
            {
                // En mode dessin, surlignage ou gomme, ne jamais fermer la barre sur un tap
                return;
            }

            // Si aucun mode d'annotation n'est actif et qu'on tape dans le vide, fermer la barre
            AnnotationBar.IsVisible = false;
            BottomTouchBar.IsVisible = true; // Réactive la zone tactile du bas !
            StickerPickerOverlay.IsVisible = false;
            _selectedAnnotation = null;
            _pendingSticker = null;
            _isAnnotationMode = false;
            LockAnnotations();

            if (StickersCollection != null)
            {
                StickersCollection.SelectedItem = null;
            }

            RenderAnnotations();
            PageIndicator.Margin = new Thickness(0, 0, 10, 2);
            return;
        }

        // Zone en haut à droite (les 60px du haut et 20% droite) : Boîte à outils
        if (y < 65 && x > width * 0.8)
        {
            OnToggleToolsDrawerTapped(this, EventArgs.Empty);
            return;
        }

        // Zone du bas (les 100 derniers pixels) : Afficher / Masquer les annotations
        if (y > height - 100)
        {
            OnAnnotationToggleTapped(this, EventArgs.Empty);
            return;
        }

        // Si on est zoomé, on bloque le changement de page pour éviter les sauts accidentels en faisant défiler l'image
        if (ZoomLayout.Scale > 1.05) return;

        string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
        string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");

        // Zone gauche (30%) : vérification du geste configuré
        if (x < width * 0.3)
        {
            if (prevGesture == "TapLeft")
            {
                OnPrevTapped(this, EventArgs.Empty);
            }
            else if (nextGesture == "TapLeft")
            {
                OnNextTapped(this, EventArgs.Empty);
            }
        }
        // Zone droite (30%) : vérification du geste configuré
        else if (x > width * 0.7)
        {
            if (nextGesture == "TapRight")
            {
                OnNextTapped(this, EventArgs.Empty);
            }
            else if (prevGesture == "TapRight")
            {
                OnPrevTapped(this, EventArgs.Empty);
            }
        }
    }

    private void OnImageSwipedLeft(object? sender, SwipedEventArgs e)
    {
        if (AnnotationBar.IsVisible || _isHighlightMode || _isDrawMode || _isEraserMode) return;
        if (ZoomLayout.Scale > 1.05) return;
        TriggerPageTurnSwipe("SwipeLeft");
    }

    private void OnImageSwipedRight(object? sender, SwipedEventArgs e)
    {
        if (AnnotationBar.IsVisible || _isHighlightMode || _isDrawMode || _isEraserMode) return;
        if (ZoomLayout.Scale > 1.05) return;
        TriggerPageTurnSwipe("SwipeRight");
    }

    private void OnImageSwipedUp(object? sender, SwipedEventArgs e)
    {
        if (AnnotationBar.IsVisible || _isHighlightMode || _isDrawMode || _isEraserMode) return;
        if (ZoomLayout.Scale > 1.05) return;
        TriggerPageTurnSwipe("SwipeUp");
    }

    private void OnImageSwipedDown(object? sender, SwipedEventArgs e)
    {
        if (AnnotationBar.IsVisible || _isHighlightMode || _isDrawMode || _isEraserMode) return;
        if (ZoomLayout.Scale > 1.05) return;
        TriggerPageTurnSwipe("SwipeDown");
    }

    private void OnImageDoubleTapped(object sender, TappedEventArgs e)
    {
        var position = e.GetPosition(this);
        if (position == null) return;

        double x = position.Value.X;
        double y = position.Value.Y;
        double width = this.Width > 0 ? this.Width : 1;
        double height = this.Height > 0 ? this.Height : 1;

        // Double tap au centre (25% - 75%) : Afficher le menu central (fonctionne toujours, même zoomé !)
        if (x >= width * 0.25 && x <= width * 0.75)
        {
            ShowMenu();
            return;
        }

        // Double tap sur les côtés :
        // Si déjà zoomé -> réinitialise le zoom à sa taille normale
        if (ZoomLayout.Scale > 1.05)
        {
            ZoomLayout.Scale = 1;
            ZoomLayout.TranslationX = 0;
            ZoomLayout.TranslationY = 0;
            currentScale = 1;
        }
        else
        {
            // Zoom rapide x2.5 centré sur le tap
            double targetScale = 2.5;
            double ox = x / width;
            double oy = y / height;

            double targetTx = - (targetScale - 1.0) * (ox - 0.5) * width;
            double targetTy = - (targetScale - 1.0) * (oy - 0.5) * height;

            double maxTx = Math.Max(0, (width * (targetScale - 1)) / 2);
            double maxTy = Math.Max(0, (height * (targetScale - 1)) / 2);

            ZoomLayout.Scale = targetScale;
            ZoomLayout.TranslationX = Math.Max(-maxTx, Math.Min(maxTx, targetTx));
            ZoomLayout.TranslationY = Math.Max(-maxTy, Math.Min(maxTy, targetTy));
            currentScale = targetScale;
        }
    }

#if ANDROID
    private float _pinchStartDist = 0;
    private double _pinchStartScale = 1.0;
    private float _pinchStartMidX = 0;
    private float _pinchStartMidY = 0;
    private double _pinchStartTx = 0;
    private double _pinchStartTy = 0;
    private float _panLastX = 0;
    private float _panLastY = 0;
    private bool _isNativePinching = false;
    private bool _isNativePanning = false;
    private long _touchDownTime = 0;
    private float _touchDownX = 0;
    private float _touchDownY = 0;
    private long _lastTapTime = 0;
    private long _lastHandledTouchDownTime = -1;
    private long _lastHandledTouchEventTime = -1;
    private int _lastHandledTouchAction = -1;

    private void SetupNativeTouchHandling()
    {
        if (AnnotationsContainer != null)
        {
            AnnotationsContainer.IsVisible = true;
            if (_isAnnotationsLocked)
            {
                AnnotationsContainer.InputTransparent = (PdfWebView != null && PdfWebView.IsVisible);
            }
        }
        if (AnnotationsContainer?.Handler?.PlatformView is Android.Views.View nativeView)
        {
            nativeView.Touch -= OnNativeAnnotationsContainerTouch;
            nativeView.Touch += OnNativeAnnotationsContainerTouch;
        }
        if (ImageContainer?.Handler?.PlatformView is Android.Views.View nativeImgContainer)
        {
            nativeImgContainer.Touch -= OnNativeAnnotationsContainerTouch;
            nativeImgContainer.Touch += OnNativeAnnotationsContainerTouch;
        }
    }

    private void OnNativeAnnotationsContainerTouch(object? sender, Android.Views.View.TouchEventArgs args)
    {
        var motionEvent = args.Event;
        if (motionEvent == null)
        {
            args.Handled = false;
            return;
        }

        if (_lastHandledTouchDownTime == motionEvent.DownTime &&
            _lastHandledTouchEventTime == motionEvent.EventTime &&
            _lastHandledTouchAction == (int)motionEvent.ActionMasked)
        {
            args.Handled = true;
            return;
        }
        _lastHandledTouchDownTime = motionEvent.DownTime;
        _lastHandledTouchEventTime = motionEvent.EventTime;
        _lastHandledTouchAction = (int)motionEvent.ActionMasked;

        float density = Android.App.Application.Context.Resources?.DisplayMetrics?.Density ?? 1.0f;
        int pointerCount = motionEvent.PointerCount;

        // 0. GESTION DU MODE GOMME (1 doigt, mode actif et déverrouillé)
        if (_isEraserMode && !_isAnnotationsLocked)
        {
            double touchX = motionEvent.GetX() / density;
            double touchY = motionEvent.GetY() / density;

            switch (motionEvent.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down:
                case Android.Views.MotionEventActions.Move:
                    (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(true);
                    EraseAnnotationsAtPoint(touchX, touchY);
                    args.Handled = true;
                    return;

                case Android.Views.MotionEventActions.Up:
                case Android.Views.MotionEventActions.Cancel:
                    (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(false);
                    args.Handled = true;
                    return;
            }
        }

        // 1. GESTION DU MODE SURLIGNAGE ET DU MODE DESSIN (1 doigt, mode actif et déverrouillé)
        if ((_isHighlightMode || _isDrawMode) && !_isAnnotationsLocked)
        {
            double touchX = motionEvent.GetX() / density;
            double touchY = motionEvent.GetY() / density;

            switch (motionEvent.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down:
                    (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(true);
                    if (_isHighlightMode) StartLiveHighlightStroke(new Point(touchX, touchY));
                    else if (_isDrawMode) StartLiveDrawStroke(new Point(touchX, touchY));
                    args.Handled = true;
                    return;

                case Android.Views.MotionEventActions.Move:
                    (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(true);
                    if (_isHighlightMode) AddLiveHighlightPoint(new Point(touchX, touchY));
                    else if (_isDrawMode) AddLiveDrawPoint(new Point(touchX, touchY));
                    args.Handled = true;
                    return;

                case Android.Views.MotionEventActions.Up:
                    (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(false);
                    if (_isHighlightMode) _ = FinishLiveHighlightStrokeAsync();
                    else if (_isDrawMode) _ = FinishLiveDrawStrokeAsync();
                    args.Handled = true;
                    return;

                case Android.Views.MotionEventActions.Cancel:
                    (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(false);
                    if (_activeLivePoints.Count >= 1)
                    {
                        if (_isHighlightMode) _ = FinishLiveHighlightStrokeAsync();
                        else if (_isDrawMode) _ = FinishLiveDrawStrokeAsync();
                    }
                    else
                    {
                        if (_activeLivePolyline != null)
                        {
                            ActiveAnnotationsContainer.Children.Remove(_activeLivePolyline);
                            _activeLivePolyline = null;
                        }
                        _activeLivePoints.Clear();
                    }
                    args.Handled = true;
                    return;
            }
        }

        // 1b. GESTION DU TAP EN MODE TEXTE (1 doigt, mode actif et déverrouillé)
        if (_isTextMode && !_isAnnotationsLocked)
        {
            double touchX = motionEvent.GetX() / density;
            double touchY = motionEvent.GetY() / density;

            switch (motionEvent.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down:
                    _touchDownTime = motionEvent.EventTime;
                    _touchDownX = (float)touchX;
                    _touchDownY = (float)touchY;
                    args.Handled = true;
                    return;

                case Android.Views.MotionEventActions.Up:
                    long duration = motionEvent.EventTime - _touchDownTime;
                    float diffX = (float)touchX - _touchDownX;
                    float diffY = (float)touchY - _touchDownY;
                    float dist = (float)Math.Sqrt(diffX * diffX + diffY * diffY);
                    if (dist < 25 && duration < 500)
                    {
                        MainThread.BeginInvokeOnMainThread(async () => await HandleTextPlacementAsync(touchX, touchY));
                    }
                    args.Handled = true;
                    return;
            }
        }

        // 1c. GESTION DU TAP EN MODE STICKER (1 doigt, sticker sélectionné et déverrouillé)
        if (_isAnnotationMode && !string.IsNullOrEmpty(_pendingSticker) && !_isAnnotationsLocked)
        {
            double touchX = motionEvent.GetX() / density;
            double touchY = motionEvent.GetY() / density;

            switch (motionEvent.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down:
                    _touchDownTime = motionEvent.EventTime;
                    _touchDownX = (float)touchX;
                    _touchDownY = (float)touchY;
                    args.Handled = true;
                    return;

                case Android.Views.MotionEventActions.Up:
                    long duration = motionEvent.EventTime - _touchDownTime;
                    float diffX = (float)touchX - _touchDownX;
                    float diffY = (float)touchY - _touchDownY;
                    float dist = (float)Math.Sqrt(diffX * diffX + diffY * diffY);
                    if (dist < 30 && duration < 500)
                    {
                        double containerHeight = ActiveAnnotationsContainer.Height;
                        bool insideOverlay = false;
                        if (StickerPickerOverlay.IsVisible)
                        {
                            double pickerHeight = StickerPickerOverlay.Height > 0 ? StickerPickerOverlay.Height : 270;
                            double pickerTop = containerHeight - pickerHeight - 45 + StickerPickerOverlay.TranslationY;
                            if (touchY >= pickerTop && touchY <= pickerTop + pickerHeight)
                                insideOverlay = true;
                        }
                        if (AnnotationBar.IsVisible)
                        {
                            double barHeight = AnnotationBar.Height > 0 ? AnnotationBar.Height : 45;
                            double barTop = containerHeight - barHeight + AnnotationBar.TranslationY;
                            if (touchY >= barTop && touchY <= barTop + barHeight)
                                insideOverlay = true;
                        }

                        if (!insideOverlay)
                        {
                            MainThread.BeginInvokeOnMainThread(async () => await PlaceStickerAtAsync(touchX, touchY));
                        }
                    }
                    args.Handled = true;
                    return;
            }
        }

        // 2. GESTION DU PINCH / ZOOM À 2 DOIGTS (Multi-touch)
        if (pointerCount >= 2)
        {
            float x0 = motionEvent.GetX(0) / density;
            float y0 = motionEvent.GetY(0) / density;
            float x1 = motionEvent.GetX(1) / density;
            float y1 = motionEvent.GetY(1) / density;

            float dist = (float)Math.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1));
            float midX = (x0 + x1) / 2.0f;
            float midY = (y0 + y1) / 2.0f;

            if (motionEvent.ActionMasked == Android.Views.MotionEventActions.PointerDown || !_isNativePinching)
            {
                _isNativePinching = true;
                _pinchStartDist = Math.Max(dist, 10f);
                _pinchStartScale = ZoomLayout.Scale;
                _pinchStartTx = ZoomLayout.TranslationX;
                _pinchStartTy = ZoomLayout.TranslationY;
                _pinchStartMidX = midX;
                _pinchStartMidY = midY;
            }
            else if (motionEvent.ActionMasked == Android.Views.MotionEventActions.Move && _pinchStartDist > 10f)
            {
                float scaleRatio = dist / _pinchStartDist;
                double newScale = Math.Clamp(_pinchStartScale * scaleRatio, 1.0, 5.0);
                ZoomLayout.Scale = newScale;
                currentScale = newScale;

                if (newScale <= 1.05)
                {
                    ZoomLayout.TranslationX = 0;
                    ZoomLayout.TranslationY = 0;
                }
                else
                {
                    double deltaScale = newScale - _pinchStartScale;
                    double w = ZoomLayout.Width > 0 ? ZoomLayout.Width : this.Width;
                    double h = ZoomLayout.Height > 0 ? ZoomLayout.Height : this.Height;

                    double maxTx = Math.Max(0, (w * (newScale - 1)) / 2.0);
                    double maxTy = Math.Max(0, (h * (newScale - 1)) / 2.0);

                    double tx = _pinchStartTx + (midX - _pinchStartMidX) - (deltaScale * (midX / w - 0.5) * w);
                    double ty = _pinchStartTy + (midY - _pinchStartMidY) - (deltaScale * (midY / h - 0.5) * h);

                    ZoomLayout.TranslationX = Math.Clamp(tx, -maxTx, maxTx);
                    ZoomLayout.TranslationY = Math.Clamp(ty, -maxTy, maxTy);
                }
            }
            args.Handled = true;
            return;
        }

        // Fin du pincement
        if (_isNativePinching && pointerCount < 2)
        {
            _isNativePinching = false;
            _pinchStartDist = 0;
            if (ZoomLayout.Scale <= 1.05)
            {
                ZoomLayout.Scale = 1.0;
                ZoomLayout.TranslationX = 0;
                ZoomLayout.TranslationY = 0;
                currentScale = 1.0;
            }
        }

        // 3. GESTION DES GESTES À 1 DOIGT (Pan, Taps, Double-Tap, Swipes)
        float curX = motionEvent.GetX() / density;
        float curY = motionEvent.GetY() / density;

        switch (motionEvent.ActionMasked)
        {
            case Android.Views.MotionEventActions.Down:
                (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(true);
                _touchDownTime = motionEvent.EventTime;
                _touchDownX = curX;
                _touchDownY = curY;
                _panLastX = curX;
                _panLastY = curY;
                _isNativePanning = ZoomLayout.Scale > 1.05 && !_isHighlightMode;
                args.Handled = true;
                return;

            case Android.Views.MotionEventActions.Move:
                (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(true);
                if (_isNativePanning && ZoomLayout.Scale > 1.05 && !_isHighlightMode)
                {
                    float dx = curX - _panLastX;
                    float dy = curY - _panLastY;
                    _panLastX = curX;
                    _panLastY = curY;

                    double w = ZoomLayout.Width > 0 ? ZoomLayout.Width : this.Width;
                    double h = ZoomLayout.Height > 0 ? ZoomLayout.Height : this.Height;
                    double maxTx = Math.Max(0, (w * (ZoomLayout.Scale - 1)) / 2.0);
                    double maxTy = Math.Max(0, (h * (ZoomLayout.Scale - 1)) / 2.0);

                    ZoomLayout.TranslationX = Math.Clamp(ZoomLayout.TranslationX + dx, -maxTx, maxTx);
                    ZoomLayout.TranslationY = Math.Clamp(ZoomLayout.TranslationY + dy, -maxTy, maxTy);
                }
                args.Handled = true;
                return;

            case Android.Views.MotionEventActions.Up:
                (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(false);
                _isNativePanning = false;
                long duration = motionEvent.EventTime - _touchDownTime;
                float diffX = curX - _touchDownX;
                float diffY = curY - _touchDownY;
                float dist = (float)Math.Sqrt(diffX * diffX + diffY * diffY);

                double width = AnnotationsContainer.Width > 0 ? AnnotationsContainer.Width : (this.Width > 0 ? this.Width : ImageContainer.Width);
                double height = AnnotationsContainer.Height > 0 ? AnnotationsContainer.Height : (this.Height > 0 ? this.Height : ImageContainer.Height);

                // Tap détection (< 28dp, < 400ms)
                if (dist < 28 && duration < 400)
                {
                    // Si la barre d'annotations est visible
                    if (AnnotationBar.IsVisible)
                    {
                        // 1. Zone basse (15%) -> Laisser le tap sur la barre d'annotations elle-même
                        if (_touchDownY > height * 0.85)
                        {
                            args.Handled = false;
                            return;
                        }

                        // 2. Sélection ou désélection d'annotation sur la partition
                        if (!_isAnnotationsLocked)
                        {
                            var hit = FindAnnotationAtPoint(curX, curY, width, height);
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                if (hit != null)
                                {
                                    _selectedAnnotation = hit;
                                    if (hit.Type == AnnotationType.Text)
                                    {
                                        _currentTextColor = hit.Color;
                                        _currentTextSize = hit.Scale <= 0 ? 14 : hit.Scale;
                                        _currentTextBorderColor = string.IsNullOrEmpty(hit.BorderColor) ? "Transparent" : hit.BorderColor;
                                        _currentTextBgColor = string.IsNullOrEmpty(hit.BackgroundColor) ? "Transparent" : hit.BackgroundColor;
                                        UpdateTextColorBorders();
                                        UpdateTextSizeButtons();
                                        UpdateTextBgBorders();
                                        UpdateTextBorderBorders();
                                    }

                                    else if (hit.Type == AnnotationType.Sticker)
                                    {
                                        _currentStickerColor = hit.Color;
                                        _currentStickerBgColor = hit.BackgroundColor;
                                        StickerSizeSlider.Value = hit.Scale;
                                    }
                                }
                                else
                                {
                                    _selectedAnnotation = null;
                                }
                                RenderAnnotations();
                            });
                        }
                        args.Handled = true;
                        return;
                    }

                    // Zone haute droite (15% haut, > 80% largeur) -> Boîte à outils (Tiroir)
                    if (_touchDownY < height * 0.15 && _touchDownX > width * 0.80)
                    {
                        MainThread.BeginInvokeOnMainThread(() => OnToggleToolsDrawerTapped(this, EventArgs.Empty));
                        args.Handled = true;
                        return;
                    }

                    // Zone haute centrale (12% haut, 25%-75% largeur) -> Déroulement de la setlist (si dans une setlist et activé)
                    if (_setlistScores != null && _setlistScores.Count > 0 && Preferences.Default.Get("ShowSetlistProgressOverlay", true) && _touchDownY < height * 0.12 && _touchDownX >= width * 0.25 && _touchDownX <= width * 0.75)
                    {
                        MainThread.BeginInvokeOnMainThread(() => ShowSetlistProgressOverlay());
                        args.Handled = true;
                        return;
                    }

                    // Zone basse (15%) -> Barre d'annotations (quand la barre est fermée)
                    if (_touchDownY > height * 0.85)
                    {
                        MainThread.BeginInvokeOnMainThread(() => OnAnnotationToggleTapped(this, EventArgs.Empty));
                        args.Handled = true;
                        return;
                    }

                    // Zone centrale -> Double-tap menu central (FONCTIONNE TOUJOURS, QUEL QUE SOIT LE ZOOM !)
                    if (_touchDownX >= width * 0.25 && _touchDownX <= width * 0.75 && _touchDownY >= height * 0.2 && _touchDownY <= height * 0.8)
                    {
                        long now = motionEvent.EventTime;
                        if (now - _lastTapTime < 400 && now - _lastTapTime > 0)
                        {
                            MainThread.BeginInvokeOnMainThread(() => ShowMenu());
                            _lastTapTime = 0;
                            args.Handled = true;
                            return;
                        }
                        _lastTapTime = now;
                        args.Handled = true;
                        return;
                    }

                    // Zones gauche / droite pour tap tourne-page (uniquement si non zoomé et barre fermée)
                    if (ZoomLayout.Scale <= 1.05 && !AnnotationBar.IsVisible)
                    {
                        string nextG = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
                        string prevG = Preferences.Default.Get("PrevPageGesture", "SwipeRight");

                        if (_touchDownX < width * 0.3)
                        {
                            if (prevG == "TapLeft")
                            {
                                MainThread.BeginInvokeOnMainThread(() => OnPrevTapped(this, EventArgs.Empty));
                                args.Handled = true;
                                return;
                            }
                            else if (nextG == "TapLeft")
                            {
                                MainThread.BeginInvokeOnMainThread(() => OnNextTapped(this, EventArgs.Empty));
                                args.Handled = true;
                                return;
                            }
                        }
                        else if (_touchDownX > width * 0.7)
                        {
                            if (nextG == "TapRight")
                            {
                                MainThread.BeginInvokeOnMainThread(() => OnNextTapped(this, EventArgs.Empty));
                                args.Handled = true;
                                return;
                            }
                            else if (prevG == "TapRight")
                            {
                                MainThread.BeginInvokeOnMainThread(() => OnPrevTapped(this, EventArgs.Empty));
                                args.Handled = true;
                                return;
                            }
                        }
                    }
                }
                // Swipe détection (>= 28dp, < 1500ms) - uniquement si non zoomé et barre fermée
                else if (dist >= 28 && duration < 1500 && ZoomLayout.Scale <= 1.05 && !AnnotationBar.IsVisible)
                {
                    string detectedG = Math.Abs(diffX) > Math.Abs(diffY)
                        ? (diffX < 0 ? "SwipeLeft" : "SwipeRight")
                        : (diffY < 0 ? "SwipeUp" : "SwipeDown");

                    TriggerPageTurnSwipe(detectedG);
                }
                args.Handled = true;
                return;

            case Android.Views.MotionEventActions.Cancel:
                (sender as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(false);
                _isNativePanning = false;
                long cancelDuration = motionEvent.EventTime - _touchDownTime;
                float cancelDiffX = curX - _touchDownX;
                float cancelDiffY = curY - _touchDownY;
                float cancelDist = (float)Math.Sqrt(cancelDiffX * cancelDiffX + cancelDiffY * cancelDiffY);
                if (cancelDist >= 28 && cancelDuration < 1500 && ZoomLayout.Scale <= 1.05 && !AnnotationBar.IsVisible)
                {
                    string detectedG = Math.Abs(cancelDiffX) > Math.Abs(cancelDiffY)
                        ? (cancelDiffX < 0 ? "SwipeLeft" : "SwipeRight")
                        : (cancelDiffY < 0 ? "SwipeUp" : "SwipeDown");
                    TriggerPageTurnSwipe(detectedG);
                }
                args.Handled = true;
                return;
        }

        args.Handled = false;
    }
#endif
}
