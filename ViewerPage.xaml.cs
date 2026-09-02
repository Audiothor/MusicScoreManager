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
    private readonly List<Score>? _setlistScores;
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

    private readonly AnnotationService _annotationService;
    private string? _pendingSticker = null;
    private bool _isAnnotationMode = false;
    private List<Annotation> _annotations = new();
    private bool _isAnnotationsLocked = false;
    private AbsoluteLayout ActiveAnnotationsContainer => AnnotationsContainer;

    private void UnlockAnnotations()
    {
        if (_isAnnotationsLocked)
        {
            _isAnnotationsLocked = false;
            if (LockUnlockBtn != null)
            {
                LockUnlockBtn.Text = "🔓";
                LockUnlockBtn.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2E7D32");
            }
        }
    }

    public ViewerPage(Score score, List<Score>? setlistScores = null, int currentIndex = -1, bool isContinuous = true)
    {
        InitializeComponent();
        _score = score;
        _setlistScores = setlistScores;
        _currentIndex = currentIndex;
        _isContinuous = isContinuous;
        _databaseService = new DatabaseService();
        _settingsService = new SettingsService();
        _annotationService = new AnnotationService();

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

        // Repositionner dynamiquement les annotations lors des changements de taille du conteneur (rotation, layout, etc.)
        AnnotationsContainer.SizeChanged += (s, e) => RenderAnnotations();
        AnnotationsContainer.HandlerChanged += (s, e) => {
#if ANDROID
            SetupNativeTouchHandling();
#endif
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[Viewer] OnAppearing appelé.");

#if ANDROID
        SetupNativeTouchHandling();
#endif
        
        // 1. Démarrer immédiatement le chargement de la partition et des rotations sans délai
        _ = LoadPageAndContentImmediatelyAsync();

        // 2. Initialiser le métronome, l'audio et les annotations en tâche de fond
        _ = Task.Run(async () => {
            InitializeMetronome();
            InitializeAudio();
            await LoadAnnotationsAsync();
            await InitializeAnnotationUI();

            try
            {
                var refreshedScore = await _databaseService.GetScoreAsync(_score.Id);
                if (refreshedScore != null)
                {
                    _score = refreshedScore;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Title = _score.Title;
                        if (MenuTitleLabel != null) MenuTitleLabel.Text = _score.Title;
                        if (MetronomeBpmLabel != null) MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
                        if (MenuMetronomeSwitch != null) MenuMetronomeSwitch.IsToggled = _score.ShowMetronome;
                        if (MenuAudioSwitch != null) MenuAudioSwitch.IsToggled = _score.ShowAudioPlayer;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur rafraîchissement score: {ex.Message}");
            }
        });
    }

    private async Task LoadPageAndContentImmediatelyAsync()
    {
        var rotationsTask = LoadPageRotationsAsync();
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            await EnsurePdfJsReadyAsync();
        }
        await rotationsTask;
        LoadContentAsync();
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
                    if (ActiveAnnotationsContainer != null) ActiveAnnotationsContainer.Opacity = 1;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] Erreur chargement annotations: {ex.Message}");
        }
    }

    private bool _isAnnotationUIInitialized = false;
    private async Task InitializeAnnotationUI()
    {
        if (_isAnnotationUIInitialized) return;
        _isAnnotationUIInitialized = true;

        try
        {
            var favorites = await _databaseService.GetFavoriteStickersAsync();
            var favTexts = favorites.Select(f => f.Text).ToList();
            var categories = _annotationService.GetStickerCategories(favTexts);
            
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
        MenuMetronomeSwitch.IsToggled = _score.ShowMetronome;
        MenuAudioSwitch.IsToggled = _score.ShowAudioPlayer;
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
        RotateBtn.Text = $"Rotation {_currentRotation}°";
    }

    private void OnPdfWebViewNavigating(object sender, WebNavigatingEventArgs e)
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
                    ActiveAnnotationsContainer.Opacity = 1;
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

        try
        {
            if (string.IsNullOrEmpty(_score.FilePath))
            {
                System.Diagnostics.Debug.WriteLine("[Viewer] ERREUR : _score.FilePath est NULL ou vide");
                await DisplayAlertAsync("Erreur", "Chemin du fichier manquant", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Viewer] Path stocké: {_score.FilePath}");
            string fullPath = _settingsService.GetAbsolutePath(_score.FilePath);
            System.Diagnostics.Debug.WriteLine($"[Viewer] FullPath calculé: {fullPath}");

            bool exists = File.Exists(fullPath);
            System.Diagnostics.Debug.WriteLine($"[Viewer] Fichier existe physiquement ? {exists}");

            if (!exists)
            {
                System.Diagnostics.Debug.WriteLine($"[Viewer] ERREUR : Le fichier n'existe pas au chemin : {fullPath}");
                await DisplayAlertAsync("Fichier introuvable", $"Le fichier est introuvable :\n{fullPath}", "OK");
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
                    PdfWebView.IsVisible = false;
                    ImageContainer.IsVisible = true;
                    AnnotationsContainer.IsVisible = true;
                    _currentPage = 1;
                    _maxPages = 1;
                    UpdatePageIndicator();
                    _isScoreReady = true;
                    RenderAnnotations();
                    if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();
                }
                catch (Exception imgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Viewer] ECHEC chargement Image: {imgEx.Message}");
                    await DisplayAlertAsync("Erreur Lecture", $"{imgEx.Message}\n\nL'application n'a pas pu charger l'image.", "OK");
                }
            }
            else if (_score.Type == ScoreType.PDF)
            {
                System.Diagnostics.Debug.WriteLine("[Viewer] Mode PDF - Démarrage chargement WebView");
                _isScoreReady = false;
                if (ActiveAnnotationsContainer != null)
                {
                    ActiveAnnotationsContainer.Opacity = 0;
                    ActiveAnnotationsContainer.Children.Clear();
                }
                ScoreImage.IsVisible = false;
                PdfWebView.IsVisible = true;
                ImageContainer.IsVisible = true;
                AnnotationsContainer.IsVisible = true;

                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    await EnsurePdfJsReadyAsync();
                    
                    string pdfjsDir = Path.Combine(FileSystem.CacheDirectory, "pdfjs");
                    string viewerPath = Path.Combine(pdfjsDir, "viewer.html");
                    
                    // Construction robuste avec 3 slashs
                    string finalViewerPath = viewerPath.StartsWith("/") ? $"file://{viewerPath}" : $"file:///{viewerPath}";
                    string finalFilePath = fullPath.StartsWith("/") ? $"file://{fullPath}" : $"file:///{fullPath}";
                    
                    string pageRotsJson = System.Text.Json.JsonSerializer.Serialize(_pageRotations);
                    string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
                    string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");
                    bool twoPages = Preferences.Default.Get("TwoPagesLandscape", true);
                    string pdfJsUrl = $"{finalViewerPath}?file={Uri.EscapeDataString(finalFilePath)}&rot={_score.Rotation}&page=1&twoPages={twoPages.ToString().ToLowerInvariant()}&pageRots={Uri.EscapeDataString(pageRotsJson)}&nextGest={nextGesture}&prevGest={prevGesture}";

                    System.Diagnostics.Debug.WriteLine($"[Viewer] WebView Source finale: {pdfJsUrl}");
                    _pdfFilePath = finalFilePath; 
                    PdfWebView.Source = new UrlWebViewSource { Url = pdfJsUrl };
                }
                else
                {
                    PdfWebView.Source = fullPath;
                }
            }

            System.Diagnostics.Debug.WriteLine("[Viewer] --- Fin de LoadContentAsync ---");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Viewer] CRASH dans LoadContentAsync: {ex.Message}");
            await DisplayAlertAsync("Erreur de chargement", $"{ex.Message}\n\nFichier: {_score.FilePath}", "OK");
        }
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

    private async void OnPdfWebViewNavigated(object sender, WebNavigatedEventArgs e)
    {
        if (_score.Type == ScoreType.PDF && e.Result == WebNavigationResult.Success)
        {
            System.Diagnostics.Debug.WriteLine("[Viewer] WebView Navigated - Shell HTML prêt.");

            // Synchronisation de sécurité des rotations personnalisées
            if (_pageRotations.Count > 0)
            {
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(_pageRotations);
                    await PdfWebView.EvaluateJavaScriptAsync($"setPageRotationsMap({json})");
                }
                catch { }
            }

            try
            {
                string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
                string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");
                await PdfWebView.EvaluateJavaScriptAsync($"setGestures('{nextGesture}', '{prevGesture}')");
            }
            catch { }

            if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();
        }
    }

    private double _startScale = 1;
    private double _startTranslationX = 0;
    private double _startTranslationY = 0;
    private double _panStartX = 0;
    private double _panStartY = 0;
    private bool _isPinching = false;

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

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Started)
        {
            _panStartX = ZoomLayout.TranslationX;
            _panStartY = ZoomLayout.TranslationY;
            return;
        }

        if (e.StatusType == GestureStatus.Running)
        {
            if (_isPinching) return;

            if (ZoomLayout.Scale > 1.0)
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
            _panStartX = ZoomLayout.TranslationX;
            _panStartY = ZoomLayout.TranslationY;
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

            UpdatePageIndicator();
        }
        catch { }
    }

    private void UpdatePageIndicator()
    {
        bool showPageNumber = Preferences.Default.Get("ShowPageNumber", true);
        double fontSize = Preferences.Default.Get("PageNumberSize", 14.0);

        if (showPageNumber)
        {
            PageIndicator.IsVisible = true;
            PageIndicator.FontSize = fontSize;
            string displayNum = !string.IsNullOrEmpty(_currentPageDisplay) ? _currentPageDisplay : _currentPage.ToString();
            PageIndicator.Text = $"{displayNum} / {_maxPages}";
            PageIndicator.IsVisible = true;
            RenderAnnotations();
        }
        else
        {
            PageIndicator.IsVisible = false;
        }
    }

    private readonly object _metronomeLock = new();
    private CancellationTokenSource? _metronomeCts = null;

    private async void InitializeMetronome()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
            MetronomeOverlay.IsVisible = _score.ShowMetronome;
        });

        try
        {
            // Extraire click.wav vers le cache local pour un chargement direct ultra-rapide
            string clickPath = Path.Combine(FileSystem.CacheDirectory, "click.wav");
            using (var src = await FileSystem.OpenAppPackageFileAsync("click.wav"))
            using (var dst = File.Create(clickPath))
            {
                await src.CopyToAsync(dst);
            }

            // Générer le wav du pré-compte (880Hz, 100ms)
            string preCountPath = Path.Combine(FileSystem.CacheDirectory, "precount.wav");
            GenerateBeepWav(preCountPath, 880, 0.1);

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

    private void MetronomeTick(bool isPreCount = false)
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
                    _soundPool.Play(soundId, 1.0f, 1.0f, 1, 0, 1.0f);
                }
            }
