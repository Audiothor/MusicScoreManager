using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace MusicScoreManager.Tests;

public class LocalizationConsistencyTests
{
    private static readonly string[] Languages = ["fr", "en", "de", "es", "it", "nl", "pl", "pt"];
    private static readonly Regex PlaceholderRegex = new(@"\{(\d+)(?::[^}]+)?\}", RegexOptions.Compiled);

    private static string GetLanguagesDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Resources", "Raw", "Languages");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Resources/Raw/Languages not found.");
    }

    [Fact]
    public void AllEightLanguageFiles_ExistAndAreValidJson()
    {
        string langDir = GetLanguagesDir();
        foreach (string lang in Languages)
        {
            string path = Path.Combine(langDir, $"{lang}.json");
            Assert.True(File.Exists(path), $"Fichier de langue manquant : {lang}.json");
            string json = File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            Assert.NotNull(dict);
            Assert.NotEmpty(dict!);
        }
    }

    [Fact]
    public void AllLanguages_ContainAllKeysFromFrenchReference()
    {
        string langDir = GetLanguagesDir();
        var fr = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(langDir, "fr.json")))!;

        var missingReport = new List<string>();
        foreach (string lang in Languages.Where(l => l != "fr"))
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(langDir, $"{lang}.json")))!;

            foreach (string key in fr.Keys)
            {
                if (!dict.ContainsKey(key))
                {
                    missingReport.Add($"{lang}.json manque la clé '{key}'");
                }
            }
        }

        Assert.True(missingReport.Count == 0,
            $"Clés manquantes ({missingReport.Count}) :\n" + string.Join("\n", missingReport.Take(25)));
    }

    [Fact]
    public void AllLanguages_HaveMatchingFormatPlaceholders()
    {
        string langDir = GetLanguagesDir();
        var fr = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(langDir, "fr.json")))!;

        var mismatches = new List<string>();
        foreach (string lang in Languages.Where(l => l != "fr"))
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(langDir, $"{lang}.json")))!;

            foreach (var (key, frValue) in fr)
            {
                if (!dict.TryGetValue(key, out string? otherValue)) continue;

                var frPlaceholders = PlaceholderRegex.Matches(frValue)
                    .Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x).ToList();
                var otherPlaceholders = PlaceholderRegex.Matches(otherValue)
                    .Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x).ToList();

                if (!frPlaceholders.SequenceEqual(otherPlaceholders))
                {
                    mismatches.Add($"{lang}.json['{key}']: fr={{{string.Join(",", frPlaceholders)}}} vs {lang}={{{string.Join(",", otherPlaceholders)}}}");
                }
            }
        }

        Assert.True(mismatches.Count == 0,
            $"Incohérences de marqueurs {{0}}/{{1}} ({mismatches.Count}) :\n" + string.Join("\n", mismatches.Take(25)));
    }
}
