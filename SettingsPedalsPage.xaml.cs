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
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            RefreshProfiles();
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
            if (active != null)
            {
                ProfileDescLabel.Text = string.IsNullOrWhiteSpace(active.Description)
                    ? LocalizationService.Instance.GetString("Settings_Pedals_No_Notes")
                    : active.Description;
            }
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

        private async void OnEditProfileClicked(object? sender, EventArgs e)
        {
            if (_pedalService.ActiveProfile != null)
            {
                await Navigation.PushAsync(new PedalProfileEditPage(_pedalService.ActiveProfile));
            }
        }

        private async void OnNewCustomProfileClicked(object sender, EventArgs e)
        {
            string title = LocalizationService.Instance.GetString("Settings_Pedals_New_Profile_Title");
            string prompt = LocalizationService.Instance.GetString("Settings_Pedals_New_Profile_Prompt");
            string okText = LocalizationService.Instance.GetString("Common_Create", "OK");
            string cancelText = LocalizationService.Instance.GetString("Common_Cancel");

            string name = await DisplayPromptAsync(
                title, 
                prompt, 
                okText, 
                cancelText, 
                placeholder: "Ex: AirTurn DUO");

            if (!string.IsNullOrWhiteSpace(name))
            {
                var newProfile = _pedalService.CreateCustomProfile(name.Trim());
                RefreshProfiles();
                // Passer directement en mode édition du nouveau profil
                await Navigation.PushAsync(new PedalProfileEditPage(newProfile));
            }
        }

        private async void OnDeleteProfileClicked(object sender, EventArgs e)
        {
            var active = _pedalService.ActiveProfile;
            if (active == null) return;

            string deleteTitle = LocalizationService.Instance.GetString("Settings_Pedals_Delete_Confirm_Title");
            string template = LocalizationService.Instance.GetString("Settings_Pedals_Delete_Confirm_Msg");
            string msg = string.Format(template, active.Name);
            string deleteBtn = LocalizationService.Instance.GetString("Settings_Pedals_Delete_Profile");
            string cancelText = LocalizationService.Instance.GetString("Common_Cancel");

            bool confirm = await DisplayAlertAsync(
                deleteTitle, 
                msg, 
                deleteBtn, 
                cancelText);

            if (confirm)
            {
                _pedalService.DeleteProfile(active.Id);
                RefreshProfiles();
            }
        }

        private async void OnResetFactoryProfilesClicked(object sender, EventArgs e)
        {
            string resetTitle = LocalizationService.Instance.GetString("Settings_Pedals_Reset_Confirm_Title");
            string resetMsg = LocalizationService.Instance.GetString("Settings_Pedals_Reset_Confirm_Msg");
            string resetBtn = LocalizationService.Instance.GetString("Settings_Pedals_Reset_Profiles");
            string cancelText = LocalizationService.Instance.GetString("Common_Cancel");
            string okText = LocalizationService.Instance.GetString("Common_OK");

            bool confirm = await DisplayAlertAsync(
                resetTitle, 
                resetMsg, 
                resetBtn, 
                cancelText);

            if (confirm)
            {
                _pedalService.ResetToFactoryDefaults();
                RefreshProfiles();
                await DisplayAlertAsync(resetTitle, resetMsg, okText);
            }
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

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
