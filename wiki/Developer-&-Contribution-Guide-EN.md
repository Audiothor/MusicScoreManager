> [🇫🇷 Version Française](Developer-&-Contribution-Guide) | **🇬🇧 English**

# 🛠️ Developer & Contribution Guide

Welcome to open-source developers and contributors interested in enhancing **Music Score Manager**.

---

## 1. Prerequisites & Environment Setup

### A. Required Tools
- **.NET 10 SDK** (version 10.0.100 or higher)
- **Visual Studio 2022** (version 17.12+ with *.NET Multi-platform App UI development* workload) or **VS Code** with the *.NET MAUI* extension.
- **Android SDK & NDK**:
  - Minimum Android API: **API 21** (Android 5.0)
  - Target Android API: **API 36.0** (Android 16)
- **Git**

### B. Installing MAUI Workloads
Open a terminal (PowerShell or Bash) and execute:
```powershell
dotnet workload install maui
dotnet workload install maui-android
dotnet workload install maui-windows
```

---

## 2. Cloning & Building the Project

```powershell
# 1. Clone the official repository
git clone https://github.com/Audiothor/MusicScoreManager.git
cd MusicScoreManager

# 2. Restore NuGet dependencies
dotnet restore

# 3. Build for Android (Debug)
dotnet build -f net10.0-android36.0

# 4. Run directly on a USB-connected Android tablet (ADB)
dotnet build -t:Run -f net10.0-android36.0

# 5. Build for Windows Desktop
dotnet build -f net10.0-windows10.0.19041.0
```

---

## 3. Codebase Structure

```
MusicScoreManager/
├── App.xaml / AppShell.xaml     # Shell navigation with 5 strict bottom tabs
├── Models/                      # Data entities (Score, Setlist, Tag, Backup, FavoriteSticker)
├── Services/                    # Core business logic and singletons:
│   ├── DatabaseService.cs       # SQLite access, async queries, transactions
│   ├── SettingsService.cs       # Preferences and file path resolution
│   ├── LocalizationService.cs   # Dynamic multilingual translation engine
│   ├── BluetoothPedalService.cs # Bluetooth page turner and MIDI controller listeners
│   ├── WifiTransferService.cs   # P2P TCP/UDP sockets and embedded HTTP server
│   └── MetronomeService.cs      # High-precision thread-safe metronome engine
├── Converters/                  # XAML data-binding value converters
├── Platforms/                   # Platform-specific native hooks (Android, Windows, iOS, Mac)
├── Resources/
│   ├── AppIcon/ & Splash/       # Visual branding assets
│   └── Raw/
│       ├── pdfjs/               # Mozilla PDF.js engine injected into WebView
│       ├── sounds/              # SoundPool audio samples (metronome clicks)
│       └── Languages/           # Multilingual JSON dictionaries (fr.json, en.json, ...)
└── wiki/                        # GitHub Wiki documentation sources
```

---

## 4. How to Add a New Translation Language

Music Score Manager's localization engine is 100% decoupled. **No C# code modifications are required** to add a new language:

1. Navigate to the `Resources/Raw/Languages/` directory.
2. Duplicate `en.json` and rename it with your target ISO language code (e.g., `ja.json` for Japanese, `it.json` for Italian, `de.json` for German).
3. Translate the JSON string values:
   ```json
   {
     "Scores_Title": "楽譜",
     "Setlists_Title": "セットリスト",
     "Common_Back": "戻る"
   }
   ```
4. In [`SettingsAppPage.xaml`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsAppPage.xaml), add the new language option to the modal selection list.
5. Rebuild and test: the new language is instantly selectable in settings!

---

## 5. Release Packaging & Keystore Signing

The `MusicScoreManager.csproj` project file automates Android signing in `Release` configuration when a keystore file is present:

```powershell
# Publish signed APK installer
dotnet publish -f net10.0-android36.0 -c Release -p:AndroidPackageFormats=apk

# Publish signed Android App Bundle (AAB) for Google Play
dotnet publish -f net10.0-android36.0 -c Release -p:AndroidPackageFormats=aab
```

Outputs are generated inside `bin/Release/net10.0-android36.0/publish/`.

---

## 6. Contribution Guidelines (Pull Requests)

1. **Working Branch**: Create a descriptive feature branch from `main` (`feature/feature-name` or `fix/bug-name`).
2. **Performance Integrity**: No blocking I/O calls on the UI thread (`MainThread`). Sheet music page turns must strictly remain under 50 ms.
3. **Zero-Tracking Policy**: Contributions containing tracking, analytics, or advertisement SDKs will not be accepted.
4. **Submission**: Open your Pull Request on [GitHub](https://github.com/Audiothor/MusicScoreManager/pulls) with clear details and real-hardware test results.
