using SQLite;

namespace MusicScoreManager.Models;

[Table("ScoreTags")]
public class ScoreTag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ScoreId { get; set; }

    [Indexed]
    public int TagId { get; set; }
}
