using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class SetlistsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private string _currentSort = "NameAsc";
    private SetlistStatus? _selectedStatusFilter = null;

    public SetlistsPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadStatusFilters();
        await LoadSetlistsAsync();

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () =>
        {
            SearchSetlistBar.Focus();
        });
    }

    private void LoadStatusFilters()
    {
        StatusFiltersStack.Children.Clear();

        // Chip "Tous"
        StatusFiltersStack.Children.Add(CreateStatusFilterChip("Tous", null));

        foreach (SetlistStatus status in Enum.GetValues(typeof(SetlistStatus)))
        {
            StatusFiltersStack.Children.Add(CreateStatusFilterChip(status.ToString(), status));
        }
    }

    private View CreateStatusFilterChip(string text, SetlistStatus? status)
    {
        bool isSelected = _selectedStatusFilter == status;
        string colorHex = status switch
        {
            SetlistStatus.Upcoming => "#007ACC",
            SetlistStatus.Active => "#28A745",
            SetlistStatus.Done => "#6C757D",
            _ => "#333333"
        };

        var border = new Border
        {
            BackgroundColor = isSelected ? Color.FromArgb(colorHex) : Color.FromArgb("#1E1E1E"),
            Stroke = Color.FromArgb(colorHex),
            StrokeThickness = 1,
            Padding = new Thickness(15, 5),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 }
        };

        border.Content = new Label
        {
            Text = text,
            TextColor = isSelected ? Colors.White : Color.FromArgb(colorHex),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            _selectedStatusFilter = status;
            LoadStatusFilters();
            await LoadSetlistsAsync(SearchSetlistBar.Text);
        };
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private async Task LoadSetlistsAsync(string query = "")
    {
        var setlists = await _databaseService.GetSetlistsAsync();
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            setlists = setlists.Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (_selectedStatusFilter.HasValue)
        {
            setlists = setlists.Where(s => s.Status == _selectedStatusFilter.Value).ToList();
        }

        if (_currentSort == "NameAsc") setlists = setlists.OrderBy(s => s.Name).ToList();
        else if (_currentSort == "NameDesc") setlists = setlists.OrderByDescending(s => s.Name).ToList();
        else if (_currentSort == "DateAsc") setlists = setlists.OrderBy(s => s.DateCreated).ToList();
        else if (_currentSort == "DateDesc") setlists = setlists.OrderByDescending(s => s.DateCreated).ToList();
        else if (_currentSort == "Status") setlists = setlists.OrderBy(s => s.Status).ToList();

        SetlistsCollectionView.ItemsSource = setlists;
    }

    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadSetlistsAsync(e.NewTextValue);
    }

    private async void OnSortClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync("Trier par", "Annuler", null, 
            "Nom (A-Z)", "Nom (Z-A)", "Date de création (Récent)", "Date de création (Ancien)", "Statut");

        if (action == "Nom (A-Z)") _currentSort = "NameAsc";
        else if (action == "Nom (Z-A)") _currentSort = "NameDesc";
        else if (action == "Date de création (Récent)") _currentSort = "DateDesc";
        else if (action == "Date de création (Ancien)") _currentSort = "DateAsc";
        else if (action == "Statut") _currentSort = "Status";

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
            
            // Redirection immédiate vers l'édition
            await Navigation.PushAsync(new SetlistEditPage(newSetlist, _databaseService));
        }
    }

    private async void OnSetlistMenuTapped(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Setlist setlist)
        {
            string action = await DisplayActionSheetAsync(setlist.Name, "Annuler", null, "Éditer", "Renommer", "Supprimer");

            if (action == "Éditer")
            {
                await Navigation.PushAsync(new SetlistEditPage(setlist, _databaseService));
            }
            else if (action == "Renommer")
            {
                await RenameSetlistAsync(setlist);
            }
            else if (action == "Supprimer")
            {
                await DeleteSetlistAsync(setlist);
            }
        }
    }

    private async Task RenameSetlistAsync(Setlist setlist)
    {
        string result = await DisplayPromptAsync("Renommer", "Entrez le nouveau nom :", initialValue: setlist.Name);
        if (!string.IsNullOrWhiteSpace(result))
        {
            setlist.Name = result.Trim();
            await _databaseService.SaveSetlistAsync(setlist);
            await LoadSetlistsAsync(SearchSetlistBar.Text);
        }
    }

    private async Task DeleteSetlistAsync(Setlist setlist)
    {
        bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer '{setlist.Name}' ?", "Oui", "Non");
        if (answer)
        {
            await _databaseService.DeleteSetlistAsync(setlist);
            await LoadSetlistsAsync(SearchSetlistBar.Text);
        }
    }

    private async void OnRenameSetlistInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Setlist setlist)
        {
            await RenameSetlistAsync(setlist);
        }
    }

    private async void OnDeleteSetlistInvoked(object sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Setlist setlist)
        {
            await DeleteSetlistAsync(setlist);
        }
    }

    private async void OnSetlistTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers[0] is TapGestureRecognizer tap && tap.CommandParameter is Setlist selectedSetlist)
        {
            await Navigation.PushAsync(new SetlistEditPage(selectedSetlist, _databaseService));
        }
    }
    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}
