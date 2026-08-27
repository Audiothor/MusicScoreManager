using SQLite;

namespace MusicScoreManager.Models
{
    public class ScorePageRotation
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int ScoreId { get; set; }

        public int PageNumber { get; set; }

        public int Rotation { get; set; }
    }
}
