using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class ScoreEditPage : ContentPage
{
    private readonly Score _score;
    private readonly DatabaseService _databaseService;
    private List<int> _selectedTagIds = new List<int>();
    private List<Tag> _allTags = new List<Tag>();

    public ScoreEditPage(Score score, DatabaseService databaseService)
    {
        InitializeComponent();
        _score = score;
        _databaseService = databaseService;

        TitleEntry.Text = _score.Title;
        PathEntry.Text = _score.FilePath;
        
        MetronomeSwitch.IsToggled = _score.ShowMetronome;
        HasMetronomeSoundSwitch.IsToggled = _score.HasMetronomeSound;
        BpmEntry.Text = _score.BPM.ToString();
        BpmStepper.Value = _score.BPM;
        PreCountEntry.Text = _score.PreCountMeasures.ToString();
        PreCountStepper.Value = _score.PreCountMeasures;

        BpmStepper.ValueChanged += (s, e) => BpmEntry.Text = ((int)e.NewValue).ToString();
        BpmEntry.TextChanged += (s, e) => { if (int.TryParse(e.NewTextValue, out int val)) BpmStepper.Value = val; };

        PreCountStepper.ValueChanged += (s, e) => PreCountEntry.Text = ((int)e.NewValue).ToString();
        PreCountEntry.TextChanged += (s, e) => { if (int.TryParse(e.NewTextValue, out int val)) PreCountStepper.Value = val; };

        if (_score.AppliedTags != null)
        {
            _selectedTagIds = _score.AppliedTags.Select(t => t.Id).ToList();
        }

        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        _allTags = await _databaseService.GetTagsAsync();
        RefreshSelectedTagsUI();
        RefreshAllTagsPickerUI();
        RefreshAudioFilesUI();
    }

    private void RefreshSelectedTagsUI()
    {
        // On garde le bouton + qui est le dernier enfant
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
            Padding = new Thickness(15, 8),
            Margin = new Thickness(0, 0, 10, 10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
            Content = new Label
            {
                Text = tag.Name,
                TextColor = isSelected ? Colors.White : Color.FromArgb("#AAAAAA"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
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
        foreach (var af in _score.AudioFiles)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } }, ColumnSpacing = 10 };
            
            var checkbox = new CheckBox { IsChecked = af.IsSelected, Color = Color.FromArgb("#007ACC"), VerticalOptions = LayoutOptions.Center };
            checkbox.CheckedChanged += (s, e) =>
            {
                if (e.Value)
                {
                    // Décocher les autres
                    foreach (var other in _score.AudioFiles.Where(x => x != af))
                    {
                        other.IsSelected = false;
                    }
                    af.IsSelected = true;
                    RefreshAudioFilesUI();
                }
                else
                {
                    af.IsSelected = false;
                }
            };

            var label = new Label { Text = af.FileName, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation };
            
            var deleteBtn = new Button { Text = "✕", BackgroundColor = Colors.Transparent, TextColor = Colors.Red, WidthRequest = 40, HeightRequest = 40, VerticalOptions = LayoutOptions.Center };
            deleteBtn.Clicked += (s, e) =>
            {
                _score.AudioFiles.Remove(af);
                RefreshAudioFilesUI();
            };

            grid.Children.Add(checkbox);
            Grid.SetColumn(checkbox, 0);
            grid.Children.Add(label);
            Grid.SetColumn(label, 1);
            grid.Children.Add(deleteBtn);
            Grid.SetColumn(deleteBtn, 2);

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
                var sanitizedFileName = result.FileName.Replace(" ", "_");
                var newFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
                var localFilePath = Path.Combine(FileSystem.AppDataDirectory, newFileName);

                using var stream = await result.OpenReadAsync();
                using var fileStream = File.Create(localFilePath);
                await stream.CopyToAsync(fileStream);

                _score.AudioFiles.Add(new ScoreAudioFile
                {
                    ScoreId = _score.Id,
                    FileName = result.FileName,
                    FilePath = localFilePath,
                    IsSelected = _score.AudioFiles.Count == 0 // Sélectionner si c'est le premier
                });

                RefreshAudioFilesUI();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erreur", "Impossible d'ajouter le fichier audio: " + ex.Message, "OK");
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
        _score.FilePath = PathEntry.Text?.Trim() ?? string.Empty;
        
        _score.ShowMetronome = MetronomeSwitch.IsToggled;
        _score.HasMetronomeSound = HasMetronomeSoundSwitch.IsToggled;
        if (int.TryParse(BpmEntry.Text, out int bpm)) _score.BPM = bpm;
        if (int.TryParse(PreCountEntry.Text, out int preCount)) _score.PreCountMeasures = preCount;

        // On sauvegarde d'abord le score pour être sûr qu'il ait un Id (si nouveau)
        await _databaseService.SaveScoreAsync(_score);
        
        // Puis on met à jour les tags
        await _databaseService.UpdateScoreTagsAsync(_score.Id, _selectedTagIds);

        // Sauvegarde des fichiers audio associés
        // On commence par supprimer les anciens liens en base
        // Note: Dans une version plus propre, on comparerait pour ne pas tout supprimer/réinsérer
        // Mais ici on va faire simple :
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
}
