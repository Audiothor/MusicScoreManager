using SQLite;

namespace MusicScoreManager.Models;

public enum AnnotationType
{
    Sticker,
    Text,
    Drawing
}

public class Annotation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ScoreId { get; set; }

    public AnnotationType Type { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    // Coordonnées relatives (0.0 à 1.0) par rapport à la taille de la partition
    public double X { get; set; }

    public double Y { get; set; }

    public double Scale { get; set; } = 1.0;

    public string Color { get; set; } = "#FFFFFF";

    public int PageNumber { get; set; } = 1;

    public DateTime DateCreated { get; set; } = DateTime.Now;
}
