using MusicScoreManager.Models;
using MusicScoreManager.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MusicScoreManager;

public partial class ScoreSelectionPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private List<Score> _allScores = new();
    private List<Tag> _allTags = new();
    private List<SelectableScore> _selectableScores = new();
    private int? _selectedTagId;
    
    public TaskCompletionSource<IEnumerable<Score>> SelectionTask { get; } = new();

    public ScoreSelectionPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _allScores = await _databaseService.GetScoresAsync();
        _allTags = await _databaseService.GetTagsAsync();
        
        // Initialiser la liste des scores sélectionnables
        _selectableScores = _allScores.Select(s => new SelectableScore { Score = s }).ToList();
        
        PopulateTags();
        FilterScores();
    }

    private void PopulateTags()
    {
        TagFiltersStack.Children.Clear();

        // "Tous" chip
        var allChip = CreateTagChip("Tous", null, "#333333");
        TagFiltersStack.Children.Add(allChip);

        foreach (var tag in _allTags)
        {
            var chip = CreateTagChip(tag.Name, tag.Id, tag.ColorHex);
            TagFiltersStack.Children.Add(chip);
        }
    }

    private View CreateTagChip(string text, int? tagId, string colorHex)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb(colorHex),
            Padding = new Thickness(15, 5, 10, 5),
            StrokeThickness = _selectedTagId == tagId ? 2 : 0,
            Stroke = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 15 }
        };

        border.Content = new Label
        {
            Text = text + "  ",
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            LineBreakMode = LineBreakMode.NoWrap
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => {
            _selectedTagId = tagId;
            PopulateTags();
            FilterScores();
        };
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private void FilterScores()
    {
        string query = SearchScoreBar.Text ?? "";
        var filtered = _selectableScores.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(s => s.Score.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (_selectedTagId.HasValue)
        {
            filtered = filtered.Where(s => s.Score.AppliedTags.Any(t => t.Id == _selectedTagId.Value));
        }

        ScoresCollectionView.ItemsSource = filtered.ToList();
    }

    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterScores();
    }

    private async void OnRowTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is SelectableScore selectable)
        {
            if (selectable.Score.IsFileMissing)
            {
                await this.DisplayAlertAsync("Fichier manquant", "Cette partition ne peut pas être ajoutée car son fichier est introuvable.", "OK");
                return;
            }
            selectable.IsSelected = !selectable.IsSelected;
        }
    }

    private async void OnConfirmSelectionClicked(object sender, EventArgs e)
    {
        var selected = _selectableScores.Where(s => s.IsSelected).Select(s => s.Score).ToList();
        SelectionTask.TrySetResult(selected);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        SelectionTask.TrySetResult(Enumerable.Empty<Score>());
        await Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Assurer que la tâche se termine si l'utilisateur utilise le bouton retour du système
        SelectionTask.TrySetResult(Enumerable.Empty<Score>());
    }
}

public class SelectableScore : INotifyPropertyChanged
{
    private bool _isSelected;
    public Score Score { get; set; } = null!;
    public bool IsSelected 
    { 
        get => _isSelected; 
        set { _isSelected = value; OnPropertyChanged(); } 
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
