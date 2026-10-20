using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MusicScoreManager.Services;

namespace MusicScoreManager.Models
{
    public class ScoreCategoryGroup : INotifyPropertyChanged
    {
        private string _name = "Général";
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(_name) ? LocalizationService.Instance.GetString("Score_Category_Default", "Général") : _name;

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExpandIcon));
                    OnPropertyChanged(nameof(FolderIcon));
                }
            }
        }

        public string ExpandIcon => _isExpanded ? "▼" : "▶";
        public string FolderIcon => _isExpanded ? "📂" : "📁";

        private ObservableCollection<Score> _scores = new();
        public ObservableCollection<Score> Scores
        {
            get => _scores;
            set
            {
                if (_scores != value)
                {
                    _scores = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Count));
                    OnPropertyChanged(nameof(FormattedCount));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        private int _scoreCount = 0;
        public int Count
        {
            get => _scoreCount > 0 ? _scoreCount : (_scores?.Count ?? 0);
            set
            {
                if (_scoreCount != value)
                {
                    _scoreCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedCount));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }
        public bool IsEmpty => Count == 0;

        public string FormattedCount
        {
            get
            {
                int c = Count;
                string singleWord = LocalizationService.Instance.GetString("Scores_Count_Single", "partition");
                string pluralWord = LocalizationService.Instance.GetString("Scores_Count_Plural", "partitions");
                return c > 1 ? $"{c} {pluralWord}" : $"{c} {singleWord}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertiesChanged()
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(FormattedCount));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(ExpandIcon));
            OnPropertyChanged(nameof(FolderIcon));
            OnPropertyChanged(nameof(DisplayName));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
