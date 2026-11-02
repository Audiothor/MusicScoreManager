using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager
{
    public partial class PedalProfileEditPage : ContentPage
    {
        private readonly PedalProfile _profile;
        private readonly PedalMidiService _pedalService;

        public PedalProfileEditPage(PedalProfile profile)
        {
            InitializeComponent();
            _profile = profile;
            _pedalService = PedalMidiService.Instance;

            ProfileNameEntry.Text = _profile.Name;
            ProfileDescEditor.Text = _profile.Description;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            RenderBindingsList();
        }

        private void RenderBindingsList()
        {
            BindingsListLayout.Children.Clear();

            int count = _profile.Bindings.Count;
            var loc = LocalizationService.Instance;
            ButtonsCountLabel.Text = count > 1 
                ? string.Format(loc.GetString("Pedal_Profile_Actions_Count_Plural", "{0} actions configurées"), count)
                : string.Format(loc.GetString("Pedal_Profile_Actions_Count_Single", "{0} action configurée"), count);
            EmptyBindingsLabel.IsVisible = count == 0;

            foreach (var binding in _profile.Bindings)
            {
                var card = CreateBindingCard(binding);
                BindingsListLayout.Children.Add(card);
            }
        }

        private Border CreateBindingCard(PedalBinding binding)
        {
            var card = new Border
            {
                Stroke = Color.FromArgb("#333333"),
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#242424"),
                Padding = new Thickness(14, 12),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }
            };

            var stack = new VerticalStackLayout { Spacing = 10 };

            // Ligne 1 : Nom du bouton + Badge Type d'appui (Simple vs Long)
            var topGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };

            string displayLabel = string.IsNullOrWhiteSpace(binding.ButtonLabel) 
                ? (string.IsNullOrWhiteSpace(binding.KeyName) ? LocalizationService.Instance.GetString("Pedal_Profile_Unnamed_Button", "Bouton sans nom") : binding.KeyName) 
                : binding.ButtonLabel;

            var nameLabel = new Label
            {
                Text = displayLabel,
                TextColor = Colors.White,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(nameLabel, 0);

            // Badge Type d'appui
            bool isLong = binding.PressType == PedalPressType.Long;
            var pressBadge = new Border
            {
                BackgroundColor = isLong ? Color.FromArgb("#E65100") : Color.FromArgb("#1B5E20"),
                Stroke = isLong ? Color.FromArgb("#FF9800") : Color.FromArgb("#4CAF50"),
                StrokeThickness = 1,
                Padding = new Thickness(8, 3),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Content = new Label
                {
                    Text = isLong 
                        ? LocalizationService.Instance.GetString("Pedal_Config_Press_Long", "⏱️ Appui long") 
                        : LocalizationService.Instance.GetString("Pedal_Config_Press_Simple", "⚡ Appui simple"),
                    TextColor = Colors.White,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold
                }
            };
            Grid.SetColumn(pressBadge, 1);

            topGrid.Children.Add(nameLabel);
            topGrid.Children.Add(pressBadge);
            stack.Children.Add(topGrid);

            // Ligne 2 : Signal détecté / Touche associée + Action déclenchée
            var detailsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 8
            };

            var keyBadge = new Border
            {
                BackgroundColor = Color.FromArgb("#1A334D"),
                Stroke = Color.FromArgb("#007ACC"),
                StrokeThickness = 1,
                Padding = new Thickness(8, 3),
                VerticalOptions = LayoutOptions.Center,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Content = new Label
                {
                    Text = string.IsNullOrWhiteSpace(binding.KeyName) 
                        ? LocalizationService.Instance.GetString("Pedal_Profile_No_Key", "Aucune touche") 
                        : binding.KeyName,
                    TextColor = Color.FromArgb("#4FC3F7"),
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold
                }
            };
            Grid.SetColumn(keyBadge, 0);

            string actionText = PedalActionHelper.GetActionDisplayName(binding.Action);
            var actionLabel = new Label
            {
                Text = $"👉 {actionText}",
                TextColor = Color.FromArgb("#81C784"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            Grid.SetColumn(actionLabel, 1);

            detailsGrid.Children.Add(keyBadge);
            detailsGrid.Children.Add(actionLabel);
            stack.Children.Add(detailsGrid);

            // Ligne de séparation subtile
            stack.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#2E2E2E"), Margin = new Thickness(0, 2) });

            // Ligne 3 : Boutons d'action (Modifier, Supprimer)
            var actionsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 10
            };

            var editBtn = new Button
            {
                Text = LocalizationService.Instance.GetString("Common_Edit", "✏️ Modifier"),
                BackgroundColor = Color.FromArgb("#2C2C2C"),
                TextColor = Color.FromArgb("#007ACC"),
                BorderColor = Color.FromArgb("#007ACC"),
                BorderWidth = 1,
                CornerRadius = 6,
                HeightRequest = 36,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold
            };
            editBtn.Clicked += async (s, e) =>
            {
                await Navigation.PushAsync(new PedalButtonConfigPage(_profile, binding, isNew: false));
            };
            Grid.SetColumn(editBtn, 0);

            var deleteBtn = new Button
            {
                Text = LocalizationService.Instance.GetString("Duplicates_Delete_Btn", "🗑️ Supprimer"),
                BackgroundColor = Color.FromArgb("#2C2C2C"),
                TextColor = Color.FromArgb("#FF6B6B"),
                BorderColor = Color.FromArgb("#FF6B6B"),
                BorderWidth = 1,
                CornerRadius = 6,
                HeightRequest = 36,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold
            };
            deleteBtn.Clicked += async (s, e) =>
            {
                var loc = LocalizationService.Instance;
                bool confirm = await DisplayAlertAsync(
                    loc.GetString("Common_Delete", "Supprimer"), 
                    string.Format(loc.GetString("Pedal_Profile_Delete_Action_Confirm", "Voulez-vous supprimer l'action associée à « {0} » ?"), displayLabel), 
                    loc.GetString("Common_Delete", "Supprimer"), 
                    loc.GetString("Common_Cancel", "Annuler"));

                if (confirm)
                {
                    _profile.Bindings.Remove(binding);
                    _pedalService.SaveProfile(_profile);
                    RenderBindingsList();
                }
            };
            Grid.SetColumn(deleteBtn, 1);

            actionsGrid.Children.Add(editBtn);
            actionsGrid.Children.Add(deleteBtn);
            stack.Children.Add(actionsGrid);

            card.Content = stack;
            return card;
        }

        private async void OnAddButtonBindingClicked(object sender, EventArgs e)
        {
            var loc = LocalizationService.Instance;
            string label = await DisplayPromptAsync(
                loc.GetString("Pedal_Profile_New_Btn_Title", "Nouveau bouton / pédale"), 
                loc.GetString("Pedal_Profile_New_Btn_Prompt", "Nom ou rôle de la pédale (ex: Pédale gauche, Bouton 1...) :"), 
                loc.GetString("Common_Continue", "Continuer"), 
                loc.GetString("Common_Cancel", "Annuler"), 
                placeholder: loc.GetString("Pedal_Profile_New_Btn_Placeholder", "Ex: Pédale droite"));

            if (!string.IsNullOrWhiteSpace(label))
            {
                var newBinding = new PedalBinding
                {
                    ButtonLabel = label.Trim(),
                    KeyName = loc.GetString("Pedal_Profile_Unassigned", "Non assigné"),
                    KeyCode = 0,
                    PressType = PedalPressType.Simple,
                    Action = PedalAction.NextPage
                };

                await Navigation.PushAsync(new PedalButtonConfigPage(_profile, newBinding, isNew: true));
            }
        }

        private async void OnSaveProfileClicked(object sender, EventArgs e)
        {
            SaveProfileData();
            var loc = LocalizationService.Instance;
            await DisplayAlertAsync(
                loc.GetString("Pedal_Profile_Saved_Title", "Profil enregistré"), 
                loc.GetString("Pedal_Profile_Saved_Msg", "Le profil et ses boutons ont été enregistrés avec succès."), 
                "OK");
            await Navigation.PopAsync();
        }

        private void SaveProfileData()
        {
            if (!string.IsNullOrWhiteSpace(ProfileNameEntry.Text))
            {
                _profile.Name = ProfileNameEntry.Text.Trim();
            }
            _profile.Description = ProfileDescEditor.Text?.Trim() ?? string.Empty;
            _pedalService.SaveProfile(_profile);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            SaveProfileData();
            await Navigation.PopAsync();
        }
    }
}
