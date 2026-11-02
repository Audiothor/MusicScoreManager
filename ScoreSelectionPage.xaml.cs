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
    private string? _selectedCategoryFilter = null;

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

        // Initialiser la liste des scores sélectionnables de manière performante
        _selectableScores = _allScores.Select(s => new SelectableScore
        {
            Score = s,
            OnSelectionChanged = UpdateConfirmButtonText
        }).ToList();

        PopulateTags();
        FilterScores();
        UpdateConfirmButtonText();
    }

    private void PopulateTags()
    {
        TagFiltersStack.Children.Clear();
        var loc = LocalizationService.Instance;

        // "Tous" chip
        bool isAll = _selectedTagIds.Count == 0;
        var allChip = CreateChip(loc.GetString("Selection_All_Chip", "Tous"), () =>
        {
            _selectedTagIds.Clear();
            PopulateTags();
            FilterScores();
        }, isAll ? "#007ACC" : "#333333", isAll);
        TagFiltersStack.Children.Add(allChip);

        // Chip Mode ET / OU si plusieurs tags sélectionnés
        if (_selectedTagIds.Count > 1)
        {
            string modeText = _matchAllTags 
                ? loc.GetString("Selection_Mode_And", "Mode : ET (Toutes) ⇄") 
                : loc.GetString("Selection_Mode_Or", "Mode : OU (Au moins une) ⇄");
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

        if (!string.IsNullOrEmpty(_selectedCategoryFilter))
        {
            filtered = filtered.Where(s => string.Equals(s.Score.Category, _selectedCategoryFilter, StringComparison.OrdinalIgnoreCase));
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

    private async void OnCategoryFilterClicked(object sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        var categories = await _databaseService.GetCategoriesAsync();
        string allFoldersLabel = loc.GetString("Selection_Filter_Folders_All", "📁 Tous les dossiers (Toutes)");
        var options = new List<string> { allFoldersLabel };
        options.AddRange(categories.Select(c => $"📁 {c}"));

        string cancel = loc.GetString("Common_Cancel", "Annuler");
        string action = await DisplayActionSheetAsync(loc.GetString("Selection_Filter_Folders_Title", "Filtrer par dossier / catégorie"), cancel, null, options.ToArray());
        if (string.IsNullOrEmpty(action) || action == cancel) return;

        if (action == allFoldersLabel || action.Contains("Tous les dossiers"))
        {
            _selectedCategoryFilter = null;
            CategoryFilterButton.BackgroundColor = Color.FromArgb("#333333");
            CategoryFilterButton.TextColor = Colors.White;
        }
        else
        {
            string catName = action.Replace("📁", "").Trim();
            _selectedCategoryFilter = catName;
            CategoryFilterButton.BackgroundColor = Color.FromArgb("#007ACC");
            CategoryFilterButton.TextColor = Colors.White;
        }

        FilterScores();
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        var loc = LocalizationService.Instance;
        string optTitleAsc = loc.GetString("Sort_TitleAsc", "Titre (A-Z)");
        string optTitleDesc = loc.GetString("Sort_TitleDesc", "Titre (Z-A)");
        string optDateDesc = loc.GetString("Sort_DateDesc", "Date d'ajout (Récent)");
        string optDateAsc = loc.GetString("Sort_DateAsc", "Date d'ajout (Ancien)");
        string optCompAsc = loc.GetString("Sort_ComposerAsc", "Compositeur (A-Z)");
        string optTagsAsc = loc.GetString("Sort_TagsAsc", "Par étiquettes (A-Z)");
        string optTagMatches = loc.GetString("Sort_TagMatches", "Correspondance d'étiquettes (Pertinence)");

        var options = new List<string>
        {
            optTitleAsc,
            optTitleDesc,
            optDateDesc,
            optDateAsc,
            optCompAsc,
            optTagsAsc
        };

        if (_selectedTagIds.Count > 0)
        {
            options.Add(optTagMatches);
        }

        string cancel = loc.GetString("Common_Cancel", "Annuler");
        string action = await DisplayActionSheetAsync(loc.GetString("Selection_Sort_Title", "Trier par"), cancel, null, options.ToArray());
        if (string.IsNullOrEmpty(action) || action == cancel) return;

        if (action == optTitleAsc) _currentSort = "TitleAsc";
        else if (action == optTitleDesc) _currentSort = "TitleDesc";
        else if (action == optDateDesc) _currentSort = "DateDesc";
        else if (action == optDateAsc) _currentSort = "DateAsc";
        else if (action == optCompAsc) _currentSort = "ComposerAsc";
        else if (action == optTagsAsc) _currentSort = "TagsAsc";
        else if (action == optTagMatches) _currentSort = "TagMatches";
        else _currentSort = "TitleAsc";

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
                var loc = LocalizationService.Instance;
                await DisplayAlertAsync(loc.GetString("Score_Edit_MissingFile_Title"), loc.GetString("Score_Edit_Missing_Msg"), "OK");
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
    public Action? OnSelectionChanged { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                OnSelectionChanged?.Invoke();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
