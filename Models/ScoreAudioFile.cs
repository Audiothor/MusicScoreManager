using SQLite;

namespace MusicScoreManager.Models
{
    public class ScoreAudioFile
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int ScoreId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public bool IsSelected { get; set; } = false;

        /// <summary>Indique s'il s'agit d'une prise enregistrée au micro (privée par défaut lors des partages).</summary>
        public bool IsRecording { get; set; } = false;

        /// <summary>Ordre d'affichage personnalisable dans la liste des fichiers audio de la partition.</summary>
        public int SortOrder { get; set; } = 0;
        
        [Ignore]
        public bool IsFileMissing { get; set; }

        [Ignore]
        public bool IsExternal { get; set; }
    }
}