#else
            if (isPreCount && _preCountAudioPlayer != null)
            {
                _preCountAudioPlayer.Play();
            }
            else if (!isPreCount && _metronomeAudioPlayer != null)
            {
                _metronomeAudioPlayer.Play();
            }
#endif
        }

        // 2. Flash visuel fluide sans bloquer l'horloge
        if (_score.ShowMetronome && !isPreCount)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                MetronomeLight.Color = Color.FromArgb("#007ACC");
                await Task.Delay(40);
                if (_isMetronomeRunning)
                {
                    MetronomeLight.Color = Color.FromArgb("#333333");
                }
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

    private async void OnMetronomeOverlayTapped(object sender, EventArgs e)
    {
        string soundOption = _score.HasMetronomeSound ? "Couper le son" : "Activer le son";
        string? action = await this.DisplayActionSheetAsync("Paramètres du Métronome", "Annuler", null, "Changer le BPM", soundOption);

        if (action == "Changer le BPM")
        {
            string? newBpmStr = await this.DisplayPromptAsync("Nouveau BPM", "Entrez le nouveau tempo (60-200) :", keyboard: Keyboard.Numeric, initialValue: _score.BPM.ToString());
            if (int.TryParse(newBpmStr, out int newBpm))
            {
                if (newBpm < 60 || newBpm > 200)
                {
                    await this.DisplayAlertAsync("BPM Invalide", "Le tempo doit être compris entre 60 et 200 BPM.", "OK");
                    return;
                }

                _score.BPM = newBpm;
                MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
                if (_isMetronomePlaying) StartMetronome(); // Redémarre avec le nouvel intervalle
                await _databaseService.SaveScoreAsync(_score);
            }
        }
        else if (action == "Couper le son" || action == "Activer le son")
        {
            _score.HasMetronomeSound = (action == "Activer le son");

            // Si on a besoin du métronome (son OU visuel) et qu'il ne tourne pas, on le lance
            if ((_score.HasMetronomeSound || _score.ShowMetronome) && !_isMetronomePlaying)
            {
                StartMetronome();
            }
            // Si on n'a plus besoin de rien, on arrête
            else if (!_score.HasMetronomeSound && !_score.ShowMetronome)
            {
                StopMetronome();
            }

            await _databaseService.SaveScoreAsync(_score);
        }
    }

    private bool _isAudioInitialized = false;

    private void InitializeAudio()
    {
        if (_isAudioInitialized) return;
        _isAudioInitialized = true;

        _selectedAudioFile = _score.AudioFiles.FirstOrDefault(af => af.IsSelected);
        AudioPlayerOverlay.IsVisible = _score.ShowAudioPlayer && _selectedAudioFile != null;
        MenuAudioSwitch.IsEnabled = _selectedAudioFile != null;

        if (_selectedAudioFile != null)
        {
            string fullPath = _settingsService.GetAbsolutePath(_selectedAudioFile.FilePath, isAudio: true);
            AudioPlayer.Source = MediaSource.FromFile(fullPath);
            AudioPlayer.PositionChanged += OnAudioPositionChanged;

            // Écouter PropertyChanged car Duration peut ne pas être prêt immédiatement à MediaOpened
            AudioPlayer.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CommunityToolkit.Maui.Views.MediaElement.Duration))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        AudioTotalTimeLabel.Text = AudioPlayer.Duration.ToString(@"m\:ss");
                        AudioSlider.Maximum = AudioPlayer.Duration.TotalSeconds;
                    });
                }
            };
        }
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

    private async void OnAudioPlayPauseClicked(object sender, EventArgs e)
    {
        if (_isAudioPlaying)
        {
            AudioPlayer.Pause();
            AudioPlayBtn.Text = "▶";
            _isAudioPlaying = false;
        }
        else
        {
            // Pré-compte synchronisé
            if (_score.PreCountMeasures > 0)
            {
                AudioPlayBtn.IsEnabled = false;

                bool wasMetronomeRunning = _isMetronomePlaying;
                StopMetronome();

                int totalBeeps = _score.PreCountMeasures;
                double bpm = _score.BPM;

                await Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < totalBeeps; i++)
                    {
                        MetronomeTick(isPreCount: true);
                        long targetTicks = (long)((i + 1) * (60.0 / bpm * Stopwatch.Frequency));
                        while (sw.ElapsedTicks < targetTicks)
                        {
                            long remainingTicks = targetTicks - sw.ElapsedTicks;
                            double remainingMs = (double)remainingTicks / Stopwatch.Frequency * 1000.0;
                            if (remainingMs > 4.0) Thread.Sleep((int)(remainingMs - 2.0));
                            else if (remainingMs > 0.05) Thread.SpinWait(20);
                        }
                    }
                });

                // DÉMARRAGE SYNCHRONISÉ : on lance l'audio ET le métronome continu en même temps
                if (wasMetronomeRunning) StartMetronome();
                AudioPlayer.Play();

                AudioPlayBtn.IsEnabled = true;
            }
            else
            {
                AudioPlayer.Play();
            }

            AudioPlayBtn.Text = "⏸";
            _isAudioPlaying = true;
        }
    }

    private void OnAudioToStartClicked(object sender, EventArgs e) => AudioPlayer.SeekTo(TimeSpan.Zero);
    private void OnAudioToEndClicked(object sender, EventArgs e) => AudioPlayer.SeekTo(AudioPlayer.Duration);

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

    private async void OnMenuMetronomeToggled(object sender, ToggledEventArgs e)
    {
        _score.ShowMetronome = e.Value;
        MetronomeOverlay.IsVisible = e.Value;

        if (e.Value)
        {
            StartMetronome();
        }
        else if (!_score.HasMetronomeSound)
        {
            StopMetronome();
        }

        await _databaseService.SaveScoreAsync(_score);
    }



    private async void OnMenuAudioToggled(object sender, ToggledEventArgs e)
    {
        _score.ShowAudioPlayer = e.Value;
        AudioPlayerOverlay.IsVisible = e.Value && _selectedAudioFile != null;
        await _databaseService.SaveScoreAsync(_score);
    }

    private void ShowMenu()
    {
        MenuTitleLabel.Text = _score.Title;
        MenuModifyAssemblyButton.IsVisible = (_score.Type == ScoreType.PDF);
        UpdateRotateButtonText();
        CentralMenuOverlay.IsVisible = true;
        ImageContainer.InputTransparent = true; // Empêche l'image de bloquer les clics sur le menu !
        ZoomLayout.InputTransparent = true; // Empêche le layout de zoom de bloquer les clics sur le menu !
        BottomTouchBar.IsVisible = false; // Désactive la zone tactile du bas quand le menu est ouvert
    }

    private void OnRotateAllPagesToggled(object sender, ToggledEventArgs e)
    {
        if (RotationScopeLabel != null)
        {
            RotationScopeLabel.Text = e.Value ? "Appliquer à TOUTE la partition" : "Appliquer à cette page uniquement";
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
            await PdfWebView.EvaluateJavaScriptAsync($"setRotation({nextRot}, {applyToAll.ToString().ToLowerInvariant()})");
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
        await Navigation.PopToRootAsync();
        await Shell.Current.GoToAsync("//ToolsPage");
        var toolsPage = Shell.Current.CurrentPage as ToolsPage 
            ?? Handler?.MauiContext?.Services.GetService<ToolsPage>();
        if (toolsPage != null)
        {
            await toolsPage.OpenScoreInAssemblerAsync(score);
        }
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
        _ = ZoomLayout.TranslateTo(0, 0, 250, Easing.CubicOut);
        await ZoomLayout.ScaleTo(1.0, 250, Easing.CubicOut);

        // Réinitialiser le scroll / zoom du visual viewport WebView
        if (_score.Type == ScoreType.PDF && PdfWebView.IsVisible)
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

    private async void OnPrevTapped(object sender, EventArgs e)
    {
        if (ZoomLayout.Scale > 1.05) return;
        if (_score.Type == ScoreType.Image)
        {
            HandleStartOfScore();
        }
        else if (_score.Type == ScoreType.PDF)
        {
            if (DeviceInfo.Platform == DevicePlatform.Android && PdfWebView.IsVisible)
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
            if (DeviceInfo.Platform == DevicePlatform.Android && PdfWebView.IsVisible)
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

    private void OnCenterTapped(object sender, EventArgs e) => ShowMenu();

    private void OnCenterSingleTapped(object sender, EventArgs e)
    {
        if (AnnotationBar.IsVisible)
        {
            if (_isAnnotationMode || _isTextMode || _isHighlightMode || _isDrawMode) return;

            AnnotationBar.IsVisible = false;
            BottomTouchBar.IsVisible = true; // Réactive la zone tactile du bas !
            StickerPickerOverlay.IsVisible = false;
            _selectedAnnotation = null;
            _pendingSticker = null;
            _isAnnotationMode = false;
            AnnotationsContainer.InputTransparent = true;

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
            if (_isHighlightMode || _isDrawMode)
            {
                // En mode dessin ou surlignage, ne jamais fermer la barre sur un tap
                return;
            }

            // Si aucun mode d'annotation n'est actif et qu'on tape dans le vide, fermer la barre
            AnnotationBar.IsVisible = false;
            BottomTouchBar.IsVisible = true; // Réactive la zone tactile du bas !
            StickerPickerOverlay.IsVisible = false;
            _selectedAnnotation = null;
            _pendingSticker = null;
            _isAnnotationMode = false;
            AnnotationsContainer.InputTransparent = true;

            if (StickersCollection != null)
            {
                StickersCollection.SelectedItem = null;
            }

            RenderAnnotations();
            PageIndicator.Margin = new Thickness(0, 0, 10, 2);
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
        if (ZoomLayout.Scale > 1.05) return;
        string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
        string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");

        if (nextGesture == "SwipeLeft") OnNextTapped(this, EventArgs.Empty);
        else if (prevGesture == "SwipeLeft") OnPrevTapped(this, EventArgs.Empty);
    }

    private void OnImageSwipedRight(object? sender, SwipedEventArgs e)
    {
        if (ZoomLayout.Scale > 1.05) return;
        string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
        string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");

        if (nextGesture == "SwipeRight") OnNextTapped(this, EventArgs.Empty);
        else if (prevGesture == "SwipeRight") OnPrevTapped(this, EventArgs.Empty);
    }

    private void OnImageSwipedUp(object? sender, SwipedEventArgs e)
    {
        if (ZoomLayout.Scale > 1.05) return;
        string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
        string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");

        if (nextGesture == "SwipeUp") OnNextTapped(this, EventArgs.Empty);
        else if (prevGesture == "SwipeUp") OnPrevTapped(this, EventArgs.Empty);
    }

    private void OnImageSwipedDown(object? sender, SwipedEventArgs e)
    {
        if (ZoomLayout.Scale > 1.05) return;
        string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
        string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");

        if (nextGesture == "SwipeDown") OnNextTapped(this, EventArgs.Empty);
        else if (prevGesture == "SwipeDown") OnPrevTapped(this, EventArgs.Empty);
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

    private async void OnGoToPageClicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("Aller à la page", $"Entrez un numéro de page (1-{_maxPages})", "OK", "Annuler", initialValue: _currentPage.ToString(), keyboard: Keyboard.Numeric);
        if (int.TryParse(result, out int pageNum))
        {
            await GoToPage(pageNum);
            CentralMenuOverlay.IsVisible = false;
            ImageContainer.InputTransparent = false; // Restaure les gestes sur l'image
            ZoomLayout.InputTransparent = false; // Restaure les gestes sur le layout de zoom
            BottomTouchBar.IsVisible = !AnnotationBar.IsVisible; // Restaure la zone tactile du bas si la barre n'est pas déjà ouverte
        }
    }

    private async Task GoToPage(int pageNum)
    {
        if (pageNum >= 1 && pageNum <= _maxPages)
        {
            _currentPage = pageNum;
            if (_score.Type == ScoreType.PDF)
            {
                _isScoreReady = false;
                RenderAnnotations();
                await PdfWebView.EvaluateJavaScriptAsync($"goToPage({pageNum})");
            }
            UpdatePageIndicator();
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        RenderAnnotations();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

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

        StopMetronome();
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
        if (MenuMetronomeSwitch != null) MenuMetronomeSwitch.IsToggled = _score.ShowMetronome;
        if (MenuAudioSwitch != null) MenuAudioSwitch.IsToggled = _score.ShowAudioPlayer;

        _currentRotation = _score.IsRotationSaved ? _score.Rotation : 0;

        await LoadPageRotationsAsync();
        await LoadAnnotationsAsync();
        InitializeAudio();
        InitializeMetronome();

        string fullPath = _settingsService.GetAbsolutePath(_score.FilePath);
        if (!File.Exists(fullPath))
        {
            await DisplayAlertAsync("Fichier introuvable", $"Le fichier est introuvable :\n{fullPath}", "OK");
            return;
        }

        if (_score.Type == ScoreType.PDF && PdfWebView.IsVisible)
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
                    
                    if (next.Type == ScoreType.PDF && DeviceInfo.Platform == DevicePlatform.Android)
                    {
                        string nextPath = _settingsService.GetAbsolutePath(next.FilePath);
                        if (File.Exists(nextPath))
                        {
                            string finalNextPath = nextPath.StartsWith("/") ? $"file://{nextPath}" : $"file:///{nextPath}";
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                try
                                {
                                    await PdfWebView.EvaluateJavaScriptAsync($"preloadPdf('{finalNextPath}');");
                                }
                                catch { }
                            });
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

                    if (prev.Type == ScoreType.PDF && DeviceInfo.Platform == DevicePlatform.Android)
                    {
                        string prevPath = _settingsService.GetAbsolutePath(prev.FilePath);
                        if (File.Exists(prevPath))
                        {
                            string finalPrevPath = prevPath.StartsWith("/") ? $"file://{prevPath}" : $"file:///{prevPath}";
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                try
                                {
                                    await PdfWebView.EvaluateJavaScriptAsync($"preloadPdf('{finalPrevPath}');");
                                }
                                catch { }
                            });
                        }
                    }
                }
            }
        }
        catch { }
    }

    #region Annotations

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
                break;
        }
    }

    private void OnAnnotationToggleTapped(object sender, EventArgs e)
    {
        AnnotationBar.IsVisible = !AnnotationBar.IsVisible;
        BottomTouchBar.IsVisible = !AnnotationBar.IsVisible; // Masque la zone tactile quand la vraie barre d'annotations est ouverte, et inversement
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
            _selectedAnnotation = null;
            _pendingSticker = null;
            _isAnnotationMode = false;
            AnnotationsContainer.InputTransparent = true;

            if (StickersCollection != null)
            {
                StickersCollection.SelectedItem = null;
            }

            RenderAnnotations();
        }
        else
        {
            AnnotationsContainer.InputTransparent = false;
            UnlockAnnotations();
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
    private Microsoft.Maui.Controls.Shapes.Polyline? _activeLivePolyline = null;
    private List<Point> _activeLivePoints = new();

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

    private void SetupNativeTouchHandling()
    {
        if (AnnotationsContainer.Handler?.PlatformView is Android.Views.View nativeView)
        {
            nativeView.Touch -= OnNativeAnnotationsContainerTouch;
            nativeView.Touch += OnNativeAnnotationsContainerTouch;
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

        float density = Android.App.Application.Context.Resources?.DisplayMetrics?.Density ?? 1.0f;
        int pointerCount = motionEvent.PointerCount;

        // 1. GESTION DU MODE SURLIGNAGE ET DU MODE DESSIN (1 doigt, mode actif et déverrouillé)
        if ((_isHighlightMode || _isDrawMode) && !_isAnnotationsLocked)
        {
            double touchX = motionEvent.GetX() / density;
            double touchY = motionEvent.GetY() / density;

            switch (motionEvent.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down:
                    if (_isHighlightMode) StartLiveHighlightStroke(new Point(touchX, touchY));
                    else if (_isDrawMode) StartLiveDrawStroke(new Point(touchX, touchY));
                    args.Handled = true;
                    return;

                case Android.Views.MotionEventActions.Move:
                    if (_isHighlightMode) AddLiveHighlightPoint(new Point(touchX, touchY));
                    else if (_isDrawMode) AddLiveDrawPoint(new Point(touchX, touchY));
                    args.Handled = true;
                    return;

                case Android.Views.MotionEventActions.Up:
                    if (_isHighlightMode) _ = FinishLiveHighlightStrokeAsync();
                    else if (_isDrawMode) _ = FinishLiveDrawStrokeAsync();
                    args.Handled = true;
                    return;

                case Android.Views.MotionEventActions.Cancel:
                    if (_activeLivePolyline != null)
                    {
                        ActiveAnnotationsContainer.Children.Remove(_activeLivePolyline);
                        _activeLivePolyline = null;
                    }
                    _activeLivePoints.Clear();
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
                            double pickerTop = containerHeight - pickerHeight - 70 + StickerPickerOverlay.TranslationY;
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
                _touchDownTime = motionEvent.EventTime;
                _touchDownX = curX;
                _touchDownY = curY;
                _panLastX = curX;
                _panLastY = curY;
                _isNativePanning = ZoomLayout.Scale > 1.05 && !_isHighlightMode;
                args.Handled = true;
                return;

            case Android.Views.MotionEventActions.Move:
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
                    args.Handled = true;
                    return;
                }
                break;

            case Android.Views.MotionEventActions.Up:
                _isNativePanning = false;
                long duration = motionEvent.EventTime - _touchDownTime;
                float diffX = curX - _touchDownX;
                float diffY = curY - _touchDownY;
                float dist = (float)Math.Sqrt(diffX * diffX + diffY * diffY);

                double width = AnnotationsContainer.Width > 0 ? AnnotationsContainer.Width : this.Width;
                double height = AnnotationsContainer.Height > 0 ? AnnotationsContainer.Height : this.Height;

                // Tap détection (< 30px, < 400ms)
                if (dist < 30 && duration < 400)
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
                                        UpdateTextColorBorders();
                                        UpdateTextSizeButtons();
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
                // Swipe détection (>= 35px, < 800ms) - uniquement si non zoomé et barre fermée
                else if (dist >= 35 && duration < 800 && ZoomLayout.Scale <= 1.05 && !AnnotationBar.IsVisible)
                {
                    string nextG = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
                    string prevG = Preferences.Default.Get("PrevPageGesture", "SwipeRight");

                    string detectedG = "";
                    if (Math.Abs(diffX) > Math.Abs(diffY))
                    {
                        detectedG = diffX < 0 ? "SwipeLeft" : "SwipeRight";
                    }
                    else
                    {
                        detectedG = diffY < 0 ? "SwipeUp" : "SwipeDown";
                    }

                    if (detectedG == nextG)
                    {
                        MainThread.BeginInvokeOnMainThread(() => OnNextTapped(this, EventArgs.Empty));
                        args.Handled = true;
                        return;
                    }
                    else if (detectedG == prevG)
                    {
                        MainThread.BeginInvokeOnMainThread(() => OnPrevTapped(this, EventArgs.Empty));
                        args.Handled = true;
                        return;
                    }
                }
                args.Handled = true;
                return;

            case Android.Views.MotionEventActions.Cancel:
                _isNativePanning = false;
                break;
        }

        args.Handled = false;
    }
#endif

    private void OnAnnotationHighlightClicked(object sender, EventArgs e)
    {
        UnlockAnnotations();

        _isHighlightMode = !_isHighlightMode;
        HighlightOptionsOverlay.IsVisible = _isHighlightMode;

        if (_isHighlightMode)
        {
            _isDrawMode = false;
            DrawOptionsOverlay.IsVisible = false;
            DrawBtn.BackgroundColor = Colors.Transparent;

            _isTextMode = false;
            TextOptionsOverlay.IsVisible = false;
            TextAnnotationBtn.BackgroundColor = Colors.Transparent;

            _isAnnotationMode = false;
            _pendingSticker = null;
            StickerPickerOverlay.IsVisible = false;

            ActiveAnnotationsContainer.InputTransparent = false;
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

    private void OnCloseHighlightOptionsClicked(object sender, EventArgs e)
    {
        _isHighlightMode = false;
        HighlightOptionsOverlay.IsVisible = false;
        HighlightBtn.BackgroundColor = Colors.Transparent;
    }

    private void OnHighlightPointerPressed(object? sender, PointerEventArgs e)
    {
        if ((!_isHighlightMode && !_isDrawMode) || _isAnnotationsLocked) return;
        var pos = e.GetPosition(ActiveAnnotationsContainer);
        if (pos == null) return;

        if (_isHighlightMode) StartLiveHighlightStroke(pos.Value);
        else if (_isDrawMode) StartLiveDrawStroke(pos.Value);
    }

    private void OnHighlightPointerMoved(object? sender, PointerEventArgs e)
    {
        if ((!_isHighlightMode && !_isDrawMode) || _isAnnotationsLocked || _activeLivePolyline == null) return;
        var pos = e.GetPosition(ActiveAnnotationsContainer);
        if (pos == null) return;

        if (_isHighlightMode) AddLiveHighlightPoint(pos.Value);
        else if (_isDrawMode) AddLiveDrawPoint(pos.Value);
    }

    private async void OnHighlightPointerReleased(object? sender, PointerEventArgs e)
    {
        if ((!_isHighlightMode && !_isDrawMode) || _isAnnotationsLocked || _activeLivePolyline == null) return;
        if (_isHighlightMode) await FinishLiveHighlightStrokeAsync();
        else if (_isDrawMode) await FinishLiveDrawStrokeAsync();
    }

    private async void OnHighlightPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if ((!_isHighlightMode && !_isDrawMode) || _isAnnotationsLocked) return;

        if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
        {
            if (_activeLivePolyline != null)
            {
                if (_isHighlightMode) await FinishLiveHighlightStrokeAsync();
                else if (_isDrawMode) await FinishLiveDrawStrokeAsync();
            }
        }
    }

    private void StartLiveHighlightStroke(Point pt)
    {
        _activeLivePoints.Clear();
        _activeLivePoints.Add(pt);

        _activeLivePolyline = new Microsoft.Maui.Controls.Shapes.Polyline
        {
            Stroke = ParseColor(_currentHighlightColor),
            StrokeThickness = _currentHighlightThickness,
            StrokeLineCap = Microsoft.Maui.Controls.Shapes.PenLineCap.Flat,
            StrokeLineJoin = Microsoft.Maui.Controls.Shapes.PenLineJoin.Round,
            Opacity = 0.28,
            InputTransparent = true,
            Points = new PointCollection { pt }
        };

        ActiveAnnotationsContainer.Children.Add(_activeLivePolyline);
    }

    private void AddLiveHighlightPoint(Point pt)
    {
        if (_activeLivePoints.Count > 0)
        {
            var last = _activeLivePoints[_activeLivePoints.Count - 1];
            double dist = Math.Sqrt(Math.Pow(pt.X - last.X, 2) + Math.Pow(pt.Y - last.Y, 2));
            if (dist < 4.0) return;
        }

        _activeLivePoints.Add(pt);
        _activeLivePolyline?.Points.Add(pt);
    }

    private async Task FinishLiveHighlightStrokeAsync()
    {
        if (_activeLivePoints.Count < 2 || ActiveAnnotationsContainer.Width <= 0 || ActiveAnnotationsContainer.Height <= 0)
        {
            if (_activeLivePolyline != null)
            {
                ActiveAnnotationsContainer.Children.Remove(_activeLivePolyline);
                _activeLivePolyline = null;
            }
            _activeLivePoints.Clear();
            return;
        }

        double containerW = ActiveAnnotationsContainer.Width;
        double containerH = ActiveAnnotationsContainer.Height;

        string content = string.Join(";", _activeLivePoints.Select(p => 
            $"{Math.Clamp(p.X / containerW, 0.0, 1.0).ToString("F4", System.Globalization.CultureInfo.InvariantCulture)},{Math.Clamp(p.Y / containerH, 0.0, 1.0).ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}"
        ));

        var annotation = new Annotation
        {
            ScoreId = _score.Id,
            Type = AnnotationType.Drawing,
            Category = "Highlight",
            Content = content,
            Color = _currentHighlightColor,
            Scale = _currentHighlightThickness / 30.0,
            PageNumber = _currentPage
        };

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

    #region Drawing Mode (Pencil)

    private bool _isDrawMode = false;
    private string _currentDrawColor = "#000000";
    private double _currentDrawThickness = 9.0; // 3mm par défaut (3px/mm : 1mm=3, 2mm=6, 3mm=9, 4mm=12, 5mm=15)

    private void OnAnnotationDrawClicked(object sender, EventArgs e)
    {
        UnlockAnnotations();

        _isDrawMode = !_isDrawMode;
        DrawOptionsOverlay.IsVisible = _isDrawMode;

        if (_isDrawMode)
        {
            _isHighlightMode = false;
            HighlightOptionsOverlay.IsVisible = false;
            HighlightBtn.BackgroundColor = Colors.Transparent;

            _isTextMode = false;
            TextOptionsOverlay.IsVisible = false;
            TextAnnotationBtn.BackgroundColor = Colors.Transparent;

            _isAnnotationMode = false;
            _pendingSticker = null;
            StickerPickerOverlay.IsVisible = false;

            ActiveAnnotationsContainer.InputTransparent = false;
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

    private void OnCloseDrawOptionsClicked(object sender, EventArgs e)
    {
        _isDrawMode = false;
        DrawOptionsOverlay.IsVisible = false;
        DrawBtn.BackgroundColor = Colors.Transparent;
    }

    private void StartLiveDrawStroke(Point pt)
    {
        _activeLivePoints.Clear();
        _activeLivePoints.Add(pt);

        _activeLivePolyline = new Microsoft.Maui.Controls.Shapes.Polyline
        {
            Stroke = ParseColor(_currentDrawColor),
            StrokeThickness = _currentDrawThickness,
            StrokeLineCap = Microsoft.Maui.Controls.Shapes.PenLineCap.Round,
            StrokeLineJoin = Microsoft.Maui.Controls.Shapes.PenLineJoin.Round,
            Opacity = 1.0, // 100% opaque, se place en premier plan
            InputTransparent = true,
            Points = new PointCollection { pt }
        };

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
        _activeLivePolyline?.Points.Add(pt);
    }

    private async Task FinishLiveDrawStrokeAsync()
    {
        if (_activeLivePoints.Count < 2 || ActiveAnnotationsContainer.Width <= 0 || ActiveAnnotationsContainer.Height <= 0)
        {
            if (_activeLivePolyline != null)
            {
                ActiveAnnotationsContainer.Children.Remove(_activeLivePolyline);
                _activeLivePolyline = null;
            }
            _activeLivePoints.Clear();
            return;
        }

        double containerW = ActiveAnnotationsContainer.Width;
        double containerH = ActiveAnnotationsContainer.Height;

        string content = string.Join(";", _activeLivePoints.Select(p => 
            $"{Math.Clamp(p.X / containerW, 0.0, 1.0).ToString("F4", System.Globalization.CultureInfo.InvariantCulture)},{Math.Clamp(p.Y / containerH, 0.0, 1.0).ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}"
        ));

        var annotation = new Annotation
        {
            ScoreId = _score.Id,
            Type = AnnotationType.Drawing,
            Category = "Pencil",
            Content = content,
            Color = _currentDrawColor,
            Scale = _currentDrawThickness / 9.0,
            PageNumber = _currentPage
        };

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

    #endregion

    #region Text Mode (T)

    private bool _isTextMode = false;
    private string _currentTextColor = "#000000";
    private double _currentTextSize = 14.0; // 8, 10, 12, 14, 16, 18 (14 par défaut)

    private void OnAnnotationTextClicked(object sender, EventArgs e)
    {
        UnlockAnnotations();

        _isTextMode = !_isTextMode;
        TextOptionsOverlay.IsVisible = _isTextMode;

        if (_isTextMode)
        {
            _isHighlightMode = false;
            HighlightOptionsOverlay.IsVisible = false;
            HighlightBtn.BackgroundColor = Colors.Transparent;

            _isDrawMode = false;
            DrawOptionsOverlay.IsVisible = false;
            DrawBtn.BackgroundColor = Colors.Transparent;

            _isAnnotationMode = false;
            _pendingSticker = null;
            StickerPickerOverlay.IsVisible = false;

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
        _isTextMode = false;
        TextOptionsOverlay.IsVisible = false;
        TextAnnotationBtn.BackgroundColor = Colors.Transparent;
    }

    private bool _isPromptingText = false;
    private async Task HandleTextPlacementAsync(double absX, double absY)
    {
        if (_isPromptingText || !_isTextMode || _isAnnotationsLocked || ActiveAnnotationsContainer.Width <= 0 || ActiveAnnotationsContainer.Height <= 0) return;

        try
        {
            _isPromptingText = true;
            double relX = Math.Clamp(absX / ActiveAnnotationsContainer.Width, 0.0, 1.0);
            double relY = Math.Clamp(absY / ActiveAnnotationsContainer.Height, 0.0, 1.0);

            string text = await DisplayPromptAsync("Ajouter du texte", "Tapez votre mot ou texte :", "OK", "Annuler");
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
                    BackgroundColor = "Transparent",
                    PageNumber = _currentPage
                };

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

        ActiveAnnotationsContainer.InputTransparent = false;
        StickerPickerOverlay.IsVisible = !StickerPickerOverlay.IsVisible;
        
        if (StickerPickerOverlay.IsVisible && StickerCategoriesCollection.ItemsSource == null)
        {
            await InitializeAnnotationUI();
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
        HighlightBtn.BackgroundColor = Colors.Transparent;
        DrawBtn.BackgroundColor = Colors.Transparent;
        TextAnnotationBtn.BackgroundColor = Colors.Transparent;
        
        // Réinitialiser la translation pour éviter qu'elle reste décalée lors de la prochaine ouverture
        AnnotationBar.TranslationY = 0;
        StickerPickerOverlay.TranslationY = 0;
        
        _selectedAnnotation = null;
        _pendingSticker = null;
        _isAnnotationMode = false;
        AnnotationsContainer.InputTransparent = true;

        if (StickersCollection != null)
        {
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
                AnnotationsContainer.InputTransparent = true;

                MainThread.BeginInvokeOnMainThread(() => {
                    if (StickersCollection != null)
                    {
                        // Appliquer les réglages actuels à TOUS les stickers de cette catégorie pour l'aperçu
                        foreach (var s in category.Stickers)
                        {
                            s.Color = _currentStickerColor;
                            s.BackgroundColor = _currentStickerBgColor;
                            s.Scale = StickerSizeSlider.Value;
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

    private string _currentStickerColor = "#FFFFFF";
    private string _currentStickerBgColor = "Transparent";

    private Color ParseColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr))
            return Colors.Transparent;

        if (colorStr.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
            return Colors.Transparent;

        try
        {
            return Color.FromArgb(colorStr);
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
                "White" => "#FFFFFF",
                "Black" => "#000000",
                "Red" => "#FF0000",
                "Blue" => "#007ACC",
                _ => "#FFFFFF"
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
                "Transparent" => "Transparent",
                "DarkGray" => "#333333",
                "Ivory" => "#FFFFE0",
                "Red" => "#FF0000",
                "Yellow" => "#FFFF00",
                "Green" => "#2ECC71",
                _ => "Transparent"
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
            await DisplayAlertAsync("Verrouillé", "Les annotations sont verrouillées. Déverrouillez-les (🔓) pour ajouter des stickers.", "OK");
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
                double pickerTop = containerHeight - pickerHeight - 70 + StickerPickerOverlay.TranslationY;
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

            double relX = position.Value.X / ActiveAnnotationsContainer.Width;
            double relY = position.Value.Y / ActiveAnnotationsContainer.Height;

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
                PageNumber = _currentPage
            };

            await _databaseService.SaveAnnotationAsync(annotation);
            _annotations.Add(annotation);
            _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Add, Annotation = annotation });
            _redoStack.Clear();
            UpdateUndoRedoButtons();
            RenderAnnotations();
        }
    }

    private void OnStickerSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is StickerItem sticker)
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
            sticker.Scale = StickerSizeSlider.Value;

            // Nouvelle sélection de sticker ➔ on désélectionne l'annotation placée
            _selectedAnnotation = null;
            RenderAnnotations();
        }
    }

    private async Task PlaceStickerAtAsync(double absX, double absY)
    {
        if (ActiveAnnotationsContainer == null || ActiveAnnotationsContainer.Width <= 0 || ActiveAnnotationsContainer.Height <= 0) return;
        if (string.IsNullOrEmpty(_pendingSticker)) return;

        UnlockAnnotations();

        // Calculer les coordonnées relatives (0.0 à 1.0)
        double relX = Math.Clamp(absX / ActiveAnnotationsContainer.Width, 0.0, 1.0);
        double relY = Math.Clamp(absY / ActiveAnnotationsContainer.Height, 0.0, 1.0);

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
            PageNumber = _currentPage
        };

        await _databaseService.SaveAnnotationAsync(annotation);
        _annotations.Add(annotation);
        _selectedAnnotation = annotation;

        _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Add, Annotation = annotation });
        _redoStack.Clear();
        UpdateUndoRedoButtons();

        // Sortir du mode placement et désélectionner l'item dans la collection
        _isAnnotationMode = false;
        _pendingSticker = null;

        if (StickersCollection != null)
        {
            StickersCollection.SelectedItem = null;
        }

        RenderAnnotations();
    }

    private async void OnPlacementTapped(object? sender, TappedEventArgs e)
    {
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
            await DisplayAlertAsync("Verrouillé", "Les annotations sont verrouillées. Déverrouillez-les (🔓) pour pouvoir modifier.", "OK");
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
        if (_isAnnotationsLocked)
        {
            await DisplayAlertAsync("Verrouillé", "Les annotations sont verrouillées. Déverrouillez-les (🔓) pour pouvoir modifier.", "OK");
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

    private async void OnDeleteSelectedAnnotationClicked(object? sender, EventArgs e)
    {
        if (_isAnnotationsLocked)
        {
            await DisplayAlertAsync("Verrouillé", "Les annotations sont verrouillées. Déverrouillez-les (🔓) pour pouvoir les modifier.", "OK");
            return;
        }

        if (_selectedAnnotation == null)
        {
            await DisplayAlertAsync("Supprimer", "Veuillez d'abord sélectionner une annotation en touchant un sticker ou un surlignage.", "OK");
            return;
        }

        var annToDelete = _selectedAnnotation;
        await _databaseService.DeleteAnnotationAsync(annToDelete);
        _annotations.Remove(annToDelete);
        _undoStack.Push(new AnnotationHistoryEntry { ActionType = AnnotationActionType.Delete, Annotation = annToDelete });
        _redoStack.Clear();
        UpdateUndoRedoButtons();

        _selectedAnnotation = null;
        RenderAnnotations();
    }

    private async void OnResetAnnotationsClicked(object? sender, EventArgs e)
    {
        if (_isAnnotationsLocked)
        {
            await DisplayAlertAsync("Verrouillé", "Les annotations sont verrouillées. Déverrouillez-les (🔓) pour pouvoir les modifier.", "OK");
            return;
        }

        if (_annotations.Count == 0)
        {
            await DisplayAlertAsync("Reset", "Il n'y a aucune annotation à supprimer sur cette partition.", "OK");
            return;
        }

        bool confirm = await DisplayAlertAsync("Reset", "Voulez-vous supprimer TOUTES les annotations de cette partition ?", "Oui", "Non");
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

    private void OnLockUnlockClicked(object? sender, EventArgs e)
    {
        _isAnnotationsLocked = !_isAnnotationsLocked;
        LockUnlockBtn.Text = _isAnnotationsLocked ? "🔒" : "🔓";
        LockUnlockBtn.BackgroundColor = _isAnnotationsLocked 
            ? Microsoft.Maui.Graphics.Color.FromArgb("#D32F2F") 
            : Microsoft.Maui.Graphics.Color.FromArgb("#2E7D32");
        
        if (_isAnnotationsLocked)
        {
            _selectedAnnotation = null;
            _isHighlightMode = false;
            _isDrawMode = false;
            _isTextMode = false;
            HighlightOptionsOverlay.IsVisible = false;
            DrawOptionsOverlay.IsVisible = false;
            TextOptionsOverlay.IsVisible = false;
            HighlightBtn.BackgroundColor = Colors.Transparent;
            DrawBtn.BackgroundColor = Colors.Transparent;
            TextAnnotationBtn.BackgroundColor = Colors.Transparent;
        }
        RenderAnnotations();
    }

    private Annotation? FindAnnotationAtPoint(double x, double y, double containerW, double containerH)
    {
        if (containerW <= 0 || containerH <= 0) return null;

        var pageAnns = _annotations.Where(a => a.PageNumber == _currentPage).Reverse().ToList();
        foreach (var ann in pageAnns)
        {
            if (ann.Type == AnnotationType.Sticker || ann.Type == AnnotationType.Text)
            {
                double ax = ann.X * containerW;
                double ay = ann.Y * containerH;
                double radius = Math.Max(30.0, 30.0 * Math.Max(0.8, ann.Scale));
                if (Math.Abs(x - ax) <= radius && Math.Abs(y - ay) <= radius)
                {
                    return ann;
                }
            }
            else if (ann.Type == AnnotationType.Drawing)
            {
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
                            double px = rx * containerW;
                            double py = ry * containerH;
                            if (Math.Abs(x - px) <= 25.0 && Math.Abs(y - py) <= 25.0)
                            {
                                return ann;
                            }
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
        ActiveAnnotationsContainer.Opacity = _isScoreReady ? 1 : 0;

        if (!_isScoreReady || ActiveAnnotationsContainer.Width <= 0 || ActiveAnnotationsContainer.Height <= 0)
        {
            return;
        }

        // On ne conserve que les éléments live en cours de dessin (s'il y en a un)
        var pageAnnotations = _annotations.Where(a => a.PageNumber == _currentPage).ToList();

        var elementsToRemove = ActiveAnnotationsContainer.Children
            .Where(c => c != _activeLivePolyline)
            .ToList();

        foreach (var el in elementsToRemove)
        {
            ActiveAnnotationsContainer.Children.Remove(el);
        }

        double containerW = ActiveAnnotationsContainer.Width;
        double containerH = ActiveAnnotationsContainer.Height;

        foreach (var ann in pageAnnotations)
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
            }
            else
            {
                var border = CreateStickerBorder(ann, containerW, containerH);
                ActiveAnnotationsContainer.Children.Add(border);
            }
        }
    }

    private Border CreateTextBorder(Annotation ann, double containerW, double containerH)
    {
        double fontSize = ann.Scale <= 0 ? 14.0 : ann.Scale;

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
            Padding = new Thickness(6, 2),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
            StrokeThickness = (ann == _selectedAnnotation) ? 1.5 : 0,
            Stroke = (ann == _selectedAnnotation) ? Color.FromArgb("#007ACC") : Colors.Transparent,
            BackgroundColor = (ann == _selectedAnnotation) ? Color.FromArgb("#33007ACC") : Colors.Transparent,
            Content = label,
            InputTransparent = _isAnnotationsLocked || _isHighlightMode || _isDrawMode
        };

        AbsoluteLayout.SetLayoutFlags(border, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);

        var size = border.Measure(double.PositiveInfinity, double.PositiveInfinity);
        double charCount = string.IsNullOrEmpty(ann.Content) ? 1 : ann.Content.Length;
        double estimatedTextWidth = charCount * (fontSize * 0.75);

        double w = Math.Max(Math.Max(size.Width + 14, estimatedTextWidth + 16), 24);
        double h = Math.Max(Math.Max(size.Height + 6, (fontSize * 1.5) + 6), 20);
        double absX = ann.X * containerW;
        double absY = ann.Y * containerH;

        AbsoluteLayout.SetLayoutBounds(border, new Rect(absX - (w / 2), absY - (h / 2), w, h));

        var selectTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        selectTap.Tapped += (s, e) => {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
            if (s is Border b && b.BindingContext is Annotation a)
            {
                _selectedAnnotation = a;
                _currentTextColor = a.Color;
                _currentTextSize = a.Scale <= 0 ? 14 : a.Scale;
                UpdateTextColorBorders();
                UpdateTextSizeButtons();
                RenderAnnotations();
            }
        };
        border.GestureRecognizers.Add(selectTap);

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += async (s, e) => {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
            if (s is Border b && b.BindingContext is Annotation a)
            {
                string newText = await DisplayPromptAsync("Modifier le texte", "Texte :", "OK", "Annuler", initialValue: a.Content);
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
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
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
                            double finalAbsX = (a.X * containerW) + b.TranslationX;
                            double finalAbsY = (a.Y * containerH) + b.TranslationY;
                            a.X = Math.Clamp(finalAbsX / containerW, 0.0, 1.0);
                            a.Y = Math.Clamp(finalAbsY / containerH, 0.0, 1.0);
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
                    pointCollection.Add(new Point(rx * containerW, ry * containerH));
                }
            }
        }

        var polyline = new Microsoft.Maui.Controls.Shapes.Polyline
        {
            BindingContext = ann,
            Points = pointCollection,
            Stroke = ParseColor(ann.Color),
            StrokeThickness = ann.Scale * 9.0, // 3px par mm (1mm=3, 2mm=6, 3mm=9, 4mm=12, 5mm=15)
            StrokeLineCap = Microsoft.Maui.Controls.Shapes.PenLineCap.Round,
            StrokeLineJoin = Microsoft.Maui.Controls.Shapes.PenLineJoin.Round,
            Opacity = 1.0, // 100% opaque, se met en premier plan et cache ce qui est derrière
            InputTransparent = _isAnnotationsLocked || _isHighlightMode || _isDrawMode
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) =>
        {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
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
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
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
                    pointCollection.Add(new Point(rx * containerW, ry * containerH));
                }
            }
        }

        var polyline = new Microsoft.Maui.Controls.Shapes.Polyline
        {
            BindingContext = ann,
            Points = pointCollection,
            Stroke = ParseColor(ann.Color),
            StrokeThickness = ann.Scale * 30.0,
            StrokeLineCap = Microsoft.Maui.Controls.Shapes.PenLineCap.Flat,
            StrokeLineJoin = Microsoft.Maui.Controls.Shapes.PenLineJoin.Round,
            Opacity = (ann == _selectedAnnotation) ? 0.55 : 0.28,
            InputTransparent = _isAnnotationsLocked || _isHighlightMode || _isDrawMode
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) =>
        {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
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
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
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
        double effectiveScale = Math.Max(0.5, ann.Scale <= 0 ? 1.0 : ann.Scale);
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
            InputTransparent = _isAnnotationsLocked || _isHighlightMode || _isDrawMode
        };

        AbsoluteLayout.SetLayoutFlags(border, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);

        var size = border.Measure(double.PositiveInfinity, double.PositiveInfinity);
        double charCount = string.IsNullOrEmpty(ann.Content) ? 1 : ann.Content.Length;
        double estimatedTextWidth = charCount * (fontSize * 0.75);

        // Garantir une largeur et hauteur amplement suffisantes pour ne JAMAIS tronquer le mot sur PDF ou Image
        double w = Math.Max(Math.Max(size.Width + 18, estimatedTextWidth + (22 * effectiveScale)), 28 * effectiveScale);
        double h = Math.Max(Math.Max(size.Height + 8, (fontSize * 1.5) + (10 * effectiveScale)), 22 * effectiveScale);
        double absX = ann.X * containerW;
        double absY = ann.Y * containerH;

        AbsoluteLayout.SetLayoutBounds(border, new Rect(absX - (w / 2), absY - (h / 2), w, h));

        var selectTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        selectTap.Tapped += (s, e) => {
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
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
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
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
            if (_isAnnotationsLocked || _isHighlightMode || _isDrawMode) return;
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
                            double finalAbsX = (a.X * containerW) + b.TranslationX;
                            double finalAbsY = (a.Y * containerH) + b.TranslationY;
                            a.X = Math.Clamp(finalAbsX / containerW, 0.0, 1.0);
                            a.Y = Math.Clamp(finalAbsY / containerH, 0.0, 1.0);
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

    #endregion
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
