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
    
    public DateTime? ConcertDate { get; set; }
    public TimeSpan? ConcertTime { get; set; }
    public SetlistStatus Status { get; set; } = SetlistStatus.Upcoming;
    public bool IsContinuousReading { get; set; } = true;
    public bool IsLocked { get; set; } = false;

    [Ignore]
    public string StatusText => Status.ToString();

    [Ignore]
    public Color StatusColor => Status switch
    {
        SetlistStatus.Upcoming => Color.FromArgb("#007ACC"),
        SetlistStatus.Active => Color.FromArgb("#28A745"),
        SetlistStatus.Done => Color.FromArgb("#6C757D"),
        _ => Colors.Gray
    };
}

public enum SetlistStatus
{
    Upcoming,
    Active,
    Done
}
