using MusicScoreManager.Models;
using MusicScoreManager.Services;

namespace MusicScoreManager;

public partial class DuplicatesPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private bool _isAnalyzingDuplicates = false;

    public DuplicatesPage(DatabaseService? databaseService = null)
    {
        InitializeComponent();
        _databaseService = databaseService ?? new DatabaseService();
        _settingsService = new SettingsService();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnAnalyzeDuplicatesClicked(object? sender, EventArgs e)
    {
        if (_isAnalyzingDuplicates) return;
        _isAnalyzingDuplicates = true;

        try
        {
            DuplicatesProgressSection.IsVisible = true;
            DuplicatesProgressBar.Progress = 0;
            DuplicatesProgressLabel.Text = "Récupération des partitions...";
            DuplicatesResultSummaryLabel.IsVisible = false;
            DuplicatesListContainer.Children.Clear();

            var scores = await _databaseService.GetScoresAsync();
            if (!scores.Any())
            {
                DuplicatesProgressSection.IsVisible = false;
                DuplicatesResultSummaryLabel.Text = "Aucune partition dans la bibliothèque.";
                DuplicatesResultSummaryLabel.TextColor = Color.FromArgb("#888888");
                DuplicatesResultSummaryLabel.IsVisible = true;
                return;
            }

            int total = scores.Count;
            var fileGroups = new Dictionary<string, List<Score>>();
            var fileSizes = new Dictionary<string, long>();

            for (int i = 0; i < total; i++)
            {
                var score = scores[i];
                double progress = (double)(i + 1) / total;
                DuplicatesProgressBar.Progress = progress;
                DuplicatesProgressLabel.Text = $"Analyse : {i + 1} / {total} ({score.Title})...";

                string fullPath = _settingsService.GetAbsolutePath(score.FilePath);
                if (!File.Exists(fullPath))
                {
                    string fileName = Path.GetFileName(score.FilePath);
                    string? found = FindFileRecursively(_settingsService.ScoresRootDirectory, fileName);
                    if (!string.IsNullOrEmpty(found) && File.Exists(found))
                    {
                        fullPath = found;
                    }
                }

                if (File.Exists(fullPath))
                {
                    try
                    {
                        var fileInfo = new FileInfo(fullPath);
                        long length = fileInfo.Length;
                        string hash = await Task.Run(() => ComputeSha256(fullPath));
                        string groupKey = $"{length}_{hash}";

                        if (!fileGroups.ContainsKey(groupKey))
                        {
                            fileGroups[groupKey] = new List<Score>();
                            fileSizes[groupKey] = length;
                        }
                        fileGroups[groupKey].Add(score);
                    }
                    catch { }
                }
            }

            DuplicatesProgressSection.IsVisible = false;

            var duplicateGroups = fileGroups.Where(g => g.Value.Count > 1).ToList();

            if (!duplicateGroups.Any())
            {
                DuplicatesResultSummaryLabel.Text = $"✅ {LocalizationService.Instance.GetString("Duplicates_None_Found_Success")}";
                DuplicatesResultSummaryLabel.TextColor = Color.FromArgb("#2ECC71");
                DuplicatesResultSummaryLabel.IsVisible = true;
                return;
            }

            int totalDuplicatesCount = duplicateGroups.Sum(g => g.Value.Count);
            string summaryTemplate = LocalizationService.Instance.GetString("Duplicates_Found_Summary");
            DuplicatesResultSummaryLabel.Text = $"⚠️ {string.Format(summaryTemplate, duplicateGroups.Count, totalDuplicatesCount)}";
            DuplicatesResultSummaryLabel.TextColor = Color.FromArgb("#E67E22");
            DuplicatesResultSummaryLabel.IsVisible = true;

            int groupIndex = 1;
            foreach (var group in duplicateGroups)
            {
                long sizeBytes = fileSizes[group.Key];
                string sizeStr = FormatBytes(sizeBytes);

                var groupCard = new Border
                {
                    BackgroundColor = Color.FromArgb("#262626"),
                    Stroke = Color.FromArgb("#444444"),
                    StrokeThickness = 1,
                    Padding = 12,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }
                };

                var groupStack = new VerticalStackLayout { Spacing = 10 };
                
                string groupTemplate = LocalizationService.Instance.GetString("Duplicates_Group_Header");
                var headerLabel = new Label
                {
                    Text = $"{string.Format(groupTemplate, groupIndex, group.Value.Count)} ({sizeStr})",
                    TextColor = Color.FromArgb("#007ACC"),
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14
                };
                groupStack.Children.Add(headerLabel);

                foreach (var score in group.Value)
                {
                    var setlists = await _databaseService.GetSetlistNamesForScoreAsync(score.Id);
                    bool isInSetlist = setlists.Any();

                    var itemBorder = new Border
                    {
                        BackgroundColor = Color.FromArgb("#1E1E1E"),
                        Stroke = isInSetlist ? Color.FromArgb("#E67E22") : Color.FromArgb("#333333"),
                        StrokeThickness = 1,
                        Padding = 10,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 }
                    };

                    var itemGrid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        ColumnSpacing = 10
                    };

                    var infoStack = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
                    
                    var titleLabel = new Label
                    {
                        Text = $"🎵 {score.Title}",
                        TextColor = Colors.White,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 13
                    };

                    var pathLabel = new Label
                    {
                        Text = $"📁 [{score.Category}] {score.FilePath}",
                        TextColor = Color.FromArgb("#888888"),
                        FontSize = 11,
                        LineBreakMode = LineBreakMode.TailTruncation
                    };

                    string usedTemplate = LocalizationService.Instance.GetString("Duplicates_Used_In_Setlists");
                    string notUsedStr = LocalizationService.Instance.GetString("Duplicates_Not_Used");
                    var usageLabel = new Label
                    {
                        Text = isInSetlist 
                            ? string.Format(usedTemplate, setlists.Count, string.Join(", ", setlists))
                            : $"🟢 {notUsedStr}",
                        TextColor = isInSetlist ? Color.FromArgb("#E67E22") : Color.FromArgb("#2ECC71"),
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold
                    };

                    infoStack.Children.Add(titleLabel);
                    infoStack.Children.Add(pathLabel);
                    infoStack.Children.Add(usageLabel);

                    var deleteBtn = new Button
                    {
                        Text = $"🗑️ {LocalizationService.Instance.GetString("Duplicates_Delete_Btn")}",
                        BackgroundColor = Color.FromArgb("#CC2929"),
                        TextColor = Colors.White,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 12,
                        HeightRequest = 36,
                        CornerRadius = 6,
                        VerticalOptions = LayoutOptions.Center
                    };

                    deleteBtn.Clicked += async (s, e) =>
                    {
                        string deleteTitle = LocalizationService.Instance.GetString("Duplicates_Delete_Title");
                        string cancelText = LocalizationService.Instance.GetString("Common_Cancel");
                        string confirmBtn = LocalizationService.Instance.GetString("Duplicates_Delete_Confirm_Btn");
                        string msg = isInSetlist 
                            ? string.Format(LocalizationService.Instance.GetString("Duplicates_Delete_Warning_Setlist"), setlists.Count, string.Join(", ", setlists), score.Title)
                            : string.Format(LocalizationService.Instance.GetString("Duplicates_Delete_Confirm"), score.Title);

                        bool confirm = await DisplayAlertAsync(deleteTitle, msg, confirmBtn, cancelText);
                        if (confirm)
                        {
                            await _databaseService.DeleteScoreAsync(score);
                            string successTitle = LocalizationService.Instance.GetString("Duplicates_Deleted_Success_Title");
                            string successMsg = LocalizationService.Instance.GetString("Duplicates_Deleted_Success_Msg");
                            await DisplayAlertAsync(successTitle, successMsg, LocalizationService.Instance.GetString("Common_OK"));
                            OnAnalyzeDuplicatesClicked(this, EventArgs.Empty);
                        }
                    };

                    itemGrid.Children.Add(infoStack);
                    Grid.SetColumn(infoStack, 0);
                    itemGrid.Children.Add(deleteBtn);
                    Grid.SetColumn(deleteBtn, 1);

                    itemBorder.Content = itemGrid;
                    groupStack.Children.Add(itemBorder);
                }

                groupCard.Content = groupStack;
                DuplicatesListContainer.Children.Add(groupCard);
                groupIndex++;
            }
        }
        catch (Exception ex)
        {
            DuplicatesProgressSection.IsVisible = false;
            await DisplayAlertAsync("Erreur d'analyse", $"Erreur lors de l'analyse des doublons : {ex.Message}", "OK");
        }
        finally
        {
            _isAnalyzingDuplicates = false;
        }
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} o";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} Ko";
        return $"{bytes / (1024.0 * 1024.0):F1} Mo";
    }

    private string? FindFileRecursively(string directory, string targetFileName)
    {
        try
        {
            if (!Directory.Exists(directory)) return null;
            var direct = Path.Combine(directory, targetFileName);
            if (File.Exists(direct)) return direct;

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                var match = FindFileRecursively(subDir, targetFileName);
                if (match != null) return match;
            }
        }
        catch { }
        return null;
    }
}
