using MusicScoreManager.Models;
using MusicScoreManager.Services;
using System.Collections.ObjectModel;

namespace MusicScoreManager;

public partial class SetlistEditPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private Setlist _setlist;
    private ObservableCollection<OrderedScore> _orderedScores = new();
    private bool _isLoaded = false;
    private bool _isLocked = false;

    public SetlistEditPage(Setlist setlist, DatabaseService databaseService)
    {
        InitializeComponent();
        _setlist = setlist;
        _databaseService = databaseService;
        _settingsService = new SettingsService();

        BindingContext = _setlist;
        SetupUI();
    }

    private void SetupUI()
    {
        NameEntry.Text = _setlist.Name;
        if (_setlist.ConcertDate.HasValue)
            ConcertDatePicker.Date = _setlist.ConcertDate.Value;
        if (_setlist.ConcertTime.HasValue)
            ConcertTimePicker.Time = _setlist.ConcertTime.Value;

        ContinuousSwitch.IsToggled = _setlist.IsContinuousReading;
        _isLocked = _setlist.IsLocked;
        UpdateLockUI(false); // Initial load, don't show alert
        PopulateStatusChips();
    }

    private async void OnLockTapped(object sender, EventArgs e)
    {
        _isLocked = !_isLocked;
        UpdateLockUI(true);
    }

    private async void UpdateLockUI(bool showAlert)
    {
        // Mise à jour de l'icône et du bouton de verrouillage
        LockBorder.BackgroundColor = _isLocked ? Color.FromArgb("#DC3545") : Color.FromArgb("#28A745");
        LockBorder.Stroke = _isLocked ? Color.FromArgb("#DC3545") : Color.FromArgb("#28A745");
        LockIconLabel.Text = _isLocked ? "VERROUILLÉ 🔒" : "DÉVERROUILLÉ 🔓";

        // Désactivation des contrôles d'édition
        NameEntry.IsEnabled = !_isLocked;
        ConcertDatePicker.IsEnabled = !_isLocked;
        ConcertTimePicker.IsEnabled = !_isLocked;
        StatusStack.IsEnabled = !_isLocked;
        StatusStack.Opacity = _isLocked ? 0.5 : 1.0;
        ContinuousSwitch.IsEnabled = !_isLocked;
        AddScoreButton.IsVisible = !_isLocked;
        DeleteSetlistButton.IsVisible = !_isLocked;

        // Désactivation du réordonnancement
        ScoresCollectionView.CanReorderItems = !_isLocked;

        // Mise à jour des partitions dans la liste
        foreach (var os in _orderedScores)
        {
            os.IsLocked = _isLocked;
        }

        if (_isLocked && showAlert)
        {
            await DisplayAlertAsync("Mode Lecture", "La setlist est verrouillée. Les modifications sont désactivées.", "OK");
        }
    }

    private void PopulateStatusChips()
    {
        StatusStack.Children.Clear();
        foreach (SetlistStatus status in Enum.GetValues(typeof(SetlistStatus)))
        {
            var chip = CreateStatusChip(status);
            StatusStack.Children.Add(chip);
        }
    }

    private View CreateStatusChip(SetlistStatus status)
    {
        bool isSelected = _setlist.Status == status;
        string colorHex = status switch
        {
            SetlistStatus.Upcoming => "#007ACC",
            SetlistStatus.Active => "#28A745",
            SetlistStatus.Done => "#6C757D",
            _ => "#333333"
        };

        var border = new Border
        {
            BackgroundColor = isSelected ? Color.FromArgb(colorHex) : Color.FromArgb("#1E1E1E"),
            Stroke = Color.FromArgb(colorHex),
            StrokeThickness = 1,
            Padding = new Thickness(15, 5),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 }
        };

        border.Content = new Label
        {
            Text = status.ToString(),
            TextColor = isSelected ? Colors.White : Color.FromArgb(colorHex),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) =>
        {
            _setlist.Status = status;
            PopulateStatusChips();
        };
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    private async void OnScoreRowTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is OrderedScore orderedScore)
        {
            if (orderedScore.Score.IsFileMissing)
            {
                await DisplayAlertAsync("Fichier manquant", $"Le fichier de la partition '{orderedScore.Score.Title}' est introuvable. Veuillez vérifier son emplacement dans les paramètres.", "OK");
                return;
            }

            var allScores = _orderedScores.Select(os => os.Score).ToList();
            int index = allScores.IndexOf(orderedScore.Score);
            
            await Navigation.PushAsync(new ViewerPage(
                orderedScore.Score, 
                allScores, 
                index, 
                ContinuousSwitch.IsToggled));
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_isLoaded)
        {
            await LoadScoresAsync();
            _isLoaded = true;
        }
    }

    private async Task LoadScoresAsync()
    {
        var scores = await _databaseService.GetScoresForSetlistAsync(_setlist.Id);
        _orderedScores = new ObservableCollection<OrderedScore>(
            scores.Select((s, index) => new OrderedScore { Score = s, DisplayOrder = index + 1, IsLocked = _isLocked }));
        ScoresCollectionView.ItemsSource = _orderedScores;
        
        // Vérification ultra-rapide des partitions
        _ = PrepareSetlistStatusAsync(scores);
    }

    private async Task PrepareSetlistStatusAsync(List<Score> scores)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() => {
                CacheStatusIcon.Text = "⚡";
                CacheStatusText.Text = "Prêt";
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Setlist status error: {ex.Message}");
        }
    }

    private void UpdateOrderNumbers()
    {
        for (int i = 0; i < _orderedScores.Count; i++)
        {
            _orderedScores[i].DisplayOrder = i + 1;
        }
        _ = PrepareSetlistStatusAsync(_orderedScores.Select(os => os.Score).ToList());
    }

    private void OnReorderCompleted(object sender, EventArgs e)
    {
        UpdateOrderNumbers();
    }

    private async void OnAddScoreClicked(object sender, EventArgs e)
    {
        var selectionPage = new ScoreSelectionPage(_databaseService);
        await Navigation.PushModalAsync(selectionPage);
        
        var selectedScores = await selectionPage.SelectionTask.Task;
        bool added = false;
        foreach (var score in selectedScores)
        {
            if (!_orderedScores.Any(os => os.Score.Id == score.Id))
            {
                _orderedScores.Add(new OrderedScore { Score = score, DisplayOrder = _orderedScores.Count + 1 });
                added = true;
            }
        }

        if (added)
        {
            _ = PrepareSetlistStatusAsync(_orderedScores.Select(os => os.Score).ToList());
        }
    }

    private void OnRemoveScoreClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is OrderedScore orderedScore)
        {
            _orderedScores.Remove(orderedScore);
            UpdateOrderNumbers();
            _ = PrepareSetlistStatusAsync(_orderedScores.Select(os => os.Score).ToList());
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("Erreur", "Le nom de la setlist est obligatoire.", "OK");
            return;
        }

        _setlist.Name = NameEntry.Text.Trim();
        _setlist.ConcertDate = ConcertDatePicker.Date;
        _setlist.ConcertTime = ConcertTimePicker.Time;
        _setlist.IsContinuousReading = ContinuousSwitch.IsToggled;
        _setlist.IsLocked = _isLocked;

        await _databaseService.SaveSetlistAsync(_setlist);
        await _databaseService.UpdateSetlistScoresAsync(_setlist.Id, _orderedScores.Select(os => os.Score.Id).ToList());

        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnDeleteSetlistClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlertAsync("Supprimer", $"Voulez-vous vraiment supprimer la setlist '{_setlist.Name}' ?", "Oui", "Non");
        if (answer)
        {
            await _databaseService.DeleteSetlistAsync(_setlist);
            await Navigation.PopAsync();
        }
    }
}

public class OrderedScore : System.ComponentModel.INotifyPropertyChanged
{
    private int _displayOrder;
    private bool _isLocked;
    public Score Score { get; set; } = null!;
    public int DisplayOrder 
    { 
        get => _displayOrder; 
        set { _displayOrder = value; OnPropertyChanged(); } 
    }
    public bool IsLocked
    {
        get => _isLocked;
        set { _isLocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLocked)); }
    }
    public bool IsNotLocked => !IsLocked;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
}
