using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class SetlistsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private string _currentSort = "NameAsc";

    public SetlistsPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSetlistsAsync();
    }

    private async Task LoadSetlistsAsync(string query = "")
    {
        var setlists = await _databaseService.GetSetlistsAsync();
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            setlists = setlists.Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (_currentSort == "NameAsc") setlists = setlists.OrderBy(s => s.Name).ToList();
        else if (_currentSort == "NameDesc") setlists = setlists.OrderByDescending(s => s.Name).ToList();
        else if (_currentSort == "DateAsc") setlists = setlists.OrderBy(s => s.DateCreated).ToList();
        else if (_currentSort == "DateDesc") setlists = setlists.OrderByDescending(s => s.DateCreated).ToList();

        SetlistsCollectionView.ItemsSource = setlists;
    }

    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadSetlistsAsync(e.NewTextValue);
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync("Trier par", "Annuler", null, 
            "Nom (A-Z)", "Nom (Z-A)", "Date de création (Récent)", "Date de création (Ancien)");

        if (action == "Nom (A-Z)") _currentSort = "NameAsc";
        else if (action == "Nom (Z-A)") _currentSort = "NameDesc";
        else if (action == "Date de création (Récent)") _currentSort = "DateDesc";
        else if (action == "Date de création (Ancien)") _currentSort = "DateAsc";

        if (action != "Annuler")
            await LoadSetlistsAsync(SearchSetlistBar.Text);
    }

    private async void OnAddSetlistClicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("Nouvelle Setlist", "Entrez le nom de la Setlist :");
        if (!string.IsNullOrWhiteSpace(result))
        {
            var newSetlist = new Setlist { Name = result.Trim(), DateCreated = DateTime.Now };
            await _databaseService.SaveSetlistAsync(newSetlist);
            await LoadSetlistsAsync(SearchSetlistBar.Text);
        }
    }

    private async void OnRenameSetlistInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Setlist setlist)
        {
            string result = await DisplayPromptAsync("Renommer", "Entrez le nouveau nom :", initialValue: setlist.Name);
            if (!string.IsNullOrWhiteSpace(result))
            {
                setlist.Name = result.Trim();
                await _databaseService.SaveSetlistAsync(setlist);
                await LoadSetlistsAsync(SearchSetlistBar.Text);
            }
        }
    }

    private async void OnDeleteSetlistInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Setlist setlist)
        {
            bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer '{setlist.Name}' ?", "Oui", "Non");
            if (answer)
            {
                await _databaseService.DeleteSetlistAsync(setlist);
                await LoadSetlistsAsync(SearchSetlistBar.Text);
            }
        }
    }

    private async void OnSetlistSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Setlist selectedSetlist)
        {
            // Future navigation to SetlistDetailPage
            // await Navigation.PushAsync(new SetlistDetailPage(selectedSetlist, _databaseService));
            
            SetlistsCollectionView.SelectedItem = null;
        }
    }
    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}
