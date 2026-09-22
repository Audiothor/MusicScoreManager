using MusicScoreManager.Models;
using MusicScoreManager.Services;
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
    private readonly HashSet<int> _selectedTagIds = new();
    private bool _matchAllTags = false;
    private string _currentSort = "TitleAsc";

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
        _selectableScores = _allScores.Select(s =>
        {
            var item = new SelectableScore { Score = s };
            item.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(SelectableScore.IsSelected))
                {
                    UpdateConfirmButtonText();
                }
            };
            return item;
        }).ToList();

        PopulateTags();
        FilterScores();
        UpdateConfirmButtonText();
    }

    private void PopulateTags()
    {
        TagFiltersStack.Children.Clear();

        // "Tous" chip
        bool isAll = _selectedTagIds.Count == 0;
        var allChip = CreateChip("Tous", () =>
        {
            _selectedTagIds.Clear();
            PopulateTags();
            FilterScores();
        }, isAll ? "#007ACC" : "#333333", isAll);
        TagFiltersStack.Children.Add(allChip);

        // Chip Mode ET / OU si plusieurs tags sélectionnés
        if (_selectedTagIds.Count > 1)
        {
            string modeText = _matchAllTags ? "Mode : ET (Toutes) ⇄" : "Mode : OU (Au moins une) ⇄";
            var modeChip = CreateChip(modeText, () =>
            {
                _matchAllTags = !_matchAllTags;
                PopulateTags();
                FilterScores();
            }, "#444444", true);
            TagFiltersStack.Children.Add(modeChip);
        }

        foreach (var tag in _allTags)
        {
            bool isSelected = _selectedTagIds.Contains(tag.Id);
            string displayText = isSelected ? $"✓ {tag.Name}" : tag.Name;
            var chip = CreateChip(displayText, () =>
            {
                if (_selectedTagIds.Contains(tag.Id))
                    _selectedTagIds.Remove(tag.Id);
                else
                    _selectedTagIds.Add(tag.Id);

                PopulateTags();
                FilterScores();
            }, tag.ColorHex, isSelected);

            TagFiltersStack.Children.Add(chip);
        }
    }

    private View CreateChip(string text, Action onTapped, string colorHex, bool isSelected)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb(colorHex),
            Padding = new Thickness(14, 5, 14, 5),
            StrokeThickness = isSelected ? 2 : 0,
            Stroke = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 15 }
        };

        border.Content = new Label
        {
            Text = text,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            LineBreakMode = LineBreakMode.NoWrap
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => onTapped();
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private void FilterScores()
    {
        string query = SearchScoreBar.Text?.Trim() ?? "";
        var filtered = _selectableScores.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(s =>
                (s.Score.Title != null && s.Score.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (s.Score.Composer != null && s.Score.Composer.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        if (_selectedTagIds.Count > 0)
        {
            if (_matchAllTags)
            {
                filtered = filtered.Where(s => _selectedTagIds.All(id => s.Score.AppliedTags.Any(t => t.Id == id)));
            }
            else
            {
                filtered = filtered.Where(s => _selectedTagIds.Any(id => s.Score.AppliedTags.Any(t => t.Id == id)));
            }
        }

        // Tri
        filtered = _currentSort switch
        {
            "TitleDesc" => filtered.OrderByDescending(s => s.Score.Title),
            "DateDesc" => filtered.OrderByDescending(s => s.Score.DateAdded),
            "DateAsc" => filtered.OrderBy(s => s.Score.DateAdded),
            "ComposerAsc" => filtered.OrderBy(s => string.IsNullOrEmpty(s.Score.Composer) ? "ZZZ" : s.Score.Composer).ThenBy(s => s.Score.Title),
            "TagsAsc" => filtered.OrderBy(s => s.Score.AppliedTags.FirstOrDefault()?.Name ?? "ZZZ").ThenBy(s => s.Score.Title),
            "TagMatches" => filtered.OrderByDescending(s => s.Score.AppliedTags.Count(t => _selectedTagIds.Contains(t.Id))).ThenBy(s => s.Score.Title),
            _ => filtered.OrderBy(s => s.Score.Title) // "TitleAsc"
        };

        ScoresCollectionView.ItemsSource = filtered.ToList();
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        var options = new List<string>
        {
            "Titre (A-Z)",
            "Titre (Z-A)",
            "Date d'ajout (Récent)",
            "Date d'ajout (Ancien)",
            "Compositeur (A-Z)",
            "Par étiquettes (A-Z)"
        };

        if (_selectedTagIds.Count > 0)
        {
            options.Add("Correspondance d'étiquettes (Pertinence)");
        }

        string action = await DisplayActionSheetAsync("Trier par", "Annuler", null, options.ToArray());
        if (string.IsNullOrEmpty(action) || action == "Annuler") return;

        _currentSort = action switch
        {
            "Titre (A-Z)" => "TitleAsc",
            "Titre (Z-A)" => "TitleDesc",
            "Date d'ajout (Récent)" => "DateDesc",
            "Date d'ajout (Ancien)" => "DateAsc",
            "Compositeur (A-Z)" => "ComposerAsc",
            "Par étiquettes (A-Z)" => "TagsAsc",
            "Correspondance d'étiquettes (Pertinence)" => "TagMatches",
            _ => "TitleAsc"
        };

        FilterScores();
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
                await DisplayAlertAsync("Fichier manquant", "Cette partition ne peut pas être ajoutée car son fichier est introuvable.", "OK");
                return;
            }
            selectable.IsSelected = !selectable.IsSelected;
        }
    }

    private void UpdateConfirmButtonText()
    {
        int count = _selectableScores.Count(s => s.IsSelected);
        string addText = LocalizationService.Instance.GetString("Setlist_Edit_Add_Scores", "Ajouter à la Setlist");
        ConfirmButton.Text = count > 0 ? $"{addText} ({count})" : addText;
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
