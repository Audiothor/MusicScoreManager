namespace MusicScoreManager;

public partial class SettingsScoresPage : ContentPage
{
    public SettingsScoresPage()
    {
        InitializeComponent();
        
        // Charger les préférences
        string defaultSort = Preferences.Default.Get("DefaultScoreSort", "DateDesc");
        DefaultSortPicker.SelectedIndex = defaultSort switch
        {
            "DateDesc" => 0,
            "DateAsc" => 1,
            "TitleAsc" => 2,
            "TitleDesc" => 3,
            "ModifiedDesc" => 4,
            "RatingDesc" => 5,
            "ComposerAsc" => 6,
            "NoTagsFirst" => 7,
            _ => 0
        };

        ComposerEmptyFirstSwitch.IsToggled = Preferences.Default.Get("ComposerSortEmptyFirst", false);

        InitSubtitleOptions();
        RenderSubtitleOptions();
        UpdateSubtitlePreview();

        ShowPageNumberSwitch.IsToggled = Preferences.Default.Get("ShowPageNumber", true);
        TwoPagesLandscapeSwitch.IsToggled = Preferences.Default.Get("TwoPagesLandscape", true);
        PageNumberSizeSlider.Value = Preferences.Default.Get("PageNumberSize", 20.0);

        string nextGesture = Preferences.Default.Get("NextPageGesture", "SwipeLeft");
        NextPageGesturePicker.SelectedIndex = nextGesture switch
        {
            "SwipeLeft" => 0,
            "TapRight" => 1,
            "SwipeUp" => 2,
            _ => 0
        };

        string prevGesture = Preferences.Default.Get("PrevPageGesture", "SwipeRight");
        PrevPageGesturePicker.SelectedIndex = prevGesture switch
        {
            "SwipeRight" => 0,
            "TapLeft" => 1,
            "SwipeDown" => 2,
            _ => 0
        };
    }

    private class SubtitleOptionItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    private readonly List<SubtitleOptionItem> _subtitleOptions = new();

    private void InitSubtitleOptions()
    {
        _subtitleOptions.Clear();

        string currentPref = Preferences.Default.Get("ScoreSubtitleDisplay", "DateAdded");
        string fullOrder = Preferences.Default.Get("ScoreSubtitleFullOrder", "");

        var allItems = new Dictionary<string, (string Title, string Icon)>
        {
            { "Composer", ("Compositeur", "🎵") },
            { "PageCount", ("Nombre de pages", "📄") },
            { "DateAdded", ("Date d'ajout", "📅") }
        };

        List<string> orderedKeys = new();
        if (!string.IsNullOrWhiteSpace(fullOrder))
        {
            var tokens = fullOrder.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var t in tokens)
            {
                if (allItems.ContainsKey(t) && !orderedKeys.Contains(t))
                {
                    orderedKeys.Add(t);
                }
            }
        }

        foreach (var k in allItems.Keys)
        {
            if (!orderedKeys.Contains(k)) orderedKeys.Add(k);
        }

