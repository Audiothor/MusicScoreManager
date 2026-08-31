using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class TagsPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public TagsPage(DatabaseService? databaseService = null)
    {
        InitializeComponent();
        _databaseService = databaseService ?? new DatabaseService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTagsAsync(SearchTagBar?.Text ?? string.Empty);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async Task LoadTagsAsync(string query = "")
    {
        var tags = await _databaseService.GetTagsAsync();
        
        if (!string.IsNullOrWhiteSpace(query))
        {
            tags = tags.Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        TagsCollectionView.ItemsSource = tags.OrderBy(t => t.Name).ToList();
    }

    private async void OnSearchBarTextChanged(object? sender, TextChangedEventArgs e)
    {
        await LoadTagsAsync(e.NewTextValue ?? string.Empty);
    }

    private async void OnAddTagClicked(object? sender, EventArgs e)
    {
        var newTag = new Tag { Name = "" };
        await Navigation.PushAsync(new TagEditPage(newTag, _databaseService));
    }

    private async void OnTagSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Tag tag)
        {
            await Navigation.PushAsync(new TagEditPage(tag, _databaseService));
            TagsCollectionView.SelectedItem = null;
        }
    }

    private async void OnRenameTagInvoked(object? sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Tag tag)
        {
            await Navigation.PushAsync(new TagEditPage(tag, _databaseService));
        }
    }

    private async void OnDeleteTagInvoked(object? sender, EventArgs e)
    {
        if (sender is SwipeItem item && item.CommandParameter is Tag tag)
        {
            bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer l'étiquette '{tag.Name}' ?", "Oui", "Non");
            if (answer)
            {
                await _databaseService.DeleteTagAsync(tag);
                await LoadTagsAsync(SearchTagBar?.Text ?? string.Empty);
            }
        }
    }
}
