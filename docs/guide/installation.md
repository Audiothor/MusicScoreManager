# 🚀 Chapitre 1 : Description de l'Application, Installation & Prérequis

## 1.1 Présentation Générale & Philosophie

**Music Score Manager** est une application multiplateforme développée avec **.NET 10 MAUI**, principalement conçue pour les tablettes et smartphones **Android** (ainsi que les postes de travail **Windows**). Conçue par des musiciens pour des musiciens, elle répond aux exigences les plus strictes de la pratique musicale : répétitions de pupitre, travail personnel, cours de musique, répétitions générales et exécution scénique en concert live.

L'application élimine définitivement les classeurs papier volumineux et remplace les visualiseurs PDF généralistes inadaptés à la scène. Elle offre une expérience taillée sur mesure :

- **100% Hors-Ligne & Sans Dépendance Réseau** : L'ensemble de la bibliothèque, de la base de données, des métadonnées, des annotations et des fichiers audio est stocké localement sur l'appareil. Aucun accès Internet n'est requis sur scène.
- **Réactivité Instantanée (< 50 ms)** : Architecture optimisée sans copie de fichiers superflue (*zero-copy*), avec pré-rendu hors-écran (*offscreen canvas double-buffering*) pour des tournes de page sans saccade ni latence.
- **Confort Visuel Scénique** : Interface native sombre (Dark Mode `#121212`) prévenant tout éblouissement sur scène et éliminant tout flash blanc lors du chargement des partitions.
- **Contrôle Mains-Libres Intégral** : Prise en charge native des pédaliers tourne-page sans fil Bluetooth (profils HID et clavier) ainsi que des contrôleurs MIDI (USB-OTG et Bluetooth MIDI).
- **Écosystème d'Annotations Professionnel** : Stylet, crayon à main levée, surligneurs Stabilo translucides, textes typographiés et plus de 100 stickers musicaux intégrés.

---

## 1.2 Les 10 Points Forts de Music Score Manager

1. **Visualiseur de Scène Sécurisé & Immersif** : Verrouillage strict anti-fausses manipulations (`🔒`), maintien de l'écran éveillé, et mode sombre / inversion des couleurs (partition blanche sur fond noir pour jouer en fosse d'orchestre sans éblouir).
2. **Gestion Puissante des Setlists avec Dérouleur Live** : Enchaînement continu automatique entre morceaux, autorisation des doublons (rappels, reprises), duplication instantanée (`⧉`), et volet scénique rétractable avec centrage automatique sur le morceau en cours.
3. **Partage P2P sans fil en Wi-Fi Direct & Hotspot (Sans Internet)** : Échange direct de tablette à tablette en coulisses sans box ni routeur, transmission complète avec annotations et fichiers audio, et mode Leader avec QR Code.
4. **Prise en Charge Totale des Pédales Bluetooth / Tourne-Pages** : Compatibilité universelle (AirTurn, PageFlip, Donner, CubeSuite...) et mappage personnalisé des touches physiques (tourne de page, métronome, lecteur audio).
5. **Atelier d'Annotations Musicales & Bibliothèque de Stickers (+100 symboles)** : Surligneur fluo biseauté translucide, crayon fin/opaque, texte libre, plus de 100 stickers classés (doigtés, nuances, reprises, respirations) et historique Undo/Redo illimité.
6. **Métronome Haute Précision à Dérive Nulle** : Horloge temps-réel régulée thread-safe (`Stopwatch` + `SpinWait`), son Android basse latence (`SoundPool`), pulsation LED synchronisée et coupure du son à la volée pour garder le signal visuel.
7. **Lecteur Audio Multipiste Synchronisé** : Rapprochement de pistes d'accompagnement (MP3, WAV, AAC, FLAC, OGG), barre de lecture compacte intégrée au visualiseur et pré-compte réglable.
8. **Import Intelligent & Fusion d'Images en PDF Haute Définition** : Contrôle strict de l'intégrité binaire `%PDF-`, conversion de photos avec modal bloquant d'avancement et fusion automatique de plusieurs photos en un unique document PDF multi-pages.
9. **Atelier d'Assemblage PDF Embarqué** : Réorganisation des pages par glisser-déposer tactile, rotation individuelle (90°, 180°, 270°), suppression de pages blanches et insertion de nouvelles pages sans outil externe.
10. **Classification Dynamique & Filtrage Multi-Étiquettes (Mode ET / OU)** : Pastilles colorées personnalisables, recherche instantanée par titre ou compositeur, et filtrage croisé par plusieurs étiquettes avec choix interactif entre intersection et union.

---

## 1.3 Prérequis Système & Matériels

