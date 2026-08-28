using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace MusicScoreManager.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    private string _currentLanguage = "fr";
    private Dictionary<string, string> _strings = new();

    public string CurrentLanguage => _currentLanguage;

    public string this[string key] => GetString(key);

    public LocalizationService()
    {
        InitializeLanguage();
    }

    private void InitializeLanguage()
    {
        // Récupérer la langue enregistrée ou détecter la langue du système
        string defaultSysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        if (defaultSysLang != "fr" && defaultSysLang != "en" && defaultSysLang != "de" && defaultSysLang != "es")
        {
            defaultSysLang = "fr";
        }

        _currentLanguage = Preferences.Default.Get("AppLanguage", defaultSysLang);
        LoadLanguageStrings(_currentLanguage);
    }

    public async Task SetLanguageAsync(string langCode)
    {
        if (langCode != "fr" && langCode != "en" && langCode != "de" && langCode != "es")
        {
            langCode = "fr";
        }

        _currentLanguage = langCode;
        Preferences.Default.Set("AppLanguage", langCode);

        await LoadLanguageFileAsync(langCode);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key, string fallback = "")
    {
        if (string.IsNullOrEmpty(key)) return fallback;
        if (_strings.TryGetValue(key, out var val))
        {
            return val;
        }
        return string.IsNullOrEmpty(fallback) ? key : fallback;
    }

    private void LoadLanguageStrings(string langCode)
    {
        // Essayer d'ouvrir le fichier JSON depuis les assets
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync($"Languages/{langCode}.json").GetAwaiter().GetResult();
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null)
            {
                _strings = dict;
                return;
            }
        }
        catch
        {
            // Si le fichier package n'est pas encore accessible, charger le dictionnaire par défaut
        }

        // Fallback secours
        _strings = GetFallbackDictionary(langCode);
    }

    private async Task LoadLanguageFileAsync(string langCode)
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync($"Languages/{langCode}.json");
            using var reader = new StreamReader(stream);
            string json = await reader.ReadToEndAsync();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null)
            {
                _strings = dict;
                return;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Localization] Error loading {langCode}.json: {ex.Message}");
        }

        _strings = GetFallbackDictionary(langCode);
    }

    private static Dictionary<string, string> GetFallbackDictionary(string langCode)
    {
        return langCode switch
        {
            "en" => new Dictionary<string, string>
            {
                ["App_Title"] = "Music Score Manager",
                ["Nav_Scores"] = "Scores",
                ["Nav_Setlists"] = "Setlists",
                ["Nav_Tags"] = "Tags",
                ["Nav_Tools"] = "Tools",
                ["Nav_Settings"] = "Settings",
                ["Nav_Quit"] = "Quit",
                ["Search_Placeholder"] = "Search...",
                ["Scores_Loading"] = "Loading sheet music...",
                ["Scores_NotFound"] = "No sheet music found.",
                ["Settings_Title"] = "Settings",
                ["Settings_Scores"] = "Score Settings",
                ["Settings_Setlists"] = "Setlist Settings",
                ["Settings_Tags"] = "Tag Settings",
                ["Settings_Annotations"] = "Annotation Settings (Favorites)",
                ["Settings_App"] = "Application Settings",
                ["Settings_About"] = "About",
                ["Settings_Language"] = "Language",
                ["Settings_Language_Desc"] = "Select the display language for menus and application interface.",
                ["Settings_Library"] = "Library",
                ["Settings_ScoresPath"] = "Sheet music directory (PDF, Images)",
                ["Settings_AudioPath"] = "Audio files directory (MP3, WAV, AIF)",
                ["Settings_ChangePath"] = "Change directory",
                ["About_Title"] = "About",
                ["About_Description"] = "Professional sheet music management, annotation and viewing app for musicians in rehearsal and live performance.",
                ["About_License"] = "License & Copyright",
                ["About_AppLicense"] = "Music Score Manager is released under the GNU General Public License v3.0 (GPLv3). Copyright © 2026 Audiothor.",
                ["About_Frameworks"] = "Third-Party Frameworks & Libraries",
                ["About_GitHub"] = "Open Source Project on GitHub",
                ["Common_Back"] = "← Back",
                ["Common_Close"] = "Close",
                ["Common_Save"] = "Save",
                ["Common_Cancel"] = "Cancel",
                ["Common_Delete"] = "Delete",
                ["Common_Edit"] = "Edit",
                ["Common_Exchange"] = "Exchange",
                ["Common_Rename"] = "Rename"
            },
            "de" => new Dictionary<string, string>
            {
                ["App_Title"] = "Music Score Manager",
                ["Nav_Scores"] = "Noten",
                ["Nav_Setlists"] = "Setlisten",
                ["Nav_Tags"] = "Etiketten",
                ["Nav_Tools"] = "Werkzeuge",
                ["Nav_Settings"] = "Einstellungen",
                ["Nav_Quit"] = "Beenden",
                ["Search_Placeholder"] = "Suchen...",
                ["Scores_Loading"] = "Noten werden geladen...",
                ["Scores_NotFound"] = "Keine Noten gefunden.",
                ["Settings_Title"] = "Einstellungen",
                ["Settings_Scores"] = "Noten-Einstellungen",
                ["Settings_Setlists"] = "Setlist-Einstellungen",
                ["Settings_Tags"] = "Etiketten-Einstellungen",
                ["Settings_Annotations"] = "Anmerkungs-Einstellungen (Favoriten)",
                ["Settings_App"] = "Anwendungs-Einstellungen",
                ["Settings_About"] = "Über",
                ["Settings_Language"] = "Sprache",
                ["Settings_Language_Desc"] = "Wählen Sie die Anzeigesprache für die Menüs und die Benutzeroberfläche.",
                ["Settings_Library"] = "Bibliothek",
                ["Settings_ScoresPath"] = "Noten-Verzeichnis (PDF, Bilder)",
                ["Settings_AudioPath"] = "Audio-Dateien-Verzeichnis (MP3, WAV, AIF)",
                ["Settings_ChangePath"] = "Verzeichnis ändern",
                ["About_Title"] = "Über",
                ["About_Description"] = "Professionelle Notenverwaltung, Annotation und Anzeige für Musiker bei Proben und Konzerten.",
                ["About_License"] = "Lizenz & Urheberrecht",
                ["About_AppLicense"] = "Music Score Manager wird unter der GNU General Public License v3.0 (GPLv3) veröffentlicht. Copyright © 2026 Audiothor.",
                ["About_Frameworks"] = "Drittanbieter-Frameworks & Bibliotheken",
                ["About_GitHub"] = "Open-Source-Projekt auf GitHub",
                ["Common_Back"] = "← Zurück",
                ["Common_Close"] = "Schließen",
                ["Common_Save"] = "Speichern",
                ["Common_Cancel"] = "Abbrechen",
                ["Common_Delete"] = "Löschen",
                ["Common_Edit"] = "Bearbeiten",
                ["Common_Exchange"] = "Tauschen",
                ["Common_Rename"] = "Umbenennen"
            },
            "es" => new Dictionary<string, string>
            {
                ["App_Title"] = "Music Score Manager",
                ["Nav_Scores"] = "Partituras",
                ["Nav_Setlists"] = "Setlists",
                ["Nav_Tags"] = "Etiquetas",
                ["Nav_Tools"] = "Herramientas",
                ["Nav_Settings"] = "Ajustes",
                ["Nav_Quit"] = "Salir",
                ["Search_Placeholder"] = "Buscar...",
                ["Scores_Loading"] = "Cargando partituras...",
                ["Scores_NotFound"] = "No se encontraron partituras.",
                ["Settings_Title"] = "Ajustes",
                ["Settings_Scores"] = "Ajustes de Partituras",
                ["Settings_Setlists"] = "Ajustes de Setlists",
                ["Settings_Tags"] = "Ajustes de Etiquetas",
                ["Settings_Annotations"] = "Ajustes de Anotaciones (Favoritos)",
                ["Settings_App"] = "Ajustes de Aplicación",
                ["Settings_About"] = "Acerca de",
                ["Settings_Language"] = "Idioma",
                ["Settings_Language_Desc"] = "Seleccione el idioma de visualización de los menús y textos de la aplicación.",
                ["Settings_Library"] = "Biblioteca",
                ["Settings_ScoresPath"] = "Directorio de partituras (PDF, Imágenes)",
                ["Settings_AudioPath"] = "Directorio de archivos de audio (MP3, WAV, AIF)",
                ["Settings_ChangePath"] = "Cambiar directorio",
                ["About_Title"] = "Acerca de",
                ["About_Description"] = "Aplicación profesional para la gestión, anotación y visualización de partituras musicales para músicos en ensayos y conciertos.",
                ["About_License"] = "Licencia & Derechos de autor",
                ["About_AppLicense"] = "Music Score Manager se publica bajo la Licencia MIT. Copyright © 2026 Audiothor.",
                ["About_Frameworks"] = "Frameworks y bibliotecas de terceros",
                ["About_GitHub"] = "Proyecto de código abierto en GitHub",
                ["Common_Back"] = "← Volver",
                ["Common_Close"] = "Cerrar",
                ["Common_Save"] = "Guardar",
                ["Common_Cancel"] = "Cancelar",
                ["Common_Delete"] = "Eliminar",
                ["Common_Edit"] = "Editar",
                ["Common_Exchange"] = "Intercambiar",
                ["Common_Rename"] = "Renombrar"
            },
            _ => new Dictionary<string, string>
            {
                ["App_Title"] = "Music Score Manager",
                ["Nav_Scores"] = "Partitions",
                ["Nav_Setlists"] = "Setlists",
                ["Nav_Tags"] = "Étiquettes",
                ["Nav_Tools"] = "Outils",
                ["Nav_Settings"] = "Paramètres",
                ["Nav_Quit"] = "Quitter",
                ["Search_Placeholder"] = "Rechercher...",
                ["Scores_Loading"] = "Chargement des partitions en cours...",
                ["Scores_NotFound"] = "Aucune partition trouvée.",
                ["Settings_Title"] = "Paramètres",
                ["Settings_Scores"] = "Paramètres Partitions",
                ["Settings_Setlists"] = "Paramètres Setlists",
                ["Settings_Tags"] = "Paramètres Etiquettes",
                ["Settings_Annotations"] = "Paramètres Annotations (Favoris)",
                ["Settings_App"] = "Paramètres application",
                ["Settings_About"] = "À propos",
                ["Settings_Language"] = "Langue",
                ["Settings_Language_Desc"] = "Sélectionnez la langue d'affichage des menus et textes de l'application.",
                ["Settings_Library"] = "Bibliothèque",
                ["Settings_ScoresPath"] = "Répertoire des partitions (PDF, Images)",
                ["Settings_AudioPath"] = "Répertoire des fichiers audio (MP3, WAV, AIF)",
                ["Settings_ChangePath"] = "Changer le répertoire",
                ["About_Title"] = "À propos",
                ["About_Description"] = "Application professionnelle de gestion, d'annotation et de visualisation de partitions musicales pour musiciens en répétition et en concert.",
                ["About_License"] = "Licence & Droits d'auteur",
                ["About_AppLicense"] = "Music Score Manager est publié sous Licence Publique Générale GNU v3.0 (GPLv3). Copyright © 2026 Audiothor.",
                ["About_Frameworks"] = "Frameworks & Bibliothèques tierces",
                ["About_GitHub"] = "Projet Open Source sur GitHub",
                ["Common_Back"] = "← Retour",
                ["Common_Close"] = "Fermer",
                ["Common_Save"] = "Enregistrer",
                ["Common_Cancel"] = "Annuler",
                ["Common_Delete"] = "Supprimer",
                ["Common_Edit"] = "Éditer",
                ["Common_Exchange"] = "Échanger",
                ["Common_Rename"] = "Renommer"
            }
        };
    }
}
