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
        DeleteButton.IsVisible = _tag.Id != 0;
        
        LoadColors();
        InitializeSliders();
        UpdatePreview();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () =>
        {
            NameEntry.Focus();
        });
    }

    private void InitializeSliders()
    {
        if (!string.IsNullOrEmpty(_tag.ColorHex))
        {
            try
            {
                var color = Color.FromArgb(_tag.ColorHex);
                RedSlider.Value = color.Red * 255;
                GreenSlider.Value = color.Green * 255;
                BlueSlider.Value = color.Blue * 255;
            }
            catch { }
        }
    }

    private void LoadColors()
    {
        foreach (var colorHex in _colors)
        {
            var border = new Border
            {
                StrokeThickness = 2,
                BackgroundColor = Color.FromArgb(colorHex),
                WidthRequest = 40,
                HeightRequest = 40,
                Margin = new Thickness(5),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 }
            };
            
            border.Stroke = colorHex.Equals(_tag.ColorHex, StringComparison.OrdinalIgnoreCase) ? Colors.White : Colors.Transparent;

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                _tag.ColorHex = colorHex;
                
                // Update sliders
                var color = Color.FromArgb(colorHex);
                RedSlider.Value = color.Red * 255;
                GreenSlider.Value = color.Green * 255;
                BlueSlider.Value = color.Blue * 255;

                UpdateSelectionBorders();
                UpdatePreview();
            };
            border.GestureRecognizers.Add(tapGesture);

            ColorsFlexLayout.Children.Add(border);
        }
    }

    private void UpdateSelectionBorders()
    {
        foreach (Border b in ColorsFlexLayout.Children)
        {
            var hex = ((Color)b.BackgroundColor).ToHex();
            // Comparaison simple pour la surbrillance
            b.Stroke = hex.Equals(_tag.ColorHex, StringComparison.OrdinalIgnoreCase) ? Colors.White : Colors.Transparent;
        }
    }

    private void OnSliderValueChanged(object sender, EventArgs e)
    {
        var color = Color.FromRgb((int)RedSlider.Value, (int)GreenSlider.Value, (int)BlueSlider.Value);
        _tag.ColorHex = color.ToHex();
        
        UpdateSelectionBorders();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (!string.IsNullOrEmpty(_tag.ColorHex))
        {
            try
            {
                ColorPreview.BackgroundColor = Color.FromArgb(_tag.ColorHex);
            }
            catch { }
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("Erreur", "Le nom ne peut pas être vide.", "OK");
            return;
        }

        _tag.Name = NameEntry.Text.Trim();
        await _databaseService.SaveTagAsync(_tag);
        await Navigation.PopAsync();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer l'étiquette '{_tag.Name}' ?\nCela la retirera de toutes les partitions.", "Oui", "Non");
        if (answer)
        {
            await _databaseService.DeleteTagAsync(_tag);
            await Navigation.PopAsync();
        }
    }
}
