using SQLite;

namespace MusicScoreManager.Models;

[Table("SetlistScores")]
public class SetlistScore
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SetlistId { get; set; }

    [Indexed]
    public int ScoreId { get; set; }

    public int Order { get; set; }
}
