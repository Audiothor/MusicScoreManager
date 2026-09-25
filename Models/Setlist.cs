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
    public string StatusText => Status switch
    {
        SetlistStatus.Upcoming => Services.LocalizationService.Instance.GetString("Setlist_Status_Upcoming", "À venir"),
        SetlistStatus.Active => Services.LocalizationService.Instance.GetString("Setlist_Status_Active", "En cours"),
        SetlistStatus.Done => Services.LocalizationService.Instance.GetString("Setlist_Status_Done", "Terminée"),
        _ => Status.ToString()
    };

    [Ignore]
    public Color StatusColor => Status switch
    {
        SetlistStatus.Upcoming => Color.FromArgb("#007ACC"),
        SetlistStatus.Active => Color.FromArgb("#28A745"),
        SetlistStatus.Done => Color.FromArgb("#6C757D"),
        _ => Colors.Gray
    };

    [Ignore]
    public string SubtitleInfo
    {
        get
        {
            var parts = new List<string>();
            if (DateCreated != default)
            {
                parts.Add(string.Format(Services.LocalizationService.Instance.GetString("Setlist_Created_On", "Créée le {0:dd/MM/yyyy}"), DateCreated));
            }
            if (ConcertDate.HasValue)
            {
                string concertFormat = ConcertTime.HasValue
                    ? Services.LocalizationService.Instance.GetString("Setlist_Concert_Date_Time", "Concert : {0:dd/MM/yyyy} à {1:hh\\:mm}")
                    : Services.LocalizationService.Instance.GetString("Setlist_Concert_Date", "Concert : {0:dd/MM/yyyy}");
                
                parts.Add(ConcertTime.HasValue 
                    ? string.Format(concertFormat, ConcertDate.Value, ConcertTime.Value) 
                    : string.Format(concertFormat, ConcertDate.Value));
            }
            return string.Join(" • ", parts);
        }
    }
}

public enum SetlistStatus
{
    Upcoming,
    Active,
    Done
}