        HashSet<string> enabledKeys = new();
        if (currentPref == "Composer")
        {
            enabledKeys.Add("Composer");
        }
        else if (currentPref == "DateAdded")
        {
            enabledKeys.Add("DateAdded");
        }
        else if (currentPref == "ComposerAndDate")
        {
            enabledKeys.Add("Composer");
            enabledKeys.Add("DateAdded");
        }
        else if (!string.IsNullOrWhiteSpace(currentPref))
        {
            var tokens = currentPref.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var t in tokens)
            {
                if (allItems.ContainsKey(t)) enabledKeys.Add(t);
            }
        }

        foreach (var key in orderedKeys)
        {
            var info = allItems[key];
            _subtitleOptions.Add(new SubtitleOptionItem
            {
                Id = key,
                Title = info.Title,
                Icon = info.Icon,
                IsEnabled = enabledKeys.Contains(key)
            });
        }
    }

    private void RenderSubtitleOptions()
    {
        SubtitleItemsLayout.Children.Clear();

        for (int i = 0; i < _subtitleOptions.Count; i++)
        {
            var item = _subtitleOptions[i];
            int index = i;

            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#1E1E26"),
                Stroke = item.IsEnabled ? Color.FromArgb("#3C3C50") : Color.FromArgb("#2A2A35"),
                StrokeThickness = 1,
                Padding = new Thickness(10, 8),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Opacity = item.IsEnabled ? 1.0 : 0.6
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                VerticalOptions = LayoutOptions.Center
            };

            var dragHandle = new Label
            {
                Text = "≡",
                TextColor = Color.FromArgb("#777788"),
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(2, 0, 4, 0)
            };
            Grid.SetColumn(dragHandle, 0);
            grid.Children.Add(dragHandle);

            var checkBox = new CheckBox
            {
                IsChecked = item.IsEnabled,
                Color = Color.FromArgb("#007ACC"),
                VerticalOptions = LayoutOptions.Center
            };
            checkBox.CheckedChanged += (s, e) =>
            {
                item.IsEnabled = e.Value;
                border.Opacity = item.IsEnabled ? 1.0 : 0.6;
                border.Stroke = item.IsEnabled ? Color.FromArgb("#3C3C50") : Color.FromArgb("#2A2A35");
                SaveSubtitlePreferences();
                UpdateSubtitlePreview();
            };
            Grid.SetColumn(checkBox, 1);
            grid.Children.Add(checkBox);

            var titleStack = new HorizontalStackLayout
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center
            };
            titleStack.Children.Add(new Label { Text = item.Icon, FontSize = 16, VerticalOptions = LayoutOptions.Center });
            titleStack.Children.Add(new Label { Text = item.Title, TextColor = Colors.White, FontSize = 14, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center });
            Grid.SetColumn(titleStack, 2);
            grid.Children.Add(titleStack);

            var upButton = new Button
            {
                Text = "▲",
                BackgroundColor = Color.FromArgb("#2A2A38"),
                TextColor = Colors.White,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 36,
                HeightRequest = 36,
                Padding = 0,
                CornerRadius = 6,
                IsEnabled = index > 0,
                Opacity = index > 0 ? 1.0 : 0.3
            };
            upButton.Clicked += (s, e) =>
            {
                if (index > 0)
                {
                    var moved = _subtitleOptions[index];
                    _subtitleOptions.RemoveAt(index);
                    _subtitleOptions.Insert(index - 1, moved);
                    RenderSubtitleOptions();
                    SaveSubtitlePreferences();
                    UpdateSubtitlePreview();
                }
            };
            Grid.SetColumn(upButton, 3);
            grid.Children.Add(upButton);

            var downButton = new Button
            {
                Text = "▼",
                BackgroundColor = Color.FromArgb("#2A2A38"),
                TextColor = Colors.White,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 36,
                HeightRequest = 36,
                Padding = 0,
                CornerRadius = 6,
                IsEnabled = index < _subtitleOptions.Count - 1,
                Opacity = index < _subtitleOptions.Count - 1 ? 1.0 : 0.3
            };
            downButton.Clicked += (s, e) =>
            {
                if (index < _subtitleOptions.Count - 1)
                {
                    var moved = _subtitleOptions[index];
                    _subtitleOptions.RemoveAt(index);
                    _subtitleOptions.Insert(index + 1, moved);
                    RenderSubtitleOptions();
                    SaveSubtitlePreferences();
                    UpdateSubtitlePreview();
                }
            };
            Grid.SetColumn(downButton, 4);
            grid.Children.Add(downButton);

            border.Content = grid;

            var dragRecognizer = new DragGestureRecognizer { CanDrag = true };
            dragRecognizer.DragStarting += (s, e) =>
            {
                e.Data.Properties["SourceItem"] = item;
            };
            border.GestureRecognizers.Add(dragRecognizer);

            var dropRecognizer = new DropGestureRecognizer { AllowDrop = true };
            dropRecognizer.Drop += (s, e) =>
            {
                if (e.Data.Properties.TryGetValue("SourceItem", out var sourceObj) && sourceObj is SubtitleOptionItem sourceItem && sourceItem != item)
                {
                    int oldIdx = _subtitleOptions.IndexOf(sourceItem);
                    int newIdx = _subtitleOptions.IndexOf(item);
                    if (oldIdx >= 0 && newIdx >= 0)
                    {
                        _subtitleOptions.RemoveAt(oldIdx);
                        _subtitleOptions.Insert(newIdx, sourceItem);
                        RenderSubtitleOptions();
                        SaveSubtitlePreferences();
                        UpdateSubtitlePreview();
                    }
                }
            };
            border.GestureRecognizers.Add(dropRecognizer);

            SubtitleItemsLayout.Children.Add(border);
        }
    }

    private void SaveSubtitlePreferences()
    {
        var enabledTokens = _subtitleOptions.Where(o => o.IsEnabled).Select(o => o.Id).ToList();
        string prefValue = string.Join(",", enabledTokens);
        Preferences.Default.Set("ScoreSubtitleDisplay", prefValue);

        var allTokens = _subtitleOptions.Select(o => o.Id).ToList();
        Preferences.Default.Set("ScoreSubtitleFullOrder", string.Join(",", allTokens));
    }

    private void UpdateSubtitlePreview()
    {
        var enabledOptions = _subtitleOptions.Where(o => o.IsEnabled).ToList();
        if (!enabledOptions.Any())
        {
            SubtitlePreviewLabel.Text = "(Aucune information sous le titre)";
            SubtitlePreviewLabel.TextColor = Color.FromArgb("#888888");
            return;
        }

        var parts = new List<string>();
        foreach (var opt in enabledOptions)
        {
            switch (opt.Id)
            {
                case "Composer":
                    parts.Add("Frédéric Chopin");
                    break;
                case "PageCount":
                    parts.Add("4 pages");
                    break;
                case "DateAdded":
                    parts.Add(DateTime.Now.ToString("dd/MM/yyyy"));
                    break;
            }
        }

        SubtitlePreviewLabel.Text = string.Join(" • ", parts);
        SubtitlePreviewLabel.TextColor = Color.FromArgb("#00B4D8");
    }

    private void OnDefaultSortChanged(object sender, EventArgs e)
    {
        string value = DefaultSortPicker.SelectedIndex switch
        {
            0 => "DateDesc",
            1 => "DateAsc",
            2 => "TitleAsc",
            3 => "TitleDesc",
            4 => "ModifiedDesc",
            5 => "RatingDesc",
            6 => "ComposerAsc",
            7 => "NoTagsFirst",
            _ => "DateDesc"
        };
        Preferences.Default.Set("DefaultScoreSort", value);
    }

    private void OnComposerEmptyFirstToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("ComposerSortEmptyFirst", e.Value);
    }

    private void OnShowPageNumberToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("ShowPageNumber", e.Value);
    }

    private void OnTwoPagesLandscapeToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("TwoPagesLandscape", e.Value);
    }

    private void OnPageNumberSizeChanged(object sender, ValueChangedEventArgs e)
    {
        Preferences.Default.Set("PageNumberSize", e.NewValue);
    }

    private void OnNextPageGestureChanged(object sender, EventArgs e)
    {
        string value = NextPageGesturePicker.SelectedIndex switch
        {
            0 => "SwipeLeft",
            1 => "TapRight",
            2 => "SwipeUp",
            _ => "SwipeLeft"
        };
        Preferences.Default.Set("NextPageGesture", value);
    }

    private void OnPrevPageGestureChanged(object sender, EventArgs e)
    {
        string value = PrevPageGesturePicker.SelectedIndex switch
        {
            0 => "SwipeRight",
            1 => "TapLeft",
            2 => "SwipeDown",
            _ => "SwipeRight"
        };
        Preferences.Default.Set("PrevPageGesture", value);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
