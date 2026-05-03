using SQLite;
using System;

namespace MusicScoreManager.Models;

[Table("Setlists")]
public class Setlist
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime DateCreated { get; set; } = DateTime.Now;
}
