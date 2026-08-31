using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MusicScoreManager.Models
{
    public enum ScoreType
    {
        PDF,
        Image
    }

    public class Score : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        
        public string Composer { get; set; } = string.Empty;

        public MusicalKey Key { get; set; } = MusicalKey.None;

        public int Rating { get; set; } = 0; // 0 to 5

        public string FilePath { get; set; } = string.Empty;

        public ScoreType Type { get; set; }

        public DateTime DateAdded { get; set; }
        public DateTime DateModified { get; set; } = DateTime.Now;

        public int Rotation { get; set; }
        public bool IsRotationSaved { get; set; } = true;

        // Metronome
        public bool ShowMetronome { get; set; } = false;
        public int BPM { get; set; } = 120;
        public bool HasMetronomeSound { get; set; } = false;

        // Audio
        public bool ShowAudioPlayer { get; set; } = false;
        public int PreCountMeasures { get; set; } = 4;

        [Ignore]
        public bool IsFileMissing { get; set; }

        [Ignore]
        public bool IsExternal { get; set; }

        private bool _isSelected;
        [Ignore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        [Ignore]
        public List<Tag> AppliedTags { get; set; } = new List<Tag>();

        [Ignore]
        public List<ScoreAudioFile> AudioFiles { get; set; } = new List<ScoreAudioFile>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
