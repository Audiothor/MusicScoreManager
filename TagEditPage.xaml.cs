using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class TagEditPage : ContentPage
{
    private readonly Tag _tag;
    private readonly DatabaseService _databaseService;

    private readonly string[] _colors = new[]
    {
        "#007ACC", // Bleu
        "#D35400", // Orange
        "#27AE60", // Vert
        "#8E44AD", // Violet
        "#C0392B", // Rouge
        "#F39C12", // Jaune
        "#16A085", // Turquoise
        "#2C3E50"  // Sombre
    };

    public TagEditPage(Tag tag, DatabaseService databaseService)
    {
        InitializeComponent();
        _tag = tag;
        _databaseService = databaseService;

        NameEntry.Text = _tag.Name;
        LoadColors();
    }

    private void LoadColors()
    {
        foreach (var colorHex in _colors)
        {
            var border = new Border
            {
                Stroke = colorHex == _tag.ColorHex ? Colors.White : Colors.Transparent,
                StrokeThickness = 2,
                BackgroundColor = Color.FromArgb(colorHex),
                WidthRequest = 50,
                HeightRequest = 50,
                Margin = new Thickness(5),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 25 }
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                _tag.ColorHex = colorHex;
                // Rafraîchir l'affichage des bordures
                foreach (Border b in ColorsFlexLayout.Children)
                {
                    b.Stroke = Colors.Transparent;
                }
                border.Stroke = Colors.White;
            };
            border.GestureRecognizers.Add(tapGesture);

            ColorsFlexLayout.Children.Add(border);
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlert("Erreur", "Le nom ne peut pas être vide.", "OK");
            return;
        }

        _tag.Name = NameEntry.Text.Trim();
        await _databaseService.SaveTagAsync(_tag);
        await Navigation.PopAsync();
    }
}
