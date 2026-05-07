using MusicScoreManager.Models;
using MusicScoreManager.Services;
using System.Collections.ObjectModel;

namespace MusicScoreManager;

public partial class SettingsAnnotationsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private ObservableCollection<FavoriteSticker> _favorites = new();

    public SettingsAnnotationsPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        LoadFavorites();
    }

    private async void LoadFavorites()
    {
        var favs = await _databaseService.GetFavoriteStickersAsync();
        _favorites = new ObservableCollection<FavoriteSticker>(favs);
        FavoritesCollection.ItemsSource = _favorites;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnAddFavoriteClicked(object sender, EventArgs e)
    {
        string text = FavoriteEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(text)) return;

        var fav = new FavoriteSticker { Text = text };
        await _databaseService.SaveFavoriteStickerAsync(fav);
        
        _favorites.Add(fav);
        FavoriteEntry.Text = "";
    }

    private async void OnDeleteFavoriteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is FavoriteSticker fav)
        {
            bool confirm = await DisplayAlertAsync("Supprimer", $"Supprimer le sticker '{fav.Text}' ?", "Oui", "Non");
            if (confirm)
            {
                await _databaseService.DeleteFavoriteStickerAsync(fav);
                _favorites.Remove(fav);
            }
        }
    }
}
