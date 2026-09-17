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

        private int _pageCount = 0;
        public int PageCount
        {
            get => _pageCount;
            set
            {
                if (_pageCount != value)
                {
                    _pageCount = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _displaySubtitle = string.Empty;
        [Ignore]
        public string DisplaySubtitle
        {
            get => _displaySubtitle;
            set
            {
                if (_displaySubtitle != value)
                {
                    _displaySubtitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GetSubtitle(string displayOption)
        {
            string dateStr = DateAdded != default ? DateAdded.ToString("dd/MM/yyyy") : "";
            string composerStr = !string.IsNullOrWhiteSpace(Composer) ? Composer.Trim() : "Compositeur non renseigné";
            int pages = PageCount > 0 ? PageCount : 1;
            string pagesStr = pages > 1 ? $"{pages} pages" : "1 page";

            // Rétrocompatibilité avec les anciennes options fixes
            if (displayOption == "Composer")
            {
                return !string.IsNullOrWhiteSpace(Composer) ? composerStr : "Compositeur non renseigné";
            }
            if (displayOption == "DateAdded")
            {
                return dateStr;
            }
            if (displayOption == "ComposerAndDate")
            {
                return !string.IsNullOrWhiteSpace(Composer)
                    ? $"{Composer.Trim()} • {dateStr}"
                    : dateStr;
            }

            // Format dynamique ordonnancé (ex: "Composer,PageCount,DateAdded")
            if (string.IsNullOrWhiteSpace(displayOption)) return string.Empty;

            var tokens = displayOption.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var parts = new List<string>();

            foreach (var token in tokens)
            {
                switch (token)
                {
                    case "Composer":
                        if (!string.IsNullOrWhiteSpace(Composer))
                        {
                            parts.Add(Composer.Trim());
                        }
                        else if (tokens.Length == 1)
                        {
                            parts.Add("Compositeur non renseigné");
                        }
                        break;
                    case "PageCount":
                        parts.Add(pagesStr);
                        break;
                    case "DateAdded":
                        if (!string.IsNullOrWhiteSpace(dateStr))
                        {
                            parts.Add(dateStr);
                        }
                        break;
                }
            }

            return parts.Count > 0 ? string.Join(" • ", parts) : string.Empty;
        }

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
