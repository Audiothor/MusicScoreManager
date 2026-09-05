using MusicScoreManager.Models;
using MusicScoreManager.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MusicScoreManager;

public class StickerCategorySelectionItem : INotifyPropertyChanged
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}

public partial class SettingsAnnotationsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private ObservableCollection<FavoriteSticker> _favorites = new();
    private ObservableCollection<StickerCategorySelectionItem> _categories = new();
    private bool _isInitializingCategories = false;

    public SettingsAnnotationsPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _settingsService = new SettingsService();
        LoadFavorites();
        LoadCategories();
    }

    private async void LoadFavorites()
    {
        var favs = await _databaseService.GetFavoriteStickersAsync();
        _favorites = new ObservableCollection<FavoriteSticker>(favs);
        FavoritesCollection.ItemsSource = _favorites;
    }

    private void LoadCategories()
    {
        _isInitializingCategories = true;
        try
        {
            var active = _settingsService.ActiveStickerCategories;
            var list = new List<StickerCategorySelectionItem>
            {
                new() { Name = "Favoris", DisplayName = "⭐ Favoris (personnalisés)", IsSelected = active.Contains("Favoris", StringComparer.OrdinalIgnoreCase) },
                new() { Name = "Doigtés", DisplayName = "✋ Doigtés (1, 2, 3, 4, 5...)", IsSelected = active.Contains("Doigtés", StringComparer.OrdinalIgnoreCase) },
                new() { Name = "Notes", DisplayName = "🎵 Notes (𝅝, 𝅗𝅥, ♩, ♪, ♫...)", IsSelected = active.Contains("Notes", StringComparer.OrdinalIgnoreCase) },
                new() { Name = "Silences", DisplayName = "𝄽 Silences (pause, soupir...)", IsSelected = active.Contains("Silences", StringComparer.OrdinalIgnoreCase) },
                new() { Name = "Altérations", DisplayName = "♯ Altérations (dièse, bémol, bécarre...)", IsSelected = active.Contains("Altérations", StringComparer.OrdinalIgnoreCase) },
                new() { Name = "Rythme", DisplayName = "⏱️ Rythme (tempo, rall, accel...)", IsSelected = active.Contains("Rythme", StringComparer.OrdinalIgnoreCase) },
                new() { Name = "Nuances", DisplayName = "𝆑 Nuances (p, mp, mf, f, ff...)", IsSelected = active.Contains("Nuances", StringComparer.OrdinalIgnoreCase) },
                new() { Name = "Structure", DisplayName = "𝄆 Structure (A, B, Coda, Segno...)", IsSelected = active.Contains("Structure", StringComparer.OrdinalIgnoreCase) },
                new() { Name = "Technique", DisplayName = "🎻 Technique (Arco, Pizz, Ped...)", IsSelected = active.Contains("Technique", StringComparer.OrdinalIgnoreCase) },
                new() { Name = "Travail", DisplayName = "🎯 Travail (!, ?, À travailler, Justesse...)", IsSelected = active.Contains("Travail", StringComparer.OrdinalIgnoreCase) }
            };

            _categories = new ObservableCollection<StickerCategorySelectionItem>(list);
            CategoriesCollection.ItemsSource = _categories;
        }
        finally
        {
            _isInitializingCategories = false;
        }
    }

    private void OnCategoryRowTapped(object? sender, EventArgs e)
    {
        if (sender is VisualElement ve && ve.BindingContext is StickerCategorySelectionItem item)
        {
            item.IsSelected = !item.IsSelected;
            SaveActiveCategories();
        }
    }

    private void OnCategoryCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_isInitializingCategories) return;
        SaveActiveCategories();
    }

    private void SaveActiveCategories()
    {
        var selected = _categories.Where(c => c.IsSelected).Select(c => c.Name).ToList();
        // S'assurer qu'au moins une catégorie reste active
        if (selected.Count == 0 && _categories.Count > 0)
        {
            _categories[0].IsSelected = true;
            selected.Add(_categories[0].Name);
        }
        _settingsService.ActiveStickerCategories = selected;
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
