using MusicScoreManager.Models;
using MusicScoreManager.Services;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using System.Linq;
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
    private double startScale = 1;
    private int _currentPage = 1;
    private int _maxPages = 1;
    private int _currentRotation = 0;

    private System.Threading.CancellationTokenSource? _metronomeCts;
    private IAudioPlayer? _metronomeAudioPlayer;
    private IAudioPlayer? _preCountAudioPlayer;
    private bool _isMetronomePlaying = false;
    private bool _isScoreReady = false;
    private bool _isAudioPlaying = false;
    private bool _isDraggingAudioSlider = false;
    private ScoreAudioFile? _selectedAudioFile;
    private string _pdfFilePath = "current.pdf";

    private readonly AnnotationService _annotationService;
    private string? _pendingSticker = null;
    private bool _isAnnotationMode = false;
    private List<Annotation> _annotations = new();

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

        if (_score.IsRotationSaved)
        {
            _currentRotation = _score.Rotation;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[Viewer] OnAppearing appelé.");
        
        // On attend que la page soit bien affichée pour charger le contenu lourd
        MainThread.BeginInvokeOnMainThread(() => {
            LoadContentAsync();
            SetupMenuUI();
        });

        // On lance le reste en arrière-plan
        _ = Task.Run(async () => {
            InitializeMetronome();
            InitializeAudio();
            await LoadAnnotationsAsync();
            await InitializeAnnotationUI();
        });
    }

    private async Task LoadAnnotationsAsync()
    {
        try
        {
            _annotations = await _databaseService.GetAnnotationsForScoreAsync(_score.Id);
            MainThread.BeginInvokeOnMainThread(() => RenderAnnotations());
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
            // Démarrer si on veut le visuel OU le son
            if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();
        }
    }

    private async void LoadContentAsync()
    {
        _score.HasMetronomeSound = false;
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

            string ext = Path.GetExtension(fullPath)?.ToLowerInvariant() ?? "";
            string cachePath = Path.Combine(FileSystem.CacheDirectory, "SetlistCache", $"{_score.Id}{ext}");
            System.Diagnostics.Debug.WriteLine($"[Viewer] CachePath cible: {cachePath}");

            _pdfFilePath = "current.pdf";

            if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 0)
            {
                fullPath = cachePath;
                _pdfFilePath = $"../SetlistCache/{_score.Id}{ext}";
                System.Diagnostics.Debug.WriteLine($"[Viewer] ✅ UTILISATION DU CACHE");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Viewer] ⚠️ Cache absent ou vide. Utilisation de l'original.");
            }

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
                    // Lecture en tâche de fond pour la robustesse (SD Card / SAF)
                    byte[] imageBytes = await Task.Run(() => File.ReadAllBytes(fullPath));
                    MainThread.BeginInvokeOnMainThread(() => {
                        ScoreImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                        ScoreImage.Rotation = _currentRotation;
                        ImageScrollView.IsVisible = true;
                        ImageTouchGrid.IsVisible = true;
                        PdfWebView.IsVisible = false;
                        _currentPage = 1;
                        _maxPages = 1;
                        UpdatePageIndicator();
                        _isScoreReady = true;
                        if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();
                    });
                }
                catch (Exception imgEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Viewer] ECHEC chargement Image: {imgEx.Message}");
                    MainThread.BeginInvokeOnMainThread(async () => {
                        await DisplayAlertAsync("Erreur Lecture", $"{imgEx.Message}\n\nL'application n'a pas la permission d'accéder à ce dossier.", "OK");
                    });
                }
            }
            else if (_score.Type == ScoreType.PDF)
            {
                System.Diagnostics.Debug.WriteLine("[Viewer] Mode PDF - Démarrage chargement WebView");
                MainThread.BeginInvokeOnMainThread(() => {
                    ImageScrollView.IsVisible = false;
                    ImageTouchGrid.IsVisible = false;
                    PdfWebView.IsVisible = true;
                    PdfWebView.Source = null; // On vide pour forcer le refresh
                });

                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    await EnsurePdfJsReadyAsync();
                    
                    // On attend un court instant que la WebView soit prête dans le layout
                    await Task.Delay(200);

                    string pdfjsDir = Path.Combine(FileSystem.CacheDirectory, "pdfjs");
                    string viewerPath = Path.Combine(pdfjsDir, "viewer.html");
                    
                    // Construction robuste avec 3 slashs
                    string finalViewerPath = viewerPath.StartsWith("/") ? $"file://{viewerPath}" : $"file:///{viewerPath}";
                    string finalFilePath = fullPath.StartsWith("/") ? $"file://{fullPath}" : $"file:///{fullPath}";
                    
                    string pdfJsUrl = $"{finalViewerPath}?file={Uri.EscapeDataString(finalFilePath)}#page=1";

                    System.Diagnostics.Debug.WriteLine($"[Viewer] WebView Source finale: {pdfJsUrl}");
                    _pdfFilePath = finalFilePath; 
                    MainThread.BeginInvokeOnMainThread(() => {
                        PdfWebView.Source = new UrlWebViewSource { Url = pdfJsUrl };
                    });
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() => {
                        PdfWebView.Source = fullPath;
                    });
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

    private async Task EnsurePdfJsReadyAsync()
    {
        string cacheDir = FileSystem.CacheDirectory;
        string pdfjsDir = Path.Combine(cacheDir, "pdfjs");

        if (!Directory.Exists(pdfjsDir)) Directory.CreateDirectory(pdfjsDir);

        string[] files = { "pdf.min.js", "pdf.worker.min.js", "viewer.html" };

        foreach (var file in files)
        {
            string dest = Path.Combine(pdfjsDir, file);

            // Pour cette version on force le rafraîchissement pour éliminer toute corruption
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

    private async void OnPdfWebViewNavigated(object sender, WebNavigatedEventArgs e)
    {
        if (_score.Type == ScoreType.PDF && e.Result == WebNavigationResult.Success)
        {
            System.Diagnostics.Debug.WriteLine("[Viewer] WebView Navigated - Auto-load en cours...");
            
            if (_currentRotation != 0)
            {
                await Task.Delay(500);
                await PdfWebView.EvaluateJavaScriptAsync($"setRotation({_currentRotation})");
            }
            
            _isScoreReady = true;
            if (_score.ShowMetronome || _score.HasMetronomeSound) StartMetronome();
        }
    }

    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            startScale = ScoreImage.Scale;
            ScoreImage.AnchorX = 0.5;
            ScoreImage.AnchorY = 0.5;
        }
        if (e.Status == GestureStatus.Running)
        {
            currentScale += (e.Scale - 1) * startScale;
            currentScale = Math.Max(1, currentScale);
            currentScale = Math.Min(4, currentScale);
            ScoreImage.Scale = currentScale;
        }
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (ScoreImage.Scale > 1 && e.StatusType == GestureStatus.Running)
        {
            ScoreImage.TranslationX += e.TotalX;
            ScoreImage.TranslationY += e.TotalY;
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
            PageIndicator.Text = $"{_currentPage} / {_maxPages}";
            PageIndicator.IsVisible = true;
            RenderAnnotations();
        }
        else
        {
            PageIndicator.IsVisible = false;
        }
    }

    private async void InitializeMetronome()
    {
        MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
        MetronomeOverlay.IsVisible = _score.ShowMetronome;

        // Initialiser la source audio via Plugin.Maui.Audio pour un son à faible latence et sans craquement
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync("click.wav");
            _metronomeAudioPlayer = AudioManager.Current.CreatePlayer(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur audio: {ex.Message}");
        }

        if (_score.ShowMetronome) StartMetronome();

        // Générer et charger le son de pré-compte
        await InitializePreCountSoundAsync();
    }

    private async Task InitializePreCountSoundAsync()
    {
        try
        {
            string preCountPath = Path.Combine(FileSystem.CacheDirectory, "precount.wav");
            GenerateBeepWav(preCountPath, 880, 0.1); // Un bip à 880Hz (La) de 100ms

            using var stream = File.OpenRead(preCountPath);
            _preCountAudioPlayer = AudioManager.Current.CreatePlayer(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur génération bip: {ex.Message}");
        }
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

    private async void StartMetronome()
    {
        StopMetronome();
        if (_score.BPM <= 0 || !_isScoreReady) return;

        _isMetronomePlaying = true;
        _metronomeCts = new System.Threading.CancellationTokenSource();
        var token = _metronomeCts.Token;

        // Boucle de haute précision sur un thread d'arrière-plan
        _ = Task.Run(async () =>
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            int intervalMs = (int)(60000.0 / _score.BPM);

            while (!token.IsCancellationRequested && _isMetronomePlaying)
            {
                stopwatch.Restart();

                // Exécuter le tick
                MetronomeTick();

                // Attendre le prochain intervalle avec précision
                int elapsed = (int)stopwatch.ElapsedMilliseconds;
                int waitTime = intervalMs - elapsed;
                if (waitTime > 0)
                {
                    await Task.Delay(waitTime, token);
                }
            }
        }, token);
    }

    private void MetronomeTick(bool isPreCount = false)
    {
        if (!_isMetronomePlaying && !isPreCount) return;

        // Flash visuel (uniquement si le métronome est visible)
        if (_score.ShowMetronome)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MetronomeLight.Color = Color.FromArgb("#007ACC");
                Task.Delay(50).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(() => MetronomeLight.Color = Color.FromArgb("#333333")));
            });
        }

        // Son
        if (_score.HasMetronomeSound)
        {
            if (isPreCount && _preCountAudioPlayer != null)
            {
                _preCountAudioPlayer.Play();
            }
            else if (!isPreCount && _metronomeAudioPlayer != null)
            {
                _metronomeAudioPlayer.Play();
            }
        }
    }

    private void StopMetronome()
    {
        _isMetronomePlaying = false;
        _metronomeCts?.Cancel();
        _metronomeCts = null;
        MainThread.BeginInvokeOnMainThread(() => MetronomeLight.Color = Color.FromArgb("#333333"));
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
                int intervalMs = (int)(60000.0 / _score.BPM);

                for (int i = 0; i < totalBeeps; i++)
                {
                    MetronomeTick(isPreCount: true);
                    await Task.Delay(intervalMs);
                }

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
        CentralMenuOverlay.IsVisible = true;
    }

    private async void OnRotateClicked(object sender, EventArgs e)
    {
        _currentRotation = (_currentRotation + 90) % 360;
        UpdateRotateButtonText();

        if (_score.Type == ScoreType.Image)
        {
            ScoreImage.Rotation = _currentRotation;
        }
        else
        {
            await PdfWebView.EvaluateJavaScriptAsync($"setRotation({_currentRotation})");
        }

        if (SaveRotationSwitch.IsToggled)
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
        }
    }

    private async Task SaveRotationToDbAsync()
    {
        _score.Rotation = _currentRotation;
        _score.IsRotationSaved = true;
        await _databaseService.SaveScoreAsync(_score);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnCloseMenuClicked(object sender, EventArgs e)
    {
        CentralMenuOverlay.IsVisible = false;
    }

    private void OnPrevTapped(object sender, EventArgs e)
    {
        if (_score.Type == ScoreType.Image)
        {
            HandleStartOfScore();
        }
    }

    private void OnNextTapped(object sender, EventArgs e)
    {
        if (_score.Type == ScoreType.Image)
        {
            HandleEndOfScore();
        }
    }

    private void OnCenterTapped(object sender, EventArgs e) => ShowMenu();

    private void OnCenterSingleTapped(object sender, EventArgs e)
    {
        if (AnnotationBar.IsVisible)
        {
            AnnotationBar.IsVisible = false;
            StickerPickerOverlay.IsVisible = false;
            PageIndicator.Margin = new Thickness(0, 0, 10, 2);
        }
    }

    private async void OnGoToPageClicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("Aller à la page", $"Entrez un numéro de page (1-{_maxPages})", "OK", "Annuler", initialValue: _currentPage.ToString(), keyboard: Keyboard.Numeric);
        if (int.TryParse(result, out int pageNum))
        {
            await GoToPage(pageNum);
            CentralMenuOverlay.IsVisible = false;
        }
    }

    private async Task GoToPage(int pageNum)
    {
        if (pageNum >= 1 && pageNum <= _maxPages)
        {
            _currentPage = pageNum;
            if (_score.Type == ScoreType.PDF)
            {
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
        StopMetronome();
        AudioPlayer.Stop();
        AudioPlayer.Source = null; // Libérer le fichier
    }

    private async void HandleEndOfScore()
    {
        if (_setlistScores != null && _currentIndex != -1)
        {
            if (_isContinuous && _currentIndex < _setlistScores.Count - 1)
            {
                _currentIndex++;
                var nextScore = _setlistScores[_currentIndex];
                await Navigation.PushAsync(new ViewerPage(nextScore, _setlistScores, _currentIndex, _isContinuous));
                Navigation.RemovePage(this);
            }
            else
            {
                await Navigation.PopAsync();
            }
        }
    }

    private async void HandleStartOfScore()
    {
        if (_setlistScores != null && _currentIndex > 0 && _isContinuous)
        {
            _currentIndex--;
            var prevScore = _setlistScores[_currentIndex];
            await Navigation.PushAsync(new ViewerPage(prevScore, _setlistScores, _currentIndex, _isContinuous));
            Navigation.RemovePage(this);
        }
        else
        {
            // Si on est au début (ou que le mode continu est OFF), on ferme le visualiseur
            await Navigation.PopAsync();
        }
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
                AnnotationBar.TranslationY = newY;
                StickerPickerOverlay.TranslationY = newY;
                StickerSettingsBar.TranslationY = newY;
                // PageIndicator.TranslationY = newY; // On ne déplace plus l'indicateur
                break;
        }
    }

    private void OnAnnotationToggleTapped(object sender, EventArgs e)
    {
        AnnotationBar.IsVisible = !AnnotationBar.IsVisible;
        if (!AnnotationBar.IsVisible)
        {
            StickerPickerOverlay.IsVisible = false;
            StickerSettingsBar.IsVisible = false;
        }

        // Ajuster la position de l'indicateur de page
        PageIndicator.Margin = new Thickness(0, 0, 10, AnnotationBar.IsVisible ? 65 : 2);
    }

    private async void OnAnnotationTextClicked(object sender, EventArgs e)
    {
        StickerPickerOverlay.IsVisible = false;
        StickerSettingsBar.IsVisible = false;
        await DisplayAlertAsync("Info", "L'annotation de texte arrive bientôt !", "OK");
    }

    private async void OnAnnotationDrawClicked(object sender, EventArgs e)
    {
        StickerPickerOverlay.IsVisible = false;
        StickerSettingsBar.IsVisible = false;
        await DisplayAlertAsync("Info", "Le mode dessin arrive bientôt !", "OK");
    }

    private void OnAnnotationStickersClicked(object sender, EventArgs e)
    {
        StickerPickerOverlay.IsVisible = !StickerPickerOverlay.IsVisible;
        StickerSettingsBar.IsVisible = StickerPickerOverlay.IsVisible;
        
        if (StickerPickerOverlay.IsVisible && StickerCategoriesCollection.ItemsSource == null)
        {
            InitializeAnnotationUI();
        }
    }

    private void OnStickerCategoryChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection.FirstOrDefault() is StickerCategory category)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    if (StickersCollection != null)
                    {
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
            // On ne masque plus les bandeaux car l'utilisateur veut qu'ils restent visibles
        }
    }

    private string _currentStickerColor = "#FFFFFF";

    private void OnStickerColorTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string color)
        {
            _currentStickerColor = color switch
            {
                "White" => "#FFFFFF",
                "Black" => "#000000",
                "Red" => "#FF0000",
                "Blue" => "#007ACC",
                _ => "#FFFFFF"
            };
            
            // Mise à jour visuelle de l'indicateur
            if (CurrentColorIndicator != null)
            {
                CurrentColorIndicator.Color = Color.FromArgb(_currentStickerColor);
            }

            // APERÇU : Mettre à jour le sticker sélectionné dans le picker
            if (StickersCollection?.SelectedItem is StickerItem selectedSticker)
            {
                selectedSticker.Color = _currentStickerColor;
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
            await Task.Delay(30, token); 
            if (StickersCollection?.SelectedItem is StickerItem selectedSticker)
            {
                selectedSticker.Scale = e.NewValue;
            }
        }
        catch (OperationCanceledException) { }
    }

    private async void OnStickerDrop(object? sender, DropEventArgs e)
    {
        if (e.Data.Properties.TryGetValue("Sticker", out var stickerObj) && stickerObj is string sticker)
        {
            // On prend DIRECTEMENT les valeurs actuelles des réglettes pour être sûr
            string color = _currentStickerColor;
            double scale = StickerSizeSlider.Value;

            var position = e.GetPosition(AnnotationsContainer);
            if (position == null) return;

            double relX = position.Value.X / AnnotationsContainer.Width;
            double relY = position.Value.Y / AnnotationsContainer.Height;

            var annotation = new Annotation
            {
                ScoreId = _score.Id,
                Type = AnnotationType.Sticker,
                Content = sticker,
                X = relX,
                Y = relY,
                Scale = scale,
                Color = color,
                PageNumber = _currentPage
            };

            await _databaseService.SaveAnnotationAsync(annotation);
            _annotations.Add(annotation);
            RenderAnnotations();

            // Une fois posé, on peut masquer les bandeaux pour libérer de l'espace si besoin
            // mais l'utilisateur a demandé qu'ils ne partent pas. On les laisse donc.
        }
    }

    private void OnStickerSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is StickerItem sticker)
        {
            _pendingSticker = sticker.Text;
            
            // Appliquer les réglages actuels pour l'aperçu immédiat
            sticker.Color = _currentStickerColor;
            sticker.Scale = StickerSizeSlider.Value;
            
            // On laisse l'overlay ouvert pour permettre la personnalisation (couleur/taille)
            // L'utilisateur glissera ensuite le sticker pour le placer
        }
    }

    private async void OnPlacementTapped(object? sender, TappedEventArgs e)
    {
        if (!_isAnnotationMode || _pendingSticker == null) return;

        var position = e.GetPosition(AnnotationsContainer);
        if (position == null) return;

        // Calculer les coordonnées relatives (0.0 à 1.0)
        double relX = position.Value.X / AnnotationsContainer.Width;
        double relY = position.Value.Y / AnnotationsContainer.Height;

        var annotation = new Annotation
        {
            ScoreId = _score.Id,
            Type = AnnotationType.Sticker,
            Content = _pendingSticker,
            X = relX,
            Y = relY,
            Scale = 1.0,
            PageNumber = _currentPage
        };

        await _databaseService.SaveAnnotationAsync(annotation);
        _annotations.Add(annotation);

        // Sortir du mode placement
        _isAnnotationMode = false;
        AnnotationsContainer.InputTransparent = true;
        AnnotationsContainer.GestureRecognizers.Clear();
        _pendingSticker = null;

        RenderAnnotations();
    }

    private Annotation? _selectedAnnotation = null;

    private async void OnDeleteSelectedAnnotationClicked(object? sender, EventArgs e)
    {
        if (_selectedAnnotation == null)
        {
            await DisplayAlertAsync("Supprimer", "Veuillez d'abord sélectionner une annotation en touchant un sticker.", "OK");
            return;
        }

        await _databaseService.DeleteAnnotationAsync(_selectedAnnotation);
        _annotations.Remove(_selectedAnnotation);
        _selectedAnnotation = null;
        RenderAnnotations();
    }

    private async void OnResetAnnotationsClicked(object? sender, EventArgs e)
    {
        if (_annotations.Count == 0)
        {
            await DisplayAlertAsync("Reset", "Il n'y a aucune annotation à supprimer sur cette partition.", "OK");
            return;
        }

        bool confirm = await DisplayAlertAsync("Reset", "Voulez-vous supprimer TOUTES les annotations de cette partition ?", "Oui", "Non");
        if (confirm)
        {
            await _databaseService.DeleteAllAnnotationsForScoreAsync(_score.Id);
            _annotations.Clear();
            _selectedAnnotation = null;
            RenderAnnotations();
        }
    }

    private void RenderAnnotations()
    {
        if (AnnotationsContainer == null) return;

        // On ne rend que les annotations de la page actuelle
        var pageAnnotations = _annotations.Where(a => a.PageNumber == _currentPage).ToList();

        // 1. Recyclage : Supprimer les labels en trop
        while (AnnotationsContainer.Children.Count > pageAnnotations.Count)
            AnnotationsContainer.Children.RemoveAt(AnnotationsContainer.Children.Count - 1);

        // 2. Mise à jour ou création des labels
        for (int i = 0; i < pageAnnotations.Count; i++)
        {
            var ann = pageAnnotations[i];
            Label label;

            if (i < AnnotationsContainer.Children.Count)
            {
                label = (Label)AnnotationsContainer.Children[i];
            }
            else
            {
                label = CreateStickerLabel();
                AnnotationsContainer.Children.Add(label);
            }

            // Mise à jour des propriétés (plus rapide que recréer)
            label.BindingContext = ann;
            label.Text = ann.Content;
            label.TextColor = Color.FromArgb(ann.Color);
            label.FontSize = 30 * ann.Scale;
            label.Opacity = (ann == _selectedAnnotation) ? 0.6 : 1.0;

            // Positionnement
            double absX = ann.X * AnnotationsContainer.Width;
            double absY = ann.Y * AnnotationsContainer.Height;
            AbsoluteLayout.SetLayoutBounds(label, new Rect(absX - 30, absY - 30, 60, 60));
        }
    }

    private Label CreateStickerLabel()
    {
        var label = new Label
        {
            BackgroundColor = Colors.Transparent,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        AbsoluteLayout.SetLayoutFlags(label, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);

        // Geste de sélection
        var selectTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
        selectTap.Tapped += (s, e) => {
            if (s is Label l && l.BindingContext is Annotation ann)
            {
                _selectedAnnotation = ann;
                RenderAnnotations(); // Mettre à jour l'opacité
            }
        };
        label.GestureRecognizers.Add(selectTap);

        // Geste de suppression rapide (double tap)
        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += async (s, e) => {
            if (s is Label l && l.BindingContext is Annotation ann)
            {
                await _databaseService.DeleteAnnotationAsync(ann);
                _annotations.Remove(ann);
                if (_selectedAnnotation == ann) _selectedAnnotation = null;
                RenderAnnotations();
            }
        };
        label.GestureRecognizers.Add(doubleTap);

        // Déplacement (Pan)
        var pan = new PanGestureRecognizer();
        double startX = 0, startY = 0;
        pan.PanUpdated += async (s, e) => {
            if (s is Label l && l.BindingContext is Annotation ann)
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        startX = l.TranslationX;
                        startY = l.TranslationY;
                        _selectedAnnotation = ann;
                        // On évite de rappeler RenderAnnotations ici si possible, ou on le fait de façon légère
                        l.Opacity = 0.6;
                        break;
                    case GestureStatus.Running:
                        l.TranslationX = startX + e.TotalX;
                        l.TranslationY = startY + e.TotalY;
                        break;
                    case GestureStatus.Completed:
                        double finalAbsX = (ann.X * AnnotationsContainer.Width) + l.TranslationX;
                        double finalAbsY = (ann.Y * AnnotationsContainer.Height) + l.TranslationY;
                        ann.X = finalAbsX / AnnotationsContainer.Width;
                        ann.Y = finalAbsY / AnnotationsContainer.Height;
                        l.TranslationX = 0;
                        l.TranslationY = 0;
                        await _databaseService.SaveAnnotationAsync(ann);
                        RenderAnnotations();
                        break;
                }
            }
        };
        label.GestureRecognizers.Add(pan);

        return label;
    }

    #endregion
}