| Composant | Prérequis Minimum | Recommandé pour la Scène |
| :--- | :--- | :--- |
| **Système d'exploitation** | **Android 12.0** (API 31) ou supérieur | **Android 13 / 14 / 15 / 16** (Target SDK 36) |
| **Alternative Desktop** | **Windows 10** (build 19041+) | **Windows 11** avec écran tactile ou stylet |
| **Écran & Affichage** | Écran 8 pouces tactile | **Tablette 10.5 à 13.3 pouces** (haute définition, ratio 4:3 ou 16:10) |
| **Stockage** | 200 Mo d'espace libre pour l'application | 4 Go+ selon l'envergure de votre bibliothèque PDF/Audio |
| **Mémoire Vive (RAM)** | 3 Go de RAM | 4 Go à 8 Go de RAM |
| **Contrôleurs Pédalier** | Écran tactile | **Pédalier Bluetooth HID** (AirTurn, PageFlip, Joyo, Donner...) ou **Contrôleur MIDI** |
| **Connexion Réseau** | Aucune connexion Internet requise | Carte Wi-Fi active uniquement pour le partage direct P2P entre musiciens |

---

## 1.4 Installation & Déploiement

### 📱 Pour Android

#### Méthode 1 : Installation via le Google Play Store (Recommandé)

C'est la méthode la plus simple, rapide et sécurisée pour installer et maintenir à jour **Music Score Manager** :

1. Ouvrez l'application **Google Play Store** sur votre tablette ou smartphone Android.
2. Recherchez **Music Score Manager** (par *Audiothor*) ou accédez directement à la fiche du store :  
   👉 [**Music Score Manager sur Google Play Store**](https://play.google.com/store/apps/details?id=com.audiothor.musicscoremanager)
3. Appuyez sur **Installer**.
4. L'application est installée en quelques secondes et recevra automatiquement toutes les futures mises à jour officielles.

#### Méthode 2 : Installation Manuelle via le Package APK (Hors-Ligne)

Pour les musiciens ne disposant pas des services Google ou souhaitant une installation autonome hors-ligne :

1. Téléchargez la dernière version du fichier APK signé (`MusicScoreManager-vX.Y.Z.apk`) depuis les [Releases officielles sur GitHub](https://github.com/Audiothor/MusicScoreManager/releases).
2. Sur votre tablette ou smartphone Android, autorisez l'installation d'applications provenant de sources inconnues pour votre navigateur ou explorateur de fichiers (*Paramètres > Sécurité > Installer applications inconnues*).
3. Ouvrez le fichier APK téléchargé et validez l'installation.
4. Au premier lancement, accordez les permissions requises pour l'accès aux fichiers multimédias, le Bluetooth (pour les pédaliers) et la détection d'appareils locaux (pour le Wi-Fi Direct).

---

### 💻 Pour Windows (PC, Portables & Tablettes Surface)

**Music Score Manager** fonctionne nativement sur **Windows 10** (build 19041+) et **Windows 11** avec support complet des écrans tactiles, du stylet, de la souris et des raccourcis clavier :

#### Méthode 1 : Téléchargement Direct de la Version Windows

1. Rendez-vous sur les [Releases GitHub](https://github.com/Audiothor/MusicScoreManager/releases) et téléchargez la dernière archive Windows (ex. `MusicScoreManager-Windows-vX.Y.Z.zip` ou l'exécutable d'installation).
2. Extrayez le contenu du fichier ZIP dans le dossier de votre choix (par exemple dans `C:\Programmes\MusicScoreManager` ou dans votre dossier utilisateur).
3. Double-cliquez sur `MusicScoreManager.exe` pour lancer l'application.  
   *(Optionnel : faites un clic droit sur l'exécutable pour créer un raccourci sur le Bureau ou l'épingler à la Barre des tâches).*
4. **Remarque Windows SmartScreen** : Si un écran bleu « Windows a protégé votre ordinateur » s'affiche au premier démarrage, cliquez sur **« Informations complémentaires »** puis sur le bouton **« Exécuter quand même »**.

---

### 🛠️ Pour les Développeurs : Compilation depuis les Sources (.NET MAUI)

Pour les développeurs souhaitant compiler et personnaliser l'application pour Android ou Windows :

1. Installez le **SDK .NET 10** et les charges de travail .NET MAUI :
   ```powershell
   dotnet workload install maui
   dotnet workload install maui-android
   dotnet workload install maui-windows
   ```
2. Clonez le dépôt Git officiel :
   ```powershell
   git clone https://github.com/Audiothor/MusicScoreManager.git
   cd MusicScoreManager
   ```
3. Compilez selon la cible souhaitée :
   ```powershell
   # Pour Android : exécution directe sur tablette Android connectée en USB
   dotnet build -t:Run -f net10.0-android

   # Pour Android : génération du package Release APK signé
   dotnet publish -f net10.0-android -c Release

   # Pour Windows : compilation de la version exécutable Windows
   dotnet build -f net10.0-windows10.0.19041.0 -c Release

   # Pour Windows : publication du binaire complet autonome
   dotnet publish -f net10.0-windows10.0.19041.0 -c Release
   ```
