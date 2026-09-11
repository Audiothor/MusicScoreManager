# 🛠️ Guide Développeur & Contribution

Bienvenue aux développeurs et contributeurs Open Source souhaitant participer à l'évolution de **Music Score Manager**.

---

## 1. Prérequis & Environnement de Développement

### A. Outils Requis
- **.NET 10 SDK** (version 10.0.100 ou supérieure)
- **Visual Studio 2022** (version 17.12+ avec charge de travail *.NET Multi-platform App UI development*) ou **VS Code** avec l'extension *.NET MAUI*.
- **Android SDK & NDK** :
  - Minimum Android API : **API 21** (Android 5.0)
  - Target Android API : **API 36.0** (Android 16)
- **Git**

### B. Installation des Charges de Travail MAUI
Ouvrez un terminal PowerShell ou Bash et exécutez :
```powershell
dotnet workload install maui
dotnet workload install maui-android
dotnet workload install maui-windows
```

---

## 2. Cloner & Compiler le Projet

```powershell
# 1. Cloner le dépôt officiel
git clone https://github.com/Audiothor/MusicScoreManager.git
cd MusicScoreManager

# 2. Restaurer les paquets NuGet
dotnet restore

# 3. Compiler pour Android (Debug)
dotnet build -f net10.0-android36.0

# 4. Exécuter directement sur une tablette Android connectée en USB (ADB)
dotnet build -t:Run -f net10.0-android36.0

# 5. Compiler pour Windows Desktop
dotnet build -f net10.0-windows10.0.19041.0
```

---

## 3. Structure du Code Source

```
MusicScoreManager/
├── App.xaml / AppShell.xaml     # Navigation Shell à 5 onglets stricts
├── Models/                      # Entités de données (Score, Setlist, Tag, Backup, FavoriteSticker)
├── Services/                    # Logique métier et singletons :
│   ├── DatabaseService.cs       # Accès SQLite, requêtes asynchrones, transactions
│   ├── SettingsService.cs       # Préférences utilisateur et résolution des chemins
│   ├── LocalizationService.cs   # Moteur de traduction dynamique multilingue
│   ├── BluetoothPedalService.cs # Écoute des pédaliers Bluetooth et contrôleurs MIDI
│   ├── WifiTransferService.cs   # Sockets P2P TCP/UDP et mini-serveur HTTP
│   └── MetronomeService.cs      # Moteur métronome haute précision thread-safe
├── Converters/                  # Convertisseurs de liaison de données XAML
├── Platforms/                   # Implémentations natives (Android, Windows, iOS, Mac)
├── Resources/
│   ├── AppIcon/ & Splash/       # Éléments visuels et branding
│   └── Raw/
│       ├── pdfjs/               # Moteur Mozilla PDF.js injecté dans la WebView
│       ├── sounds/              # Échantillons audio SoundPool (clics métronome)
│       └── Languages/           # Dictionnaires de traduction JSON (fr.json, en.json, ...)
└── wiki/                        # Documentation technique du Wiki GitHub
```

---

## 4. Comment Ajouter une Nouvelle Langue (Internationalisation)

L'architecture multilingue de Music Score Manager est 100% découplée. **Aucune ligne de code C# n'est requise** pour ajouter une nouvelle langue :

1. Naviguez dans le dossier `Resources/Raw/Languages/`.
2. Dupliquez `fr.json` ou `en.json` et renommez-le avec le code ISO de votre langue (par exemple `ja.json` pour le japonais ou `it.json` pour l'italien).
3. Traduisez les valeurs textuelles des clés JSON :
   ```json
   {
     "Scores_Title": "楽譜",
     "Setlists_Title": "セットリスト",
     "Common_Back": "戻る"
   }
   ```
4. Dans [`SettingsAppPage.xaml`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsAppPage.xaml), ajoutez l'entrée correspondante dans la liste du modal de sélection de langue.
5. Compilez et testez : la nouvelle langue est immédiatement opérationnelle !

---

## 5. Processus de Release & Keystore Android

Le fichier projet `MusicScoreManager.csproj` automatise la signature Android en configuration `Release` si le fichier de clé keystore est présent :

```powershell
# Publication du package APK signé
dotnet publish -f net10.0-android36.0 -c Release -p:AndroidPackageFormats=apk

# Publication du package Android App Bundle (AAB) pour le Play Store
dotnet publish -f net10.0-android36.0 -c Release -p:AndroidPackageFormats=aab
```

Les livrables générés se situent dans `bin/Release/net10.0-android36.0/publish/`.

---

## 6. Règles de Contribution (Pull Requests)

1. **Branche de travail** : Créez une branche dédiée à partir de `main` (`feature/nom-de-fonctionnalite` ou `fix/nom-du-bug`).
2. **Respect des performances** : Aucun appel réseau bloquant sur le thread UI (`MainThread`). Les tournes de pages du visualiseur doivent impérativement rester sous le seuil de 50 ms.
3. **Respect de la vie privée** : L'application n'acceptera aucune contribution intégrant un SDK de pistage, de télémétrie ou de publicité.
4. **Soumission** : Ouvrez votre Pull Request sur [GitHub](https://github.com/Audiothor/MusicScoreManager/pulls) avec une description claire des modifications apportées et des tests effectués sur matériel réel.
