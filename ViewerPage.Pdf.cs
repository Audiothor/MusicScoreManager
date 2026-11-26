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
    private int GetRotationForPage(int pageNum)
    {
        if (_pageRotations.TryGetValue(pageNum, out int rot))
        {
            return rot;
        }
        return _score.Rotation;
    }

    private void UpdateCanvasBounds(double containerW, double containerH, int imgW, int imgH)
    {
        var bounds = AnnotationGeometryHelper.ComputeCanvasBounds(containerW, containerH, imgW, imgH);
        _canvasXRel = bounds.XRel;
        _canvasYRel = bounds.YRel;
        _canvasWRel = bounds.WRel;
        _canvasHRel = bounds.HRel;
    }

    private (double width, double height, bool isLandscape) GetEffectiveViewportInfo()
    {
        double w = _lastAllocatedWidth > 0 ? _lastAllocatedWidth : (this.Width > 0 ? this.Width : ImageContainer.Width);
        double h = _lastAllocatedHeight > 0 ? _lastAllocatedHeight : (this.Height > 0 ? this.Height : ImageContainer.Height);

#if ANDROID
        try
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            var config = activity?.Resources?.Configuration;
            if (config != null && config.Orientation != Android.Content.Res.Orientation.Undefined)
            {
                bool isLandConfig = (config.Orientation == Android.Content.Res.Orientation.Landscape);
                if (w > 0 && h > 0)
                {
                    if (isLandConfig && w < h) { double tmp = w; w = h; h = tmp; }
                    else if (!isLandConfig && w > h) { double tmp = w; w = h; h = tmp; }
                }
                else
                {
                    var decor = activity?.Window?.DecorView;
                    float density = activity?.Resources?.DisplayMetrics?.Density ?? 1.0f;
                    if (density <= 0) density = 1.0f;
                    if (decor != null && decor.Width > 0 && decor.Height > 0)
                    {
                        w = decor.Width / density;
                        h = decor.Height / density;
                        if (isLandConfig && w < h) { double tmp = w; w = h; h = tmp; }
                        else if (!isLandConfig && w > h) { double tmp = w; w = h; h = tmp; }
                    }
                }
                return (w, h, isLandConfig);
            }
        }
        catch { }
