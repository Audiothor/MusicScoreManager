using MusicScoreManager.Models;
using MusicScoreManager.Services;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using System.Linq;
using System.Diagnostics;
using Plugin.Maui.Audio;

namespace MusicScoreManager;

public partial class ViewerPage : ContentPage
{
    private Score _score;
    private List<Score>? _setlistScores;
    private readonly Setlist? _setlist;
    private int _currentIndex = -1;
    private bool _isContinuous = true;
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;

    private double currentScale = 1;
    private int _currentPage = 1;
    private string _currentPageDisplay = "1";
    private int _maxPages = 1;
    private int _currentRotation = 0;
    private Dictionary<int, int> _pageRotations = new();
    private DateTime _lastLoadingOverlayShowTime = DateTime.UtcNow;
    private bool _isInitialOpenCompleted = false;

    private Thread? _metronomeThread;
    private volatile bool _isMetronomeRunning = false;
    private bool _isMetronomePlaying = false;
#if !ANDROID
    private IAudioPlayer? _metronomeAudioPlayer;
    private IAudioPlayer? _preCountAudioPlayer;
#endif

#if ANDROID
    private Android.Media.SoundPool? _soundPool;
    private int _metronomeSoundId = 0;
    private int _preCountSoundId = 0;
#endif
    private bool _isScoreReady = false;
    private bool _isAudioPlaying = false;
    private bool _isDraggingAudioSlider = false;
    private ScoreAudioFile? _selectedAudioFile;
    private string _pdfFilePath = "current.pdf";
    private WebView? PdfWebView;

