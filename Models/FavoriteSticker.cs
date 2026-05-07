using SQLite;

namespace MusicScoreManager.Models;

public class FavoriteSticker
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "Transparent";

    public DateTime DateCreated { get; set; } = DateTime.Now;
}
