using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager
{
    public partial class PedalButtonConfigPage : ContentPage
    {
        private readonly PedalProfile _profile;
        private readonly PedalBinding _binding;
        private readonly bool _isNew;
        private readonly PedalMidiService _pedalService;

        private PedalPressType _selectedPressType = PedalPressType.Simple;
        private int _detectedKeyCode;
        private string _detectedKeyName = string.Empty;
        private PedalInputType _detectedInputType = PedalInputType.KeyboardKey;

        private readonly List<(string Name, int Code)> _manualKeys = new()
        {
            ("PageDown (Page suivante)", 93),
            ("PageUp (Page précédente)", 92),
            ("ArrowRight (Flèche droite)", 22),
            ("ArrowLeft (Flèche gauche)", 21),
            ("ArrowDown (Flèche bas)", 20),
            ("ArrowUp (Flèche haut)", 19),
            ("Space (Espace)", 62),
            ("Return (Entrée)", 66),
            ("Home (Début)", 122),
            ("End (Fin)", 123),
            ("F3", 114),
            ("F5", 116),
            ("F6", 117),
            ("F7", 118),
            ("F8", 119)
        };

        public PedalButtonConfigPage(PedalProfile profile, PedalBinding binding, bool isNew)
        {
            InitializeComponent();
            _profile = profile;
            _binding = binding;
            _isNew = isNew;
            _pedalService = PedalMidiService.Instance;

            // 1. Initialiser le libellé
            ButtonLabelEntry.Text = _binding.ButtonLabel;

            // 2. Touche existante
            _detectedKeyCode = _binding.KeyCode;
            _detectedKeyName = _binding.KeyName;
            _detectedInputType = _binding.InputType;

            if (_binding.KeyCode != 0 || !string.IsNullOrWhiteSpace(_binding.KeyName))
            {
                DetectedKeyLabel.Text = _binding.KeyName;
                DetectedDetailsLabel.Text = $"Code actuel : 0x{_binding.KeyCode:X2} ({_binding.KeyCode})";
            }

            // 3. Remplir le sélecteur manuel de touches
            ManualKeyPicker.ItemsSource = _manualKeys.Select(k => k.Name).ToList();

            // 4. Initialiser le type d'appui
            _selectedPressType = _binding.PressType;
            UpdatePressTypeUI();

            // 5. Initialiser les 13 actions strictes
            var actionDisplayNames = PedalActionHelper.AvailableConfigurableActions
                .Select(PedalActionHelper.GetActionDisplayName)
                .ToList();
            ActionPicker.ItemsSource = actionDisplayNames;

            int actionIdx = PedalActionHelper.AvailableConfigurableActions.IndexOf(_binding.Action);
            ActionPicker.SelectedIndex = actionIdx >= 0 ? actionIdx : 1; // Default "Page suivante"
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _pedalService.RawEventReceived += OnRawEventReceived;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _pedalService.RawEventReceived -= OnRawEventReceived;
        }

        private void OnRawEventReceived(PedalRawEvent rawEvent)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _detectedKeyCode = rawEvent.KeyCode;
                _detectedKeyName = rawEvent.KeyName;
                _detectedInputType = rawEvent.InputType;

                DetectedKeyLabel.Text = rawEvent.KeyName;
                DetectedDetailsLabel.Text = $"Détecté : {rawEvent.HexCode} • {rawEvent.Source}";
                DetectedSourceLabel.Text = rawEvent.Source;

                // Flash visuel vert pour signifier la capture réussie
                DetectedKeyBox.Stroke = Color.FromArgb("#4CAF50");
                SignalBorderCard.Stroke = Color.FromArgb("#4CAF50");
                ListeningBadge.BackgroundColor = Color.FromArgb("#1B5E20");
                ListeningLabel.Text = "✓ Signal capté !";

                await Task.Delay(400);

                DetectedKeyBox.Stroke = Color.FromArgb("#444444");
                SignalBorderCard.Stroke = Color.FromArgb("#007ACC");
                ListeningBadge.BackgroundColor = Color.FromArgb("#1B3820");
                ListeningLabel.Text = "🟢 En écoute...";
            });
        }

        private void OnManualKeyPickerChanged(object? sender, EventArgs e)
        {
            int idx = ManualKeyPicker.SelectedIndex;
            if (idx >= 0 && idx < _manualKeys.Count)
            {
                var item = _manualKeys[idx];
                _detectedKeyCode = item.Code;
                _detectedKeyName = item.Name.Split(' ')[0];
                _detectedInputType = PedalInputType.KeyboardKey;

                DetectedKeyLabel.Text = _detectedKeyName;
                DetectedDetailsLabel.Text = $"Sélection manuelle : 0x{item.Code:X2} ({item.Code})";
                DetectedSourceLabel.Text = "Sélection manuelle";
            }
        }

        private void OnSimplePressSelected(object sender, EventArgs e)
        {
            _selectedPressType = PedalPressType.Simple;
            UpdatePressTypeUI();
        }

        private void OnLongPressSelected(object sender, EventArgs e)
        {
            _selectedPressType = PedalPressType.Long;
            UpdatePressTypeUI();
        }

        private void UpdatePressTypeUI()
        {
            if (_selectedPressType == PedalPressType.Simple)
            {
                SimplePressBtn.BackgroundColor = Color.FromArgb("#007ACC");
                SimplePressBtn.TextColor = Colors.White;
                SimplePressBtn.BorderWidth = 0;

                LongPressBtn.BackgroundColor = Color.FromArgb("#2A2A2A");
                LongPressBtn.TextColor = Color.FromArgb("#888888");
                LongPressBtn.BorderColor = Color.FromArgb("#444444");
                LongPressBtn.BorderWidth = 1;
            }
            else
            {
                LongPressBtn.BackgroundColor = Color.FromArgb("#E65100");
                LongPressBtn.TextColor = Colors.White;
                LongPressBtn.BorderWidth = 0;

                SimplePressBtn.BackgroundColor = Color.FromArgb("#2A2A2A");
                SimplePressBtn.TextColor = Color.FromArgb("#888888");
                SimplePressBtn.BorderColor = Color.FromArgb("#444444");
                SimplePressBtn.BorderWidth = 1;
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            string label = ButtonLabelEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(label))
            {
                label = !string.IsNullOrWhiteSpace(_detectedKeyName) ? _detectedKeyName : "Pédale";
            }

            int selectedActionIdx = ActionPicker.SelectedIndex;
            PedalAction selectedAction = selectedActionIdx >= 0 && selectedActionIdx < PedalActionHelper.AvailableConfigurableActions.Count
                ? PedalActionHelper.AvailableConfigurableActions[selectedActionIdx]
                : PedalAction.NextPage;

            // Mettre à jour l'objet binding
            _binding.ButtonLabel = label;
            _binding.PressType = _selectedPressType;
            _binding.Action = selectedAction;

            if (_detectedKeyCode != 0 || !string.IsNullOrWhiteSpace(_detectedKeyName))
            {
                _binding.KeyCode = _detectedKeyCode;
                _binding.KeyName = _detectedKeyName;
                _binding.InputType = _detectedInputType;
            }

            if (_isNew)
            {
                _profile.Bindings.Add(_binding);
            }

            _pedalService.SaveProfile(_profile);
            await Navigation.PopAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
