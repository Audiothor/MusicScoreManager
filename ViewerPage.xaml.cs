using MusicScoreManager.Models;
using MusicScoreManager.Services;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Plugin.Maui.Audio;

namespace MusicScoreManager;

public partial class ViewerPage : ContentPage
{
    private Score _score;
    private readonly List<Score>? _setlistScores;
    private int _currentIndex = -1;
    private bool _isContinuous = true;
    private readonly DatabaseService _databaseService;

    private double currentScale = 1;
    private double startScale = 1;
    private int _currentPage = 1;
    private int _maxPages = 1;
    private int _currentRotation = 0;

    private System.Threading.Timer? _metronomeTimer;
    private IAudioPlayer? _metronomeAudioPlayer;
    private bool _isMetronomePlaying = false;
    private bool _isAudioPlaying = false;
    private bool _isDraggingAudioSlider = false;
    private ScoreAudioFile? _selectedAudioFile;

    public ViewerPage(Score score, List<Score>? setlistScores = null, int currentIndex = -1, bool isContinuous = true)
    {
        InitializeComponent();
        _score = score;
        _setlistScores = setlistScores;
        _currentIndex = currentIndex;
        _isContinuous = isContinuous;
        _databaseService = new DatabaseService();
        Title = _score.Title;

        if (_score.IsRotationSaved)
        {
            _currentRotation = _score.Rotation;
        }

        LoadContentAsync();
        SetupMenuUI();
        // Le métronome et l'audio sont initialisés en différé dans OnAppearing
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Optimisation : on laisse l'interface et la partition se charger d'abord
        await Task.Delay(500);
        InitializeMetronome();
        InitializeAudio();
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
    }

