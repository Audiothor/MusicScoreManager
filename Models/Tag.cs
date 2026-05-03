using SQLite;

namespace MusicScoreManager.Models;

[Table("Tags")]
public class Tag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; }

    public string ColorHex { get; set; } = "#007ACC";
}