#endif

        if (w <= 0 || h <= 0)
        {
            try
            {
                var display = DeviceDisplay.MainDisplayInfo;
                double density = Math.Max(1.0, display.Density);
                w = display.Width / density;
                h = display.Height / density;
            }
            catch { }
        }

        bool isLandscape = (w > h);
        try
        {
            var display = DeviceDisplay.MainDisplayInfo;
            if (display.Orientation == DisplayOrientation.Landscape) isLandscape = true;
            else if (display.Orientation == DisplayOrientation.Portrait) isLandscape = false;
        }
        catch { }

        return (w, h, isLandscape);
    }

    private async Task RenderPdfCurrentAsync(int pageNum)
    {
        try
        {
            string fullPath = _settingsService.GetAbsolutePath(_score.FilePath);
            if (!File.Exists(fullPath)) return;

            if (!_renderStopwatch.IsRunning)
            {
                _renderStopwatch.Restart();
            }

            _isScoreReady = false;

            if (_score.PageCount > 1)
            {
                _maxPages = _score.PageCount;
            }
            int cachedCount = _pdfService.GetCachedPageCount(fullPath);
            if (cachedCount > 0)
            {
                _maxPages = cachedCount;
                if (_score.PageCount != cachedCount)
                {
                    _score.PageCount = cachedCount;
                    _ = _databaseService.SaveScoreAsync(_score);
                }
            }

            var (containerW, containerH, isLandscape) = GetEffectiveViewportInfo();

            bool twoPagesPref = Preferences.Default.Get("TwoPagesLandscape", true);
            int effectivePageCount = cachedCount > 0 ? cachedCount : _score.PageCount;
            bool tryTwoPages = twoPagesPref && isLandscape && (effectivePageCount != 1);

            RenderedPdfPage? rendered = null;

            if (tryTwoPages)
            {
                _isTwoPagesMode = true;
                int leftNum = (pageNum % 2 == 0) ? (pageNum - 1) : pageNum;
                if (leftNum < 1) leftNum = 1;
                int rightNum = leftNum + 1;
                int leftRot = GetRotationForPage(leftNum);
                int rightRot = GetRotationForPage(rightNum);

                rendered = await _pdfService.RenderPdfTwoPagesAsync(fullPath, leftNum, rightNum, leftRot, rightRot);
                if (rendered != null)
                {
                    if (rendered.TotalPages > 0)
                    {
                        _maxPages = rendered.TotalPages;
                        if (_score.PageCount != _maxPages)
                        {
                            _score.PageCount = _maxPages;
                            _ = _databaseService.SaveScoreAsync(_score);
                        }
                    }

                    if (!rendered.IsTwoPages || _maxPages <= 1)
                    {
                        _isTwoPagesMode = false;
                        if (rendered.IsTwoPages)
                        {
                            int rot = GetRotationForPage(1);
                            rendered = await _pdfService.RenderPdfPageAsync(fullPath, 1, rot);
                        }
                        if (rendered != null)
                        {
                            _currentPage = rendered.PageNumber;
                            _lastSinglePageNumber = _currentPage;
                            _currentPageDisplay = _currentPage.ToString();
                        }
                    }
                    else
                    {
                        _isTwoPagesMode = true;
                        _leftPageNumber = rendered.LeftPage;
                        _rightPageNumber = rendered.RightPage;
                        _hasRightPage = rendered.HasRightPage;
                        _currentPage = _leftPageNumber;
                        _currentPageDisplay = _hasRightPage ? $"{_leftPageNumber}-{_rightPageNumber}" : $"{_leftPageNumber}";
                    }
                }
                else
                {
                    // Sécurité : repli sur le rendu 1 page si le rendu 2 pages échoue
                    _isTwoPagesMode = false;
                    int pNum = _maxPages > 0 ? Math.Clamp(pageNum, 1, _maxPages) : Math.Max(1, pageNum);
                    rendered = await _pdfService.RenderPdfPageAsync(fullPath, pNum, GetRotationForPage(pNum));
                    if (rendered != null)
                    {
                        if (rendered.TotalPages > 0)
                        {
                            _maxPages = rendered.TotalPages;
                            if (_score.PageCount != _maxPages)
                            {
                                _score.PageCount = _maxPages;
                                _ = _databaseService.SaveScoreAsync(_score);
                            }
                        }
                        _currentPage = rendered.PageNumber;
                        _lastSinglePageNumber = _currentPage;
                        _currentPageDisplay = _currentPage.ToString();
                    }
                }
            }
            else
            {
                _isTwoPagesMode = false;
                int pNum = _maxPages > 0 ? Math.Clamp(pageNum, 1, _maxPages) : Math.Max(1, pageNum);
                int rot = GetRotationForPage(pNum);

                _currentPage = pNum;
                _currentPageDisplay = _currentPage.ToString();

                rendered = await _pdfService.RenderPdfPageAsync(fullPath, pNum, rot);
                if (rendered != null)
                {
                    if (rendered.TotalPages > 0)
                    {
                        _maxPages = rendered.TotalPages;
                        if (_score.PageCount != _maxPages)
                        {
                            _score.PageCount = _maxPages;
                            _ = _databaseService.SaveScoreAsync(_score);
                        }
                    }
                    _currentPage = rendered.PageNumber;
                    _currentPageDisplay = _currentPage.ToString();
                }
            }

            if (rendered != null)
            {
                var (_, _, latestIsLandscape) = GetEffectiveViewportInfo();
                bool shouldBeTwoPagesNow = twoPagesPref && latestIsLandscape && (_maxPages > 1);
                if (shouldBeTwoPagesNow != _isTwoPagesMode)
                {
                    if (shouldBeTwoPagesNow)
                    {
                        int leftNum = (pageNum % 2 == 0) ? (pageNum - 1) : pageNum;
                        if (leftNum < 1) leftNum = 1;
                        int rightNum = leftNum + 1;
                        var twoRendered = await _pdfService.RenderPdfTwoPagesAsync(fullPath, leftNum, rightNum, GetRotationForPage(leftNum), GetRotationForPage(rightNum));
                        if (twoRendered != null && twoRendered.IsTwoPages)
                        {
                            rendered = twoRendered;
                            _isTwoPagesMode = true;
                            _leftPageNumber = rendered.LeftPage;
                            _rightPageNumber = rendered.RightPage;
                            _hasRightPage = rendered.HasRightPage;
                            _currentPage = _leftPageNumber;
                            _currentPageDisplay = _hasRightPage ? $"{_leftPageNumber}-{_rightPageNumber}" : $"{_leftPageNumber}";
                        }
                    }
                    else
                    {
                        int pNum = Math.Clamp(pageNum, 1, Math.Max(1, _maxPages));
                        var oneRendered = await _pdfService.RenderPdfPageAsync(fullPath, pNum, GetRotationForPage(pNum));
                        if (oneRendered != null)
                        {
                            rendered = oneRendered;
                            _isTwoPagesMode = false;
                            _currentPage = rendered.PageNumber;
                            _currentPageDisplay = _currentPage.ToString();
                        }
                    }
                }
            }

            if (rendered != null && (rendered.ImageBytes.Length > 0
#if ANDROID
                || (rendered.NativeBitmap != null && !rendered.NativeBitmap.IsRecycled)
#endif
            ))
            {
                _currentRenderedWidth = rendered.Width;
                _currentRenderedHeight = rendered.Height;

                ScoreImage.Rotation = 0;
#if ANDROID
                var nativeImageView = ScoreImage.Handler?.PlatformView as Android.Widget.ImageView;
                for (int i = 0; i < 20 && nativeImageView == null; i++)
                {
                    await Task.Delay(16);
                    nativeImageView = ScoreImage.Handler?.PlatformView as Android.Widget.ImageView;
                }

                if (nativeImageView != null && rendered.NativeBitmap != null && !rendered.NativeBitmap.IsRecycled)
                {
                    if (ScoreImage.Source != null)
                    {
                        ScoreImage.Source = null;
                    }
                    nativeImageView.SetScaleType(Android.Widget.ImageView.ScaleType.FitCenter);
                    nativeImageView.SetImageBitmap(rendered.NativeBitmap);
                    nativeImageView.Invalidate();
                }
                else if (rendered.ImageBytes.Length > 0)
                {
                    ScoreImage.Source = ImageSource.FromStream(() => new MemoryStream(rendered.ImageBytes));
                }
#else
                if (rendered.ImageBytes.Length > 0)
                {
                    ScoreImage.Source = ImageSource.FromStream(() => new MemoryStream(rendered.ImageBytes));
                }
#endif

                var (vpW, vpH, _) = GetEffectiveViewportInfo();
                double finalW = (ActiveAnnotationsContainer?.Width > 0) ? ActiveAnnotationsContainer.Width : vpW;
                double finalH = (ActiveAnnotationsContainer?.Height > 0) ? ActiveAnnotationsContainer.Height : vpH;

                UpdateCanvasBounds(finalW, finalH, rendered.Width, rendered.Height);

                _isScoreReady = true;

#if ANDROID
                if (nativeImageView != null)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    nativeImageView.Post(() => tcs.TrySetResult(true));
                    await Task.WhenAny(tcs.Task, Task.Delay(50));
                }
                else
                {
                    await Task.Delay(20);
                }
#else
                await Task.Delay(20);
#endif

                HideViewerLoadingOverlay();
                UpdatePageIndicator();
                RenderAnnotations();

                if (ActiveAnnotationsContainer != null)
                {
                    ActiveAnnotationsContainer.IsVisible = true;
                    ActiveAnnotationsContainer.Opacity = (_score.ShowAnnotations && _isScoreReady) ? 1 : 0;
                }

                if (_lastAllocatedWidth > 0 && _lastAllocatedHeight > 0)
                {
                    bool finalIsLandscape = (_lastAllocatedWidth > _lastAllocatedHeight);
#if ANDROID
                    try
                    {
                        var config = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Resources?.Configuration;
                        if (config != null && config.Orientation != Android.Content.Res.Orientation.Undefined)
                        {
                            finalIsLandscape = (config.Orientation == Android.Content.Res.Orientation.Landscape);
                        }
                    }
                    catch { }
#endif
                    bool finalShouldBeTwoPages = twoPagesPref && finalIsLandscape && (_maxPages > 1);
                    if (finalShouldBeTwoPages != _isTwoPagesMode)
                    {
                        _isTwoPagesMode = finalShouldBeTwoPages;
                        int targetPage = finalShouldBeTwoPages
                            ? ((_lastSinglePageNumber % 2 == 0) ? Math.Max(1, _lastSinglePageNumber - 1) : _lastSinglePageNumber)
                            : (_lastSinglePageNumber > 0 ? _lastSinglePageNumber : _currentPage);
                        _ = RenderPdfCurrentAsync(targetPage);
                        return;
                    }
                }

                if (_renderStopwatch.IsRunning)
                {
                    _renderStopwatch.Stop();
                    long ms = _renderStopwatch.ElapsedMilliseconds;
                    System.Diagnostics.Debug.WriteLine($"[BENCHMARK] Rendu PDF Natif terminé en {ms} ms (Page {_currentPageDisplay}/{_maxPages}) !");
                }

                if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();

                _ = Task.Run(() => PreloadAdjacentPdfPagesAsync(fullPath, _currentPage));
            }
        }
        catch (Exception ex)
        {
            HideViewerLoadingOverlay();
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur RenderPdfCurrentAsync: {ex.Message}");
        }
        finally
        {
            if (!_isScoreReady)
            {
                HideViewerLoadingOverlay();
            }
        }
    }

    private async Task PreloadAdjacentPdfPagesAsync(string fullPath, int currentLeft)
    {
        try
        {
            await Task.Delay(150);
            if (_isTwoPagesMode)
            {
                if (currentLeft + 2 <= _maxPages)
                {
                    await _pdfService.RenderPdfTwoPagesAsync(fullPath, currentLeft + 2, currentLeft + 3, GetRotationForPage(currentLeft + 2), GetRotationForPage(currentLeft + 3));
                }
                if (currentLeft - 2 >= 1)
                {
                    await _pdfService.RenderPdfTwoPagesAsync(fullPath, currentLeft - 2, currentLeft - 1, GetRotationForPage(currentLeft - 2), GetRotationForPage(currentLeft - 1));
                }
            }
            else
            {
                if (currentLeft + 1 <= _maxPages)
                {
                    await _pdfService.RenderPdfPageAsync(fullPath, currentLeft + 1, GetRotationForPage(currentLeft + 1));
                }
                if (currentLeft - 1 >= 1)
                {
                    await _pdfService.RenderPdfPageAsync(fullPath, currentLeft - 1, GetRotationForPage(currentLeft - 1));
                }
            }
        }
        catch { }
    }

    private static bool _isPdfJsReady = false;

    private static async Task EnsurePdfJsReadyAsync()
    {
        if (_isPdfJsReady) return;

        string cacheDir = FileSystem.CacheDirectory;
        string pdfjsDir = Path.Combine(cacheDir, "pdfjs");

        if (!Directory.Exists(pdfjsDir)) Directory.CreateDirectory(pdfjsDir);

        string[] files = { "pdf.min.js", "pdf.worker.min.js", "viewer.html" };

        foreach (var file in files)
        {
            string dest = Path.Combine(pdfjsDir, file);
            // Toujours rafraîchir viewer.html pour intégrer les améliorations du lecteur
            if (!File.Exists(dest) || file == "viewer.html")
            {
                try
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync($"pdfjs/{file}");
                    using var fileStream = File.Create(dest);
                    await stream.CopyToAsync(fileStream);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error copying {file}: {ex.Message}");
                }
            }
        }

        _isPdfJsReady = true;
    }

    private async void OnPdfWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        HideViewerLoadingOverlay();
        if (_score.Type == ScoreType.PDF && e.Result == WebNavigationResult.Success)
        {
            System.Diagnostics.Debug.WriteLine("[Viewer] WebView Navigated - Shell HTML prêt.");

            // Synchronisation de sécurité des rotations personnalisées
            if (_pageRotations.Count > 0)
            {
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(_pageRotations);
                    if (PdfWebView != null) await PdfWebView.EvaluateJavaScriptAsync($"setPageRotationsMap({json})");
                }
                catch { }
            }

            try
            {
                string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
                string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");
                if (PdfWebView != null) await PdfWebView.EvaluateJavaScriptAsync($"setGestures('{nextGesture}', '{prevGesture}')");
            }
            catch { }

            if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();
        }
    }


    private void ParsePageParams(string url)
    {
        try
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

            if (int.TryParse(query["max"], out int max)) _maxPages = max;
            if (int.TryParse(query["current"], out int current)) _currentPage = current;
            string? display = query["displayCurrent"];
            _currentPageDisplay = !string.IsNullOrEmpty(display) ? display : _currentPage.ToString();

            if (int.TryParse(query["twoPages"], out int tp))
            {
                _isTwoPagesMode = (tp == 1);
            }

            if (int.TryParse(query["leftPage"], out int lp)) _leftPageNumber = lp;
            else _leftPageNumber = _currentPage;

            if (int.TryParse(query["rightPage"], out int rp)) _rightPageNumber = rp;
            else _rightPageNumber = _currentPage + 1;

            if (int.TryParse(query["hasRight"], out int hr)) _hasRightPage = (hr == 1);
            else _hasRightPage = _isTwoPagesMode && (_rightPageNumber <= _maxPages);

            if (double.TryParse(query["cx"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double cx)) _canvasXRel = Math.Clamp(cx, 0.0, 0.45);
            if (double.TryParse(query["cy"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double cy)) _canvasYRel = Math.Clamp(cy, 0.0, 0.45);
            if (double.TryParse(query["cw"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double cw) && cw > 0.1) _canvasWRel = Math.Clamp(cw, 0.1, 1.0);
            if (double.TryParse(query["ch"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double ch) && ch > 0.1) _canvasHRel = Math.Clamp(ch, 0.1, 1.0);

            UpdatePageIndicator();
        }
        catch { }
    }

    private void UpdatePageIndicator()
    {
        bool showPageNumber = _score != null && _score.ShowPageNumber && Preferences.Default.Get("ShowPageNumber", true);
        double fontSize = Preferences.Default.Get("PageNumberSize", 14.0);

        if (showPageNumber)
        {
            PageIndicator.IsVisible = true;
            PageIndicator.FontSize = fontSize;
            string displayNum = !string.IsNullOrEmpty(_currentPageDisplay) ? _currentPageDisplay : _currentPage.ToString();
            PageIndicator.Text = $"{displayNum} / {_maxPages}";
        }
        else
        {
            PageIndicator.IsVisible = false;
        }

        UpdateBookmarkIndicator();
    }


    private async void OnPrevTapped(object sender, EventArgs e)
    {
        if (ZoomLayout.Scale > 1.05) return;
        if (_score.Type == ScoreType.Image)
        {
            HandleStartOfScore();
        }
        else if (_score.Type == ScoreType.PDF)
        {
            if (PdfService.IsNativePdfSupported)
            {
                if (_isTwoPagesMode)
                {
                    if (_leftPageNumber <= 0)
                    {
                        _leftPageNumber = (_currentPage % 2 == 0) ? (_currentPage - 1) : _currentPage;
                        if (_leftPageNumber < 1) _leftPageNumber = 1;
                    }

                    int prevLeft = _leftPageNumber - 2;
                    if (prevLeft >= 1)
                    {
                        await RenderPdfCurrentAsync(prevLeft);
                    }
                    else
                    {
                        HandleStartOfScore();
                    }
                }
                else
                {
                    if (_currentPage > 1)
                    {
                        await RenderPdfCurrentAsync(_currentPage - 1);
                    }
                    else
                    {
                        HandleStartOfScore();
                    }
                }
            }
            else if (DeviceInfo.Platform == DevicePlatform.Android && PdfWebView != null && PdfWebView.IsVisible)
            {
                await PdfWebView.EvaluateJavaScriptAsync("prevPage();");
            }
            else
            {
                if (_currentPage > 1)
                {
                    await GoToPage(_currentPage - 1);
                }
                else
                {
                    HandleStartOfScore();
                }
            }
        }
    }

    private async void OnNextTapped(object sender, EventArgs e)
    {
        if (ZoomLayout.Scale > 1.05) return;
        if (_score.Type == ScoreType.Image)
        {
            HandleEndOfScore();
        }
        else if (_score.Type == ScoreType.PDF)
        {
            if (PdfService.IsNativePdfSupported)
            {
                if (_isTwoPagesMode)
                {
                    if (_leftPageNumber <= 0)
                    {
                        _leftPageNumber = (_currentPage % 2 == 0) ? (_currentPage - 1) : _currentPage;
                        if (_leftPageNumber < 1) _leftPageNumber = 1;
                    }

                    int nextLeft = _leftPageNumber + 2;
                    if (nextLeft <= _maxPages)
                    {
                        await RenderPdfCurrentAsync(nextLeft);
                    }
                    else
                    {
                        HandleEndOfScore();
                    }
                }
                else
                {
                    if (_currentPage < _maxPages)
                    {
                        await RenderPdfCurrentAsync(_currentPage + 1);
                    }
                    else
                    {
                        HandleEndOfScore();
                    }
                }
            }
            else if (DeviceInfo.Platform == DevicePlatform.Android && PdfWebView != null && PdfWebView.IsVisible)
            {
                await PdfWebView.EvaluateJavaScriptAsync("nextPage();");
            }
            else
            {
                if (_currentPage < _maxPages)
                {
                    await GoToPage(_currentPage + 1);
                }
                else
                {
                    HandleEndOfScore();
                }
            }
        }
    }


    private void OnGoToPageClicked(object sender, EventArgs e)
    {
        CentralMenuOverlay.IsVisible = false;
        GoToPageEntry.Text = _currentPage.ToString();
        GoToPageSubtitleLabel.Text = $"Page (1-{_maxPages}) :";
        GoToPageShowNumberSwitch.IsToggled = _score.ShowPageNumber;
        GoToPageModal.IsVisible = true;
    }

    private void OnGoToPageCancelClicked(object sender, EventArgs e)
    {
        GoToPageModal.IsVisible = false;
        ImageContainer.InputTransparent = false;
        ZoomLayout.InputTransparent = false;
        BottomTouchBar.IsVisible = !AnnotationBar.IsVisible;
    }

    private async void OnGoToPageOkClicked(object sender, EventArgs e)
    {
        GoToPageModal.IsVisible = false;
        ImageContainer.InputTransparent = false;
        ZoomLayout.InputTransparent = false;
        BottomTouchBar.IsVisible = !AnnotationBar.IsVisible;

        bool showNumberChanged = _score.ShowPageNumber != GoToPageShowNumberSwitch.IsToggled;
        _score.ShowPageNumber = GoToPageShowNumberSwitch.IsToggled;

        if (int.TryParse(GoToPageEntry.Text, out int pageNum))
        {
            await GoToPage(pageNum);
        }

        if (showNumberChanged)
        {
            UpdatePageIndicator();
            await _databaseService.SaveScoreAsync(_score);
            UpdateToolsDrawerUI();
        }
    }

    private async Task GoToPage(int pageNum)
    {
        if (pageNum >= 1 && pageNum <= _maxPages)
        {
            _currentPage = pageNum;
            if (_score.Type == ScoreType.PDF)
            {
                if (PdfService.IsNativePdfSupported)
                {
                    await RenderPdfCurrentAsync(pageNum);
                }
                else
                {
                    _isScoreReady = false;
                    RenderAnnotations();
                    if (PdfWebView != null) await PdfWebView.EvaluateJavaScriptAsync($"goToPage({pageNum})");
                }
            }
            UpdatePageIndicator();
        }
    }


    private bool _isSwitchingScore = false;

    private async void HandleEndOfScore()
    {
        if (_setlistScores != null && _currentIndex != -1 && !_isSwitchingScore)
        {
            if (_isContinuous && _currentIndex < _setlistScores.Count - 1)
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
            else
            {
                // Si la lecture en continu n'est pas activée (ou si on est à la toute fin de la setlist)
                if (!_isContinuous && Preferences.Default.Get("ReturnToSetlistOnEndOfScore", false))
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Navigation.PopAsync();
                    });
                }
                else
                {
                    // Bloquer le retour à l'accueil en fin de partition/setlist
                    System.Diagnostics.Debug.WriteLine("[Viewer] Bloqué : fin de la partition/setlist, retour accueil évité.");
                }
            }
        }
    }

    private async void HandleStartOfScore()
    {
        if (_setlistScores != null && _currentIndex > 0 && _isContinuous && !_isSwitchingScore)
        {
            _isSwitchingScore = true;
            try
            {
                int prevIndex = _currentIndex - 1;
                var prevScore = _setlistScores[prevIndex];
                await SwitchToScoreAsync(prevScore, prevIndex, toLastPage: true);
            }
            finally
            {
                _isSwitchingScore = false;
            }
        }
        else
        {
            // Bloquer le retour à l'accueil au début de la partition/setlist
            System.Diagnostics.Debug.WriteLine("[Viewer] Bloqué : début de la setlist/partition, retour accueil évité.");
        }
    }

    private async Task SwitchToScoreAsync(Score targetScore, int targetIndex, bool toLastPage)
    {
        if (targetScore == null) return;

        System.Diagnostics.Debug.WriteLine($"[Viewer] SwitchToScoreAsync: Passage fluide à la partition '{targetScore.Title}' (Index {targetIndex}, toLastPage: {toLastPage})");

        CancelPreCount();
        StopMetronome();
        if (AudioRecorderService.Instance.IsRecording)
        {
            await StopAndSaveRecordingAsync();
        }
        AudioRecorderService.Instance.StopPreview();
        try
        {
            AudioPlayer.Stop();
            AudioPlayer.Source = null;
        }
        catch { }

        _score = targetScore;
        _currentIndex = targetIndex;
        Title = _score.Title;
        _isScoreReady = false;
        if (ActiveAnnotationsContainer != null)
        {
            ActiveAnnotationsContainer.Opacity = 0;
            ActiveAnnotationsContainer.Children.Clear();
        }

        // Réinitialiser le zoom et les translations
        ZoomLayout.Scale = 1;
        ZoomLayout.TranslationX = 0;
        ZoomLayout.TranslationY = 0;
        currentScale = 1;

        // Réinitialiser historique des annotations
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateUndoRedoButtons();
        _selectedAnnotation = null;
        _pendingSticker = null;

        // Correction automatique du type si nécessaire
        if (!string.IsNullOrEmpty(_score.FilePath))
        {
            string ext = Path.GetExtension(_score.FilePath)?.ToLowerInvariant() ?? "";
            if (ext == ".pdf") _score.Type = ScoreType.PDF;
            else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".webp" || ext == ".bmp") _score.Type = ScoreType.Image;
        }

        // Rafraîchir depuis la base de données
        try
        {
            var refreshed = await _databaseService.GetScoreAsync(_score.Id);
            if (refreshed != null) _score = refreshed;
        }
        catch { }

        Title = _score.Title;
        if (MenuTitleLabel != null) MenuTitleLabel.Text = _score.Title;
        if (MetronomeBpmLabel != null) MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
        if (AnnotationBar != null && AnnotationBar.IsVisible)
        {
            _score.ShowAnnotations = true;
        }
        UpdateToolsDrawerUI();

        _currentRotation = _score.IsRotationSaved ? _score.Rotation : 0;

        await LoadPageRotationsAsync();
        await LoadAnnotationsAsync();
        await LoadBookmarksAsync();
        InitializeAudio(forceReload: true);
        InitializeMetronome();
        if (RecorderOverlay != null && RecorderOverlay.IsVisible)
        {
            RefreshRecorderTakesUI();
        }

        string fullPath = _settingsService.GetAbsolutePath(_score.FilePath);
        if (!File.Exists(fullPath))
        {
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(
                loc.GetString("Viewer_File_Not_Found_Title", "Fichier introuvable"),
                string.Format(loc.GetString("Viewer_File_Not_Found_Msg", "Le fichier est introuvable :\n{0}"), fullPath),
                loc.GetString("Common_OK", "OK"));
            return;
        }

        if (_score.Type == ScoreType.PDF && PdfService.IsNativePdfSupported)
        {
            _maxPages = _score.PageCount > 0 ? _score.PageCount : await _pdfService.GetPdfPageCountAsync(fullPath);
            int targetPage = toLastPage ? _maxPages : 1;
            await RenderPdfCurrentAsync(targetPage);
        }
        else if (_score.Type == ScoreType.PDF && PdfWebView != null && PdfWebView.IsVisible)
        {
            // Transition fluide ultra-rapide dans le canvas existant sans rechargement de WebView
            string finalFilePath = fullPath.StartsWith("/") ? $"file://{fullPath}" : $"file:///{fullPath}";
            string pageRotsJson = System.Text.Json.JsonSerializer.Serialize(_pageRotations);
            string targetPageStr = toLastPage ? "'last'" : "1";

            try
            {
                await PdfWebView.EvaluateJavaScriptAsync($"setPageRotationsMap({pageRotsJson});");
                await PdfWebView.EvaluateJavaScriptAsync($"loadPdf('{finalFilePath}', {_score.Rotation}, {targetPageStr});");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur EvaluateJavaScriptAsync loadPdf: {ex.Message}");
                LoadContentAsync();
            }
        }
        else
        {
            LoadContentAsync();
        }

        // Préchargement asynchrone en tâche de fond des partitions adjacentes (précédente et suivante)
        _ = Task.Run(async () => await PreloadAdjacentScoresAsync());
    }

    private async Task PreloadAdjacentScoresAsync()
    {
        if (_setlistScores == null || !_isContinuous) return;

        try
        {
            await Task.Delay(200); // Petit répit pour laisser le rendu actuel 100% prioritaire

            // 1. Partition suivante (+1)
            if (_currentIndex + 1 < _setlistScores.Count)
            {
                var next = _setlistScores[_currentIndex + 1];
                if (next != null)
                {
                    _ = _databaseService.GetAnnotationsForScoreAsync(next.Id);
                    _ = _databaseService.GetPageRotationsForScoreAsync(next.Id);
                    
                    if (next.Type == ScoreType.PDF)
                    {
                        string nextPath = _settingsService.GetAbsolutePath(next.FilePath);
                        if (File.Exists(nextPath))
                        {
                            if (PdfService.IsNativePdfSupported)
                            {
                                _ = _pdfService.PreloadFirstPageAsync(nextPath, next.Rotation, next.PageCount);
                            }
                            else if (DeviceInfo.Platform == DevicePlatform.Android)
                            {
                                string finalNextPath = nextPath.StartsWith("/") ? $"file://{nextPath}" : $"file:///{nextPath}";
                                MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    try
                                    {
                                        if (PdfWebView != null)
                                            await PdfWebView.EvaluateJavaScriptAsync($"preloadPdf('{finalNextPath}');");
                                    }
                                    catch { }
                                });
                            }
                        }
                    }
                }
            }

            // 2. Partition précédente (-1)
            if (_currentIndex - 1 >= 0)
            {
                var prev = _setlistScores[_currentIndex - 1];
                if (prev != null)
                {
                    _ = _databaseService.GetAnnotationsForScoreAsync(prev.Id);
                    _ = _databaseService.GetPageRotationsForScoreAsync(prev.Id);

                    if (prev.Type == ScoreType.PDF)
                    {
                        string prevPath = _settingsService.GetAbsolutePath(prev.FilePath);
                        if (File.Exists(prevPath))
                        {
                            if (PdfService.IsNativePdfSupported)
                            {
                                _ = _pdfService.PreloadFirstPageAsync(prevPath, prev.Rotation, prev.PageCount);
                            }
                            else if (DeviceInfo.Platform == DevicePlatform.Android)
                            {
                                string finalPrevPath = prevPath.StartsWith("/") ? $"file://{prevPath}" : $"file:///{prevPath}";
                                MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    try
                                    {
                                        if (PdfWebView != null)
                                            await PdfWebView.EvaluateJavaScriptAsync($"preloadPdf('{finalPrevPath}');");
                                    }
                                    catch { }
                                });
                            }
                        }
                    }
                }
            }
        }
        catch { }
    }


    #region Setlist Progress Overlay & Bookmarks

    private void OnTopSetlistTouchTapped(object? sender, EventArgs e)
    {
        if (_setlistScores != null && _setlistScores.Count > 0 && Preferences.Default.Get("ShowSetlistProgressOverlay", true))
        {
            ShowSetlistProgressOverlay();
        }
    }

    private void OnCloseSetlistProgressClicked(object? sender, EventArgs e)
    {
        HideSetlistProgressOverlay();
    }

    private void ShowSetlistProgressOverlay()
    {
        if (_setlistScores == null || _setlistScores.Count == 0) return;

        // Mettre à jour le compteur
        int total = _setlistScores.Count;
        int currentDisplayIndex = (_currentIndex >= 0 && _currentIndex < total) ? _currentIndex + 1 : 1;
        if (SetlistProgressCounterLabel != null)
        {
            SetlistProgressCounterLabel.Text = $"{currentDisplayIndex} / {total}";
        }

        // Construire la liste des éléments
        Border? currentItemBorder = null;
        if (SetlistProgressStack != null)
        {
            SetlistProgressStack.Children.Clear();

            for (int i = 0; i < _setlistScores.Count; i++)
            {
                var s = _setlistScores[i];
                int itemIndex = i;
                bool isCurrent = (i == _currentIndex) || (_score != null && s.Id == _score.Id);
                bool isPassed = i < _currentIndex;

                // Border conteneur de chaque partition
                var itemBorder = new Border
                {
                    Padding = new Thickness(12, 10),
                    StrokeThickness = isCurrent ? 2 : 1,
                    BackgroundColor = isCurrent 
                        ? Microsoft.Maui.Graphics.Color.FromArgb("#243B55") 
                        : (isPassed ? Microsoft.Maui.Graphics.Color.FromArgb("#141418") : Microsoft.Maui.Graphics.Color.FromArgb("#1C1E26")),
                    Stroke = isCurrent 
                        ? Microsoft.Maui.Graphics.Color.FromArgb("#00B4D8") 
                        : (isPassed ? Microsoft.Maui.Graphics.Color.FromArgb("#2C2C38") : Microsoft.Maui.Graphics.Color.FromArgb("#3C3C50")),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }
                };

                if (isCurrent)
                {
                    currentItemBorder = itemBorder;
                }

                var itemGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 12
                };

                // Badge statut / numéro
                string statusIcon = isCurrent ? "▶" : (isPassed ? "✓" : $"{i + 1}");
                var badgeLabel = new Label
                {
                    Text = statusIcon,
                    TextColor = isCurrent 
                        ? Microsoft.Maui.Graphics.Color.FromArgb("#00FFB2") 
                        : (isPassed ? Microsoft.Maui.Graphics.Color.FromArgb("#52B788") : Microsoft.Maui.Graphics.Color.FromArgb("#74C0FC")),
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    WidthRequest = 26,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };
                Grid.SetColumn(badgeLabel, 0);
                itemGrid.Children.Add(badgeLabel);

                // Informations du morceau (Titre & Compositeur)
                var infoStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                var titleLabel = new Label
                {
                    Text = s.Title,
                    FontAttributes = isCurrent ? FontAttributes.Bold : FontAttributes.None,
                    FontSize = isCurrent ? 14 : 13,
                    TextColor = isCurrent 
                        ? Microsoft.Maui.Graphics.Colors.White 
                        : (isPassed ? Microsoft.Maui.Graphics.Color.FromArgb("#8A95A5") : Microsoft.Maui.Graphics.Color.FromArgb("#EAF2FF")),
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                infoStack.Children.Add(titleLabel);

                if (!string.IsNullOrWhiteSpace(s.Composer))
                {
                    var composerLabel = new Label
                    {
                        Text = s.Composer,
                        FontSize = 11,
                        TextColor = isCurrent 
                            ? Microsoft.Maui.Graphics.Color.FromArgb("#A0D8EF") 
                            : (isPassed ? Microsoft.Maui.Graphics.Color.FromArgb("#5C6773") : Microsoft.Maui.Graphics.Color.FromArgb("#A5C8E4")),
                        LineBreakMode = LineBreakMode.TailTruncation
                    };
                    infoStack.Children.Add(composerLabel);
                }
                Grid.SetColumn(infoStack, 1);
                itemGrid.Children.Add(infoStack);

                // Indicateur texte droite
                var locState = LocalizationService.Instance;
                string stateText = isCurrent
                    ? locState.GetString("Viewer_Setlist_State_Current", "En cours")
                    : (isPassed
                        ? locState.GetString("Viewer_Setlist_State_Passed", "Passé")
                        : locState.GetString("Viewer_Setlist_State_Upcoming", "À venir"));
                var stateLabel = new Label
                {
                    Text = stateText,
                    FontSize = 11,
                    FontAttributes = isCurrent ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = isCurrent 
                        ? Microsoft.Maui.Graphics.Color.FromArgb("#00B4D8") 
                        : (isPassed ? Microsoft.Maui.Graphics.Color.FromArgb("#52B788") : Microsoft.Maui.Graphics.Color.FromArgb("#4DABF7")),
                    VerticalTextAlignment = TextAlignment.Center
                };
                Grid.SetColumn(stateLabel, 2);
                itemGrid.Children.Add(stateLabel);

                itemBorder.Content = itemGrid;

                // Taper sur un morceau permet d'y aller directement
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (sdr, ev) =>
                {
                    HideSetlistProgressOverlay();
                    if (itemIndex != _currentIndex)
                    {
                        await SwitchToScoreAsync(s, itemIndex, toLastPage: false);
                    }
                };
                itemBorder.GestureRecognizers.Add(tapGesture);

                SetlistProgressStack.Children.Add(itemBorder);
            }
        }

        if (SetlistProgressModal != null)
        {
            SetlistProgressModal.IsVisible = true;
            SetlistProgressOverlay.Opacity = 0;
            SetlistProgressOverlay.TranslationY = -60;
            _ = Task.WhenAll(
                SetlistProgressOverlay.FadeToAsync(1, 200, Easing.CubicOut),
                SetlistProgressOverlay.TranslateToAsync(0, 0, 200, Easing.CubicOut)
            );

            // Centrer automatiquement le morceau en cours dans la boîte de défilement
            if (currentItemBorder != null && SetlistProgressScrollView != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Petit délai pour laisser le temps au layout de calculer les positions des éléments
                    await Task.Delay(100);
                    try
                    {
                        await SetlistProgressScrollView.ScrollToAsync(currentItemBorder, ScrollToPosition.Center, animated: true);
                    }
                    catch { }
                });
            }
        }
    }

    private void HideSetlistProgressOverlay()
    {
        if (SetlistProgressModal != null && SetlistProgressModal.IsVisible)
        {
            _ = Task.WhenAll(
                SetlistProgressOverlay.FadeToAsync(0, 150, Easing.CubicIn),
                SetlistProgressOverlay.TranslateToAsync(0, -60, 150, Easing.CubicIn)
            ).ContinueWith(t =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SetlistProgressModal.IsVisible = false;
                });
            });
        }
    }

    #region Bookmarks / Marqueurs de page

    private async Task LoadBookmarksAsync()
    {
        try
        {
            _bookmarks = await _databaseService.GetBookmarksForScoreAsync(_score.Id);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateBookmarkIndicator();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur chargement marqueurs: {ex.Message}");
        }
    }

    private void UpdateBookmarkIndicator()
    {
        if (PageBookmarkBadge == null || PageBookmarkBadgeLabel == null) return;

        var currentBm = _bookmarks.FirstOrDefault(b => b.PageNumber == _currentPage);
        if (currentBm != null)
        {
            PageBookmarkBadge.IsVisible = true;
            PageBookmarkBadgeLabel.Text = currentBm.Name;
        }
        else
        {
            PageBookmarkBadge.IsVisible = false;
        }

        if (CentralMenuOverlay != null && CentralMenuOverlay.IsVisible)
        {
            UpdateMenuBookmarkState();
        }
    }

    private string GetNextAvailableBookmarkName()
    {
        var usedNames = _bookmarks.Select(b => b.Name.Trim().ToUpperInvariant()).ToHashSet();
        for (char c = 'A'; c <= 'Z'; c++)
        {
            string name = c.ToString();
            if (!usedNames.Contains(name))
                return name;
        }
        for (int i = 1; i <= 99; i++)
        {
            string name = $"M{i}";
            if (!usedNames.Contains(name))
                return name;
        }
        return "BM";
    }

    private async Task GoToNextBookmarkAsync()
    {
        if (_bookmarks == null || _bookmarks.Count == 0) return;
        var sorted = _bookmarks.OrderBy(b => b.PageNumber).ToList();
        var next = sorted.FirstOrDefault(b => b.PageNumber > _currentPage);
        if (next != null)
        {
            await GoToPage(next.PageNumber);
        }
        else
        {
            // Boucler au tout premier marqueur
            await GoToPage(sorted.First().PageNumber);
        }
    }

    private async Task GoToPreviousBookmarkAsync()
    {
        if (_bookmarks == null || _bookmarks.Count == 0) return;
        var sorted = _bookmarks.OrderBy(b => b.PageNumber).ToList();
        var prev = sorted.LastOrDefault(b => b.PageNumber < _currentPage);
        if (prev != null)
        {
            await GoToPage(prev.PageNumber);
        }
        else
        {
            // Boucler au tout dernier marqueur
            await GoToPage(sorted.Last().PageNumber);
        }
    }

    private void UpdateMenuBookmarkState()
    {
        if (MenuBookmarkSwitch == null) return;
        _isUpdatingBookmarkMenu = true;
        try
        {
            var currentBm = _bookmarks.FirstOrDefault(b => b.PageNumber == _currentPage);
            MenuBookmarkSwitch.IsToggled = (currentBm != null);
            if (MenuBookmarkStatusLabel != null)
            {
                var loc = LocalizationService.Instance;
                MenuBookmarkStatusLabel.Text = currentBm != null
                    ? string.Format(loc.GetString("Viewer_Bookmark_Page_Active"), currentBm.Name, _currentPage)
                    : string.Format(loc.GetString("Viewer_Bookmark_Page_Inactive"), _currentPage);
            }

            bool hasOtherBookmarks = _bookmarks.Count > 1 || (_bookmarks.Count == 1 && currentBm == null);
            if (MenuBookmarkNavRow != null)
            {
                MenuBookmarkNavRow.IsVisible = _bookmarks.Count > 0;
            }
            if (MenuPrevBookmarkBtn != null)
            {
                MenuPrevBookmarkBtn.IsEnabled = hasOtherBookmarks;
            }
            if (MenuNextBookmarkBtn != null)
            {
                MenuNextBookmarkBtn.IsEnabled = hasOtherBookmarks;
            }
        }
        finally
        {
            _isUpdatingBookmarkMenu = false;
        }
    }

    private async void OnMenuBookmarkToggled(object sender, ToggledEventArgs e)
    {
        if (_isUpdatingBookmarkMenu) return;

        var currentBm = _bookmarks.FirstOrDefault(b => b.PageNumber == _currentPage);
        if (e.Value && currentBm == null)
        {
            string nextName = GetNextAvailableBookmarkName();
            var newBm = new ScoreBookmark
            {
                ScoreId = _score.Id,
                PageNumber = _currentPage,
                Name = nextName
            };
            await _databaseService.SaveBookmarkAsync(newBm);
            _bookmarks = await _databaseService.GetBookmarksForScoreAsync(_score.Id);
            UpdateBookmarkIndicator();
            UpdateMenuBookmarkState();
        }
        else if (!e.Value && currentBm != null)
        {
            await _databaseService.DeleteBookmarkAsync(currentBm);
            _bookmarks = await _databaseService.GetBookmarksForScoreAsync(_score.Id);
            UpdateBookmarkIndicator();
            UpdateMenuBookmarkState();
        }
    }

    private async void OnMenuPrevBookmarkClicked(object sender, EventArgs e)
    {
        await GoToPreviousBookmarkAsync();
        UpdateMenuBookmarkState();
    }

    private async void OnMenuNextBookmarkClicked(object sender, EventArgs e)
    {
        await GoToNextBookmarkAsync();
        UpdateMenuBookmarkState();
    }

    private void OnPageBookmarkBadgeTapped(object sender, EventArgs e)
    {
        OpenBookmarkModal();
    }

    private void OpenBookmarkModal()
    {
        var currentBm = _bookmarks.FirstOrDefault(b => b.PageNumber == _currentPage);
        if (BookmarkModalPageLabel != null)
        {
            BookmarkModalPageLabel.Text = $"Page {_currentPage} / {_maxPages}";
        }
        if (BookmarkNameEntry != null)
        {
            BookmarkNameEntry.Text = currentBm?.Name ?? "";
        }
        if (BookmarkErrorLabel != null)
        {
            BookmarkErrorLabel.IsVisible = false;
        }
        UpdateBookmarkModalChips();

        bool hasOtherBookmarks = _bookmarks.Count > 1 || (_bookmarks.Count == 1 && currentBm == null);
        if (ModalPrevBookmarkBtn != null)
        {
            ModalPrevBookmarkBtn.IsEnabled = hasOtherBookmarks;
        }
        if (ModalNextBookmarkBtn != null)
        {
            ModalNextBookmarkBtn.IsEnabled = hasOtherBookmarks;
        }

        if (BookmarkModal != null)
        {
            BookmarkModal.IsVisible = true;
        }
        ImageContainer.InputTransparent = true;
        ZoomLayout.InputTransparent = true;
        BottomTouchBar.IsVisible = false;
    }

    private void OnCloseBookmarkModalClicked(object sender, EventArgs e)
    {
        if (BookmarkModal != null)
        {
            BookmarkModal.IsVisible = false;
        }
        ImageContainer.InputTransparent = false;
        ZoomLayout.InputTransparent = false;
        BottomTouchBar.IsVisible = !AnnotationBar.IsVisible;
    }

    private void UpdateBookmarkModalChips()
    {
        if (BookmarkChipsContainer == null) return;
        BookmarkChipsContainer.Children.Clear();
        var sorted = _bookmarks.OrderBy(b => b.PageNumber).ToList();
        if (!sorted.Any())
        {
            BookmarkChipsContainer.Children.Add(new Label
            {
                Text = LocalizationService.Instance.GetString("Viewer_Bookmark_None", "Aucun marqueur défini"),
                TextColor = Color.FromArgb("#888888"),
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            });
            return;
        }

        foreach (var bm in sorted)
        {
            bool isCurrent = bm.PageNumber == _currentPage;
            var border = new Border
            {
                BackgroundColor = isCurrent ? Color.FromArgb("#007ACC") : Color.FromArgb("#2D2D38"),
                Stroke = isCurrent ? Colors.White : Color.FromArgb("#444455"),
                StrokeThickness = 1,
                Padding = new Thickness(10, 4),
                Margin = new Thickness(0, 0, 4, 0),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }
            };
            var label = new Label
            {
                Text = $"🔖 {bm.Name} (p.{bm.PageNumber})",
                TextColor = isCurrent ? Colors.White : Color.FromArgb("#00E5FF"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            };
            border.Content = label;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, args) =>
            {
                OnCloseBookmarkModalClicked(this, EventArgs.Empty);
                await GoToPage(bm.PageNumber);
            };
            border.GestureRecognizers.Add(tap);
            BookmarkChipsContainer.Children.Add(border);
        }
    }

    private async void OnRenameBookmarkClicked(object sender, EventArgs e)
    {
        var currentBm = _bookmarks.FirstOrDefault(b => b.PageNumber == _currentPage);
        if (currentBm == null) return;

        string newName = BookmarkNameEntry?.Text?.Trim().ToUpperInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(newName))
        {
            if (BookmarkErrorLabel != null)
            {
                var loc = LocalizationService.Instance;
            BookmarkErrorLabel.Text = loc.GetString("Viewer_Bookmark_Name_Empty", "Le nom ne peut pas être vide.");
                BookmarkErrorLabel.IsVisible = true;
            }
            return;
        }
        if (newName.Length > 3)
        {
            if (BookmarkErrorLabel != null)
            {
                var loc = LocalizationService.Instance;
            BookmarkErrorLabel.Text = loc.GetString("Viewer_Bookmark_Name_Length", "3 caractères maximum (ex: A, B, C1...).");
                BookmarkErrorLabel.IsVisible = true;
            }
            return;
        }
        if (_bookmarks.Any(b => b.Id != currentBm.Id && b.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
        {
            if (BookmarkErrorLabel != null)
            {
                var loc = LocalizationService.Instance;
                BookmarkErrorLabel.Text = string.Format(loc.GetString("Viewer_Bookmark_Name_Duplicate", "Le marqueur '{0}' existe déjà sur cette partition."), newName);
                BookmarkErrorLabel.IsVisible = true;
            }
            return;
        }

        currentBm.Name = newName;
        await _databaseService.SaveBookmarkAsync(currentBm);
        _bookmarks = await _databaseService.GetBookmarksForScoreAsync(_score.Id);
        if (BookmarkErrorLabel != null)
        {
            BookmarkErrorLabel.IsVisible = false;
        }
        UpdateBookmarkIndicator();
        UpdateBookmarkModalChips();
    }

    private async void OnDeleteBookmarkClicked(object sender, EventArgs e)
    {
        var currentBm = _bookmarks.FirstOrDefault(b => b.PageNumber == _currentPage);
        if (currentBm != null)
        {
            await _databaseService.DeleteBookmarkAsync(currentBm);
            _bookmarks = await _databaseService.GetBookmarksForScoreAsync(_score.Id);
            UpdateBookmarkIndicator();
        }
        OnCloseBookmarkModalClicked(this, EventArgs.Empty);
    }

    private async void OnModalPrevBookmarkClicked(object sender, EventArgs e)
    {
        await GoToPreviousBookmarkAsync();
        OpenBookmarkModal();
    }

    private async void OnModalNextBookmarkClicked(object sender, EventArgs e)
    {
        await GoToNextBookmarkAsync();
        OpenBookmarkModal();
    }

    #endregion

    #endregion
}
