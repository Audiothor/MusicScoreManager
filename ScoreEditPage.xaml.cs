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
        
        if (_score.AppliedTags != null)
        {
            _selectedTagIds = _score.AppliedTags.Select(t => t.Id).ToList();
        }

        LoadTagsAsync();
    }

    private async void LoadTagsAsync()
    {
        _allTags = await _databaseService.GetTagsAsync();
        RefreshSelectedTagsUI();
        RefreshAllTagsPickerUI();
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

        // On sauvegarde d'abord le score pour être sûr qu'il ait un Id (si nouveau)
        await _databaseService.SaveScoreAsync(_score);
        
        // Puis on met à jour les tags
        await _databaseService.UpdateScoreTagsAsync(_score.Id, _selectedTagIds);

        await Navigation.PopAsync();
    }
}
