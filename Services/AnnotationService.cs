using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MusicScoreManager.Services;

public class StickerItem : INotifyPropertyChanged
{
    public string Text { get; set; } = string.Empty;
    
    private string _color = "#FFFFFF";
    public string Color
    {
        get => _color;
        set { _color = value; OnPropertyChanged(); }
    }

    private double _scale = 1.0;
    public double Scale
    {
        get => _scale;
        set { _scale = value; OnPropertyChanged(); }
    }

    private string _bgColor = "Transparent";
    public string BackgroundColor
    {
        get => _bgColor;
        set { _bgColor = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class StickerCategory
{
    public string Name { get; set; } = string.Empty;
    public List<StickerItem> Stickers { get; set; } = new();
}

public class AnnotationService
{
    public List<StickerCategory> GetStickerCategories(List<string>? favorites = null)
    {
        var categories = new List<StickerCategory>();

        categories.Add(new StickerCategory
        {
            Name = "Favoris",
            Stickers = (favorites ?? new List<string>()).Select(s => new StickerItem { Text = s }).ToList()
        });

        var predefined = new List<(string Name, List<string> Items)>
        {
            ("Doigtés", new List<string> { "1", "2", "3", "4", "5", "0", "T", "+", "1-2", "2-3", "3-4", "4-5" }),
            ("Rythme", new List<string> { "↑", "↓", "v", "•", "|", "Rall.", "Accel.", "A tempo", "Rit.", "♩ =" }),
            ("Nuances", new List<string> { "pp", "p", "mp", "mf", "f", "ff", "sfz", "<", ">", "Cantabile", "Dolce", "Marcato", "Legato", "Staccato" }),
            ("Structure", new List<string> { "A", "B", "C", "D", "1", "2", "3", "Coda", "Segno", "D.C.", "Fine", "VI-DE", "→", "V.S." }),
            ("Technique", new List<string> { "Π", "V", "Pizz.", "Arco", "Ped.", "*", ",", "//" }),
            ("Travail", new List<string> { "!", "?", "👁", "À travailler", "OK", "Rythme faux", "Justesse", "🔴", "🟢", "🔵" })
        };

        foreach (var p in predefined)
        {
            categories.Add(new StickerCategory
            {
                Name = p.Name,
                Stickers = p.Items.Select(s => new StickerItem { Text = s }).ToList()
            });
        }

        return categories;
    }
}
