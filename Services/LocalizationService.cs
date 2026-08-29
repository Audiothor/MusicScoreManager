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
        // Détecter la langue du système d'exploitation
        string sysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(sysLang))
        {
            sysLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLowerInvariant();
        }

        // Si la langue de l'OS figure parmi nos 8 langues supportées, on la retient par défaut, sinon anglais ("en")
        string defaultSysLang = sysLang switch
        {
            "fr" => "fr",
            "en" => "en",
            "de" => "de",
            "es" => "es",
            "it" => "it",
            "pl" => "pl",
            "nl" => "nl",
            "pt" => "pt",
            _ => "en"
        };

        // Si l'utilisateur a déjà configuré une langue dans ses préférences, on la charge
        _currentLanguage = Preferences.Default.Get("AppLanguage", defaultSysLang);

        // Sécurité supplémentaire : vérifier que la langue issue des préférences est bien supportée
        if (_currentLanguage != "fr" && _currentLanguage != "en" && _currentLanguage != "de" &&
            _currentLanguage != "es" && _currentLanguage != "it" && _currentLanguage != "pl" &&
            _currentLanguage != "nl" && _currentLanguage != "pt")
        {
            _currentLanguage = defaultSysLang;
        }

        LoadLanguageStrings(_currentLanguage);
    }

    public async Task SetLanguageAsync(string langCode)
    {
        if (langCode != "fr" && langCode != "en" && langCode != "de" && langCode != "es" &&
            langCode != "it" && langCode != "pl" && langCode != "nl" && langCode != "pt")
        {
            langCode = "en";
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
                ["Nav_Quit_Confirm_Title"] = "Quit Application",
                ["Nav_Quit_Confirm_Msg"] = "Do you really want to exit Music Score Manager?",
                ["Search_Placeholder"] = "Search for a score, composer, tag...",
                ["Scores_Title"] = "Scores",
                ["Scores_Loading"] = "Loading sheet music...",
                ["Scores_NotFound"] = "No sheet music found.",
                ["Scores_Add"] = "Add Score",
                ["Scores_Import"] = "Import Scores",
                ["Scores_Delete_Confirm"] = "Are you sure you want to delete this score?",
                ["Scores_Total"] = "Total Scores",
                ["Scores_PDF_Count"] = "PDF Scores",
                ["Scores_Image_Count"] = "Image Scores",
                ["Scores_Filter_All"] = "All",
                ["Scores_Filter_PDF"] = "PDF",
                ["Scores_Filter_Image"] = "Images",
                ["Scores_Sort_Title"] = "Title",
                ["Scores_Sort_Date"] = "Date Added",
                ["Setlists_Title"] = "Setlists",
                ["Setlists_New"] = "New Setlist",
                ["Setlists_Edit"] = "Edit Setlist",
                ["Setlists_Delete_Confirm"] = "Are you sure you want to delete this setlist?",
                ["Setlists_Empty"] = "No setlist created.",
                ["Setlist_Name"] = "Setlist Name",
                ["Setlist_Concert_Date"] = "Concert Date",
                ["Setlist_Concert_Time"] = "Concert Time",
                ["Setlist_Status"] = "Status",
                ["Setlist_Status_Draft"] = "Draft",
                ["Setlist_Status_Active"] = "Active",
                ["Setlist_Status_Archived"] = "Archived",
                ["Setlist_Add_Score"] = "Add Scores",
                ["Setlist_Continuous_Mode"] = "Continuous Mode (auto-advance)",
                ["Setlist_Lock_Order"] = "Lock Order",
                ["Tags_Title"] = "Tags",
                ["Tags_New"] = "New Tag",
                ["Tags_Manage"] = "Manage Tags",
                ["Tags_Name_Placeholder"] = "Tag Name",
                ["Tags_Color"] = "Color",
                ["Tags_Empty"] = "No tag defined.",
                ["Tags_Delete_Confirm"] = "Delete this tag?",
                ["Tools_Title"] = "Tools",
                ["Tools_Tags_Section"] = "Tags",
                ["Tools_Backup_Section"] = "Backup Management",
                ["Tools_Bluetooth_Transfer"] = "Bluetooth Transfer (Share)",
                ["Tools_Backup_Create"] = "Create Immediate Backup",
                ["Tools_Backup_Restore"] = "Restore Backup",
                ["Tools_Backup_Auto"] = "Daily Automatic Backup",
                ["Tools_Backup_Retention"] = "Backup Retention (days)",
                ["Tools_Backup_History"] = "Backup History",
                ["Viewer_Menu_Title"] = "Score Menu",
                ["Viewer_Rotate"] = "Rotate (+90°)",
                ["Viewer_Rotate_Page_Only"] = "Apply to this page only",
                ["Viewer_Rotate_All_Pages"] = "Apply to ALL pages",
                ["Viewer_Remember_Rotation"] = "Remember rotations",
                ["Viewer_Metronome"] = "Show Metronome",
                ["Viewer_Audio_Player"] = "Show Audio Player",
                ["Viewer_Edit_Score"] = "Edit Score",
                ["Viewer_Home"] = "Return Home",
                ["Viewer_GoToPage"] = "Go to Page",
                ["Viewer_Page_Indicator"] = "Page",
                ["Viewer_Locked_Alert"] = "Annotations are locked. Unlock them (🔓) to edit annotations.",
                ["Viewer_Text_Prompt_Title"] = "Add Text",
                ["Viewer_Text_Prompt_Msg"] = "Type your text or word:",
                ["Viewer_Text_Edit_Title"] = "Edit Text",
                ["Viewer_Clean_Annotations_Confirm"] = "Delete all annotations on this page?",
                ["Annotation_Highlighter"] = "Highlighter",
                ["Annotation_Pencil"] = "Pencil",
                ["Annotation_Text"] = "Text",
                ["Annotation_Stickers"] = "Stickers",
                ["Annotation_Undo"] = "Undo",
                ["Annotation_Redo"] = "Redo",
                ["Annotation_Clean"] = "Clear Page",
                ["Annotation_Lock"] = "Lock",
                ["Annotation_Unlock"] = "Unlock",
                ["Annotation_Delete"] = "Delete Selection",
                ["Annotation_Size"] = "Size",
                ["Annotation_Color"] = "Color",
                ["Annotation_BgColor"] = "Background Color",
                ["Settings_Title"] = "Settings",
                ["Settings_Scores"] = "Score Settings",
                ["Settings_Setlists"] = "Setlist Settings",
                ["Settings_Tags"] = "Tag Settings",
                ["Settings_Annotations"] = "Annotation Settings (Favorites)",
                ["Settings_App"] = "Application Settings",
                ["Settings_Help"] = "Help & User Guide",
                ["Settings_About"] = "About",
                ["Settings_Language"] = "Language",
                ["Settings_Language_Desc"] = "Select the display language for menus and application interface.",
                ["Settings_Library"] = "Library",
                ["Settings_ScoresPath"] = "Sheet music directory (PDF, Images)",
                ["Settings_AudioPath"] = "Audio files directory (MP3, WAV, AIF)",
                ["Settings_ChangePath"] = "Change directory",
                ["Settings_Display"] = "Display",
                ["Settings_Ergonomics"] = "Ergonomics",
                ["Settings_Next_Page_Gesture"] = "Next Page Gesture",
                ["Settings_Prev_Page_Gesture"] = "Previous Page Gesture",
                ["Settings_Turn_Page_Touch_Zone"] = "Page Turn Touch Zones",
                ["BT_Title"] = "Bluetooth Transfer",
                ["BT_Send"] = "Send Scores",
                ["BT_Receive"] = "Receive Scores",
                ["BT_Scanning"] = "Scanning for devices...",
                ["BT_Scan_Devices"] = "Scan for Devices",
                ["BT_Stop_Scan"] = "Stop Scan",
                ["BT_Select_Device"] = "Select Target Device",
                ["BT_Transfer_Progress"] = "Transfer in progress...",
                ["BT_Transfer_Success"] = "Transfer completed successfully!",
                ["BT_Transfer_Failed"] = "Bluetooth transfer failed.",
                ["BT_Confirm_Receive_Title"] = "Incoming Transfer",
                ["BT_Confirm_Receive_Msg"] = "Accept sheet music sent by {0}?",
                ["About_Title"] = "About",
                ["About_Description"] = "Professional sheet music management, annotation and viewing app for musicians in rehearsal and live performance.",
                ["About_Version"] = "Version",
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
                ["Common_Rename"] = "Rename",
                ["Common_OK"] = "OK",
                ["Common_Yes"] = "Yes",
                ["Common_No"] = "No",
                ["Common_Loading"] = "Loading...",
                ["Common_Ready"] = "Ready",
                ["Common_Error"] = "Error",
                ["Common_Success"] = "Success",
                ["Common_Warning"] = "Warning",
                ["Common_Info"] = "Information"
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
                ["Nav_Quit_Confirm_Title"] = "Anwendung beenden",
                ["Nav_Quit_Confirm_Msg"] = "Möchten Sie Music Score Manager wirklich beenden?",
                ["Search_Placeholder"] = "Noten, Komponist, Etikett suchen...",
                ["Scores_Title"] = "Noten",
                ["Scores_Loading"] = "Noten werden geladen...",
                ["Scores_NotFound"] = "Keine Noten gefunden.",
                ["Scores_Add"] = "Noten hinzufügen",
                ["Scores_Import"] = "Noten importieren",
                ["Scores_Delete_Confirm"] = "Sind Sie sicher, dass Sie diese Noten löschen möchten?",
                ["Scores_Total"] = "Noten insgesamt",
                ["Scores_PDF_Count"] = "PDF-Noten",
                ["Scores_Image_Count"] = "Bild-Noten",
                ["Scores_Filter_All"] = "Alle",
                ["Scores_Filter_PDF"] = "PDF",
                ["Scores_Filter_Image"] = "Bilder",
                ["Scores_Sort_Title"] = "Titel",
                ["Scores_Sort_Date"] = "Hinzugefügt am",
                ["Setlists_Title"] = "Setlisten",
                ["Setlists_New"] = "Neue Setlist",
                ["Setlists_Edit"] = "Setlist bearbeiten",
                ["Setlists_Delete_Confirm"] = "Sind Sie sicher, dass Sie diese Setlist löschen möchten?",
                ["Setlists_Empty"] = "Keine Setlisten erstellt.",
                ["Setlist_Name"] = "Setlist-Name",
                ["Setlist_Concert_Date"] = "Konzertdatum",
                ["Setlist_Concert_Time"] = "Konzertuhrzeit",
                ["Setlist_Status"] = "Status",
                ["Setlist_Status_Draft"] = "Entwurf",
                ["Setlist_Status_Active"] = "Aktiv",
                ["Setlist_Status_Archived"] = "Archiviert",
                ["Setlist_Add_Score"] = "Noten hinzufügen",
                ["Setlist_Continuous_Mode"] = "Fortlaufender Modus (automatischer Übergang)",
                ["Setlist_Lock_Order"] = "Reihenfolge sperren",
                ["Tags_Title"] = "Etiketten",
                ["Tags_New"] = "Neues Etikett",
                ["Tags_Manage"] = "Etiketten-Verwaltung",
                ["Tags_Name_Placeholder"] = "Etikettenname",
                ["Tags_Color"] = "Farbe",
                ["Tags_Empty"] = "Keine Etiketten definiert.",
                ["Tags_Delete_Confirm"] = "Dieses Etikett löschen?",
                ["Tools_Title"] = "Werkzeuge",
                ["Tools_Tags_Section"] = "Etiketten",
                ["Tools_Backup_Section"] = "Sicherungsverwaltung",
                ["Tools_Bluetooth_Transfer"] = "Bluetooth-Übertragung (Teilen)",
                ["Tools_Backup_Create"] = "Sofortige Sicherung erstellen",
                ["Tools_Backup_Restore"] = "Sicherung wiederherstellen",
                ["Tools_Backup_Auto"] = "Tägliche automatische Sicherung",
                ["Tools_Backup_Retention"] = "Sicherungsaufbewahrung (Tage)",
                ["Tools_Backup_History"] = "Sicherungsverlauf",
                ["Viewer_Menu_Title"] = "Noten-Menü",
                ["Viewer_Rotate"] = "Drehung (+90°)",
                ["Viewer_Rotate_Page_Only"] = "Nur auf diese Seite anwenden",
                ["Viewer_Rotate_All_Pages"] = "Auf GESAMTE Noten anwenden",
                ["Viewer_Remember_Rotation"] = "Drehungen merken",
                ["Viewer_Metronome"] = "Metronom anzeigen",
                ["Viewer_Audio_Player"] = "Audio-Player anzeigen",
                ["Viewer_Edit_Score"] = "Noten bearbeiten",
                ["Viewer_Home"] = "Zurück zur Startseite",
                ["Viewer_GoToPage"] = "Gehe zu Seite",
                ["Viewer_Page_Indicator"] = "Seite",
                ["Viewer_Locked_Alert"] = "Anmerkungen sind gesperrt. Entsperren Sie sie (🔓), um Notizen zu machen.",
                ["Viewer_Text_Prompt_Title"] = "Text hinzufügen",
                ["Viewer_Text_Prompt_Msg"] = "Geben Sie Ihren Text oder Ihr Wort ein:",
                ["Viewer_Text_Edit_Title"] = "Text bearbeiten",
                ["Viewer_Clean_Annotations_Confirm"] = "Alle Anmerkungen auf dieser Seite löschen?",
                ["Annotation_Highlighter"] = "Textmarker",
                ["Annotation_Pencil"] = "Bleistift",
                ["Annotation_Text"] = "Text",
                ["Annotation_Stickers"] = "Aufkleber",
                ["Annotation_Undo"] = "Rückgängig",
                ["Annotation_Redo"] = "Wiederholen",
                ["Annotation_Clean"] = "Seite löschen",
                ["Annotation_Lock"] = "Sperren",
                ["Annotation_Unlock"] = "Entsperren",
                ["Annotation_Delete"] = "Auswahl löschen",
                ["Annotation_Size"] = "Größe",
                ["Annotation_Color"] = "Farbe",
                ["Annotation_BgColor"] = "Hintergrundfarbe",
                ["Settings_Title"] = "Einstellungen",
                ["Settings_Scores"] = "Noten-Einstellungen",
                ["Settings_Setlists"] = "Setlist-Einstellungen",
                ["Settings_Tags"] = "Etiketten-Einstellungen",
                ["Settings_Annotations"] = "Anmerkungs-Einstellungen (Favoriten)",
                ["Settings_App"] = "Anwendungs-Einstellungen",
                ["Settings_Help"] = "Hilfe & Benutzerhandbuch",
                ["Settings_About"] = "Über",
                ["Settings_Language"] = "Sprache",
                ["Settings_Language_Desc"] = "Wählen Sie die Anzeigesprache für die Menüs und die Benutzeroberfläche.",
                ["Settings_Library"] = "Bibliothek",
                ["Settings_ScoresPath"] = "Noten-Verzeichnis (PDF, Bilder)",
                ["Settings_AudioPath"] = "Audio-Dateien-Verzeichnis (MP3, WAV, AIF)",
                ["Settings_ChangePath"] = "Verzeichnis ändern",
                ["Settings_Display"] = "Anzeige",
                ["Settings_Ergonomics"] = "Ergonomie",
                ["Settings_Next_Page_Gesture"] = "Geste für nächste Seite",
                ["Settings_Prev_Page_Gesture"] = "Geste für vorherige Seite",
                ["Settings_Turn_Page_Touch_Zone"] = "Touch-Bereiche zum Umblättern",
                ["BT_Title"] = "Bluetooth-Übertragung",
                ["BT_Send"] = "Noten senden",
                ["BT_Receive"] = "Noten empfangen",
                ["BT_Scanning"] = "Suche nach Geräten...",
                ["BT_Scan_Devices"] = "Geräte suchen",
                ["BT_Stop_Scan"] = "Suche beenden",
                ["BT_Select_Device"] = "Zielgerät auswählen",
                ["BT_Transfer_Progress"] = "Übertragung läuft...",
                ["BT_Transfer_Success"] = "Übertragung erfolgreich abgeschlossen!",
                ["BT_Transfer_Failed"] = "Bluetooth-Übertragung fehlgeschlagen.",
                ["BT_Confirm_Receive_Title"] = "Eingehende Übertragung",
                ["BT_Confirm_Receive_Msg"] = "Von {0} gesendete Noten annehmen?",
                ["About_Title"] = "Über",
                ["About_Description"] = "Professionelle Notenverwaltung, Annotation und Anzeige für Musiker bei Proben und Konzerten.",
                ["About_Version"] = "Version",
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
                ["Common_Rename"] = "Umbenennen",
                ["Common_OK"] = "OK",
                ["Common_Yes"] = "Ja",
                ["Common_No"] = "Nein",
                ["Common_Loading"] = "Wird geladen...",
                ["Common_Ready"] = "Bereit",
                ["Common_Error"] = "Fehler",
                ["Common_Success"] = "Erfolg",
                ["Common_Warning"] = "Warnung",
                ["Common_Info"] = "Information"
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
                ["Nav_Quit_Confirm_Title"] = "Salir de la aplicación",
                ["Nav_Quit_Confirm_Msg"] = "¿Realmente desea salir de Music Score Manager?",
                ["Search_Placeholder"] = "Buscar partitura, compositor, etiqueta...",
                ["Scores_Title"] = "Partituras",
                ["Scores_Loading"] = "Cargando partituras...",
                ["Scores_NotFound"] = "No se encontraron partituras.",
                ["Scores_Add"] = "Añadir partitura",
                ["Scores_Import"] = "Importar partituras",
                ["Scores_Delete_Confirm"] = "¿Está seguro de que desea eliminar esta partitura?",
                ["Scores_Total"] = "Total de partituras",
                ["Scores_PDF_Count"] = "Partituras PDF",
                ["Scores_Image_Count"] = "Partituras Imágenes",
                ["Scores_Filter_All"] = "Todas",
                ["Scores_Filter_PDF"] = "PDF",
                ["Scores_Filter_Image"] = "Imágenes",
                ["Scores_Sort_Title"] = "Título",
                ["Scores_Sort_Date"] = "Fecha de adición",
                ["Setlists_Title"] = "Setlists",
                ["Setlists_New"] = "Nueva Setlist",
                ["Setlists_Edit"] = "Editar Setlist",
                ["Setlists_Delete_Confirm"] = "¿Está seguro de que desea eliminar esta setlist?",
                ["Setlists_Empty"] = "No hay setlists creadas.",
                ["Setlist_Name"] = "Nombre de la Setlist",
                ["Setlist_Concert_Date"] = "Fecha del concierto",
                ["Setlist_Concert_Time"] = "Hora del concierto",
                ["Setlist_Status"] = "Estado",
                ["Setlist_Status_Draft"] = "Borrador",
                ["Setlist_Status_Active"] = "En curso",
                ["Setlist_Status_Archived"] = "Archivada",
                ["Setlist_Add_Score"] = "Añadir partituras",
                ["Setlist_Continuous_Mode"] = "Modo continuo (avance automático)",
                ["Setlist_Lock_Order"] = "Bloquear orden",
                ["Tags_Title"] = "Etiquetas",
                ["Tags_New"] = "Nueva etiqueta",
                ["Tags_Manage"] = "Gestión de etiquetas",
                ["Tags_Name_Placeholder"] = "Nombre de la etiqueta",
                ["Tags_Color"] = "Color",
                ["Tags_Empty"] = "No hay etiquetas definidas.",
                ["Tags_Delete_Confirm"] = "¿Eliminar esta etiqueta?",
                ["Tools_Title"] = "Herramientas",
                ["Tools_Tags_Section"] = "Etiquetas",
                ["Tools_Backup_Section"] = "Gestión de copias de seguridad",
                ["Tools_Bluetooth_Transfer"] = "Transferencia Bluetooth (Compartir)",
                ["Tools_Backup_Create"] = "Crear copia de seguridad inmediata",
                ["Tools_Backup_Restore"] = "Restaurar copia de seguridad",
                ["Tools_Backup_Auto"] = "Copia de seguridad automática diaria",
                ["Tools_Backup_Retention"] = "Retención de copias de seguridad (días)",
                ["Tools_Backup_History"] = "Historial de copias de seguridad",
                ["Viewer_Menu_Title"] = "Menú de Partitura",
                ["Viewer_Rotate"] = "Rotación (+90°)",
                ["Viewer_Rotate_Page_Only"] = "Aplicar solo a esta página",
                ["Viewer_Rotate_All_Pages"] = "Aplicar a TODA la partitura",
                ["Viewer_Remember_Rotation"] = "Recordar rotaciones",
                ["Viewer_Metronome"] = "Mostrar metrónomo",
                ["Viewer_Audio_Player"] = "Mostrar reproductor de audio",
                ["Viewer_Edit_Score"] = "Modificar partitura",
                ["Viewer_Home"] = "Volver al inicio",
                ["Viewer_GoToPage"] = "Ir a la página",
                ["Viewer_Page_Indicator"] = "Página",
                ["Viewer_Locked_Alert"] = "Las anotaciones están bloqueadas. Desbloquéelas (🔓) para poder anotar.",
                ["Viewer_Text_Prompt_Title"] = "Añadir texto",
                ["Viewer_Text_Prompt_Msg"] = "Escriba su texto o palabra:",
                ["Viewer_Text_Edit_Title"] = "Modificar texto",
                ["Viewer_Clean_Annotations_Confirm"] = "¿Eliminar todas las anotaciones de esta página?",
                ["Annotation_Highlighter"] = "Subrayador",
                ["Annotation_Pencil"] = "Lápiz",
                ["Annotation_Text"] = "Texto",
                ["Annotation_Stickers"] = "Pegatinas",
                ["Annotation_Undo"] = "Deshacer",
                ["Annotation_Redo"] = "Rehacer",
                ["Annotation_Clean"] = "Borrar página",
                ["Annotation_Lock"] = "Bloquear",
                ["Annotation_Unlock"] = "Desbloquear",
                ["Annotation_Delete"] = "Eliminar selección",
                ["Annotation_Size"] = "Tamaño",
                ["Annotation_Color"] = "Color",
                ["Annotation_BgColor"] = "Color de fondo",
                ["Settings_Title"] = "Ajustes",
                ["Settings_Scores"] = "Ajustes de Partituras",
                ["Settings_Setlists"] = "Ajustes de Setlists",
                ["Settings_Tags"] = "Ajustes de Etiquetas",
                ["Settings_Annotations"] = "Ajustes de Anotaciones (Favoritos)",
                ["Settings_App"] = "Ajustes de Aplicación",
                ["Settings_Help"] = "Ayuda y Guía del Usuario",
                ["Settings_About"] = "Acerca de",
                ["Settings_Language"] = "Idioma",
                ["Settings_Language_Desc"] = "Seleccione el idioma de visualización de los menús y textos de la aplicación.",
                ["Settings_Library"] = "Biblioteca",
                ["Settings_ScoresPath"] = "Directorio de partituras (PDF, Imágenes)",
                ["Settings_AudioPath"] = "Directorio de archivos de audio (MP3, WAV, AIF)",
                ["Settings_ChangePath"] = "Cambiar directorio",
                ["Settings_Display"] = "Visualización",
                ["Settings_Ergonomics"] = "Ergonomía",
                ["Settings_Next_Page_Gesture"] = "Gesto de página siguiente",
                ["Settings_Prev_Page_Gesture"] = "Gesto de página anterior",
                ["Settings_Turn_Page_Touch_Zone"] = "Zonas táctiles para pasar página",
                ["BT_Title"] = "Intercambio Bluetooth",
                ["BT_Send"] = "Enviar partituras",
                ["BT_Receive"] = "Recibir partituras",
                ["BT_Scanning"] = "Buscando dispositivos...",
                ["BT_Scan_Devices"] = "Buscar dispositivos",
                ["BT_Stop_Scan"] = "Detener búsqueda",
                ["BT_Select_Device"] = "Seleccione un dispositivo de destino",
                ["BT_Transfer_Progress"] = "Transferencia en curso...",
                ["BT_Transfer_Success"] = "¡Transferencia realizada con éxito!",
                ["BT_Transfer_Failed"] = "Error en la transferencia Bluetooth.",
                ["BT_Confirm_Receive_Title"] = "Transferencia entrante",
                ["BT_Confirm_Receive_Msg"] = "¿Aceptar las partituras enviadas por {0}?",
                ["About_Title"] = "Acerca de",
                ["About_Description"] = "Aplicación profesional para la gestión, anotación y visualización de partituras musicales para músicos en ensayos y conciertos.",
                ["About_Version"] = "Versión",
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
                ["Common_Rename"] = "Renombrar",
                ["Common_OK"] = "OK",
                ["Common_Yes"] = "Sí",
                ["Common_No"] = "No",
                ["Common_Loading"] = "Cargando...",
                ["Common_Ready"] = "Listo",
                ["Common_Error"] = "Error",
                ["Common_Success"] = "Éxito",
                ["Common_Warning"] = "Advertencia",
                ["Common_Info"] = "Información"
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
                ["Nav_Quit_Confirm_Title"] = "Quitter l'application",
                ["Nav_Quit_Confirm_Msg"] = "Voulez-vous vraiment quitter Music Score Manager ?",
                ["Search_Placeholder"] = "Rechercher une partition, un auteur, une étiquette...",
                ["Scores_Title"] = "Partitions",
                ["Scores_Loading"] = "Chargement des partitions en cours...",
                ["Scores_NotFound"] = "Aucune partition trouvée.",
                ["Scores_Add"] = "Ajouter une partition",
                ["Scores_Import"] = "Importer des partitions",
                ["Scores_Delete_Confirm"] = "Êtes-vous sûr de vouloir supprimer cette partition ?",
                ["Scores_Total"] = "Total partitions",
                ["Scores_PDF_Count"] = "Partitions PDF",
                ["Scores_Image_Count"] = "Partitions Images",
                ["Scores_Filter_All"] = "Toutes",
                ["Scores_Filter_PDF"] = "PDF",
                ["Scores_Filter_Image"] = "Images",
                ["Scores_Sort_Title"] = "Titre",
                ["Scores_Sort_Date"] = "Date d'ajout",
                ["Setlists_Title"] = "Setlists",
                ["Setlists_New"] = "Nouvelle Setlist",
                ["Setlists_Edit"] = "Éditer la Setlist",
                ["Setlists_Delete_Confirm"] = "Êtes-vous sûr de vouloir supprimer cette setlist ?",
                ["Setlists_Empty"] = "Aucune setlist créée.",
                ["Setlist_Name"] = "Nom de la Setlist",
                ["Setlist_Concert_Date"] = "Date du concert",
                ["Setlist_Concert_Time"] = "Heure du concert",
                ["Setlist_Status"] = "Statut",
                ["Setlist_Status_Draft"] = "Brouillon",
                ["Setlist_Status_Active"] = "En cours",
                ["Setlist_Status_Archived"] = "Archivée",
                ["Setlist_Add_Score"] = "Ajouter des partitions",
                ["Setlist_Continuous_Mode"] = "Mode continu (enchaînement auto)",
                ["Setlist_Lock_Order"] = "Verrouiller l'ordre",
                ["Tags_Title"] = "Étiquettes",
                ["Tags_New"] = "Nouvelle étiquette",
                ["Tags_Manage"] = "Gestion des étiquettes",
                ["Tags_Name_Placeholder"] = "Nom de l'étiquette",
                ["Tags_Color"] = "Couleur",
                ["Tags_Empty"] = "Aucune étiquette définie.",
                ["Tags_Delete_Confirm"] = "Supprimer cette étiquette ?",
                ["Tools_Title"] = "Outils",
                ["Tools_Tags_Section"] = "Étiquettes",
                ["Tools_Backup_Section"] = "Gestion des sauvegardes",
                ["Tools_Bluetooth_Transfer"] = "Échange Bluetooth (Partage)",
                ["Tools_Backup_Create"] = "Créer une sauvegarde immédiate",
                ["Tools_Backup_Restore"] = "Restaurer une sauvegarde",
                ["Tools_Backup_Auto"] = "Sauvegarde automatique quotidienne",
                ["Tools_Backup_Retention"] = "Rétention des sauvegardes (jours)",
                ["Tools_Backup_History"] = "Historique des sauvegardes",
                ["Viewer_Menu_Title"] = "Menu Partition",
                ["Viewer_Rotate"] = "Rotation (+90°)",
                ["Viewer_Rotate_Page_Only"] = "Appliquer à cette page uniquement",
                ["Viewer_Rotate_All_Pages"] = "Appliquer à TOUTE la partition",
                ["Viewer_Remember_Rotation"] = "Mémoriser les rotations",
                ["Viewer_Metronome"] = "Afficher le métronome",
                ["Viewer_Audio_Player"] = "Afficher le lecteur audio",
                ["Viewer_Edit_Score"] = "Modifier la partition",
                ["Viewer_Home"] = "Retour à l'accueil",
                ["Viewer_GoToPage"] = "Aller à la page",
                ["Viewer_Page_Indicator"] = "Page",
                ["Viewer_Locked_Alert"] = "Les annotations sont verrouillées. Déverrouillez-les (🔓) pour pouvoir annoter.",
                ["Viewer_Text_Prompt_Title"] = "Ajouter du texte",
                ["Viewer_Text_Prompt_Msg"] = "Tapez votre mot ou texte :",
                ["Viewer_Text_Edit_Title"] = "Modifier le texte",
                ["Viewer_Clean_Annotations_Confirm"] = "Supprimer toutes les annotations de cette page ?",
                ["Annotation_Highlighter"] = "Surligneur",
                ["Annotation_Pencil"] = "Crayon",
                ["Annotation_Text"] = "Texte",
                ["Annotation_Stickers"] = "Stickers",
                ["Annotation_Undo"] = "Annuler",
                ["Annotation_Redo"] = "Rétablir",
                ["Annotation_Clean"] = "Effacer la page",
                ["Annotation_Lock"] = "Verrouiller",
                ["Annotation_Unlock"] = "Déverrouiller",
                ["Annotation_Delete"] = "Supprimer la sélection",
                ["Annotation_Size"] = "Taille",
                ["Annotation_Color"] = "Couleur",
                ["Annotation_BgColor"] = "Couleur de fond",
                ["Settings_Title"] = "Paramètres",
                ["Settings_Scores"] = "Paramètres Partitions",
                ["Settings_Setlists"] = "Paramètres Setlists",
                ["Settings_Tags"] = "Paramètres Étiquettes",
                ["Settings_Annotations"] = "Paramètres Annotations (Favoris)",
                ["Settings_App"] = "Paramètres application",
                ["Settings_Help"] = "Aide & Guide d'utilisation",
                ["Settings_About"] = "À propos",
                ["Settings_Language"] = "Langue",
                ["Settings_Language_Desc"] = "Sélectionnez la langue d'affichage des menus et textes de l'application.",
                ["Settings_Library"] = "Bibliothèque",
                ["Settings_ScoresPath"] = "Répertoire des partitions (PDF, Images)",
                ["Settings_AudioPath"] = "Répertoire des fichiers audio (MP3, WAV, AIF)",
                ["Settings_ChangePath"] = "Changer le répertoire",
                ["Settings_Display"] = "Affichage",
                ["Settings_Ergonomics"] = "Ergonomie",
                ["Settings_Next_Page_Gesture"] = "Geste page suivante",
                ["Settings_Prev_Page_Gesture"] = "Geste page précédente",
                ["Settings_Turn_Page_Touch_Zone"] = "Zones tactiles de tourne de page",
                ["BT_Title"] = "Échange Bluetooth",
                ["BT_Send"] = "Envoyer des partitions",
                ["BT_Receive"] = "Recevoir des partitions",
                ["BT_Scanning"] = "Recherche d'appareils en cours...",
                ["BT_Scan_Devices"] = "Rechercher des appareils",
                ["BT_Stop_Scan"] = "Arrêter la recherche",
                ["BT_Select_Device"] = "Sélectionnez un appareil cible",
                ["BT_Transfer_Progress"] = "Transfert en cours...",
                ["BT_Transfer_Success"] = "Transfert réussi avec succès !",
                ["BT_Transfer_Failed"] = "Échec du transfert Bluetooth.",
                ["BT_Confirm_Receive_Title"] = "Transfert entrant",
                ["BT_Confirm_Receive_Msg"] = "Accepter les partitions envoyées par {0} ?",
                ["About_Title"] = "À propos",
                ["About_Description"] = "Application professionnelle de gestion, d'annotation et de visualisation de partitions musicales pour musiciens en répétition et en concert.",
                ["About_Version"] = "Version",
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
                ["Common_Rename"] = "Renommer",
                ["Common_OK"] = "OK",
                ["Common_Yes"] = "Oui",
                ["Common_No"] = "Non",
                ["Common_Loading"] = "Chargement...",
                ["Common_Ready"] = "Prêt",
                ["Common_Error"] = "Erreur",
                ["Common_Success"] = "Succès",
                ["Common_Warning"] = "Attention",
                ["Common_Info"] = "Information"
            }
        };
    }
}
