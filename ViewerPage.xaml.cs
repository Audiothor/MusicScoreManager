using MusicScoreManager.Models;

namespace MusicScoreManager;

public partial class ViewerPage : ContentPage
{
    private readonly Score _score;
    private double currentScale = 1;
    private double startScale = 1;
    private int _currentPage = 1;
    private int _maxPages = 1;
    private int _currentRotation = 0;

    public ViewerPage(Score score)
    {
        InitializeComponent();
        _score = score;
        Title = score.Title;

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
                    ImageScrollView.IsVisible = true;
                    PdfWebView.IsVisible = false;
                    UpdatePageIndicator();
                }
                else 
                {
                    await DisplayAlert("Erreur", "Fichier introuvable: " + _score.FilePath, "OK");
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

                // Délai réduit pour éviter le clignotement
                await Task.Delay(200);
                await PdfWebView.EvaluateJavaScriptAsync($"loadPdf('{base64}')");
                
                // ImageTouchGrid.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERREUR", ex.Message, "OK");
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
            // On force l'écrasement pour être sûr d'avoir la version sans bug
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur lors de l'injection du PDF : " + ex.Message);
                }
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
            currentScale = Math.Max(1, currentScale); // Empêche de trop dézoomer
            currentScale = Math.Min(4, currentScale); // Empêche de trop zoomer
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
                else
                {
                    // Pour une image, maxPage est tjs 1
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

    // Gestionnaires pour Images
    private void OnPrevTapped(object sender, EventArgs e) { /* Image unique, pas de page préc */ }
    private void OnNextTapped(object sender, EventArgs e) { /* Image unique, pas de page suiv */ }
    private void OnCenterTapped(object sender, EventArgs e) => ShowMenu();
    private void OnBottomTapped(object sender, EventArgs e) { /* Annotations futures */ }

    private void OnExitApplicationClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}
