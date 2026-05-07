using MusicScoreManager.Models;

namespace MusicScoreManager.Services;

public class StickerCategory
{
    public string Name { get; set; } = string.Empty;
    public List<string> Stickers { get; set; } = new();
}

public class AnnotationService
{
    public List<StickerCategory> GetStickerCategories(List<string>? favorites = null)
    {
        var categories = new List<StickerCategory>();

        // Toujours ajouter la catégorie Favoris (même si vide pour l'instant)
        categories.Add(new StickerCategory
        {
            Name = "Favoris",
            Stickers = favorites ?? new List<string>()
        });

        categories.AddRange(new List<StickerCategory>
        {
            new StickerCategory 
            { 
                Name = "Doigtés", 
                Stickers = new List<string> { "1", "2", "3", "4", "5", "0", "T", "+", "1-2", "2-3", "3-4", "4-5" } 
            },
            new StickerCategory 
            { 
                Name = "Rythme", 
                Stickers = new List<string> { "↑", "↓", "v", "•", "|", "Rall.", "Accel.", "A tempo", "Rit.", "♩ =" } 
            },
            new StickerCategory 
            { 
                Name = "Nuances", 
                Stickers = new List<string> { "pp", "p", "mp", "mf", "f", "ff", "sfz", "<", ">", "Cantabile", "Dolce", "Marcato", "Legato", "Staccato" } 
            },
            new StickerCategory 
            { 
                Name = "Structure", 
                Stickers = new List<string> { "A", "B", "C", "D", "1", "2", "3", "Coda", "Segno", "D.C.", "Fine", "VI-DE", "→", "V.S." } 
            },
            new StickerCategory 
            { 
                Name = "Technique", 
                Stickers = new List<string> { "Π", "V", "Pizz.", "Arco", "Ped.", "*", ",", "//" } 
            },
            new StickerCategory 
            { 
                Name = "Travail", 
                Stickers = new List<string> { "!", "?", "👁", "À travailler", "OK", "Rythme faux", "Justesse", "🔴", "🟢", "🔵" } 
            }
        });

        return categories;
    }
}
