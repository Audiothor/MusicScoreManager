using SQLite;

namespace MusicScoreManager.Models;

public class ScoreBookmark
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ScoreId { get; set; }

    [Indexed]
    public int PageNumber { get; set; } = 1;

    [MaxLength(3)]
    public string Name { get; set; } = string.Empty;

    public DateTime DateCreated { get; set; } = DateTime.Now;
}
