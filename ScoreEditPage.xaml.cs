using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class ScoreEditPage : ContentPage
{
    private readonly Score _score;
    private readonly DatabaseService _databaseService;
    private List<int> _selectedTagIds = new List<int>();

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
        var allTags = await _databaseService.GetTagsAsync();
        TagsFlexLayout.Children.Clear();

        foreach (var tag in allTags)
        {
            bool isSelected = _selectedTagIds.Contains(tag.Id);

            var border = new Border
            {
                BackgroundColor = isSelected ? Color.FromArgb(tag.ColorHex) : Color.FromArgb("#333333"),
                StrokeThickness = 0,
                Padding = new Thickness(15, 8),
                Margin = new Thickness(0, 0, 10, 10),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 }
            };

            var label = new Label
            {
                Text = tag.Name,
                TextColor = isSelected ? Colors.White : Color.FromArgb("#AAAAAA"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            
            border.Content = label;

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                if (_selectedTagIds.Contains(tag.Id))
                {
                    _selectedTagIds.Remove(tag.Id);
                    border.BackgroundColor = Color.FromArgb("#333333");
                    label.TextColor = Color.FromArgb("#AAAAAA");
                }
                else
                {
                    _selectedTagIds.Add(tag.Id);
                    border.BackgroundColor = Color.FromArgb(tag.ColorHex);
                    label.TextColor = Colors.White;
                }
            };
            border.GestureRecognizers.Add(tapGesture);

            TagsFlexLayout.Children.Add(border);
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

        // On sauvegarde d'abord le score pour être sûr qu'il ait un Id (si nouveau)
        await _databaseService.SaveScoreAsync(_score);
        
        // Puis on met à jour les tags
        await _databaseService.UpdateScoreTagsAsync(_score.Id, _selectedTagIds);

        await Navigation.PopAsync();
    }
}
