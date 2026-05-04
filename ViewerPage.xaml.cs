using MusicScoreManager.Models;

namespace MusicScoreManager;

public partial class ViewerPage : ContentPage
{
    private Score _score;
    private readonly List<Score>? _setlistScores;
    private int _currentIndex = -1;
    private bool _isContinuous = true;

    private double currentScale = 1;
    private double startScale = 1;
    private int _currentPage = 1;
    private int _maxPages = 1;
    private int _currentRotation = 0;

    public ViewerPage(Score score, List<Score>? setlistScores = null, int currentIndex = -1, bool isContinuous = true)
    {
        InitializeComponent();
        _score = score;
        _setlistScores = setlistScores;
        _currentIndex = currentIndex;
        _isContinuous = isContinuous;
        Title = _score.Title;

        if (_score.IsRotationSaved)
        {
            _currentRotation = _score.Rotation;
        }

        LoadContentAsync();
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

    private void ShowMenu()
    {
        MenuTitleLabel.Text = _score.Title;
        MenuTypeLabel.Text = _score.Type.ToString();
        CentralMenuOverlay.IsVisible = true;
    }

    private async void OnRotateClicked(object sender, EventArgs e)
    {
        _currentRotation = (_currentRotation + 90) % 360;
        
        if (_score.Type == ScoreType.Image)
        {
            ScoreImage.Rotation = _currentRotation;
        }
        else
        {
            await PdfWebView.EvaluateJavaScriptAsync($"setRotation({_currentRotation})");
        }
    }

    private async void OnGoToPageClicked(object sender, EventArgs e)
    {
        CentralMenuOverlay.IsVisible = false;
        string result = await DisplayPromptAsync("Navigation", $"Entrez un numéro de page (1 - {_maxPages}) :", keyboard: Keyboard.Numeric);
        if (int.TryParse(result, out int pageNum))
        {
            if (pageNum >= 1 && pageNum <= _maxPages)
            {
                if (_score.Type == ScoreType.PDF)
                {
                    await PdfWebView.EvaluateJavaScriptAsync($"goToPage({pageNum})");
                }
            }
        }
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
