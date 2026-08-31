using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class ScoreEditPage : ContentPage
{
    private readonly Score _score;
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private List<int> _selectedTagIds = new List<int>();
    private List<Tag> _allTags = new List<Tag>();
    private readonly List<MusicalKey> _musicalKeyValues = new();
    private int _currentRating = 0;

    public ScoreEditPage(Score score, DatabaseService databaseService)
    {
        InitializeComponent();
        _score = score;
        _databaseService = databaseService;
        _settingsService = new SettingsService();

        // 1. Titre, Compositeur & Chemin
        TitleEntry.Text = _score.Title;
        ComposerEntry.Text = _score.Composer;
        PathEntry.Text = _score.FilePath;
        UpdateFilePathLabel(_score.FilePath);

        PathEntry.TextChanged += (s, e) =>
        {
            UpdateFilePathLabel(e.NewTextValue);
            UpdateExternalIndicators();
        };
        
        // 2. Tempo
        int bpm = _score.BPM > 0 ? _score.BPM : 120;
        BpmEntry.Text = bpm.ToString();
        BpmStepper.Value = Math.Clamp(bpm, 40, 250);

        BpmStepper.ValueChanged += (s, e) => BpmEntry.Text = ((int)e.NewValue).ToString();
        BpmEntry.TextChanged += (s, e) => { 
            if (int.TryParse(e.NewTextValue, out int val)) {
                if (val >= 40 && val <= 250) BpmStepper.Value = val; 
            }
        };

        // 3. Tonalité (Picker Enum avec notations Do-Ré-Mi et A-B-C)
        PopulateMusicalKeyPicker();

        // 4. Évaluation (Étoiles)
        _currentRating = Math.Clamp(_score.Rating, 0, 5);
        UpdateRatingStarsUI();

        // 5. Métronome
        MetronomeSwitch.IsToggled = _score.ShowMetronome;
        MetronomeSoundSwitch.IsToggled = _score.HasMetronomeSound;

        // 6. Audio
        PreCountEntry.Text = _score.PreCountMeasures.ToString();
        PreCountStepper.Value = Math.Clamp(_score.PreCountMeasures, 0, 8);
        PreCountStepper.ValueChanged += (s, e) => PreCountEntry.Text = ((int)e.NewValue).ToString();
        PreCountEntry.TextChanged += (s, e) => { if (int.TryParse(e.NewTextValue, out int val)) PreCountStepper.Value = val; };

        // 7. Tags
        if (_score.AppliedTags != null)
        {
            _selectedTagIds = _score.AppliedTags.Select(t => t.Id).ToList();
        }

        UpdateExternalIndicators();
        LoadFileMetadata();
        LoadDataAsync();
    }

    private void PopulateMusicalKeyPicker()
    {
        KeyPicker.Items.Clear();
        _musicalKeyValues.Clear();

        foreach (MusicalKey key in Enum.GetValues(typeof(MusicalKey)))
        {
            _musicalKeyValues.Add(key);
            KeyPicker.Items.Add(key.ToDisplayName());
        }

        int selectedIndex = _musicalKeyValues.IndexOf(_score.Key);
        KeyPicker.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
    }

    private void OnStarClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string param && int.TryParse(param, out int rating))
        {
            _currentRating = rating;
            UpdateRatingStarsUI();
        }
    }

    private void OnClearRatingClicked(object? sender, EventArgs e)
    {
        _currentRating = 0;
        UpdateRatingStarsUI();
    }

    private void UpdateRatingStarsUI()
    {
        var buttons = new[] { Star1Btn, Star2Btn, Star3Btn, Star4Btn, Star5Btn };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < _currentRating)
            {
                buttons[i].TextColor = Color.FromArgb("#FFD700"); // Or brillant
            }
            else
            {
                buttons[i].TextColor = Color.FromArgb("#444444"); // Éteint
            }
        }

        if (_currentRating == 0)
        {
            RatingTextLabel.Text = "Non notée";
            RatingTextLabel.TextColor = Color.FromArgb("#888888");
        }
        else
        {
            RatingTextLabel.Text = $"{_currentRating} / 5";
            RatingTextLabel.TextColor = Color.FromArgb("#FFD700");
        }
    }

    private void OnToggleMetronomeAccordionTapped(object? sender, EventArgs e)
    {
        bool isCurrentlyVisible = MetronomeAccordionBody.IsVisible;
        MetronomeAccordionBody.IsVisible = !isCurrentlyVisible;
        MetronomeAccordionArrow.Text = !isCurrentlyVisible ? "▼" : "▶";
    }

    private void OnToggleAudioAccordionTapped(object? sender, EventArgs e)
    {
        bool isCurrentlyVisible = AudioAccordionBody.IsVisible;
        AudioAccordionBody.IsVisible = !isCurrentlyVisible;
        AudioAccordionArrow.Text = !isCurrentlyVisible ? "▼" : "▶";
    }

    private void LoadFileMetadata()
    {
        try
        {
            string fullPath = _settingsService.GetAbsolutePath(_score.FilePath);
            
            if (File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                
                long bytes = fileInfo.Length;
                string formattedSize = FormatBytes(bytes);
                FileSizeLabel.Text = formattedSize;
                
                DateTime lastWriteTime = fileInfo.LastWriteTime;
                FileModifiedDateLabel.Text = lastWriteTime.ToString("dd/MM/yyyy HH:mm");
            }
            else
            {
                FileSizeLabel.Text = "Fichier introuvable";
                FileModifiedDateLabel.Text = "N/A";
            }

            DateTime addedDate = _score.DateAdded != default ? _score.DateAdded : DateTime.Now;
            FileCreatedDateLabel.Text = addedDate.ToString("dd/MM/yyyy HH:mm");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScoreEditPage] Erreur lecture métadonnées fichier: {ex.Message}");
            FileSizeLabel.Text = "N/A";
            FileModifiedDateLabel.Text = "N/A";
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F2} Mo";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F1} Ko";
        return $"{bytes} octets";
    }

    private void UpdateExternalIndicators()
    {
        bool isExternal = Path.IsPathRooted(PathEntry.Text);
        ExternalScoreIcon.IsVisible = isExternal;
        ExternalScoreLabel.IsVisible = isExternal;
        ImportScoreButton.IsVisible = isExternal;
    }

    private void UpdateFilePathLabel(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            FilePathTitleLabel.Text = "Chemin du fichier";
            return;
        }

        try
        {
            string fullPath = _settingsService.GetAbsolutePath(filePath);
            string? dir = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                dir = Path.GetDirectoryName(filePath);
            }
            
            FilePathTitleLabel.Text = !string.IsNullOrWhiteSpace(dir) ? $"Chemin du fichier ({dir})" : "Chemin du fichier";
        }
        catch
        {
            FilePathTitleLabel.Text = "Chemin du fichier";
        }
    }

    private async void LoadDataAsync()
    {
        _allTags = await _databaseService.GetTagsAsync();
        RefreshSelectedTagsUI();
        RefreshAllTagsPickerUI();
        RefreshAudioFilesUI();

        // Chargement du statut des annotations
        try
        {
            var annotations = await _databaseService.GetAnnotationsForScoreAsync(_score.Id);
            int count = annotations?.Count ?? 0;
            if (count > 0)
            {
                AnnotationsLabel.Text = $"Oui ({count})";
                AnnotationsLabel.TextColor = Color.FromArgb("#4CAF50");
            }
            else
            {
                AnnotationsLabel.Text = "Aucune";
                AnnotationsLabel.TextColor = Color.FromArgb("#888888");
            }
        }
        catch
        {
            AnnotationsLabel.Text = "Aucune";
            AnnotationsLabel.TextColor = Color.FromArgb("#888888");
        }
    }

    private void RefreshSelectedTagsUI()
    {
        var plusButton = SelectedTagsFlexLayout.Children.LastOrDefault();
        SelectedTagsFlexLayout.Children.Clear();

        var selectedTags = _allTags.Where(t => _selectedTagIds.Contains(t.Id)).ToList();

        foreach (var tag in selectedTags)
        {
            var border = CreateTagBubble(tag, true);
            SelectedTagsFlexLayout.Children.Add(border);
        }

        if (plusButton != null)
            SelectedTagsFlexLayout.Children.Add(plusButton);
    }

    private void RefreshAllTagsPickerUI(string filter = "")
    {
        AllTagsFlexLayout.Children.Clear();
        var filteredTags = string.IsNullOrWhiteSpace(filter) 
            ? _allTags 
            : _allTags.Where(t => t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var tag in filteredTags)
        {
            bool isSelected = _selectedTagIds.Contains(tag.Id);
            var border = CreateTagBubble(tag, isSelected);
            
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                if (_selectedTagIds.Contains(tag.Id))
                    _selectedTagIds.Remove(tag.Id);
                else
                    _selectedTagIds.Add(tag.Id);
                
                RefreshSelectedTagsUI();
                RefreshAllTagsPickerUI(TagSearchBar.Text);
            };
            border.GestureRecognizers.Add(tapGesture);
            
            AllTagsFlexLayout.Children.Add(border);
        }
    }

    private Border CreateTagBubble(Tag tag, bool isSelected)
    {
        return new Border
        {
            BackgroundColor = isSelected ? Color.FromArgb(tag.ColorHex) : Color.FromArgb("#333333"),
            StrokeThickness = 0,
            Padding = new Thickness(15, 8, 10, 8),
            Margin = new Thickness(0, 0, 10, 10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
            Content = new Label
            {
                Text = tag.Name + "  ",
                TextColor = isSelected ? Colors.White : Color.FromArgb("#AAAAAA"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.NoWrap
            }
        };
    }

    private void OnAddTagClicked(object sender, EventArgs e)
    {
        TagPickerOverlay.IsVisible = true;
    }

    private void OnClosePickerClicked(object sender, EventArgs e)
    {
        TagPickerOverlay.IsVisible = false;
    }

    private void OnTagSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshAllTagsPickerUI(e.NewTextValue);
    }

    private void RefreshAudioFilesUI()
    {
        AudioFilesList.Children.Clear();
        int count = _score.AudioFiles.Count;
        AudioAccordionTitleLabel.Text = count > 0 ? $"Fichiers Audio ({count})" : "Fichiers Audio";

        foreach (var af in _score.AudioFiles)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { 
                new ColumnDefinition { Width = GridLength.Auto }, 
                new ColumnDefinition { Width = GridLength.Star }, 
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto } 
            }, ColumnSpacing = 10 };
            
            var checkbox = new CheckBox { IsChecked = af.IsSelected, Color = Color.FromArgb("#007ACC"), VerticalOptions = LayoutOptions.Center };
            checkbox.CheckedChanged += (s, e) => {
                if (e.Value) {
                    foreach (var other in _score.AudioFiles.Where(x => x != af)) other.IsSelected = false;
                    af.IsSelected = true;
                    RefreshAudioFilesUI();
                } else af.IsSelected = false;
            };

            var nameStack = new HorizontalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center };
            if (Path.IsPathRooted(af.FilePath))
            {
                nameStack.Children.Add(new Label { Text = "🔗", TextColor = Color.FromArgb("#007ACC"), VerticalOptions = LayoutOptions.Center });
            }
            nameStack.Children.Add(new Label { Text = af.FileName, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation });

            var actionsStack = new HorizontalStackLayout { Spacing = 5 };

            if (Path.IsPathRooted(af.FilePath))
            {
                var importBtn = new Button { Text = "Rapatrier", FontSize = 10, HeightRequest = 30, Padding = new Thickness(5, 0), BackgroundColor = Color.FromArgb("#007ACC"), TextColor = Colors.White };
                importBtn.Clicked += async (s, e) => await OnImportAudioToLibraryClicked(af);
                actionsStack.Children.Add(importBtn);
            }

            var deleteBtn = new Button { Text = "✕", BackgroundColor = Colors.Transparent, TextColor = Colors.Red, WidthRequest = 40, HeightRequest = 40, VerticalOptions = LayoutOptions.Center };
            deleteBtn.Clicked += (s, e) => {
                _score.AudioFiles.Remove(af);
                RefreshAudioFilesUI();
            };
            actionsStack.Children.Add(deleteBtn);

            grid.Children.Add(checkbox);
            Grid.SetColumn(checkbox, 0);
            grid.Children.Add(nameStack);
            Grid.SetColumn(nameStack, 1);
            grid.Children.Add(actionsStack);
            Grid.SetColumn(actionsStack, 2);

            AudioFilesList.Children.Add(grid);
        }
    }

    private async void OnAddAudioClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.audio" } },
                    { DevicePlatform.Android, new[] { "audio/*" } },
                    { DevicePlatform.WinUI, new[] { ".mp3", ".wav", ".aif", ".aiff" } },
                });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Sélectionnez un fichier audio",
                FileTypes = customFileType
            });

            if (result != null)
            {
                var rootDir = _settingsService.AudioRootDirectory;
                if (!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);

                string finalStoredPath;
                bool isAlreadyInRoot = result.FullPath.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase);

                if (!isAlreadyInRoot)
                {
                    try 
                    {
                        var fileInfoSource = new FileInfo(result.FullPath);
                        long sourceLength = fileInfoSource.Length;
                        string targetFileName = result.FileName;

                        string directPath = Path.Combine(rootDir, targetFileName);
                        if (File.Exists(directPath) && new FileInfo(directPath).Length == sourceLength)
                        {
                            isAlreadyInRoot = true;
                            result = new FileResult(directPath);
                        }
                        
                        if (!isAlreadyInRoot)
                        {
                            string? foundPath = FindFileRecursively(rootDir, targetFileName, sourceLength);
                            if (foundPath != null)
                            {
                                isAlreadyInRoot = true;
                                result = new FileResult(foundPath);
                            }
                        }
                    }
                    catch { /* Ignore */ }
                }

                if (isAlreadyInRoot)
                {
                    finalStoredPath = _settingsService.GetRelativePath(result.FullPath, isAudio: true);
                }
                else
                {
                    string? action = await this.DisplayActionSheetAsync("Fichier audio externe", "Annuler", null, 
                        "Copier vers la bibliothèque Audio", 
                        "Lier le fichier original (Externe)");

                    if (action == "Copier vers la bibliothèque Audio")
                    {
                        var sanitizedFileName = result.FileName.Replace(" ", "_");
                        var localFilePath = Path.Combine(rootDir, sanitizedFileName);
                        
                        if (File.Exists(localFilePath))
                        {
                            var fileNameOnly = Path.GetFileNameWithoutExtension(sanitizedFileName);
                            var extension = Path.GetExtension(sanitizedFileName);
                            localFilePath = Path.Combine(rootDir, $"{fileNameOnly}_{DateTime.Now:yyyyMMddHHmmss}{extension}");
                        }

                        using var stream = await result.OpenReadAsync();
                        using var fileStream = File.Create(localFilePath);
                        await stream.CopyToAsync(fileStream);
                        
                        finalStoredPath = _settingsService.GetRelativePath(localFilePath, isAudio: true);
                    }
                    else if (action == "Lier le fichier original (Externe)")
                    {
                        finalStoredPath = result.FullPath;
                    }
                    else
                    {
                        return;
                    }
                }

                _score.AudioFiles.Add(new ScoreAudioFile
                {
                    ScoreId = _score.Id,
                    FileName = result.FileName,
                    FilePath = finalStoredPath,
                    IsSelected = _score.AudioFiles.Count == 0
                });

                RefreshAudioFilesUI();
            }
        }
        catch (Exception ex)
        {
            await this.DisplayAlertAsync("Erreur", $"Impossible d'importer le fichier : {ex.Message}", "OK");
        }
    }

    private async void OnImportScoreToLibraryClicked(object sender, EventArgs e)
    {
        string currentPath = PathEntry.Text;
        if (!Path.IsPathRooted(currentPath)) return;

        string? newPath = await CopyFileToLibrary(currentPath, false);
        if (newPath != null)
        {
            PathEntry.Text = newPath;
            UpdateExternalIndicators();
            await this.DisplayAlertAsync("Succès", "La partition a été rapatriée dans votre dossier Scores.", "OK");
        }
    }

    private async Task OnImportAudioToLibraryClicked(ScoreAudioFile af)
    {
        if (!Path.IsPathRooted(af.FilePath)) return;

        string? newPath = await CopyFileToLibrary(af.FilePath, true);
        if (newPath != null)
        {
            af.FilePath = newPath;
            RefreshAudioFilesUI();
            await this.DisplayAlertAsync("Succès", "Le fichier audio a été rapatrié dans votre dossier Audio.", "OK");
        }
    }

    private async Task<string?> CopyFileToLibrary(string sourcePath, bool isAudio)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                await this.DisplayAlertAsync("Erreur", "Le fichier original n'est plus accessible.", "OK");
                return null;
            }

            var rootDir = isAudio ? _settingsService.AudioRootDirectory : _settingsService.ScoresRootDirectory;
            if (!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);

            var fileName = Path.GetFileName(sourcePath).Replace(" ", "_");
            var destPath = Path.Combine(rootDir, fileName);

            if (File.Exists(destPath))
            {
                var fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                destPath = Path.Combine(rootDir, $"{fileNameOnly}_{DateTime.Now:yyyyMMddHHmmss}{extension}");
            }

            File.Copy(sourcePath, destPath);
            return _settingsService.GetRelativePath(destPath, isAudio);
        }
        catch (Exception ex)
        {
            await this.DisplayAlertAsync("Erreur", "Échec de la copie : " + ex.Message, "OK");
            return null;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlertAsync("Erreur", "Le titre ne peut pas être vide.", "OK");
            return;
        }

        _score.Title = TitleEntry.Text.Trim();
        _score.Composer = ComposerEntry.Text?.Trim() ?? string.Empty;
        _score.FilePath = PathEntry.Text?.Trim() ?? string.Empty;
        
        // Tempo
        if (int.TryParse(BpmEntry.Text, out int bpm))
        {
            if (bpm < 40 || bpm > 250)
            {
                await this.DisplayAlertAsync("BPM Invalide", "Le tempo doit être compris entre 40 et 250 BPM.", "OK");
                return;
            }
            _score.BPM = bpm;
        }

        // Tonalité
        if (KeyPicker.SelectedIndex >= 0 && KeyPicker.SelectedIndex < _musicalKeyValues.Count)
        {
            _score.Key = _musicalKeyValues[KeyPicker.SelectedIndex];
        }

        // Évaluation
        _score.Rating = _currentRating;
        
        // Métronome
        _score.ShowMetronome = MetronomeSwitch.IsToggled;
        _score.HasMetronomeSound = MetronomeSoundSwitch.IsToggled;
        
        // Audio
        if (int.TryParse(PreCountEntry.Text, out int preCount)) _score.PreCountMeasures = preCount;

        // Sauvegarde de la partition
        await _databaseService.SaveScoreAsync(_score);
        
        // Mise à jour des tags
        await _databaseService.UpdateScoreTagsAsync(_score.Id, _selectedTagIds);

        // Sauvegarde des fichiers audio associés
        var existingAudio = await _databaseService.GetScoreAsync(_score.Id);
        if (existingAudio != null)
        {
            foreach (var af in existingAudio.AudioFiles)
            {
                await _databaseService.DeleteAudioFileAsync(af);
            }
        }

        foreach (var af in _score.AudioFiles)
        {
            af.ScoreId = _score.Id;
            await _databaseService.SaveAudioFileAsync(af);
        }

        await Navigation.PopAsync();
    }

    private string? FindFileRecursively(string dir, string fileName, long size)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                if (Path.GetFileName(file).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (new FileInfo(file).Length == size) return file;
                    }
                    catch { }
                }
            }

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                var found = FindFileRecursively(subDir, fileName, size);
                if (found != null) return found;
            }
        }
        catch { }
        return null;
    }
}
