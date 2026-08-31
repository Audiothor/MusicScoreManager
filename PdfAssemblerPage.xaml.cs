using MusicScoreManager.Models;
using MusicScoreManager.Services;
using System.Collections.ObjectModel;

namespace MusicScoreManager;

public partial class PdfAssemblerPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private readonly PdfService _pdfService;
    private readonly ObservableCollection<PdfPageItem> _pdfPages = new();
    private Score? _editingScore;
    private readonly List<Score> _allScoresForPicker = new();
    private readonly ObservableCollection<Score> _filteredScoresForPicker = new();
    private int _previewingPageIndex = -1;

    public PdfAssemblerPage(DatabaseService? databaseService = null, PdfService? pdfService = null, Score? initialScore = null)
    {
        InitializeComponent();
        _databaseService = databaseService ?? new DatabaseService();
        _settingsService = new SettingsService();
        _pdfService = pdfService ?? new PdfService();

        PdfAssemblerPagesCollectionView.ItemsSource = _pdfPages;
        ScorePickerCollectionView.ItemsSource = _filteredScoresForPicker;

        if (initialScore != null)
        {
            Dispatcher.Dispatch(async () => await LoadScoreIntoAssemblerAsync(initialScore));
        }
    }

    public async Task LoadScoreAsync(Score score)
    {
        await LoadScoreIntoAssemblerAsync(score);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_pdfPages.Any())
        {
            bool leave = await DisplayAlertAsync("Quitter", "Voulez-vous quitter l'atelier d'assemblage ? Les modifications non enregistrées seront perdues.", "Quitter", "Rester");
            if (!leave) return;
        }

        ClearPdfAssemblerData();
        await Navigation.PopAsync();
    }

    private async void OnPdfAssemblerPickImagesClicked(object? sender, EventArgs e)
    {
        _editingScore = null;
        await PickAndAddImagesToAssemblerAsync(clearFirst: true);
    }

    private async void OnPdfAssemblerPickScoreClicked(object? sender, EventArgs e)
    {
        try
        {
            var scores = await _databaseService.GetScoresAsync();
            var pdfScores = scores.Where(s => s.Type == ScoreType.PDF).ToList();

            string subtitlePref = Preferences.Default.Get("ScoreSubtitleDisplay", "DateAdded");
            foreach (var s in pdfScores)
            {
                s.DisplaySubtitle = s.GetSubtitle(subtitlePref);
            }

            _allScoresForPicker.Clear();
            _allScoresForPicker.AddRange(pdfScores.OrderBy(s => s.Title, new NaturalComparer()));

            ScorePickerSearchBar.Text = string.Empty;
            FilterScoresForPicker(string.Empty);

            ScorePickerOverlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Impossible de charger les partitions : {ex.Message}", "OK");
        }
    }

    private void OnScorePickerCloseClicked(object? sender, EventArgs e)
    {
        ScorePickerOverlay.IsVisible = false;
    }

    private void OnScorePickerSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        FilterScoresForPicker(e.NewTextValue ?? string.Empty);
    }

    private void FilterScoresForPicker(string query)
    {
        _filteredScoresForPicker.Clear();
        var trimmed = query.Trim().ToLowerInvariant();

        var matches = string.IsNullOrEmpty(trimmed)
            ? _allScoresForPicker
            : _allScoresForPicker.Where(s =>
                (s.Title?.ToLowerInvariant().Contains(trimmed) ?? false) ||
                (s.Composer?.ToLowerInvariant().Contains(trimmed) ?? false));

        foreach (var s in matches)
        {
            _filteredScoresForPicker.Add(s);
        }
    }

    private async void OnScorePickerScoreSelected(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Score score)
        {
            ScorePickerOverlay.IsVisible = false;
            await LoadScoreIntoAssemblerAsync(score);
        }
    }

    private async void OnScorePickerBrowseFileClicked(object? sender, EventArgs e)
    {
        ScorePickerOverlay.IsVisible = false;
        await PickAndLoadExternalPdfAsync();
    }

    private async void OnPdfAssemblerPickExternalPdfClicked(object? sender, EventArgs e)
    {
        await PickAndLoadExternalPdfAsync();
    }

    private async Task PickAndLoadExternalPdfAsync()
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "com.adobe.pdf" } },
                    { DevicePlatform.Android, new[] { "application/pdf" } },
                    { DevicePlatform.WinUI, new[] { ".pdf" } },
                    { DevicePlatform.MacCatalyst, new[] { "com.adobe.pdf" } },
                });

            var options = new PickOptions
            {
                PickerTitle = "Sélectionnez un document PDF à charger et modifier",
                FileTypes = customFileType,
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result == null || string.IsNullOrEmpty(result.FullPath)) return;

            PdfLoadingLabel.Text = $"⏳ Extraction des pages de '{result.FileName}'...";
            PdfLoadingOverlay.IsVisible = true;

            ClearPdfAssemblerData();
            _editingScore = null;

            var tempPdfPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}_{result.FileName}");
            using (var src = await result.OpenReadAsync())
            using (var dst = File.Create(tempPdfPath))
            {
                await src.CopyToAsync(dst);
            }

            var extracted = await _pdfService.ExtractPdfPagesAsync(tempPdfPath, FileSystem.CacheDirectory);
            try { if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath); } catch { }

            if (!extracted.Any())
            {
                await DisplayAlertAsync("Avertissement", "Aucune page n'a pu être extraite de ce document PDF.", "OK");
                return;
            }

            foreach (var item in extracted)
            {
                _pdfPages.Add(item);
            }

            PdfAssemblerTitleEntry.Text = Path.GetFileNameWithoutExtension(result.FileName);
            UpdatePdfAssemblerUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Impossible d'ouvrir le fichier PDF : {ex.Message}", "OK");
        }
        finally
        {
            PdfLoadingOverlay.IsVisible = false;
        }
    }

    private async Task LoadScoreIntoAssemblerAsync(Score score)
    {
        string fullPath = _settingsService.GetAbsolutePath(score.FilePath);
        if (!File.Exists(fullPath))
        {
            await DisplayAlertAsync("Fichier introuvable", $"Le fichier PDF '{score.Title}' est introuvable sur le stockage.", "OK");
            return;
        }

        PdfLoadingLabel.Text = $"⏳ Extraction des pages de '{score.Title}'...";
        PdfLoadingOverlay.IsVisible = true;

        try
        {
            ClearPdfAssemblerData();

            _editingScore = score;
            var extracted = await _pdfService.ExtractPdfPagesAsync(fullPath, FileSystem.CacheDirectory);

            if (!extracted.Any())
            {
                await DisplayAlertAsync("Avertissement", "Aucune page n'a pu être extraite de ce document PDF.", "OK");
                return;
            }

            foreach (var item in extracted)
            {
                _pdfPages.Add(item);
            }

            PdfAssemblerTitleEntry.Text = score.Title;
            UpdatePdfAssemblerUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Impossible d'extraire les pages : {ex.Message}", "OK");
        }
        finally
        {
            PdfLoadingOverlay.IsVisible = false;
        }
    }

    private async void OnPdfAssemblerAddImagesClicked(object? sender, EventArgs e)
    {
        await PickAndAddImagesToAssemblerAsync(clearFirst: false);
    }

    private async void OnPdfAssemblerAddPdfClicked(object? sender, EventArgs e)
    {
        await PickAndAddPdfToAssemblerAsync();
    }

    private async void OnPdfAssemblerAddBlankPageClicked(object? sender, EventArgs e)
    {
        try
        {
            var blankPage = await _pdfService.CreateBlankPageItemAsync(FileSystem.CacheDirectory, _pdfPages.Count + 1);
            _pdfPages.Add(blankPage);
            RenumberPdfPages();
            UpdatePdfAssemblerUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Impossible d'insérer une page blanche : {ex.Message}", "OK");
        }
    }

    private void OnPdfAssemblerReverseOrderClicked(object? sender, EventArgs e)
    {
        if (_pdfPages.Count <= 1) return;

        var reversed = _pdfPages.Reverse().ToList();
        _pdfPages.Clear();
        foreach (var p in reversed)
        {
            _pdfPages.Add(p);
        }
        RenumberPdfPages();
        UpdatePdfAssemblerUI();
    }

    private void OnPdfAssemblerRotateAllClicked(object? sender, EventArgs e)
    {
        foreach (var p in _pdfPages)
        {
            p.Rotation = (p.Rotation + 90) % 360;
        }
        if (PagePreviewOverlay.IsVisible && _previewingPageIndex >= 0 && _previewingPageIndex < _pdfPages.Count)
        {
            UpdatePreviewUI();
        }
    }

    private async Task PickAndAddPdfToAssemblerAsync()
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "com.adobe.pdf" } },
                    { DevicePlatform.Android, new[] { "application/pdf" } },
                    { DevicePlatform.WinUI, new[] { ".pdf" } },
                    { DevicePlatform.MacCatalyst, new[] { "com.adobe.pdf" } },
                });

            var options = new PickOptions
            {
                PickerTitle = "Sélectionnez un document PDF à ajouter",
                FileTypes = customFileType,
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result == null || string.IsNullOrEmpty(result.FullPath)) return;

            PdfLoadingLabel.Text = "⏳ Extraction des pages du PDF...";
            PdfLoadingOverlay.IsVisible = true;

            var tempPdfPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}_{result.FileName}");
            using (var src = await result.OpenReadAsync())
            using (var dst = File.Create(tempPdfPath))
            {
                await src.CopyToAsync(dst);
            }

            var extracted = await _pdfService.ExtractPdfPagesAsync(tempPdfPath, FileSystem.CacheDirectory);
            try { if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath); } catch { }

            foreach (var item in extracted)
            {
                item.DisplayName = $"{result.FileName} - {item.DisplayName}";
                _pdfPages.Add(item);
            }

            RenumberPdfPages();
            UpdatePdfAssemblerUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Impossible d'ajouter le PDF : {ex.Message}", "OK");
        }
        finally
        {
            PdfLoadingOverlay.IsVisible = false;
        }
    }

    private async Task PickAndAddImagesToAssemblerAsync(bool clearFirst)
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.image" } },
                    { DevicePlatform.Android, new[] { "image/*" } },
                    { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.image" } },
                });

            var options = new PickOptions
            {
                PickerTitle = "Sélectionnez les photos / images de la partition",
                FileTypes = customFileType,
            };

            var results = await FilePicker.Default.PickMultipleAsync(options);
            var validResults = results.Where(r => r != null && !string.IsNullOrEmpty(r.FullPath)).Cast<FileResult>().ToList();
            if (!validResults.Any()) return;

            if (clearFirst)
            {
                ClearPdfAssemblerData();
            }

            var sortedResults = validResults.OrderBy(r => r.FileName ?? r.FullPath, new NaturalComparer()).ToList();

            foreach (var res in sortedResults)
            {
                var tempPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}_{res.FileName}");
                using (var src = await res.OpenReadAsync())
                using (var dst = File.Create(tempPath))
                {
                    await src.CopyToAsync(dst);
                }

                var item = new PdfPageItem
                {
                    ImagePath = tempPath,
                    DisplayName = res.FileName,
                    ThumbnailSource = ImageSource.FromFile(tempPath),
                    PageNumber = _pdfPages.Count + 1,
                    Rotation = 0
                };

                _pdfPages.Add(item);
            }

            if (string.IsNullOrWhiteSpace(PdfAssemblerTitleEntry.Text) && _pdfPages.Any())
            {
                var first = _pdfPages.First().DisplayName;
                var raw = Path.GetFileNameWithoutExtension(first);
                var suggested = System.Text.RegularExpressions.Regex.Replace(raw, @"[_-]?\d+$", "").Trim();
                PdfAssemblerTitleEntry.Text = string.IsNullOrEmpty(suggested) ? raw : suggested;
            }

            UpdatePdfAssemblerUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"Impossible de charger les images : {ex.Message}", "OK");
        }
    }

    private void UpdatePdfAssemblerUI()
    {
        bool hasPages = _pdfPages.Any();
        PdfAssemblerFormSection.IsVisible = hasPages;
        PdfAssemblerPagesCountLabel.Text = $"Pages ({_pdfPages.Count}) :";

        if (_editingScore != null)
        {
            PdfAssemblerEditScoreBanner.IsVisible = true;
            PdfAssemblerEditingScoreLabel.Text = $"Modification de la partition : {_editingScore.Title}";
            PdfAssemblerEditButtonsSection.IsVisible = true;
            PdfAssemblerCreateButtonsSection.IsVisible = false;
        }
        else
        {
            PdfAssemblerEditScoreBanner.IsVisible = false;
            PdfAssemblerEditButtonsSection.IsVisible = false;
            PdfAssemblerCreateButtonsSection.IsVisible = true;
        }
    }

    private void ClearPdfAssemblerData()
    {
        foreach (var p in _pdfPages)
        {
            try { if (!string.IsNullOrEmpty(p.ImagePath) && File.Exists(p.ImagePath)) File.Delete(p.ImagePath); } catch { }
        }
        _pdfPages.Clear();
        _editingScore = null;
        PdfAssemblerTitleEntry.Text = string.Empty;
        UpdatePdfAssemblerUI();
    }

    private async void OnPdfAssemblerClearClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Réinitialiser", "Voulez-vous effacer toutes les pages chargées ?", "Oui", "Non");
        if (confirm)
        {
            ClearPdfAssemblerData();
        }
    }

    private async void OnPdfAssemblerCancelClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Annuler", "Voulez-vous annuler l'assemblage et réinitialiser les modifications ?", "Oui", "Non");
        if (confirm)
        {
            ClearPdfAssemblerData();
        }
    }

    private void OnPdfAssemblerMoveUpClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PdfPageItem item)
        {
            int index = _pdfPages.IndexOf(item);
            if (index > 0)
            {
                _pdfPages.Move(index, index - 1);
                RenumberPdfPages();
            }
        }
    }

    private void OnPdfAssemblerMoveDownClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PdfPageItem item)
        {
            int index = _pdfPages.IndexOf(item);
            if (index >= 0 && index < _pdfPages.Count - 1)
            {
                _pdfPages.Move(index, index + 1);
                RenumberPdfPages();
            }
        }
    }

    private void OnPdfAssemblerRotateClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PdfPageItem item)
        {
            item.Rotation = (item.Rotation + 90) % 360;
            if (PagePreviewOverlay.IsVisible && _previewingPageIndex >= 0 && _previewingPageIndex < _pdfPages.Count && _pdfPages[_previewingPageIndex] == item)
            {
                UpdatePreviewUI();
            }
        }
    }

    private void OnPdfAssemblerDuplicatePageClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PdfPageItem item)
        {
            int index = _pdfPages.IndexOf(item);
            if (index >= 0 && !string.IsNullOrEmpty(item.ImagePath) && File.Exists(item.ImagePath))
            {
                try
                {
                    string copyPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}_copy_{Path.GetFileName(item.ImagePath)}");
                    File.Copy(item.ImagePath, copyPath);

                    var copyItem = new PdfPageItem
                    {
                        ImagePath = copyPath,
                        DisplayName = $"{item.DisplayName} (Copie)",
                        ThumbnailSource = ImageSource.FromFile(copyPath),
                        PageNumber = index + 2,
                        Rotation = item.Rotation
                    };

                    _pdfPages.Insert(index + 1, copyItem);
                    RenumberPdfPages();
                    UpdatePdfAssemblerUI();
                }
                catch (Exception ex)
                {
                    DisplayAlertAsync("Erreur", $"Impossible de dupliquer la page : {ex.Message}", "OK");
                }
            }
        }
    }

    private void OnPdfAssemblerDeletePageClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PdfPageItem item)
        {
            try { if (!string.IsNullOrEmpty(item.ImagePath) && File.Exists(item.ImagePath)) File.Delete(item.ImagePath); } catch { }
            _pdfPages.Remove(item);
            RenumberPdfPages();
            UpdatePdfAssemblerUI();
        }
    }

    private void OnPdfAssemblerPreviewThumbnailTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is PdfPageItem item)
        {
            OpenPagePreview(item);
        }
    }

    private void OnPdfAssemblerPreviewPageClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PdfPageItem item)
        {
            OpenPagePreview(item);
        }
    }

    private void OpenPagePreview(PdfPageItem item)
    {
        int index = _pdfPages.IndexOf(item);
        if (index >= 0)
        {
            _previewingPageIndex = index;
            UpdatePreviewUI();
            PagePreviewOverlay.IsVisible = true;
        }
    }

    private void UpdatePreviewUI()
    {
        if (_previewingPageIndex < 0 || _previewingPageIndex >= _pdfPages.Count)
        {
            PagePreviewOverlay.IsVisible = false;
            return;
        }

        var item = _pdfPages[_previewingPageIndex];
        PagePreviewTitleLabel.Text = $"Page {_previewingPageIndex + 1} sur {_pdfPages.Count}";
        PagePreviewInfoLabel.Text = $"{item.DisplayName} • Rotation : {item.RotationDisplay}";
        PagePreviewImage.Source = item.ThumbnailSource;
        PagePreviewImage.Rotation = item.Rotation;

        PreviewPrevButton.IsEnabled = _previewingPageIndex > 0;
        PreviewNextButton.IsEnabled = _previewingPageIndex < _pdfPages.Count - 1;
    }

    private void OnPreviewPrevPageClicked(object? sender, EventArgs e)
    {
        if (_previewingPageIndex > 0)
        {
            _previewingPageIndex--;
            UpdatePreviewUI();
        }
    }

    private void OnPreviewNextPageClicked(object? sender, EventArgs e)
    {
        if (_previewingPageIndex < _pdfPages.Count - 1)
        {
            _previewingPageIndex++;
            UpdatePreviewUI();
        }
    }

    private void OnPreviewRotatePageClicked(object? sender, EventArgs e)
    {
        if (_previewingPageIndex >= 0 && _previewingPageIndex < _pdfPages.Count)
        {
            var item = _pdfPages[_previewingPageIndex];
            item.Rotation = (item.Rotation + 90) % 360;
            UpdatePreviewUI();
        }
    }

    private void OnPreviewCloseClicked(object? sender, EventArgs e)
    {
        PagePreviewOverlay.IsVisible = false;
        _previewingPageIndex = -1;
    }

    private void RenumberPdfPages()
    {
        for (int i = 0; i < _pdfPages.Count; i++)
        {
            _pdfPages[i].PageNumber = i + 1;
        }
    }

    private async void OnPdfAssemblerSaveOverwriteClicked(object? sender, EventArgs e)
    {
        if (_editingScore == null) return;

        if (!_pdfPages.Any())
        {
            await DisplayAlertAsync("Aucune page", "Veuillez conserver au moins une page.", "OK");
            return;
        }

        string title = PdfAssemblerTitleEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            await DisplayAlertAsync("Titre manquant", "Veuillez entrer un titre pour la partition.", "OK");
            return;
        }

        bool confirm = await DisplayAlertAsync(
            "Remplacer la partition",
            $"Voulez-vous enregistrer les modifications apportées à '{_editingScore.Title}' ({_pdfPages.Count} pages) ?\n\nLe document PDF sera remplacé avec le nouvel ordonnancement des pages.",
            "Remplacer",
            "Annuler");

        if (!confirm) return;

        PdfAssemblerOverwriteButton.IsEnabled = false;
        PdfAssemblerOverwriteButton.Text = "⏳ Enregistrement du PDF...";

        try
        {
            string targetPath = _settingsService.GetAbsolutePath(_editingScore.FilePath);
            string tempOutPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}_assembled.pdf");

            var imagePaths = _pdfPages.Select(p => p.ImagePath!).ToList();
            var rotations = _pdfPages.Select(p => p.Rotation).ToList();

            await _pdfService.ConvertImagesToPdfAsync(imagePaths, tempOutPath, rotations);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Move(tempOutPath, targetPath);

            _editingScore.Title = title;
            _editingScore.DateModified = DateTime.Now;
            await _databaseService.SaveScoreAsync(_editingScore);

            var savedScore = _editingScore;
            int pageCount = _pdfPages.Count;

            ClearPdfAssemblerData();

            bool openNow = await DisplayAlertAsync(
                "Partition modifiée !",
                $"La partition '{title}' a été mise à jour avec succès ({pageCount} pages).\n\nSouhaitez-vous l'ouvrir maintenant ?",
                "Ouvrir",
                "Fermer");

            if (openNow)
            {
                await Navigation.PushAsync(new ViewerPage(savedScore));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"La mise à jour a échoué : {ex.Message}", "OK");
        }
        finally
        {
            PdfAssemblerOverwriteButton.IsEnabled = true;
            PdfAssemblerOverwriteButton.Text = "💾 Enregistrer et remplacer la partition existante";
        }
    }

    private async void OnPdfAssemblerSaveNewClicked(object? sender, EventArgs e)
    {
        await GenerateAndSaveScoreAsync(isNewCopy: true);
    }

    private async void OnPdfAssemblerGenerateClicked(object? sender, EventArgs e)
    {
        await GenerateAndSaveScoreAsync(isNewCopy: false);
    }

    private async Task GenerateAndSaveScoreAsync(bool isNewCopy)
    {
        if (!_pdfPages.Any())
        {
            await DisplayAlertAsync("Aucune page", "Veuillez ajouter au moins une page pour créer la partition.", "OK");
            return;
        }

        string title = PdfAssemblerTitleEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            await DisplayAlertAsync("Titre manquant", "Veuillez entrer un titre pour la partition.", "OK");
            return;
        }

        if (isNewCopy && _editingScore != null && title == _editingScore.Title)
        {
            title = $"{title} (Copie)";
        }

        PdfAssemblerGenerateButton.IsEnabled = false;
        PdfAssemblerGenerateButton.Text = "⏳ Génération du PDF...";

        try
        {
            var rootDir = _settingsService.ScoresRootDirectory;
            if (!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);

            var invalidChars = Path.GetInvalidFileNameChars();
            var cleanTitle = new string(title.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Replace(" ", "_");

            string localPdfPath = Path.Combine(rootDir, $"{cleanTitle}.pdf");
            int counter = 1;
            while (File.Exists(localPdfPath))
            {
                localPdfPath = Path.Combine(rootDir, $"{cleanTitle}_{counter}.pdf");
                counter++;
            }

            var imagePaths = _pdfPages.Select(p => p.ImagePath!).ToList();
            var rotations = _pdfPages.Select(p => p.Rotation).ToList();

            await _pdfService.ConvertImagesToPdfAsync(imagePaths, localPdfPath, rotations);

            var score = new Score
            {
                Title = title,
                FilePath = _settingsService.GetRelativePath(localPdfPath),
                Type = ScoreType.PDF,
                DateAdded = DateTime.Now,
                Composer = _editingScore?.Composer ?? string.Empty,
                BPM = _editingScore?.BPM ?? 120,
                Key = _editingScore?.Key ?? MusicalKey.None,
                Rating = _editingScore?.Rating ?? 0
            };

            await _databaseService.SaveScoreAsync(score);

            int pageCount = _pdfPages.Count;

            ClearPdfAssemblerData();

            bool openNow = await DisplayAlertAsync(
                "Partition créée !",
                $"La partition '{title}' a été créée avec succès ({pageCount} pages) et ajoutée à votre bibliothèque.\n\nSouhaitez-vous l'ouvrir maintenant ?",
                "Ouvrir",
                "Fermer");

            if (openNow)
            {
                await Navigation.PushAsync(new ViewerPage(score));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", $"La génération a échoué : {ex.Message}", "OK");
        }
        finally
        {
            PdfAssemblerGenerateButton.IsEnabled = true;
            PdfAssemblerGenerateButton.Text = "✅ Générer la partition PDF et l'ajouter";
        }
    }

    private class NaturalComparer : IComparer<string>
    {
        public int Compare(string? x, string? y) => NaturalStringComparer.Compare(x, y);
    }
}
