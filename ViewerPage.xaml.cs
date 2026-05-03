using MusicScoreManager.Models;

namespace MusicScoreManager;

public partial class ViewerPage : ContentPage
{
    private readonly Score _score;
    private double currentScale = 1;
    private double startScale = 1;

    public ViewerPage(Score score)
    {
        InitializeComponent();
        _score = score;
        Title = score.Title;

        LoadContentAsync();
    }

    private async void LoadContentAsync()
    {
        if (_score.Type == ScoreType.Image)
        {
            ImageScrollView.IsVisible = true;
            ImageBottomMenuZone.IsVisible = true;
            ScoreImage.Source = ImageSource.FromFile(_score.FilePath);
        }
        else if (_score.Type == ScoreType.PDF)
        {
            PdfWebView.IsVisible = true;
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await EnsurePdfJsReadyAsync();
                string viewerPath = Path.Combine(FileSystem.CacheDirectory, "pdfjs", "viewer.html");
                PdfWebView.Source = $"file://{viewerPath}";
            }
            else
            {
                PdfWebView.Source = _score.FilePath;
            }
        }
    }

    private async Task EnsurePdfJsReadyAsync()
    {
        string cacheDir = FileSystem.CacheDirectory;
        string pdfjsDir = Path.Combine(cacheDir, "pdfjs");
        if (!Directory.Exists(pdfjsDir))
        {
            Directory.CreateDirectory(pdfjsDir);
        }

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
                Console.WriteLine($"Error copying {file}: {ex.Message}");
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

    private void OnPdfWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url != null && e.Url.StartsWith("http://musicscore/menu"))
        {
            e.Cancel = true; // Empêche la navigation réelle
            
            int max = 1;
            int current = 1;
            
            try 
            {
                var query = e.Url.Split('?')[1];
                var parts = query.Split('&');
                foreach(var part in parts)
                {
                    if (part.StartsWith("max=")) max = int.Parse(part.Substring(4));
                    if (part.StartsWith("current=")) current = int.Parse(part.Substring(8));
                }
            } 
            catch { }

            ShowMenu(max, current);
        }
    }

    private async void ShowMenu(int maxPages, int currentPage)
    {
        string action = await DisplayActionSheet($"Page {currentPage} / {maxPages}", "Annuler", null, "Aller à une page...", "Retour à l'accueil");
        
        if (action == "Retour à l'accueil")
        {
            await Navigation.PopAsync();
        }
        else if (action == "Aller à une page...")
        {
            string result = await DisplayPromptAsync("Navigation", $"Entrez un numéro de page (1 - {maxPages}) :", keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int pageNum))
            {
                if (pageNum >= 1 && pageNum <= maxPages)
                {
                    await PdfWebView.EvaluateJavaScriptAsync($"goToPage({pageNum})");
                }
            }
        }
    }

    private async void OnBottomMenuTapped(object sender, TappedEventArgs e)
    {
        string action = await DisplayActionSheet("Image", "Annuler", null, "Retour à l'accueil");
        if (action == "Retour à l'accueil")
        {
            await Navigation.PopAsync();
        }
    }
}