    private WebView EnsurePdfWebViewCreated()
    {
        if (PdfWebView == null)
        {
            PdfWebView = new WebView
            {
                IsVisible = false,
                BackgroundColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            PdfWebView.Navigated += OnPdfWebViewNavigated;
            PdfWebView.Navigating += OnPdfWebViewNavigating;
            ZoomLayout.Children.Insert(0, PdfWebView);
        }
        return PdfWebView;
    }

    private readonly AnnotationService _annotationService;
    private readonly PdfService _pdfService = new();
    private readonly System.Diagnostics.Stopwatch _renderStopwatch = new();
    private int _currentRenderedWidth = 0;
    private int _currentRenderedHeight = 0;
    private string? _pendingSticker = null;
    private bool _isAnnotationMode = false;
    private List<Annotation> _annotations = new();
    private bool _isAnnotationsLocked = true;
    private AbsoluteLayout ActiveAnnotationsContainer => AnnotationsContainer;
    private List<ScoreBookmark> _bookmarks = new();
    private bool _isUpdatingBookmarkMenu = false;

    private bool _isTwoPagesMode = false;
    private int _leftPageNumber = 1;
    private int _rightPageNumber = 2;
    private bool _hasRightPage = false;
    private bool _isEraserMode = false;
#if !ANDROID
    private bool _isPointerEraserPressed = false;
#endif
    private readonly HashSet<int> _erasingAnnotationIds = new();
    private int _lastSinglePageNumber = 1;
    private double _canvasXRel = 0.0;
    private double _canvasYRel = 0.0;
    private double _canvasWRel = 1.0;
    private double _canvasHRel = 1.0;
    private double _lastAllocatedWidth = -1;
    private double _lastAllocatedHeight = -1;
    private DateTime _lastSwipeTurnUtc = DateTime.MinValue;

    private void HideViewerLoadingOverlay()
    {
        if (ViewerLoadingOverlay != null && ViewerLoadingOverlay.IsVisible)
        {
            ViewerLoadingOverlay.Opacity = 0;
            ViewerLoadingOverlay.IsVisible = false;
            if (ViewerLoadingText != null)
            {
                ViewerLoadingText.Text = LocalizationService.Instance.GetString("Score_Loading_Opening", "Ouverture de la partition...");
            }
        }

        _isInitialOpenCompleted = true;

        try
        {
            if (Preferences.Default.Get("ImmersiveMode", true))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    ApplyImmersiveMode(true);
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur ImmersiveMode dans HideViewerLoadingOverlay: {ex.Message}");
        }
    }

    public ViewerPage(Score score, List<Score>? setlistScores = null, int currentIndex = -1, bool isContinuous = true, Setlist? setlist = null, double initialLoadingTopY = -1, bool isSetlistOpening = false)
    {
        InitializeComponent();
        _score = score;
        _setlistScores = setlistScores;
        _currentIndex = currentIndex;
        _isContinuous = isContinuous;
        _setlist = setlist;
        _databaseService = new DatabaseService();
        _settingsService = new SettingsService();
        _annotationService = new AnnotationService();

        if (ViewerLoadingBorder != null)
        {
            if (initialLoadingTopY >= 0)
            {
                ViewerLoadingBorder.VerticalOptions = LayoutOptions.Start;
                ViewerLoadingBorder.Margin = new Thickness(0, initialLoadingTopY, 0, 0);
            }
            else
            {
                ViewerLoadingBorder.VerticalOptions = LayoutOptions.Center;
                ViewerLoadingBorder.Margin = new Thickness(0);
            }
        }

        if (ViewerLoadingText != null)
        {
            ViewerLoadingText.Text = isSetlistOpening
                ? LocalizationService.Instance.GetString("Setlist_Loading_Opening", "Ouverture de la setlist...")
                : LocalizationService.Instance.GetString("Score_Loading_Opening", "Ouverture de la partition...");
        }

        Title = _score.Title;

        // Correction robuste et immédiate du type de partition basé sur l'extension du fichier réel
        if (!string.IsNullOrEmpty(_score.FilePath))
        {
            try
            {
                string ext = Path.GetExtension(_score.FilePath)?.ToLowerInvariant() ?? "";
                var originalType = _score.Type;
                if (ext == ".pdf")
                {
                    _score.Type = ScoreType.PDF;
                }
                else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".webp" || ext == ".bmp")
                {
                    _score.Type = ScoreType.Image;
                }

                if (_score.Type != originalType)
                {
                    // Sauvegarder la correction en base de données de manière asynchrone
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _databaseService.SaveScoreAsync(_score);
                            System.Diagnostics.Debug.WriteLine($"[Viewer] Type corrigé en {_score.Type} pour {_score.Title} dans la base de données.");
                        }
                        catch { }
                    });
                }
            }
            catch { }
        }

        if (_score.IsRotationSaved)
        {
            _currentRotation = _score.Rotation;
        }

        if (_score.PageCount > 0)
        {
            _maxPages = _score.PageCount;
        }
        else if (!string.IsNullOrEmpty(_score.FilePath))
        {
            try
            {
                string fullPath = _settingsService.GetAbsolutePath(_score.FilePath);
                int cached = _pdfService.GetCachedPageCount(fullPath);
                if (cached > 0)
                {
                    _maxPages = cached;
                    _score.PageCount = cached;
                }
            }
            catch { }
        }

        // 🚀 Démarrage immédiat du préchargement de la partition dès la création du Viewer
        if (_score.Type == ScoreType.PDF && PdfService.IsNativePdfSupported && !string.IsNullOrEmpty(_score.FilePath))
        {
            try
            {
                string fullPath = _settingsService.GetAbsolutePath(_score.FilePath);
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    _ = _pdfService.PreloadFirstPageAsync(fullPath, _score.Rotation, _score.PageCount);
                }
            }
            catch { }
        }

        // Verrouillage systématique par défaut pour ne pas bloquer les interactions et le changement de page
        LockAnnotations();

        // Activer la zone tactile supérieure si nous sommes dans une setlist et que l'option est activée
        if (TopSetlistTouchBar != null)
        {
            bool isSetlistProgressEnabled = Preferences.Default.Get("ShowSetlistProgressOverlay", true);
            TopSetlistTouchBar.IsVisible = isSetlistProgressEnabled && _setlistScores != null && _setlistScores.Count > 0;
        }

        // Repositionner dynamiquement les annotations lors des changements de taille du conteneur (rotation, layout, etc.)
        AnnotationsContainer.SizeChanged += (s, e) =>
        {
            if (AnnotationsContainer.Width > 0 && AnnotationsContainer.Height > 0 && _currentRenderedWidth > 0 && _currentRenderedHeight > 0)
            {
                UpdateCanvasBounds(AnnotationsContainer.Width, AnnotationsContainer.Height, _currentRenderedWidth, _currentRenderedHeight);
            }
            RenderAnnotations();
        };
        AnnotationsContainer.HandlerChanged += (s, e) => {
#if ANDROID
            SetupNativeTouchHandling();
#endif
        };
        ImageContainer.HandlerChanged += (s, e) => {
#if ANDROID
            SetupNativeTouchHandling();
#endif
        };

        UpdateAnnotationsLanguage();
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    public void UpdateSetlistScores(List<Score> scores, int currentIndex)
    {
        _setlistScores = scores;
        _currentIndex = currentIndex;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (TopSetlistTouchBar != null)
            {
                bool isSetlistProgressEnabled = Preferences.Default.Get("ShowSetlistProgressOverlay", true);
                TopSetlistTouchBar.IsVisible = isSetlistProgressEnabled && _setlistScores != null && _setlistScores.Count > 0;
            }
            UpdateTopScoreTitleUI();
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[Viewer] OnAppearing appelé.");

        if (TopSetlistTouchBar != null)
        {
            bool isSetlistProgressEnabled = Preferences.Default.Get("ShowSetlistProgressOverlay", true);
            TopSetlistTouchBar.IsVisible = isSetlistProgressEnabled && _setlistScores != null && _setlistScores.Count > 0;
        }

        UpdateTopScoreTitleUI();
        UpdateAnnotationsLanguage();
        LockAnnotations();
        PedalMidiService.Instance.ActionTriggered += OnPedalActionTriggered;

        // Application du profil de pédale spécifique de la setlist si configuré
        if (_setlist != null && !string.IsNullOrEmpty(_setlist.PedalProfileId))
        {
            var customProfile = PedalMidiService.Instance.Profiles.FirstOrDefault(p => p.Id == _setlist.PedalProfileId);
            if (customProfile != null)
            {
                PedalMidiService.Instance.SetOverrideProfile(customProfile);
                System.Diagnostics.Debug.WriteLine($"[Viewer] Profil de pédale spécifique appliqué pour la setlist '{_setlist.Name}' : {customProfile.Name} ({customProfile.Id})");
            }
        }

        try
        {
            bool keepScreenOn = Preferences.Default.Get("KeepScreenOn", true);
            DeviceDisplay.Current.KeepScreenOn = keepScreenOn;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur KeepScreenOn: {ex.Message}");
        }

        try
        {
            bool immersiveMode = Preferences.Default.Get("ImmersiveMode", true);
            // Si l'overlay de chargement n'est pas actif (ex: réouverture depuis l'arrière-plan ou reprise), activer l'immersion après délai.
            // S'il est actif, l'immersion sera appliquée dès que l'overlay se masquera dans HideViewerLoadingOverlay()
            // pour garantir une stabilité visuelle absolue au pixel près du message de chargement.
            if (immersiveMode && (ViewerLoadingOverlay == null || !ViewerLoadingOverlay.IsVisible))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    ApplyImmersiveMode(true);
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur ImmersiveMode: {ex.Message}");
        }

#if ANDROID
        SetupNativeTouchHandling();
#endif
        
        // 1. Démarrer immédiatement le chargement de la partition et des rotations sans délai
        _ = LoadPageAndContentImmediatelyAsync();

        // 2. Initialiser les annotations, le métronome et l'audio en tâche de fond après un délai
        //    afin de laisser 100% du CPU/IO au rendu de la première page PDF
        _ = Task.Run(async () => {
            await Task.Delay(300);
            await LoadAnnotationsAsync();
            InitializeMetronome();
            await LoadBookmarksAsync();

            try
            {
                if (_score.AudioFiles == null || _score.AudioFiles.Count == 0)
                {
                    var audioFiles = await _databaseService.GetAudioFilesForScoreAsync(_score.Id);
                    if (audioFiles != null && audioFiles.Count > 0)
                    {
                        _score.AudioFiles = audioFiles;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur rafraîchissement audio: {ex.Message}");
            }

            InitializeAudio(forceReload: true);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Title = _score.Title;
                if (MenuTitleLabel != null) MenuTitleLabel.Text = _score.Title;
                if (MetronomeBpmLabel != null) MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
                UpdateToolsDrawerUI();
            });
        });
    }

    private async Task LoadPageAndContentImmediatelyAsync()
    {
        // 1. Lancement asynchrone des rotations de pages en arrière-plan sans bloquer
        _ = LoadPageRotationsAsync();

        // 2. Rendu immédiat et direct de la partition
        LoadContentAsync();
        await Task.CompletedTask;
    }

    private async Task LoadPageRotationsAsync()
    {
        try
        {
            var savedRotations = await _databaseService.GetPageRotationsForScoreAsync(_score.Id);
            _pageRotations = savedRotations.ToDictionary(r => r.PageNumber, r => r.Rotation);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur chargement rotations de page: {ex.Message}");
        }
    }

    private async Task LoadAnnotationsAsync()
    {
        try
        {
            _annotations = await _databaseService.GetAnnotationsForScoreAsync(_score.Id);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isScoreReady)
                {
                    RenderAnnotations();
                    if (ActiveAnnotationsContainer != null)
                    {
                        ActiveAnnotationsContainer.IsVisible = true;
                        ActiveAnnotationsContainer.Opacity = (_score.ShowAnnotations && _isScoreReady) ? 1 : 0;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur chargement annotations: {ex.Message}");
        }
    }

    private bool _isAnnotationUIInitialized = false;
    private async Task InitializeAnnotationUI(bool force = false)
    {
        if (_isAnnotationUIInitialized && !force) return;
        _isAnnotationUIInitialized = true;

        try
        {
            var favorites = await _databaseService.GetFavoriteStickersAsync();
            var favTexts = favorites.Select(f => f.Text).ToList();
            var categories = _annotationService.GetStickerCategories(favTexts, _settingsService.ActiveStickerCategories);
            
            MainThread.BeginInvokeOnMainThread(() => {
                if (StickerCategoriesCollection != null)
                {
                    // Appliquer réglages initiaux
                    foreach(var cat in categories)
                    {
                        foreach(var s in cat.Stickers)
                        {
                            s.Color = _currentStickerColor;
                            s.BackgroundColor = _currentStickerBgColor;
                            s.Scale = StickerSizeSlider.Value;
                        }
                    }
                    StickerCategoriesCollection.ItemsSource = categories;

                    var defaultCat = categories.FirstOrDefault(c => c.Stickers.Count > 0) ?? categories.FirstOrDefault();
                    if (defaultCat != null)
                    {
                        StickerCategoriesCollection.SelectedItem = defaultCat;
                        if (StickersCollection != null)
                        {
                            StickersCollection.ItemsSource = defaultCat.Stickers;
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _isAnnotationUIInitialized = false;
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur initialisation UI: {ex.Message}");
        }
    }

    private void SetupMenuUI()
    {
        UpdateToolsDrawerUI();
        SaveRotationSwitch.IsToggled = _score.IsRotationSaved;
        UpdateRotateButtonText();
    }

    private void UpdateRotateButtonText()
    {
        bool applyToAll = RotateAllPagesSwitch != null && RotateAllPagesSwitch.IsToggled;
        if (applyToAll)
        {
            _currentRotation = _score.Rotation;
        }
        else if (_pageRotations.TryGetValue(_currentPage, out int pageRot))
        {
            _currentRotation = pageRot;
        }
        else
        {
            _currentRotation = _score.Rotation;
        }
        string rotateWord = LocalizationService.Instance.GetString("Viewer_Rotate_Word");
        RotateBtn.Text = $"↻ {rotateWord} {_currentRotation}°";
    }

    private void OnPdfWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url != null && e.Url.StartsWith("app://musicscore/menu"))
        {
            e.Cancel = true;
            ParsePageParams(e.Url);
            ShowMenu();
        }
        else if (e.Url != null && e.Url.StartsWith("app://musicscore/pageinfo"))
        {
            e.Cancel = true;
            ParsePageParams(e.Url);
            if (_isScoreReady)
            {
                MainThread.BeginInvokeOnMainThread(RenderAnnotations);
            }
        }
        else if (e.Url != null && e.Url.StartsWith("app://musicscore/rendering"))
        {
            e.Cancel = true;
            _isScoreReady = false;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ActiveAnnotationsContainer != null)
                {
                    ActiveAnnotationsContainer.Opacity = 0;
                }
            });
        }
        else if (e.Url != null && e.Url.StartsWith("app://musicscore/end"))
        {
            e.Cancel = true;
            HandleEndOfScore();
        }
        else if (e.Url != null && e.Url.StartsWith("app://musicscore/start"))
        {
            e.Cancel = true;
            HandleStartOfScore();
        }
        else if (e.Url != null && e.Url.StartsWith("app://musicscore/ready"))
        {
            e.Cancel = true;
            _isScoreReady = true;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RenderAnnotations();
                if (ActiveAnnotationsContainer != null)
                {
                    ActiveAnnotationsContainer.IsVisible = _score.ShowAnnotations;
                    ActiveAnnotationsContainer.Opacity = (_score.ShowAnnotations && _isScoreReady) ? 1 : 0;
                }
            });
            // Démarrer si on veut le visuel OU le son
            if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();
        }
        else if (e.Url != null && e.Url.StartsWith("app://musicscore/annotations"))
        {
            e.Cancel = true;
            MainThread.BeginInvokeOnMainThread(() => OnAnnotationToggleTapped(this, EventArgs.Empty));
        }
    }

    private async void LoadContentAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[Viewer] --- Début chargement partition: {_score.Title} ---");
        if (ViewerLoadingOverlay != null)
        {
            _lastLoadingOverlayShowTime = DateTime.UtcNow;
            ViewerLoadingOverlay.Opacity = 1;
            ViewerLoadingOverlay.IsVisible = true;
        }

        try
        {
            if (string.IsNullOrEmpty(_score.FilePath))
            {
                HideViewerLoadingOverlay();
                System.Diagnostics.Debug.WriteLine("[Viewer] ERREUR : _score.FilePath est NULL ou vide");
                var loc = LocalizationService.Instance;
                await DisplayAlertAsync(loc.GetString("Common_Error", "Erreur"), loc.GetString("Viewer_Error_MissingPath", "Chemin du fichier manquant"), loc.GetString("Common_OK", "OK"));
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Viewer] Path stocké: {_score.FilePath}");
            string fullPath = _settingsService.GetAbsolutePath(_score.FilePath);
            System.Diagnostics.Debug.WriteLine($"[Viewer] FullPath calculé: {fullPath}");

            bool exists = File.Exists(fullPath);
            System.Diagnostics.Debug.WriteLine($"[Viewer] Fichier existe physiquement ? {exists}");

            if (!exists)
            {
                HideViewerLoadingOverlay();
                System.Diagnostics.Debug.WriteLine($"[Viewer] ERREUR : Le fichier n'existe pas au chemin : {fullPath}");
                var loc = LocalizationService.Instance;
                await DisplayAlertAsync(loc.GetString("Viewer_Error_FileNotFound_Title", "Fichier introuvable"), string.Format(loc.GetString("Viewer_Error_FileNotFound_Msg", "Le fichier est introuvable :\n{0}"), fullPath), loc.GetString("Common_OK", "OK"));
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Viewer] Type détecté");

            if (_score.Type == ScoreType.Image)
            {
                System.Diagnostics.Debug.WriteLine("[Viewer] Mode IMAGE");
                try
                {
                    ScoreImage.Source = ImageSource.FromFile(fullPath);
                    ScoreImage.Rotation = _currentRotation;
                    ScoreImage.IsVisible = true;
                    if (PdfWebView != null) PdfWebView.IsVisible = false;
                    ImageContainer.IsVisible = true;
                    AnnotationsContainer.IsVisible = true;
                    _currentPage = 1;
                    _maxPages = 1;
                    _lastSinglePageNumber = 1;
                    UpdatePageIndicator();

                    try
                    {
#if ANDROID
                        var opts = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
                        Android.Graphics.BitmapFactory.DecodeFile(fullPath, opts);
                        if (opts.OutWidth > 0 && opts.OutHeight > 0)
                        {
                            _currentRenderedWidth = (_currentRotation % 180 != 0) ? opts.OutHeight : opts.OutWidth;
                            _currentRenderedHeight = (_currentRotation % 180 != 0) ? opts.OutWidth : opts.OutHeight;
                        }
#else
                        try
                        {
                            var imgInfo = SixLabors.ImageSharp.Image.Identify(fullPath);
                            if (imgInfo != null)
                            {
                                _currentRenderedWidth = (_currentRotation % 180 != 0) ? imgInfo.Height : imgInfo.Width;
                                _currentRenderedHeight = (_currentRotation % 180 != 0) ? imgInfo.Width : imgInfo.Height;
                            }
                        }
                        catch { }
#endif
                        if (_currentRenderedWidth > 0 && _currentRenderedHeight > 0)
                        {
                            var (vpW, vpH, _) = GetEffectiveViewportInfo();
                            double finalW = (ActiveAnnotationsContainer?.Width > 0) ? ActiveAnnotationsContainer.Width : vpW;
                            double finalH = (ActiveAnnotationsContainer?.Height > 0) ? ActiveAnnotationsContainer.Height : vpH;
                            UpdateCanvasBounds(finalW, finalH, _currentRenderedWidth, _currentRenderedHeight);
                        }
                    }
                    catch { }

                    _isScoreReady = true;
#if ANDROID
                    SetupNativeTouchHandling();
                    if (ScoreImage.Handler?.PlatformView is Android.Widget.ImageView iv)
                    {
                        var tcs = new TaskCompletionSource<bool>();
                        iv.Post(() => tcs.TrySetResult(true));
                        await Task.WhenAny(tcs.Task, Task.Delay(80));
                    }
                    else
                    {
                        await Task.Delay(40);
                    }
#else
                    await Task.Delay(40);
#endif
                    HideViewerLoadingOverlay();
                    RenderAnnotations();
                    if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();
                }
                catch (Exception imgEx)
                {
                    HideViewerLoadingOverlay();
                    System.Diagnostics.Debug.WriteLine($"[Viewer] ECHEC chargement Image: {imgEx.Message}");
                    var loc = LocalizationService.Instance;
                await DisplayAlertAsync(loc.GetString("Viewer_Error_Read_Image", "Erreur Lecture"), string.Format(loc.GetString("Viewer_Error_Load_Image_Msg", "{0}\n\nL'application n'a pas pu charger l'image."), imgEx.Message), loc.GetString("Common_OK", "OK"));
                }
            }
            else if (_score.Type == ScoreType.PDF)
            {
                if (PdfService.IsNativePdfSupported)
                {
                    System.Diagnostics.Debug.WriteLine("[Viewer] Mode PDF - Démarrage rendu NATIF ultra-rapide");
                    _renderStopwatch.Restart();
                    _isScoreReady = false;
                    if (ActiveAnnotationsContainer != null)
                    {
                        ActiveAnnotationsContainer.Opacity = 0;
                        ActiveAnnotationsContainer.Children.Clear();
                    }
                    if (PdfWebView != null) PdfWebView.IsVisible = false;
                    ScoreImage.IsVisible = true;
                    ImageContainer.IsVisible = true;
                    AnnotationsContainer.IsVisible = true;
#if ANDROID
                    SetupNativeTouchHandling();
#endif

                    await RenderPdfCurrentAsync(_currentPage > 0 ? _currentPage : 1);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Viewer] Mode PDF - Repli WebView");
                    _isScoreReady = false;
                    if (ActiveAnnotationsContainer != null)
                    {
                        ActiveAnnotationsContainer.Opacity = 0;
                        ActiveAnnotationsContainer.Children.Clear();
                    }
                    ScoreImage.IsVisible = false;
                    var webView = EnsurePdfWebViewCreated();
                    webView.IsVisible = true;
                    ImageContainer.IsVisible = true;
                    AnnotationsContainer.IsVisible = _score.ShowAnnotations;

                    if (DeviceInfo.Platform == DevicePlatform.Android)
                    {
                        await EnsurePdfJsReadyAsync();
                        
                        string pdfjsDir = Path.Combine(FileSystem.CacheDirectory, "pdfjs");
                        string viewerPath = Path.Combine(pdfjsDir, "viewer.html");
                        
                        string finalViewerPath = viewerPath.StartsWith("/") ? $"file://{viewerPath}" : $"file:///{viewerPath}";
                        string finalFilePath = fullPath.StartsWith("/") ? $"file://{fullPath}" : $"file:///{fullPath}";
                        
                        string pageRotsJson = System.Text.Json.JsonSerializer.Serialize(_pageRotations);
                        string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
                        string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");
                        bool twoPages = Preferences.Default.Get("TwoPagesLandscape", true);
                        string pdfJsUrl = $"{finalViewerPath}?file={Uri.EscapeDataString(finalFilePath)}&rot={_score.Rotation}&page=1&twoPages={twoPages.ToString().ToLowerInvariant()}&pageRots={Uri.EscapeDataString(pageRotsJson)}&nextGest={nextGesture}&prevGest={prevGesture}";

                        System.Diagnostics.Debug.WriteLine($"[Viewer] WebView Source finale: {pdfJsUrl}");
                        _pdfFilePath = finalFilePath; 
                        webView.Source = new UrlWebViewSource { Url = pdfJsUrl };
                    }
                    else
                    {
                        webView.Source = fullPath;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("[Viewer] --- Fin de LoadContentAsync ---");
        }
        catch (Exception ex)
        {
            HideViewerLoadingOverlay();
            System.Diagnostics.Debug.WriteLine($"[Viewer] CRASH dans LoadContentAsync: {ex.Message}");
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(loc.GetString("Viewer_Error_Load_Title", "Erreur de chargement"), $"{ex.Message}\n\nFichier: {_score.FilePath}", loc.GetString("Common_OK", "OK"));
        }
    }


    private void OnToggleToolsDrawerTapped(object sender, EventArgs e)
    {
        bool willBeVisible = !(ToolsDrawerOverlay?.IsVisible ?? false);
        SetToolsDrawerVisibility(willBeVisible);
    }

    private void OnCloseToolsDrawerClicked(object sender, EventArgs e)
    {
        SetToolsDrawerVisibility(false);
    }

    private void SetToolsDrawerVisibility(bool isVisible)
    {
        if (ToolsDrawerOverlay == null) return;

        UpdateToolsDrawerUI();
        ToolsDrawerOverlay.IsVisible = isVisible;
        if (ToolsDrawerBackdrop != null)
        {
            ToolsDrawerBackdrop.IsVisible = isVisible;
        }

        if (isVisible)
        {
            if (CentralMenuOverlay != null && CentralMenuOverlay.IsVisible)
            {
                OnCloseMenuClicked(this, EventArgs.Empty);
            }
        }
    }

    private void UpdateToolsDrawerUI()
    {
        if (ToolsIconButton != null)
        {
            ToolsIconButton.IsVisible = (_score?.ShowToolsButton ?? true) || Preferences.Default.Get("ShowToolsButton", true);
        }

        if (DrawerMetronomeTile == null) return;

        bool isMetronomeOn = _score?.ShowMetronome == true || (MetronomeOverlay != null && MetronomeOverlay.IsVisible);
        DrawerMetronomeStatusLabel.Text = isMetronomeOn ? "ON" : "OFF";
        DrawerMetronomeBadge.BackgroundColor = isMetronomeOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444444");
        DrawerMetronomeTile.Stroke = isMetronomeOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444455");

        bool hasAudio = _selectedAudioFile != null || (_score?.AudioFiles != null && _score.AudioFiles.Count > 0);
        bool isAudioOn = _score?.ShowAudioPlayer == true && (AudioPlayerOverlay != null && AudioPlayerOverlay.IsVisible);
        
        if (!hasAudio)
        {
            DrawerAudioStatusLabel.Text = "-";
            DrawerAudioBadge.BackgroundColor = Color.FromArgb("#333333");
            DrawerAudioTile.Opacity = 0.5;
            DrawerAudioTile.Stroke = Color.FromArgb("#333333");
        }
        else
        {
            DrawerAudioTile.Opacity = 1.0;
            DrawerAudioStatusLabel.Text = isAudioOn ? "ON" : "OFF";
            DrawerAudioBadge.BackgroundColor = isAudioOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444444");
            DrawerAudioTile.Stroke = isAudioOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444455");
        }

        bool isAnnotationsOn = _score?.ShowAnnotations == true || (AnnotationBar != null && AnnotationBar.IsVisible);
        DrawerAnnotationsStatusLabel.Text = isAnnotationsOn ? "ON" : "OFF";
        DrawerAnnotationsBadge.BackgroundColor = isAnnotationsOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444444");
        DrawerAnnotationsTile.Stroke = isAnnotationsOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444455");

        bool isPageNumberOn = _score?.ShowPageNumber == true && Preferences.Default.Get("ShowPageNumber", true);
        if (DrawerPageNumberStatusLabel != null && DrawerPageNumberBadge != null && DrawerPageNumberTile != null)
        {
            DrawerPageNumberStatusLabel.Text = isPageNumberOn ? "ON" : "OFF";
            DrawerPageNumberBadge.BackgroundColor = isPageNumberOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444444");
        }

        bool isTunerOn = TunerOverlay != null && TunerOverlay.IsVisible;
        if (DrawerTunerStatusLabel != null && DrawerTunerBadge != null && DrawerTunerTile != null)
        {
            DrawerTunerStatusLabel.Text = isTunerOn ? "ON" : "OFF";
            DrawerTunerBadge.BackgroundColor = isTunerOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444444");
            DrawerTunerTile.Stroke = isTunerOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444455");
        }

        bool isRecorderOn = RecorderOverlay != null && RecorderOverlay.IsVisible;
        if (DrawerRecorderStatusLabel != null && DrawerRecorderBadge != null && DrawerRecorderTile != null)
        {
            DrawerRecorderStatusLabel.Text = isRecorderOn ? "ON" : "OFF";
            DrawerRecorderBadge.BackgroundColor = isRecorderOn ? Color.FromArgb("#E74C3C") : Color.FromArgb("#444444");
            DrawerRecorderTile.Stroke = isRecorderOn ? Color.FromArgb("#E74C3C") : Color.FromArgb("#444455");
        }

        bool isTitleOn = TopScoreTitleBar != null && TopScoreTitleBar.IsVisible;
        if (DrawerScoreTitleStatusLabel != null && DrawerScoreTitleBadge != null && DrawerScoreTitleTile != null)
        {
            DrawerScoreTitleStatusLabel.Text = isTitleOn ? "ON" : "OFF";
            DrawerScoreTitleBadge.BackgroundColor = isTitleOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444444");
            DrawerScoreTitleTile.Stroke = isTitleOn ? Color.FromArgb("#007ACC") : Color.FromArgb("#444455");
        }
    }

    private async void OnDrawerTunerTapped(object sender, EventArgs e)
    {
        if (TunerOverlay == null) return;
        bool newState = !TunerOverlay.IsVisible;
        if (newState)
        {
            // Sécurité micro : bloquer l'accordeur si une prise est en train d'enregistrer
            if (AudioRecorderService.Instance.IsRecording)
            {
                var loc = LocalizationService.Instance;
                await DisplayAlertAsync(
                    loc.GetString("Recorder_Overlay_Title", "🎙️ Enregistreur"),
                    loc.GetString("Recorder_Stop_Before_Tuner", "Arrêtez d'abord l'enregistrement en cours avant d'ouvrir l'accordeur."),
                    loc.GetString("Common_OK", "OK"));
                return;
            }
            // Sinon bascule fluide : fermer l'enregistreur ouvert à l'arrêt
            if (RecorderOverlay != null && RecorderOverlay.IsVisible)
            {
                AudioRecorderService.Instance.StopPreview();
                RecorderOverlay.IsVisible = false;
            }

            TunerOverlay.IsVisible = true;
            InitTunerUIIfNeeded();
        }
        else
        {
            TunerOverlay.IsVisible = false;
            StopTunerListening();
        }
        UpdateToolsDrawerUI();
    }

    private async void OnDrawerMetronomeTapped(object sender, EventArgs e)
    {
        if (_score == null) return;
        bool newState = !_score.ShowMetronome;
        _score.ShowMetronome = newState;
        MetronomeOverlay.IsVisible = newState;

        if (newState)
        {
            StartMetronome();
        }
        else if (!_score.HasMetronomeSound)
        {
            StopMetronome();
        }

        await _databaseService.SaveScoreAsync(_score);
        UpdateToolsDrawerUI();
    }

    private async void OnDrawerAudioTapped(object sender, EventArgs e)
    {
        if (_score == null) return;
        bool hasAudio = _selectedAudioFile != null || (_score.AudioFiles != null && _score.AudioFiles.Count > 0);
        if (!hasAudio) return;

        bool newState = !_score.ShowAudioPlayer;
        _score.ShowAudioPlayer = newState;

        if (newState)
        {
            if (_selectedAudioFile == null || AudioPlayer.Source == null)
            {
                InitializeAudio(forceReload: true);
            }
            AudioPlayerOverlay.IsVisible = (_selectedAudioFile != null);
        }
        else
        {
            AudioPlayerOverlay.IsVisible = false;
        }

        await _databaseService.SaveScoreAsync(_score);
        UpdateToolsDrawerUI();
    }

    private async void OnDrawerAnnotationsTapped(object sender, EventArgs e)
    {
        if (_score == null) return;
        bool newState = !_score.ShowAnnotations;
        _score.ShowAnnotations = newState;
        ApplyAnnotationsVisibility(newState);

        await _databaseService.SaveScoreAsync(_score);
        UpdateToolsDrawerUI();
    }

    private async void OnDrawerPageNumberTapped(object sender, EventArgs e)
    {
        if (_score == null) return;
        bool newState = !_score.ShowPageNumber;
        _score.ShowPageNumber = newState;
        if (newState && !Preferences.Default.Get("ShowPageNumber", true))
        {
            Preferences.Default.Set("ShowPageNumber", true);
        }
        UpdatePageIndicator();

        await _databaseService.SaveScoreAsync(_score);
        UpdateToolsDrawerUI();
    }

    private async void OnDrawerScoreTitleTapped(object sender, EventArgs e)
    {
        if (_score == null) return;
        bool isCurrentlyVisible = TopScoreTitleBar != null && TopScoreTitleBar.IsVisible;
        bool newState = !isCurrentlyVisible;
        _score.ShowScoreTitle = newState;
        if (!newState && Preferences.Default.Get("ShowScoreTitle", false))
        {
            Preferences.Default.Set("ShowScoreTitle", false);
        }
        UpdateTopScoreTitleUI();

        await _databaseService.SaveScoreAsync(_score);
        UpdateToolsDrawerUI();
    }

    private void UpdateTopScoreTitleUI()
    {
        if (TopScoreTitleBar == null || TopScoreTitleLabel == null) return;

        bool shouldShow = (_score?.ShowScoreTitle == true) || Preferences.Default.Get("ShowScoreTitle", false);
        TopScoreTitleBar.IsVisible = shouldShow && !string.IsNullOrWhiteSpace(_score?.Title);
        if (shouldShow && _score != null)
        {
            TopScoreTitleLabel.Text = _score.Title;
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateAnnotationsLanguage();
        });
    }

    private void UpdateAnnotationsLanguage()
    {
        var loc = LocalizationService.Instance;

        // Overlays d'options d'annotations
        if (HighlightOptionsSizeLabel != null)
            HighlightOptionsSizeLabel.Text = loc.GetString("Viewer_Tool_Size", "Taille :");
        if (HighlightOptionsOpacityLabel != null)
            HighlightOptionsOpacityLabel.Text = loc.GetString("Viewer_Tool_Opacity", "Opacité :");
        if (HighlightOpacityLightBtn != null)
            HighlightOpacityLightBtn.Text = loc.GetString("Viewer_Opacity_Light", "Clair");
        if (HighlightOpacityMediumBtn != null)
            HighlightOpacityMediumBtn.Text = loc.GetString("Viewer_Opacity_Medium", "Moyen");
        if (HighlightOpacityDarkBtn != null)
            HighlightOpacityDarkBtn.Text = loc.GetString("Viewer_Opacity_Dark", "Foncé");

        if (DrawOptionsSizeLabel != null)
            DrawOptionsSizeLabel.Text = loc.GetString("Viewer_Tool_Size", "Taille :");
        if (DrawOptionsOpacityLabel != null)
            DrawOptionsOpacityLabel.Text = loc.GetString("Viewer_Tool_Opacity", "Opacité :");
        if (DrawOpacityLightBtn != null)
            DrawOpacityLightBtn.Text = loc.GetString("Viewer_Opacity_Light", "Clair");
        if (DrawOpacityMediumBtn != null)
            DrawOpacityMediumBtn.Text = loc.GetString("Viewer_Opacity_Medium", "Moyen");
        if (DrawOpacityDarkBtn != null)
            DrawOpacityDarkBtn.Text = loc.GetString("Viewer_Opacity_Opaque", "Opaque");

        if (StickerSizeLabel != null)
            StickerSizeLabel.Text = loc.GetString("Viewer_Tool_Size", "Taille :");
        if (StickerSettingsTitleLabel != null)
            StickerSettingsTitleLabel.Text = loc.GetString("Viewer_Stickers_Settings", "Réglages des stickers");
        if (StickerTextColorLabel != null)
            StickerTextColorLabel.Text = loc.GetString("Viewer_Sticker_Text_Color", "Texte :");
        if (StickerBgColorLabel != null)
            StickerBgColorLabel.Text = loc.GetString("Viewer_Sticker_Bg_Color", "Fond :");
        if (TextColorLabel != null)
            TextColorLabel.Text = loc.GetString("Viewer_Text_Color_Label", "Couleur");
        if (TextSizeLabel != null)
            TextSizeLabel.Text = loc.GetString("Viewer_Text_Size_Label", "Taille");
        if (TextBgLabel != null)
            TextBgLabel.Text = loc.GetString("Viewer_Text_Bg_Label", "Fond");
        if (TextBorderLabel != null)
            TextBorderLabel.Text = loc.GetString("Viewer_Text_Border_Label", "Bordure");

        // Tooltips de la barre d'annotations
        if (HighlightBtn != null)
            ToolTipProperties.SetText(HighlightBtn, loc.GetString("Annotation_Highlighter", "Surligneur"));
        if (TextAnnotationBtn != null)
            ToolTipProperties.SetText(TextAnnotationBtn, loc.GetString("Annotation_Text", "Texte"));
        if (DrawBtn != null)
            ToolTipProperties.SetText(DrawBtn, loc.GetString("Annotation_Pencil", "Crayon"));
        if (StickersBtn != null)
            ToolTipProperties.SetText(StickersBtn, loc.GetString("Annotation_Stickers", "Stickers"));
        if (UndoBtn != null)
            ToolTipProperties.SetText(UndoBtn, loc.GetString("Annotation_Undo", "Annuler"));
        if (RedoBtn != null)
            ToolTipProperties.SetText(RedoBtn, loc.GetString("Annotation_Redo", "Rétablir"));
        if (ResetAnnotationsBtn != null)
            ToolTipProperties.SetText(ResetAnnotationsBtn, loc.GetString("Annotation_Clean", "Effacer la page"));
        if (EraserBtn != null)
            ToolTipProperties.SetText(EraserBtn, loc.GetString("Annotation_Eraser", "Gomme"));
        if (CloseAnnotationBarBtn != null)
            ToolTipProperties.SetText(CloseAnnotationBarBtn, loc.GetString("Common_Close", "Fermer"));
    }

    private void ApplyAnnotationsVisibility(bool isVisible)
    {
        if (ActiveAnnotationsContainer != null)
        {
            ActiveAnnotationsContainer.IsVisible = true;
            ActiveAnnotationsContainer.Opacity = (isVisible && _isScoreReady) ? 1 : 0;
        }

        if (isVisible)
        {
            RenderAnnotations();
        }
        else
        {
            if (AnnotationBar != null && AnnotationBar.IsVisible)
            {
                OnCloseAnnotationBarClicked(this, EventArgs.Empty);
            }

            if (ActiveAnnotationsContainer != null)
            {
                var elementsToRemove = ActiveAnnotationsContainer.Children
                    .Where(c => c != _activeLivePolyline)
                    .ToList();

                foreach (var el in elementsToRemove)
                {
                    ActiveAnnotationsContainer.Children.Remove(el);
                }
            }
        }
    }

    private void ShowMenu()
    {
        MenuTitleLabel.Text = _score.Title;
        MenuModifyAssemblyButton.IsVisible = (_score.Type == ScoreType.PDF);

        SetToolsDrawerVisibility(false);

        UpdateRotateButtonText();
        UpdateMenuBookmarkState();
        UpdateCentralMenuSize(this.Height);
        CentralMenuOverlay.IsVisible = true;
        ImageContainer.InputTransparent = true; // Empêche l'image de bloquer les clics sur le menu !
        ZoomLayout.InputTransparent = true; // Empêche le layout de zoom de bloquer les clics sur le menu !
        BottomTouchBar.IsVisible = false; // Désactive la zone tactile du bas quand le menu est ouvert
    }

    private void UpdateCentralMenuSize(double screenHeight)
    {
        if (screenHeight <= 0)
        {
            screenHeight = this.Height > 0 ? this.Height : DeviceDisplay.MainDisplayInfo.Height / (DeviceDisplay.MainDisplayInfo.Density > 0 ? DeviceDisplay.MainDisplayInfo.Density : 1);
        }

        double menuWidth = DeviceInfo.Idiom == DeviceIdiom.Phone ? 340 : 360;
        double innerWidth = menuWidth - 32; // Padding 16 gauche + 16 droite

        // Hauteur naturelle compacte requise par l'en-tête fixe (boutons du haut + titre complet en dessous) + marges
        double headerAndPadding = 112;
        try
        {
            if (CentralMenuHeaderStack != null)
            {
                var headerMeasured = CentralMenuHeaderStack.Measure(innerWidth, double.PositiveInfinity);
                if (headerMeasured.Height > 30)
                {
                    headerAndPadding = headerMeasured.Height + 44; // Padding haut/bas (32) + espacement (12)
                }
            }
        }
        catch { }

        double naturalContentHeight = 400;
        try
        {
            if (CentralMenuContentStack != null)
            {
                var measured = CentralMenuContentStack.Measure(innerWidth, double.PositiveInfinity);
                if (measured.Height > 100)
                {
                    naturalContentHeight = measured.Height + headerAndPadding + 6;
                }
            }
        }
        catch { }

        // Si l'écran est trop petit pour afficher le menu en entier (ex: écran paysage de smartphone)
        if (screenHeight > 0 && screenHeight < (naturalContentHeight + 36))
        {
            double constrainedHeight = Math.Max(260, screenHeight - 36);
            CentralMenuOverlay.HeightRequest = constrainedHeight;
            CentralMenuOverlay.MaximumHeightRequest = constrainedHeight;
            if (CentralMenuScrollView != null)
            {
                CentralMenuScrollView.HeightRequest = Math.Max(160, constrainedHeight - headerAndPadding);
                CentralMenuScrollView.VerticalScrollBarVisibility = ScrollBarVisibility.Default;
            }
        }
        else
        {
            // Écran de taille suffisante : boîte ajustée au contenu complet sans aucun ascenseur !
            CentralMenuOverlay.HeightRequest = naturalContentHeight;
            CentralMenuOverlay.MaximumHeightRequest = naturalContentHeight;
            if (CentralMenuScrollView != null)
            {
                CentralMenuScrollView.HeightRequest = Math.Max(200, naturalContentHeight - headerAndPadding);
                CentralMenuScrollView.VerticalScrollBarVisibility = ScrollBarVisibility.Never;
            }
        }
    }

    private void OnRotateAllPagesToggled(object sender, ToggledEventArgs e)
    {
        if (RotationScopeLabel != null)
        {
            RotationScopeLabel.Text = e.Value 
                ? Services.LocalizationService.Instance.GetString("Viewer_Rotate_All_Pages", "Appliquer à TOUTE la partition") 
                : Services.LocalizationService.Instance.GetString("Viewer_Rotate_Page_Only", "Appliquer à cette page uniquement");
        }
        UpdateRotateButtonText();
    }

    private async void OnRotateClicked(object sender, EventArgs e)
    {
        bool applyToAll = RotateAllPagesSwitch != null && RotateAllPagesSwitch.IsToggled;

        // Récupérer la rotation actuelle en fonction du mode (global ou page spécifique)
        int currentRot = 0;
        if (applyToAll)
        {
            currentRot = _score.Rotation;
        }
        else
        {
            if (!_pageRotations.TryGetValue(_currentPage, out currentRot))
            {
                currentRot = _score.Rotation;
            }
        }

        int nextRot = (currentRot + 90) % 360;
        _currentRotation = nextRot;

        if (applyToAll)
        {
            _pageRotations.Clear();
            _score.Rotation = nextRot;
        }
        else
        {
            _pageRotations[_currentPage] = nextRot;
        }

        RotateBtn.Text = $"Rotation {nextRot}°";

        if (_score.Type == ScoreType.Image)
        {
            ScoreImage.Rotation = nextRot;
        }
        else
        {
            if (PdfService.IsNativePdfSupported)
            {
                await RenderPdfCurrentAsync(_currentPage);
            }
            else
            {
                if (PdfWebView != null) await PdfWebView.EvaluateJavaScriptAsync($"setRotation({nextRot}, {applyToAll.ToString().ToLowerInvariant()})");
            }
        }

        if (SaveRotationSwitch != null && SaveRotationSwitch.IsToggled)
        {
            await SaveRotationToDbAsync();
        }
    }

    private async void OnSaveRotationToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            await SaveRotationToDbAsync();
        }
        else
        {
            _score.IsRotationSaved = false;
            await _databaseService.SaveScoreAsync(_score);
            await _databaseService.DeletePageRotationsForScoreAsync(_score.Id);
            _pageRotations.Clear();
        }
    }

    private async Task SaveRotationToDbAsync()
    {
        bool applyToAll = RotateAllPagesSwitch != null && RotateAllPagesSwitch.IsToggled;
        
        _score.IsRotationSaved = true;

        if (applyToAll)
        {
            _score.Rotation = _currentRotation;
            await _databaseService.SaveScoreAsync(_score);
            await _databaseService.DeletePageRotationsForScoreAsync(_score.Id);
            _pageRotations.Clear();
        }
        else
        {
            await _databaseService.SaveScoreAsync(_score);
            foreach (var kvp in _pageRotations)
            {
                await _databaseService.SavePageRotationAsync(_score.Id, kvp.Key, kvp.Value);
            }
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnMenuModifyAssemblyClicked(object sender, EventArgs e)
    {
        CentralMenuOverlay.IsVisible = false;
        ImageContainer.InputTransparent = false;
        ZoomLayout.InputTransparent = false;
        BottomTouchBar.IsVisible = !AnnotationBar.IsVisible;

        var score = _score;
        await ToolsPage.NavigateAndOpenScoreAsync(score, Navigation);
    }

    private async void OnEditScoreClicked(object sender, EventArgs e)
    {
        CentralMenuOverlay.IsVisible = false;
        ImageContainer.InputTransparent = false;
        ZoomLayout.InputTransparent = false;
        BottomTouchBar.IsVisible = !AnnotationBar.IsVisible;

        await Navigation.PushAsync(new ScoreEditPage(_score, _databaseService));
    }

    private async void OnResetZoomClicked(object sender, EventArgs e)
    {
        CentralMenuOverlay.IsVisible = false;
        ImageContainer.InputTransparent = false;
        ZoomLayout.InputTransparent = false;
        BottomTouchBar.IsVisible = !AnnotationBar.IsVisible;

        // Réinitialiser le zoom MAUI avec animation fluide
        _ = ZoomLayout.TranslateToAsync(0, 0, 250, Easing.CubicOut);
        await ZoomLayout.ScaleToAsync(1.0, 250, Easing.CubicOut);

        // Réinitialiser le scroll / zoom du visual viewport WebView
        if (_score.Type == ScoreType.PDF && PdfWebView != null && PdfWebView.IsVisible)
        {
            _ = PdfWebView.EvaluateJavaScriptAsync("if (window.visualViewport) { window.scrollTo(0, 0); }");
        }
    }

    private void OnCloseMenuClicked(object sender, EventArgs e)
    {
        CentralMenuOverlay.IsVisible = false;
        ImageContainer.InputTransparent = false; // Restaure les gestes sur l'image
        ZoomLayout.InputTransparent = false; // Restaure les gestes sur le layout de zoom
        BottomTouchBar.IsVisible = !AnnotationBar.IsVisible; // Restaure la zone tactile du bas si la barre n'est pas déjà ouverte
    }


    private void UpdateAnnotationOverlaysLayout(double width, double height)
    {
        if (width <= 0 || height <= 0) return;
        bool isPortrait = width < 480 || width < height;

        // 1. Crayon (DrawOptionsOverlay)
        if (DrawOptionsLayout != null)
        {
            DrawOptionsLayout.Orientation = isPortrait ? StackOrientation.Vertical : StackOrientation.Horizontal;
        }
        if (DrawOptionsSeparator != null)
        {
            DrawOptionsSeparator.IsVisible = !isPortrait;
        }

        // 2. Surlignage / Stabilo (HighlightOptionsOverlay)
        if (HighlightOptionsLayout != null)
        {
            HighlightOptionsLayout.Orientation = isPortrait ? StackOrientation.Vertical : StackOrientation.Horizontal;
        }
        if (HighlightOptionsSeparator != null)
        {
            HighlightOptionsSeparator.IsVisible = !isPortrait;
        }

        // 4. Stickers (StickerPickerOverlay)
        if (StickerPickerOverlay != null)
        {
            if (isPortrait)
            {
                StickerPickerOverlay.MaximumHeightRequest = Math.Min(350, height * 0.55);
            }
            else
            {
                StickerPickerOverlay.MaximumHeightRequest = Math.Min(250, height * 0.75);
            }
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0 && height > 0)
        {
            _lastAllocatedWidth = width;
            _lastAllocatedHeight = height;
            UpdateAnnotationOverlaysLayout(width, height);

            if (_score?.Type == ScoreType.PDF && PdfService.IsNativePdfSupported)
            {
                if (!_isInitialOpenCompleted)
                {
                    // Lors de l'ouverture initiale de la page, le rendu est déjà pris en charge par LoadContentAsync.
                    // RenderPdfCurrentAsync vérifiera automatiquement _lastAllocatedWidth/_lastAllocatedHeight à la fin de son rendu.
                    if (_currentRenderedWidth > 0 && _currentRenderedHeight > 0)
                    {
                        UpdateCanvasBounds(width, height, _currentRenderedWidth, _currentRenderedHeight);
                    }
                    return;
                }

                bool isLandscape = width > height;
#if ANDROID
                try
                {
                    var config = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Resources?.Configuration;
                    if (config != null && config.Orientation != Android.Content.Res.Orientation.Undefined)
                    {
                        isLandscape = (config.Orientation == Android.Content.Res.Orientation.Landscape);
                    }
                }
                catch { }
#endif
                bool twoPagesPref = Preferences.Default.Get("TwoPagesLandscape", true);
                bool shouldBeTwoPages = twoPagesPref && isLandscape && _maxPages > 1;

                if (shouldBeTwoPages != _isTwoPagesMode)
                {
                    _isTwoPagesMode = shouldBeTwoPages;
                    int targetPage = shouldBeTwoPages
                        ? ((_lastSinglePageNumber % 2 == 0) ? Math.Max(1, _lastSinglePageNumber - 1) : _lastSinglePageNumber)
                        : (_lastSinglePageNumber > 0 ? _lastSinglePageNumber : _currentPage);
                    _ = RenderPdfCurrentAsync(targetPage);
                    return;
                }

                if (_currentRenderedWidth > 0 && _currentRenderedHeight > 0)
                {
                    UpdateCanvasBounds(width, height, _currentRenderedWidth, _currentRenderedHeight);
                }
            }
            if (CentralMenuOverlay != null && CentralMenuOverlay.IsVisible)
            {
                UpdateCentralMenuSize(height);
            }
            RenderAnnotations();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            DeviceDisplay.Current.KeepScreenOn = false;
        }
        catch { }

        try
        {
            ApplyImmersiveMode(false);
        }
        catch { }

        PedalMidiService.Instance.ClearOverrideProfile();
        PedalMidiService.Instance.ActionTriggered -= OnPedalActionTriggered;
        LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
        // Laisser le cache mémoire actif (plafonné à 12 pages) pour un rendu instantané à 0ms en réouverture
        CancelPreCount();

        try
        {
            StopTunerListening();
            CleanupRecorderOnExit();
        }
        catch { }

        // Nettoyage asynchrone non-bloquant pour une fermeture de page instantanée (< 50ms)
        _ = Task.Run(() =>
        {
            try
            {
                StopMetronome();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        AudioPlayer.Stop();
                        AudioPlayer.Source = null;
                    }
                    catch { }
                });

#if ANDROID
                try
                {
                    _soundPool?.Release();
                    _soundPool = null;
                    _metronomeSoundId = 0;
                    _preCountSoundId = 0;
                }
                catch { }
#endif
            }
            catch { }
        });
    }

    private void ApplyImmersiveMode(bool enable)
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                var window = activity?.Window;
                if (window == null) return;

                var insetsController = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
                if (insetsController != null)
                {
                    if (enable)
                    {
                        insetsController.Hide(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
                        insetsController.SystemBarsBehavior = AndroidX.Core.View.WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
                    }
                    else
                    {
                        insetsController.Show(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur ApplyImmersiveMode: {ex.Message}");
            }
        });
#endif
    }

}
