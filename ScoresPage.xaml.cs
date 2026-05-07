using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class ScoresPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ImportService _importService;
    private string _currentSort = "TitleAsc";
    private int? _selectedTagFilterId = null;
    private Models.Score? _selectedScoreForMenu;

    public ScoresPage(DatabaseService databaseService, ImportService importService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _importService = importService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = Task.Run(async () =>
        {
            try 
            {
                // On laisse un peu de temps pour l'animation de transition
                await Task.Delay(50);
                await LoadTagFiltersAsync();
                await LoadScoresAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ScoresPage.OnAppearing: {ex.Message}");
            }
        });
    }

    private async Task LoadTagFiltersAsync()
    {
        var tags = await _databaseService.GetTagsAsync();
        
        MainThread.BeginInvokeOnMainThread(() => {
            TagFiltersStack.Children.Clear();
            foreach (var tag in tags)
            {
                bool isSelected = _selectedTagFilterId == tag.Id;

                var border = new Border
                {
                    BackgroundColor = Color.FromArgb(tag.ColorHex),
                    StrokeThickness = isSelected ? 2 : 0,
                    Stroke = Colors.White,
                    Padding = new Thickness(15, 8),
                    VerticalOptions = LayoutOptions.Center,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 }
                };

                var label = new Label
                {
                    Text = tag.Name,
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };
                
                border.Content = label;

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    if (_selectedTagFilterId == tag.Id)
                        _selectedTagFilterId = null;
                    else
                        _selectedTagFilterId = tag.Id;

                    await LoadTagFiltersAsync();
                    await LoadScoresAsync(SearchScoreBar.Text);
                };
                border.GestureRecognizers.Add(tapGesture);

                TagFiltersStack.Children.Add(border);
            }
        });
    }

    private async Task LoadScoresAsync(string query = "")
    {
        var scores = await _databaseService.SearchScoresAsync(query, _selectedTagFilterId);

        if (_currentSort == "TitleAsc") scores = scores.OrderBy(s => s.Title).ToList();
        else if (_currentSort == "TitleDesc") scores = scores.OrderByDescending(s => s.Title).ToList();
        else if (_currentSort == "DateAsc") scores = scores.OrderBy(s => s.DateAdded).ToList();
        else if (_currentSort == "DateDesc") scores = scores.OrderByDescending(s => s.DateAdded).ToList();

        MainThread.BeginInvokeOnMainThread(() => {
            ScoresCollectionView.ItemsSource = scores;
        });
    }

    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadScoresAsync(e.NewTextValue);
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        var score = await _importService.ImportScoreAsync();
        if (score != null)
        {
            // Recharger la liste après l'import réussi
            await LoadScoresAsync(SearchScoreBar.Text);
        }
    }

    private async void OnScoreSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Models.Score selectedScore)
        {
            if (selectedScore.IsFileMissing)
            {
                await this.DisplayAlertAsync("Fichier manquant", "Le fichier de cette partition est introuvable. Veuillez vérifier votre répertoire de partitions dans les paramètres.", "OK");
                ScoresCollectionView.SelectedItem = null;
                return;
            }

            // Naviguer vers le ViewerPage
            await Navigation.PushAsync(new ViewerPage(selectedScore));
            
            // Désélectionner l'élément
            ScoresCollectionView.SelectedItem = null;
        }
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync("Trier par", "Annuler", null, 
            "Titre (A-Z)", "Titre (Z-A)", "Date d'ajout (Récent)", "Date d'ajout (Ancien)");

        if (action == "Titre (A-Z)") _currentSort = "TitleAsc";
        else if (action == "Titre (Z-A)") _currentSort = "TitleDesc";
        else if (action == "Date d'ajout (Récent)") _currentSort = "DateDesc";
        else if (action == "Date d'ajout (Ancien)") _currentSort = "DateAsc";

        if (action != "Annuler")
            await LoadScoresAsync(SearchScoreBar.Text);
    }

    private void OnScoreOptionsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Models.Score score)
        {
            _selectedScoreForMenu = score;
            ScoreMenuTitleLabel.Text = score.Title;
            ScoreMenuOverlay.IsVisible = true;
        }
    }

    private async void OnMenuEditClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            await Navigation.PushAsync(new ScoreEditPage(_selectedScoreForMenu, _databaseService));
        }
    }

    private async void OnMenuRenameClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            string newTitle = await DisplayPromptAsync("Renommer", "Nouveau titre :", "OK", "Annuler", initialValue: _selectedScoreForMenu.Title);
            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                _selectedScoreForMenu.Title = newTitle.Trim();
                await _databaseService.SaveScoreAsync(_selectedScoreForMenu);
                await LoadScoresAsync(SearchScoreBar.Text);
            }
        }
    }

    private async void OnMenuDeleteClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        if (_selectedScoreForMenu != null)
        {
            bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer '{_selectedScoreForMenu.Title}' ?", "Oui", "Non");
            if (answer)
            {
                await _databaseService.DeleteScoreAsync(_selectedScoreForMenu);
                await LoadScoresAsync(SearchScoreBar.Text);
            }
        }
    }

    private void OnMenuCancelClicked(object sender, EventArgs e)
    {
        ScoreMenuOverlay.IsVisible = false;
        _selectedScoreForMenu = null;
    }
    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}
