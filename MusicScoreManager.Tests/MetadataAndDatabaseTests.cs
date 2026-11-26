using System.Text.Json;
using MusicScoreManager.Models;
using MusicScoreManager.Services;
using SQLite;
using Xunit;

namespace MusicScoreManager.Tests;

public class MetadataAndDatabaseTests
{
    [Fact]
    public void AnnotationMetadata_JsonRoundTrip_PreservesOpacityAndBorderColor()
    {
        var original = new ScoreTransferMetadata
        {
            Title = "Clair de Lune",
            FileName = "clair_de_lune.pdf",
            ShowAnnotations = true,
            Annotations =
            [
                new AnnotationMetadata
                {
                    PageNumber = 2,
                    Type = AnnotationType.Drawing,
                    Category = "Highlight",
                    Content = "0.1000,0.2000;0.5000,0.2000",
                    X = 0.1,
                    Y = 0.2,
                    Scale = 1.0,
                    Color = "#FFFF00",
                    BackgroundColor = "Transparent",
                    BorderColor = "Transparent",
                    Opacity = 0.28
                },
                new AnnotationMetadata
                {
                    PageNumber = 1,
                    Type = AnnotationType.Text,
                    Category = "Text",
                    Content = "pp subito",
                    X = 0.45,
                    Y = 0.32,
                    Scale = 14.0,
                    Color = "#FF0000",
                    BackgroundColor = "#FFFFFF",
                    BorderColor = "#007ACC",
                    Opacity = 0.85
                }
            ]
        };

        string json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<ScoreTransferMetadata>(json);

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.Annotations.Count);

        var hl = restored.Annotations[0];
        Assert.Equal(0.28, hl.Opacity, 4);
        Assert.Equal(2, hl.PageNumber);
        Assert.Equal("Highlight", hl.Category);

        var txt = restored.Annotations[1];
        Assert.Equal(0.85, txt.Opacity, 4);
        Assert.Equal("#007ACC", txt.BorderColor);
        Assert.Equal("#FFFFFF", txt.BackgroundColor);
        Assert.Equal(14.0, txt.Scale, 4);
    }

    [Fact]
    public async Task SqliteDatabase_RoundTrip_PreservesScoreSetlistAndAnnotationOpacity()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"msm_test_{Guid.NewGuid():N}.db3");
        var db = new SQLiteAsyncConnection(tempDb);
        try
        {
            await db.CreateTableAsync<Score>();
            await db.CreateTableAsync<Setlist>();
            await db.CreateTableAsync<SetlistScore>();
            await db.CreateTableAsync<Annotation>();

            var score = new Score { Title = "Test Sonata", FilePath = "test.pdf", Type = ScoreType.PDF, ShowAnnotations = true };
            await db.InsertAsync(score);

            var ann = new Annotation
            {
                ScoreId = score.Id,
                PageNumber = 2,
                Type = AnnotationType.Drawing,
                Category = "Highlight",
                Content = "0.1,0.2;0.8,0.2",
                Color = "#00E676",
                BorderColor = "#FF4081",
                Opacity = 0.42,
                Scale = 1.5
            };
            await db.InsertAsync(ann);

            var loaded = await db.Table<Annotation>().Where(a => a.ScoreId == score.Id).ToListAsync();
            Assert.Single(loaded);
            Assert.Equal(0.42, loaded[0].Opacity, 4);
            Assert.Equal("#FF4081", loaded[0].BorderColor);
            Assert.Equal(2, loaded[0].PageNumber);
        }
        finally
        {
            await db.CloseAsync();
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }
}
