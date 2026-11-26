namespace MusicScoreManager.Services;

/// <summary>
/// Stub léger de LocalizationService pour les tests unitaires hors environnement MAUI.
/// </summary>
public sealed class LocalizationService
{
    public static LocalizationService Instance { get; } = new();
    public string CurrentLanguage { get; set; } = "fr";
    public string GetString(string key, string fallback = "") => string.IsNullOrEmpty(fallback) ? key : fallback;
}
