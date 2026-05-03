using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class ScoresPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ImportService _importService;
    private string _currentSort = "TitleAsc";
    private int? _selectedTagFilterId = null;

    public ScoresPage(DatabaseService databaseService, ImportService importService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _importService = importService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try 
        {
            // Un petit délai laisse le temps au layout de se stabiliser
            await Task.Delay(100);
            await LoadTagFiltersAsync();
            await LoadScoresAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ScoresPage.OnAppearing: {ex.Message}");
        }
    }

    private async Task LoadTagFiltersAsync()
    {
        var tags = await _databaseService.GetTagsAsync();
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
                    _selectedTagFilterId = null; // Désélectionner
                else
                    _selectedTagFilterId = tag.Id; // Sélectionner

                // Recharger l'UI des filtres pour la surbrillance
                await LoadTagFiltersAsync();
                await LoadScoresAsync(SearchScoreBar.Text);
            };
            border.GestureRecognizers.Add(tapGesture);

            TagFiltersStack.Children.Add(border);
        }
    }

    private async Task LoadScoresAsync(string query = "")
    {
        var scores = await _databaseService.SearchScoresAsync(query, _selectedTagFilterId);

        if (_currentSort == "TitleAsc") scores = scores.OrderBy(s => s.Title).ToList();
        else if (_currentSort == "TitleDesc") scores = scores.OrderByDescending(s => s.Title).ToList();
        else if (_currentSort == "DateAsc") scores = scores.OrderBy(s => s.DateAdded).ToList();
        else if (_currentSort == "DateDesc") scores = scores.OrderByDescending(s => s.DateAdded).ToList();

        ScoresCollectionView.ItemsSource = scores;
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

    private async void OnScoreOptionsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Models.Score score)
        {
            string action = await DisplayActionSheetAsync("Options de la partition", "Annuler", "Supprimer", 
                "Éditer les caractéristiques", "Transférer (à venir)");

            if (action == "Éditer les caractéristiques")
            {
                await Navigation.PushAsync(new ScoreEditPage(score, _databaseService));
            }
            else if (action == "Transférer (à venir)")
            {
                await DisplayAlertAsync("Transfert", "Le transfert vers un autre appareil sera disponible dans une prochaine mise à jour.", "OK");
            }
            else if (action == "Supprimer")
            {
                bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer '{score.Title}' ?", "Oui", "Non");
                if (answer)
                {
                    await _databaseService.DeleteScoreAsync(score);
                    await LoadScoresAsync(SearchScoreBar.Text);
                }
            }
        }
    }
    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}
