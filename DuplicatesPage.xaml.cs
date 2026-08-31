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
                DuplicatesResultSummaryLabel.Text = "✅ Aucun doublon détecté dans votre bibliothèque !";
                DuplicatesResultSummaryLabel.TextColor = Color.FromArgb("#2ECC71");
                DuplicatesResultSummaryLabel.IsVisible = true;
                return;
            }

            int totalDuplicatesCount = duplicateGroups.Sum(g => g.Value.Count);
            DuplicatesResultSummaryLabel.Text = $"⚠️ {duplicateGroups.Count} groupe(s) de doublons trouvés ({totalDuplicatesCount} partitions concernées) :";
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
                
                var headerLabel = new Label
                {
                    Text = $"Groupe #{groupIndex} — {group.Value.Count} partitions identiques ({sizeStr})",
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
                        Text = $"📁 {score.FilePath}",
                        TextColor = Color.FromArgb("#888888"),
                        FontSize = 11,
                        LineBreakMode = LineBreakMode.TailTruncation
                    };

                    var usageLabel = new Label
                    {
                        Text = isInSetlist 
                            ? $"⚠️ Utilisé dans : {string.Join(", ", setlists)}" 
                            : "🟢 Non utilisé en setlist (suppression sûre)",
                        TextColor = isInSetlist ? Color.FromArgb("#E67E22") : Color.FromArgb("#2ECC71"),
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold
                    };

                    infoStack.Children.Add(titleLabel);
                    infoStack.Children.Add(pathLabel);
                    infoStack.Children.Add(usageLabel);

                    var deleteBtn = new Button
                    {
                        Text = "🗑️ Supprimer",
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
                        string msg = isInSetlist 
                            ? $"⚠️ ATTENTION : Cette partition '{score.Title}' est actuellement utilisée dans : {string.Join(", ", setlists)}.\n\nVoulez-vous vraiment supprimer cette référence de doublon ?" 
                            : $"Voulez-vous supprimer le doublon '{score.Title}' ?";

                        bool confirm = await DisplayAlertAsync("Supprimer ce doublon", msg, "Oui, supprimer", "Annuler");
                        if (confirm)
                        {
                            await _databaseService.DeleteScoreAsync(score);
                            await DisplayAlertAsync("Supprimé", $"Le doublon '{score.Title}' a été supprimé.", "OK");
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
}
