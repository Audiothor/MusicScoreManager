using SQLite;

namespace MusicScoreManager.Models
{
    public class ScoreAudioFile
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int ScoreId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public bool IsSelected { get; set; } = false;
        
        [Ignore]
        public bool IsFileMissing { get; set; }

        [Ignore]
        public bool IsExternal { get; set; }
    }
}
