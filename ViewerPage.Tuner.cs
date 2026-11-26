using MusicScoreManager.Models;
using MusicScoreManager.Services;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using System.Linq;
using System.Diagnostics;
using Plugin.Maui.Audio;

namespace MusicScoreManager;

public partial class ViewerPage
{
    #region Tuner (Accordeur)

    private bool _isTunerInitialized = false;
    private TunerInstrument? _currentTunerInstrument;
    private TuningNote? _selectedTunerNote;
    private readonly List<double> _tunerA4Frequencies = new() { 440.0, 442.0, 415.0, 432.0 };
    private bool _isUpdatingTunerPickers = false;
    private int _selectedChromaticNoteIndex = -1; // -1 = Auto
    private int _selectedChromaticOctave = 4;     // Octave par défaut (4)

    private void InitTunerUIIfNeeded()
    {
        if (_isTunerInitialized) return;
        _isTunerInitialized = true;

        TunerService.Instance.PitchDetected += OnTunerPitchDetected;

        if (TunerA4Picker != null)
        {
            var loc = LocalizationService.Instance;
            TunerA4Picker.ItemsSource = new List<string>
            {
                loc.GetString("Tuner_A4_440", "440 Hz (Standard international)"),
                loc.GetString("Tuner_A4_442", "442 Hz (Orchestre symphonique)"),
                loc.GetString("Tuner_A4_415", "415 Hz (Musique baroque)"),
                loc.GetString("Tuner_A4_432", "432 Hz (Fréquence alternative)")
            };
            double currentA4 = TunerService.Instance.A4Frequency;
            int selIdx = _tunerA4Frequencies.FindIndex(f => Math.Abs(f - currentA4) < 0.1);
            TunerA4Picker.SelectedIndex = selIdx >= 0 ? selIdx : 0;
        }

        if (TunerOctavePicker != null)
        {
            var loc = LocalizationService.Instance;
            string octPrefix = loc.GetString("Tuner_Octave", "Octave");
            TunerOctavePicker.ItemsSource = new List<string>
            {
                $"{octPrefix} 1",
                $"{octPrefix} 2",
                $"{octPrefix} 3",
                $"{octPrefix} 4",
                $"{octPrefix} 5",
                $"{octPrefix} 6",
                $"{octPrefix} 7"
            };
            TunerOctavePicker.SelectedIndex = Math.Clamp(_selectedChromaticOctave - 1, 0, 6);
        }

        if (TunerInstrumentPicker != null)
        {
            var loc = LocalizationService.Instance;
            TunerInstrumentPicker.ItemsSource = TunerService.Instance.Instruments.Select(i =>
            {
                string key = $"Tuner_Instrument_{i.Id}";
                string name = loc.GetString(key);
                return (!string.IsNullOrEmpty(name) && name != key) ? name : i.Name;
            }).ToList();
            TunerInstrumentPicker.SelectedIndex = 0;
            _currentTunerInstrument = TunerService.Instance.Instruments[0];
            PopulateTunerNotePicker(_currentTunerInstrument);
        }
    }


    private double _tunerXBase = 0;
    private double _tunerYBase = 0;

    private void OnTunerBoxTapped(object? sender, EventArgs e)
    {
        // Intercepte les clics simples et doubles dans la boîte de l'accordeur
        // pour empêcher l'ouverture intempestive du menu central ou le changement de page
    }

    private void OnCloseTunerClicked(object? sender, EventArgs e)
    {
        if (TunerOverlay != null)
        {
            TunerOverlay.IsVisible = false;
            StopTunerListening();
            UpdateToolsDrawerUI();
        }
    }

