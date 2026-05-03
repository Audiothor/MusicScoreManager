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

        public string Title { get; set; }

        public string FilePath { get; set; }

        public ScoreType Type { get; set; }

        public DateTime DateAdded { get; set; }

        [Ignore]
        public List<Tag> AppliedTags { get; set; } = new List<Tag>();
    }
}
