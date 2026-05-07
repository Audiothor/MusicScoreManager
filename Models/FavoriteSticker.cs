using SQLite;

namespace MusicScoreManager.Models;

public class FavoriteSticker
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime DateCreated { get; set; } = DateTime.Now;
}