    private async void LoadContentAsync()
    {
        try 
        {
            if (_score.Type == ScoreType.Image)
            {
                if (File.Exists(_score.FilePath))
                {
                    ScoreImage.Source = ImageSource.FromFile(_score.FilePath);
                    ScoreImage.Rotation = _currentRotation;
                    ImageScrollView.IsVisible = true;
                    ImageTouchGrid.IsVisible = true;
                    PdfWebView.IsVisible = false;
                    _currentPage = 1;
                    _maxPages = 1;
                    UpdatePageIndicator();
                }
                else 
                {
                    await DisplayAlertAsync("Erreur", "Fichier introuvable: " + _score.FilePath, "OK");
                }
            }
            else if (_score.Type == ScoreType.PDF)
            {
                await EnsurePdfJsReadyAsync();
                
                string pdfjsDir = Path.Combine(FileSystem.CacheDirectory, "pdfjs");
                string viewerPath = Path.Combine(pdfjsDir, "viewer.html");

                var data = await File.ReadAllBytesAsync(_score.FilePath);
                string base64 = Convert.ToBase64String(data);

                PdfWebView.Source = new UrlWebViewSource { Url = $"file://{viewerPath}" };
                PdfWebView.IsVisible = true;
                ImageTouchGrid.IsVisible = false;

                await Task.Delay(200);
                await PdfWebView.EvaluateJavaScriptAsync($"loadPdf('{base64}')");
                
                if (_currentRotation != 0)
                {
                    await PdfWebView.EvaluateJavaScriptAsync($"setRotation({_currentRotation})");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("ERREUR", ex.Message, "OK");
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
        if (_score.Type == ScoreType.PDF && DeviceInfo.Platform == DevicePlatform.Android)
        {
            if (e.Result == WebNavigationResult.Success)
            {
                try
                {
                    byte[] pdfBytes = await File.ReadAllBytesAsync(_score.FilePath);
                    string base64 = Convert.ToBase64String(pdfBytes);
                    await PdfWebView.EvaluateJavaScriptAsync($"loadPdf('{base64}')");
                    if (_currentRotation != 0)
                        await PdfWebView.EvaluateJavaScriptAsync($"setRotation({_currentRotation})");
                }
                catch { }
            }
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
    }

    private void StartMetronome()
    {
        StopMetronome();
        if (_score.BPM <= 0) return;

        int intervalMs = (int)(60000.0 / _score.BPM);
        _metronomeTimer = new System.Threading.Timer(MetronomeTick, null, 0, intervalMs);
        _isMetronomePlaying = true;
    }

    private void MetronomeTick(object? state)
    {
        // Flash visuel sur le thread principal
        MainThread.BeginInvokeOnMainThread(() => {
            MetronomeLight.Color = Colors.Red;
            Task.Delay(100).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(() => MetronomeLight.Color = Color.FromArgb("#333333")));
        });
        
        // Son à faible latence sur un thread d'arrière-plan
        if (_score.HasMetronomeSound && _metronomeAudioPlayer != null)
        {
            _metronomeAudioPlayer.Seek(0);
            _metronomeAudioPlayer.Play();
        }
    }

    private void StopMetronome()
    {
        if (_metronomeTimer != null)
        {
            _metronomeTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            _metronomeTimer.Dispose();
            _metronomeTimer = null;
        }
        _isMetronomePlaying = false;
        MainThread.BeginInvokeOnMainThread(() => MetronomeLight.Color = Color.FromArgb("#333333"));
    }

    private async void OnMetronomeOverlayTapped(object sender, EventArgs e)
    {
        string soundOption = _score.HasMetronomeSound ? "Couper le son" : "Activer le son";
        string action = await DisplayActionSheet("Paramètres du Métronome", "Annuler", null, "Changer le BPM", soundOption);

        if (action == "Changer le BPM")
        {
            string newBpmStr = await DisplayPromptAsync("Nouveau BPM", "Entrez le nouveau tempo :", keyboard: Keyboard.Numeric, initialValue: _score.BPM.ToString());
            if (int.TryParse(newBpmStr, out int newBpm) && newBpm > 0)
            {
                _score.BPM = newBpm;
                MetronomeBpmLabel.Text = $"{_score.BPM} BPM";
                if (_isMetronomePlaying) StartMetronome(); // Redémarre avec le nouvel intervalle
                await _databaseService.SaveScoreAsync(_score);
            }
        }
        else if (action == "Couper le son" || action == "Activer le son")
        {
            _score.HasMetronomeSound = (action == "Activer le son");
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
            AudioPlayer.Source = MediaSource.FromFile(_selectedAudioFile.FilePath);
            AudioPlayer.PositionChanged += OnAudioPositionChanged;
            
            // Écouter PropertyChanged car Duration peut ne pas être prêt immédiatement à MediaOpened
            AudioPlayer.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(CommunityToolkit.Maui.Views.MediaElement.Duration))
                {
                    MainThread.BeginInvokeOnMainThread(() => {
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
            MainThread.BeginInvokeOnMainThread(() => {
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
            // Pré-compte si défini
            if (_score.PreCountMeasures > 0)
            {
                AudioPlayBtn.IsEnabled = false;
                int totalBeeps = _score.PreCountMeasures; // On assume 4 temps par mesure pour l'instant
                for (int i = 0; i < totalBeeps; i++)
                {
                    MetronomeLight.Color = Colors.Yellow;
                    
                    if (_score.HasMetronomeSound && _metronomeAudioPlayer != null)
                    {
                        _metronomeAudioPlayer.Seek(0);
                        _metronomeAudioPlayer.Play();
                    }

                    await Task.Delay(100);
                    MetronomeLight.Color = Color.FromArgb("#333333");
                    await Task.Delay((int)(60000.0 / _score.BPM) - 100);
                }
                AudioPlayBtn.IsEnabled = true;
            }

            AudioPlayer.Play();
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
        if (e.Value) StartMetronome(); else StopMetronome();
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
        if (_setlistScores != null && _currentIndex > 0)
        {
            _currentIndex--;
            var prevScore = _setlistScores[_currentIndex];
            await Navigation.PushAsync(new ViewerPage(prevScore, _setlistScores, _currentIndex, _isContinuous));
            Navigation.RemovePage(this);
        }
    }
}
