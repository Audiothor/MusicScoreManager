using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager
{
    public partial class SettingsPedalsPage : ContentPage
    {
        private readonly PedalMidiService _pedalService;
        private ObservableCollection<PedalBindingViewModel> _bindings = new();
        private bool _isLearningKey = false;
        private List<(int Ms, string Label)> _cooldownOptions = new();

        private void InitCooldownOptions()
        {
            var loc = LocalizationService.Instance;
            _cooldownOptions = new List<(int Ms, string Label)>
            {
                (300, loc.GetString("Settings_Pedals_Cooldown_Fast", "300 ms (Rapide)")),
                (450, loc.GetString("Settings_Pedals_Cooldown_Standard", "450 ms (Standard recommandé)")),
                (600, loc.GetString("Settings_Pedals_Cooldown_Safe", "600 ms (Sécurisé concert)")),
                (800, loc.GetString("Settings_Pedals_Cooldown_Strict", "800 ms (Strict)"))
            };

            FastTurnDelayPicker.ItemsSource = _cooldownOptions.Select(o => o.Label).ToList();
            int currentCooldown = _pedalService.FastTurnCooldownMs;
            int matchedIdx = _cooldownOptions.FindIndex(o => o.Ms == currentCooldown);
            FastTurnDelayPicker.SelectedIndex = matchedIdx >= 0 ? matchedIdx : 1; // Default 450 ms
        }

        public SettingsPedalsPage()
        {
            InitializeComponent();
            _pedalService = PedalMidiService.Instance;

            ThresholdSlider.Value = _pedalService.LongPressThresholdMs;
            ThresholdValueLabel.Text = $"{_pedalService.LongPressThresholdMs} ms";
            ServiceEnabledSwitch.IsToggled = _pedalService.IsEnabled;

            // Protection anti-double saut de page
            BlockFastTurnSwitch.IsToggled = _pedalService.BlockFastPageTurn;
            FastTurnDelayRow.IsVisible = _pedalService.BlockFastPageTurn;

            InitCooldownOptions();
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                InitCooldownOptions();
                UpdateProfileUI();
                if (!_isLearningKey)
                {
                    ListeningLabel.Text = LocalizationService.Instance.GetString("Pedals_Listening_Status", "🟢 En écoute...");
                }
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _pedalService.RawEventReceived += OnRawEventReceived;
            RefreshProfiles();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _pedalService.RawEventReceived -= OnRawEventReceived;
        }

        private void RefreshProfiles()
        {
            ProfilesPicker.SelectedIndexChanged -= OnProfileSelectionChanged;

            ProfilesPicker.ItemsSource = _pedalService.Profiles.Select(p => p.Name).ToList();
            var activeIndex = _pedalService.Profiles.FindIndex(p => p.Id == _pedalService.ActiveProfile.Id);
            ProfilesPicker.SelectedIndex = activeIndex >= 0 ? activeIndex : 0;

            ProfilesPicker.SelectedIndexChanged += OnProfileSelectionChanged;

            UpdateProfileUI();
        }

        private void UpdateProfileUI()
        {
            var active = _pedalService.ActiveProfile;
            ProfileDescLabel.Text = active.Description;
            var format = LocalizationService.Instance.GetString("Settings_Pedals_Shortcuts_For_Profile", "Raccourcis du profil : {0}");
            ProfileShortcutsTitleLabel.Text = string.Format(format, active.Name);

            if (active.IsBuiltIn)
            {
                DeleteProfileBtn.IsVisible = false;
                Grid.SetColumnSpan(NewProfileBtn, 2);
            }
            else
            {
                DeleteProfileBtn.IsVisible = true;
                DeleteProfileBtn.IsEnabled = true;
                DeleteProfileBtn.Opacity = 1.0;
                Grid.SetColumnSpan(NewProfileBtn, 1);
            }

            _bindings.Clear();
            foreach (var b in active.Bindings)
            {
                var vm = new PedalBindingViewModel(b, () => _pedalService.SaveCustomProfiles());
                _bindings.Add(vm);
            }

            BindingsCollection.ItemsSource = _bindings;
        }

        private void OnProfileSelectionChanged(object? sender, EventArgs e)
        {
            int index = ProfilesPicker.SelectedIndex;
            if (index >= 0 && index < _pedalService.Profiles.Count)
            {
                _pedalService.ActiveProfile = _pedalService.Profiles[index];
                UpdateProfileUI();
            }
        }

        private async void OnNewCustomProfileClicked(object sender, EventArgs e)
        {
            string name = await DisplayPromptAsync("Nouveau profil", "Entrez le nom de votre configuration de pédale :", "Créer", "Annuler", placeholder: "Ma Pédale Perso");
            if (!string.IsNullOrWhiteSpace(name))
            {
                _pedalService.CreateCustomProfile(name.Trim());
                RefreshProfiles();
            }
        }

        private async void OnDeleteProfileClicked(object sender, EventArgs e)
        {
            if (_pedalService.ActiveProfile.IsBuiltIn) return;

            bool confirm = await DisplayAlertAsync("Supprimer le profil", $"Voulez-vous vraiment supprimer le profil personnalisé « {_pedalService.ActiveProfile.Name} » ?", "Supprimer", "Annuler");
            if (confirm)
            {
                _pedalService.DeleteCustomProfile(_pedalService.ActiveProfile.Id);
                RefreshProfiles();
            }
        }

        private async void OnLearnKeyClicked(object sender, EventArgs e)
        {
            _isLearningKey = true;
            ListeningBadge.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#B71C1C");
            ListeningLabel.Text = "🔴 " + LocalizationService.Instance.GetString("Pedals_Press_Pedal", "Appuyez sur la pédale...");

            await DisplayAlertAsync("Mode Apprentissage", "Actionnez la pédale ou commande MIDI que vous souhaitez associer.\n\nLe signal sera détecté automatiquement.", "OK");
        }

        private void OnDeleteBindingClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is PedalBindingViewModel vm)
            {
                _pedalService.ActiveProfile.Bindings.Remove(vm.Binding);
                _bindings.Remove(vm);
                _pedalService.SaveCustomProfiles();
            }
        }

        private void OnRawEventReceived(PedalRawEvent rawEvent)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Mettre à jour l'affichage en direct
                EventKeyLabel.Text = rawEvent.KeyName;
                EventSourceLabel.Text = rawEvent.Source;
                EventCodeLabel.Text = $"Code : {rawEvent.HexCode}";
                EventTimeLabel.Text = $"Heure : {rawEvent.FormattedTime}";
                EventLongPressBadge.IsVisible = rawEvent.IsLongPress;

                if (rawEvent.MatchedAction != PedalAction.None)
                {
                    EventActionLabel.Text = PedalMidiService.GetActionDisplayName(rawEvent.MatchedAction);
                    EventActionLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#4CAF50");
                }
                else
                {
                    EventActionLabel.Text = "Aucune action configurée";
                    EventActionLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#888888");
                }

                // Animation flash sur la carte du testeur
                LastEventCard.Stroke = Microsoft.Maui.Graphics.Color.FromArgb("#007ACC");
                _ = Task.Delay(300).ContinueWith(_ => MainThread.BeginInvokeOnMainThread(() =>
                {
                    LastEventCard.Stroke = Microsoft.Maui.Graphics.Color.FromArgb("#444444");
                }));

                // Si nous sommes en mode apprentissage (Learn Mode)
                if (_isLearningKey)
                {
                    _isLearningKey = false;
                    ListeningBadge.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#1A334D");
                    ListeningLabel.Text = LocalizationService.Instance.GetString("Pedals_Listening_Status", "🟢 En écoute...");

                    var existing = _pedalService.ActiveProfile.Bindings.FirstOrDefault(b => b.InputType == rawEvent.InputType && b.KeyCode == rawEvent.KeyCode);
                    var loc = LocalizationService.Instance;
                    if (existing == null)
                    {
                        var newBinding = new PedalBinding
                        {
                            InputType = rawEvent.InputType,
                            KeyCode = rawEvent.KeyCode,
                            KeyName = rawEvent.KeyName,
                            Action = PedalAction.NextPage,
                            Description = loc.GetString("Settings_Pedals_Key_Detected_Title", "Touche apprise")
                        };
                        _pedalService.ActiveProfile.Bindings.Add(newBinding);
                        _pedalService.SaveCustomProfiles();
                        UpdateProfileUI();
                        await DisplayAlertAsync(
                            loc.GetString("Settings_Pedals_Key_Detected_Title", "Touche Détectée !"), 
                            string.Format(loc.GetString("Settings_Pedals_Key_Detected_Msg", "La commande « {0} » a été ajoutée au profil.\nVous pouvez maintenant lui assigner l'action de votre choix."), rawEvent.KeyName), 
                            loc.GetString("Common_OK", "OK"));
                    }
                    else
                    {
                        await DisplayAlertAsync(
                            loc.GetString("Settings_Pedals_Key_Exists_Title", "Touche Déjà Présente"), 
                            string.Format(loc.GetString("Settings_Pedals_Key_Exists_Msg", "La commande « {0} » est déjà configurée dans ce profil."), rawEvent.KeyName), 
                            loc.GetString("Common_OK", "OK"));
                    }
                }
            });
        }

        private void OnServiceEnabledToggled(object sender, ToggledEventArgs e)
        {
            _pedalService.IsEnabled = e.Value;
            ListeningBadge.IsVisible = e.Value;
        }

        private void OnThresholdSliderChanged(object sender, ValueChangedEventArgs e)
        {
            int val = (int)e.NewValue;
            _pedalService.LongPressThresholdMs = val;
            ThresholdValueLabel.Text = $"{val} ms";
        }

        private void OnBlockFastTurnToggled(object sender, ToggledEventArgs e)
        {
            _pedalService.BlockFastPageTurn = e.Value;
            FastTurnDelayRow.IsVisible = e.Value;
        }

        private void OnFastTurnDelayChanged(object? sender, EventArgs e)
        {
            int idx = FastTurnDelayPicker.SelectedIndex;
            if (idx >= 0 && idx < _cooldownOptions.Count)
            {
                _pedalService.FastTurnCooldownMs = _cooldownOptions[idx].Ms;
            }
        }

        private void OnToggleShortcutsAccordionClicked(object? sender, EventArgs e)
        {
            bool willBeVisible = !ShortcutsContentLayout.IsVisible;
            ShortcutsContentLayout.IsVisible = willBeVisible;
            AccordionChevronLabel.Text = willBeVisible ? "▼" : "▶";
            var loc = LocalizationService.Instance;
            ShortcutsSubtitleLabel.Text = willBeVisible 
                ? loc.GetString("Settings_Pedals_Shortcuts_Hide", "Toucher pour rabattre les touches configurées") 
                : loc.GetString("Settings_Pedals_Shortcuts_Show", "Toucher pour afficher les touches configurées");
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class PedalBindingViewModel : INotifyPropertyChanged
    {
        public PedalBinding Binding { get; }
        private readonly Action _onChanged;

        public static List<string> AvailableActions { get; } = Enum.GetValues<PedalAction>()
            .Select(PedalMidiService.GetActionDisplayName)
            .ToList();

        public string KeyName => Binding.KeyName;
        public string InputTypeDisplay => Binding.InputType switch
        {
            PedalInputType.KeyboardKey => "Clavier / BT",
            PedalInputType.MidiCC => "MIDI CC",
            PedalInputType.MidiNote => "MIDI Note",
            PedalInputType.MidiProgramChange => "MIDI Program",
            _ => "Entrée"
        };

        public string SelectedActionDisplay
        {
            get => PedalMidiService.GetActionDisplayName(Binding.Action);
            set
            {
                var action = GetActionFromDisplayName(value);
                if (Binding.Action != action)
                {
                    Binding.Action = action;
                    _onChanged?.Invoke();
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedLongPressActionDisplay
        {
            get => PedalMidiService.GetActionDisplayName(Binding.LongPressAction);
            set
            {
                var action = GetActionFromDisplayName(value);
                if (Binding.LongPressAction != action)
                {
                    Binding.LongPressAction = action;
                    _onChanged?.Invoke();
                    OnPropertyChanged();
                }
            }
        }

        public PedalBindingViewModel(PedalBinding binding, Action onChanged)
        {
            Binding = binding;
            _onChanged = onChanged;
        }

        private static PedalAction GetActionFromDisplayName(string displayName)
        {
            foreach (var a in Enum.GetValues<PedalAction>())
            {
                if (PedalMidiService.GetActionDisplayName(a) == displayName)
                {
                    return a;
                }
            }
            return PedalAction.None;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
