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

### Méthode 1 : Installation du Package APK (Android)

1. Téléchargez la dernière version du fichier APK signé (`MusicScoreManager-vX.Y.Z.apk`) depuis les [Releases GitHub](https://github.com/Audiothor/MusicScoreManager/releases).
2. Sur votre tablette ou smartphone Android, autorisez l'installation d'applications provenant de sources inconnues pour votre navigateur ou explorateur de fichiers (*Paramètres > Sécurité > Installer applications inconnues*).
3. Ouvrez le fichier APK téléchargé et validez l'installation.
4. Au premier lancement, accordez les permissions requises pour l'accès aux fichiers multimédias, le Bluetooth et la détection d'appareils locaux.

### Méthode 2 : Compilation & Déploiement depuis les Sources

Pour les développeurs souhaitant compiler et personnaliser l'application :

1. Installez le **SDK .NET 10** et les charges de travail .NET MAUI :
   ```powershell
   dotnet workload install maui
   dotnet workload install maui-android
   ```
2. Clonez le dépôt Git officiel :
   ```powershell
   git clone https://github.com/Audiothor/MusicScoreManager.git
   cd MusicScoreManager
   ```
3. Ouvrez le projet dans **Visual Studio 2022** (avec composants MAUI) ou compilez en ligne de commande :
   ```powershell
   # Compilation et exécution directe sur tablette Android connectée en USB
   dotnet build -t:Run -f net10.0-android

   # Génération du package Release APK signé pour distribution
   dotnet publish -f net10.0-android -c Release
   ```