    private void OnTunerPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (TunerOverlay == null) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _tunerXBase = TunerOverlay.TranslationX;
                _tunerYBase = TunerOverlay.TranslationY;
                break;
            case GestureStatus.Running:
                double newX = _tunerXBase + e.TotalX;
                double newY = _tunerYBase + e.TotalY;
                if (this.Width > 0 && this.Height > 0)
                {
                    double w = TunerOverlay.Width > 0 ? TunerOverlay.Width : 360;
                    double h = TunerOverlay.Height > 0 ? TunerOverlay.Height : 480;
                    double halfW = Math.Max(0, (this.Width - w) / 2.0);
                    double halfH = Math.Max(0, (this.Height - h) / 2.0);
                    newX = Math.Clamp(newX, -halfW, halfW);
                    newY = Math.Clamp(newY, -halfH, halfH);
                }
                TunerOverlay.TranslationX = newX;
                TunerOverlay.TranslationY = newY;
                break;
        }
    }

    private void OnTunerA4PickerChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingTunerPickers || TunerA4Picker == null || TunerA4Picker.SelectedIndex < 0) return;
        int idx = TunerA4Picker.SelectedIndex;
        if (idx >= 0 && idx < _tunerA4Frequencies.Count)
        {
            double freq = _tunerA4Frequencies[idx];
            TunerService.Instance.A4Frequency = freq;

            if (_selectedTunerNote != null && _currentTunerInstrument != null && _currentTunerInstrument.IsChromatic && _selectedChromaticNoteIndex >= 0)
            {
                _selectedTunerNote = TunerService.CreateChromaticNote(_selectedChromaticNoteIndex, _selectedChromaticOctave, freq);
            }
            UpdateTunerDisplayForSelectedNote();
        }
    }

    private void OnTunerInstrumentChanged(object? sender, EventArgs e)
    {
        if (TunerInstrumentPicker == null || TunerInstrumentPicker.SelectedIndex < 0) return;
        int idx = TunerInstrumentPicker.SelectedIndex;
        if (idx >= 0 && idx < TunerService.Instance.Instruments.Count)
        {
            _currentTunerInstrument = TunerService.Instance.Instruments[idx];
            _selectedTunerNote = null;
            _selectedChromaticNoteIndex = -1;
            PopulateTunerNotePicker(_currentTunerInstrument);
        }
    }

    private void PopulateTunerNotePicker(TunerInstrument instrument)
    {
        if (TunerNotePicker == null) return;
        _isUpdatingTunerPickers = true;

        var loc = LocalizationService.Instance;
        var items = new List<string> { loc.GetString("Tuner_Auto_Detect", "Détection auto") };

        if (instrument.IsChromatic)
        {
            if (TunerOctavesContainer != null) TunerOctavesContainer.IsVisible = true;
            string[] solfeges = {
                loc.GetString("Note_Do", "Do"),
                loc.GetString("Note_Do_Sharp", "Do#"),
                loc.GetString("Note_Re", "Ré"),
                loc.GetString("Note_Re_Sharp", "Ré#"),
                loc.GetString("Note_Mi", "Mi"),
                loc.GetString("Note_Fa", "Fa"),
                loc.GetString("Note_Fa_Sharp", "Fa#"),
                loc.GetString("Note_Sol", "Sol"),
                loc.GetString("Note_Sol_Sharp", "Sol#"),
                loc.GetString("Note_La", "La"),
                loc.GetString("Note_Si_Flat", "Si♭"),
                loc.GetString("Note_Si", "Si")
            };
            string[] anglo = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            for (int i = 0; i < 12; i++)
            {
                items.Add($"{solfeges[i]} ({anglo[i]})");
            }
        }

        else
        {
            if (TunerOctavesContainer != null) TunerOctavesContainer.IsVisible = false;
            foreach (var note in instrument.Notes)
            {
                items.Add($"{note.NoteName} ({note.Solfege})");
            }
        }

        TunerNotePicker.ItemsSource = items;
        TunerNotePicker.SelectedIndex = 0; // "Détection auto" par défaut
        _selectedTunerNote = null;
        _selectedChromaticNoteIndex = -1;

        _isUpdatingTunerPickers = false;
        UpdateTunerDisplayForSelectedNote();
    }

    private void OnTunerNotePickerChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingTunerPickers || TunerNotePicker == null || _currentTunerInstrument == null) return;
        int idx = TunerNotePicker.SelectedIndex;
        if (idx <= 0)
        {
            // Détection auto
            _selectedTunerNote = null;
            _selectedChromaticNoteIndex = -1;
        }
        else
        {
            int noteIdx = idx - 1;
            if (_currentTunerInstrument.IsChromatic)
            {
                _selectedChromaticNoteIndex = noteIdx;
                _selectedTunerNote = TunerService.CreateChromaticNote(noteIdx, _selectedChromaticOctave, TunerService.Instance.A4Frequency);
            }
            else
            {
                if (noteIdx < _currentTunerInstrument.Notes.Count)
                {
                    _selectedTunerNote = _currentTunerInstrument.Notes[noteIdx];
                }
            }
        }

        UpdateTunerDisplayForSelectedNote();
    }

    private void OnTunerOctavePickerChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingTunerPickers || TunerOctavePicker == null || TunerOctavePicker.SelectedIndex < 0) return;
        _selectedChromaticOctave = TunerOctavePicker.SelectedIndex + 1;
        if (_selectedChromaticNoteIndex >= 0 && _currentTunerInstrument != null && _currentTunerInstrument.IsChromatic)
        {
            _selectedTunerNote = TunerService.CreateChromaticNote(_selectedChromaticNoteIndex, _selectedChromaticOctave, TunerService.Instance.A4Frequency);
        }
        UpdateTunerDisplayForSelectedNote();
    }

    private void UpdateTunerDisplayForSelectedNote()
    {
        var loc = LocalizationService.Instance;
        if (_selectedTunerNote != null)
        {
            if (TunerNoteLabel != null) TunerNoteLabel.Text = _selectedTunerNote.NoteName;
            if (TunerSolfegeLabel != null) TunerSolfegeLabel.Text = string.Format(loc.GetString("Viewer_Tuner_Target"), _selectedTunerNote.Solfege, $"{_selectedTunerNote.Frequency:F1}");
            if (TunerFreqLabel != null) TunerFreqLabel.Text = $"{_selectedTunerNote.Frequency:F1} Hz";
            if (TunerPlayRefBtn != null) TunerPlayRefBtn.Text = string.Format(loc.GetString("Viewer_Tuner_Listen_Tone"), _selectedTunerNote.Solfege, $"{_selectedTunerNote.Frequency:F1}");
            if (TunerStatusLabel != null && !TunerService.Instance.IsListening)
            {
                TunerStatusLabel.Text = string.Format(loc.GetString("Viewer_Tuner_Target"), _selectedTunerNote.Solfege, $"{_selectedTunerNote.Frequency:F1}");
                TunerStatusLabel.TextColor = Color.FromArgb("#00B4D8");
            }
        }
        else
        {
            if (TunerNoteLabel != null) TunerNoteLabel.Text = "--";
            if (TunerSolfegeLabel != null) TunerSolfegeLabel.Text = (_currentTunerInstrument != null && _currentTunerInstrument.IsChromatic)
                ? loc.GetString("Viewer_Tuner_Auto_Mode")
                : loc.GetString("Viewer_Tuner_Play_Or_Mic");
            if (TunerFreqLabel != null) TunerFreqLabel.Text = "-- Hz";
            if (TunerPlayRefBtn != null) TunerPlayRefBtn.Text = $"🔊 {loc.GetString("Viewer_Tuner_Listen_Ref")}";
            if (TunerStatusLabel != null && !TunerService.Instance.IsListening)
            {
                TunerStatusLabel.Text = loc.GetString("Viewer_Tuner_Waiting_Signal");
                TunerStatusLabel.TextColor = Color.FromArgb("#888899");
            }
        }
    }

    private async void OnTunerMicClicked(object sender, EventArgs e)
    {
        if (TunerService.Instance.IsListening)
        {
            StopTunerListening();
        }
        else
        {
            await StartTunerListeningAsync();
        }
    }

    private async Task StartTunerListeningAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            if (status != PermissionStatus.Granted)
            {
                var loc = LocalizationService.Instance;
                await DisplayAlertAsync(loc.GetString("Viewer_Tuner_Mic_Permission_Title"), loc.GetString("Viewer_Tuner_Mic_Permission_Msg"), "OK");
                return;
            }

            await TunerService.Instance.StartListeningAsync();
            UpdateTunerMicButtonUI(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Tuner] StartListeningAsync error: {ex.Message}");
        }
    }

    private void StopTunerListening()
    {
        TunerService.Instance.StopListening();
        UpdateTunerMicButtonUI(false);
        ResetTunerDisplay();
    }

    private void UpdateTunerMicButtonUI(bool isListening)
    {
        if (TunerMicButton != null)
        {
            var loc = LocalizationService.Instance;
            TunerMicButton.Text = isListening ? loc.GetString("Viewer_Tuner_Mic_Stop") : loc.GetString("Viewer_Tuner_Mic_Start");
            TunerMicButton.BackgroundColor = isListening ? Color.FromArgb("#A32828") : Color.FromArgb("#252535");
        }
    }

    private void ResetTunerDisplay()
    {
        var loc = LocalizationService.Instance;
        if (TunerNoteLabel != null) TunerNoteLabel.Text = "--";
        if (TunerSolfegeLabel != null) TunerSolfegeLabel.Text = loc.GetString("Viewer_Tuner_Play_Or_Mic");
        if (TunerFreqLabel != null) TunerFreqLabel.Text = "-- Hz";
        if (TunerNeedle != null)
        {
            TunerNeedle.TranslationX = 0;
            TunerNeedle.Color = Colors.White;
        }
        if (TunerStatusLabel != null)
        {
            TunerStatusLabel.Text = loc.GetString("Viewer_Tuner_Waiting_Signal");
            TunerStatusLabel.TextColor = Color.FromArgb("#888899");
        }
    }

    private void OnTunerPitchDetected(object? sender, PitchDetectedEventArgs result)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (TunerOverlay == null || !TunerOverlay.IsVisible) return;

            var loc = LocalizationService.Instance;
            if (!result.HasSignal)
            {
                if (TunerStatusLabel != null)
                {
                    TunerStatusLabel.Text = loc.GetString("Viewer_Tuner_Waiting_Signal");
                    TunerStatusLabel.TextColor = Color.FromArgb("#888899");
                }
                return;
            }

            double cents = result.Cents;
            string noteName = result.NoteName;
            string solfege = result.SolfegeName;
            double targetFreq = result.TargetFrequency;

            if (_selectedTunerNote != null)
            {
                noteName = _selectedTunerNote.NoteName;
                solfege = _selectedTunerNote.Solfege;
                targetFreq = _selectedTunerNote.Frequency;
                if (result.Frequency > 10.0 && targetFreq > 10.0)
                {
                    cents = 1200.0 * Math.Log2(result.Frequency / targetFreq);
                    cents = Math.Clamp(cents, -50.0, 50.0);
                }
            }

            if (TunerNoteLabel != null)
            {
                TunerNoteLabel.Text = $"{noteName}";
            }
            if (TunerSolfegeLabel != null)
            {
                TunerSolfegeLabel.Text = $"{solfege} (Cible : {targetFreq:F1} Hz)";
            }
            if (TunerFreqLabel != null)
            {
                TunerFreqLabel.Text = $"{result.Frequency:F1} Hz";
            }

            // Aiguille : max translation +/- 120 px
            double needleOffset = (cents / 50.0) * 120.0;
            needleOffset = Math.Clamp(needleOffset, -120.0, 120.0);
            if (TunerNeedle != null)
            {
                TunerNeedle.TranslationX = needleOffset;
            }

            // Paliers 3 couleurs : Vert (<= 4 cents), Orange (5 à 15 cents), Rouge (> 15 cents) avec direction claire
            double absCents = Math.Abs(cents);
            if (TunerStatusLabel != null)
            {
                if (absCents <= 4.0)
                {
                    TunerStatusLabel.Text = loc.GetString("Viewer_Tuner_Perfect");
                    TunerStatusLabel.TextColor = Color.FromArgb("#00E676");
                    if (TunerNeedle != null) TunerNeedle.Color = Color.FromArgb("#00E676");
                }
                else if (cents < -15.0)
                {
                    TunerStatusLabel.Text = string.Format(loc.GetString("Viewer_Tuner_Too_Low_Advice"), $"{cents:F0}");
                    TunerStatusLabel.TextColor = Color.FromArgb("#FF3D00");
                    if (TunerNeedle != null) TunerNeedle.Color = Color.FromArgb("#FF3D00");
                }
                else if (cents < -4.0)
                {
                    TunerStatusLabel.Text = string.Format(loc.GetString("Viewer_Tuner_Slightly_Low_Advice"), $"{cents:F0}");
                    TunerStatusLabel.TextColor = Color.FromArgb("#FF9800");
                    if (TunerNeedle != null) TunerNeedle.Color = Color.FromArgb("#FF9800");
                }
                else if (cents > 15.0)
                {
                    TunerStatusLabel.Text = string.Format(loc.GetString("Viewer_Tuner_Too_High_Advice"), $"{cents:F0}");
                    TunerStatusLabel.TextColor = Color.FromArgb("#FF3D00");
                    if (TunerNeedle != null) TunerNeedle.Color = Color.FromArgb("#FF3D00");
                }
                else
                {
                    TunerStatusLabel.Text = string.Format(loc.GetString("Viewer_Tuner_Slightly_High_Advice"), $"{cents:F0}");
                    TunerStatusLabel.TextColor = Color.FromArgb("#FF9800");
                    if (TunerNeedle != null) TunerNeedle.Color = Color.FromArgb("#FF9800");
                }
            }
        });
    }

    private void OnTunerVolumeSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        int volPercent = (int)Math.Round(e.NewValue);
        if (TunerVolumeLabel != null) TunerVolumeLabel.Text = $"{volPercent}%";
        if (TunerTonePlayer != null)
        {
            TunerTonePlayer.Volume = volPercent / 100.0;
        }
    }

    private void OnTunerPlayRefClicked(object sender, EventArgs e)
    {
        try
        {
            double a4Ref = TunerService.Instance.A4Frequency > 100.0 ? TunerService.Instance.A4Frequency : 440.0;
            double freq = _selectedTunerNote?.Frequency ?? a4Ref;
            string wavPath = TunerService.Instance.GenerateReferenceToneWav(freq);
            if (TunerTonePlayer != null)
            {
                TunerTonePlayer.Volume = (TunerVolumeSlider != null ? TunerVolumeSlider.Value : 100.0) / 100.0;
                TunerTonePlayer.Stop();
                TunerTonePlayer.Source = MediaSource.FromFile(wavPath);
                TunerTonePlayer.Play();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Tuner] OnTunerPlayRefClicked error: {ex.Message}");
        }
    }

    #endregion
}
