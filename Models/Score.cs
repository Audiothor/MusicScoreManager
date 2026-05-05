using SQLite;

namespace MusicScoreManager.Models
{
    public enum ScoreType
    {
        PDF,
        Image
    }

    public class Score
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        
        public string FilePath { get; set; } = string.Empty;

        public ScoreType Type { get; set; }

        public DateTime DateAdded { get; set; }

        public int Rotation { get; set; }
        public bool IsRotationSaved { get; set; } = true;

        // Metronome
        public bool ShowMetronome { get; set; } = false;
        public int BPM { get; set; } = 120;
        public bool HasMetronomeSound { get; set; } = true;

        // Audio
        public bool ShowAudioPlayer { get; set; } = false;
        public int PreCountMeasures { get; set; } = 4;

        [Ignore]
        public List<Tag> AppliedTags { get; set; } = new List<Tag>();

        [Ignore]
        public List<ScoreAudioFile> AudioFiles { get; set; } = new List<ScoreAudioFile>();
    }
}
