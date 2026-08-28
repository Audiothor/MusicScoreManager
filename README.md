<p align="center">
  <img src="Resources/Splash/app_splash_padded.png" width="400" />
</p>

# Music Score Manager v1.8.16

**Music Score Manager** est une application mobile multiplateforme construite avec **.NET MAUI** (ciblant principalement Android) conçue pour les musiciens afin de gérer, organiser, annoter et visualiser leurs partitions (PDF et Images) de manière efficace, particulièrement en situation de concert.

---

## 🚀 Fonctionnalités Clés

### ⏱️ Métronome Temps Réel Haute Précision (v1.8.22)
- **Horloge Audio Temps Réel à Dérive Nulle (0.00 ms)** : Moteur de timing fonctionnant sur un thread natif dédié à priorité maximale (`ThreadPriority.Highest`) basé sur un calcul absolu par rapport au chronomètre matériel. Le rythme ne ralentit ni n'accélère jamais, même sous forte charge CPU ou lors du Garbage Collection.
- **Latence Matérielle Instantanée (SoundPool Android)** : Lecture des sons de clic et de pré-compte via `SoundPool` pré-chargé en mémoire vive sans instanciation ni décodage en cours de jeu.
- **Pré-compte Synchronisé au Microseconde Près** : Décompte avant démarrage audio rigoureusement calé sur la pulsation du morceau.

### 🔍 Zoom & Gestes Tactiles Avancés (v1.8.0 - v1.8.19)
- **Zoom Unifié & Synchronisation Parfaite des Stickers (v1.8.19)** : Zoom et pan unifiés sur `ZoomLayout` pour PDF et Images. Les stickers (annotations) suivent le zoom et le déplacement en temps réel avec une proportion et un alignement millimétrique sur les portées musicales.
- **Ergonomie & Gestes de Tourne-Page Personnalisables (v1.8.18)** : Configuration sur mesure des gestes pour page suivante (glisser à gauche, taper à droite, glisser vers le haut) et page précédente (glisser à droite, taper à gauche, glisser vers le bas) dans les Paramètres Partitions.
- **Fluid & Precision Zoom** : Zoom dynamique et pan fluide pour les partitions au format Image et PDF.
- **Rendu PDF Ultra-Rapide avec Pré-rendu (v1.8.14)** : Pré-rendu hors-écran (*Offscreen Canvas Cache*) des pages adjacentes pour des sauts de page instantanés à 0 ms de latence.
- **Rotation Hybride par Page (v1.8.14 - v1.8.17)** : Choix flexible de pivoter une page spécifique à 90° ou toute la partition, avec persistance SQLite dédiée et synchronisation temps réel par page.
- **Indicateur Cadenas Annotations** : Visualisation claire avec fond rouge (verrouillé) et vert (déverrouillé).
- **Métadonnées de Fichier Complètes (v1.8.15 - v1.8.21)** : Affichage de la taille du fichier, de sa date de dernière modification, de son horodatage d'ajout et de son **type précis avec extension** (ex: *PDF (.pdf)*, *Image (.png)*) dans la page d'édition.
- **Sécurisation des Gestes (Safe Boundaries)** : Gestion intelligente des zones tactiles pour éviter les sorties d'écran et la navigation intempestive aux extrémités de la partition.
- **Accès Direct au Saut de Page** : Clic direct sur l'indicateur de numérotation de page (`1/5`) pour ouvrir le prompt de changement de page.

### 🎨 Édition & Système d'Annotations Dynamiques (v1.8.1 - v1.8.23)
- **Surlignage Translucide Tactile / Stabilo (v1.8.23)** : Nouvel outil `🖍` placé avant le texte dans la barre d'outils. Permet de surligner les portées ou le texte au doigt avec un rendu translucide naturel (sans masquer les notes et annotations sous-jacentes). Palette ergonomique de couleurs (Jaune fluo par défaut, Vert, Bleu, Rose/Rouge) et sélection rapide de l'épaisseur du trait (5 mm, 10 mm par défaut, 18 mm).
- **Tiroir à Stickers Unifié & Bouton Fermer ✕** : Sélection rapide avec bouton de fermeture dédié sur l'overlay.
- **Réglette de Taille Tactile Élargie** : Slider grand format ergonomique sur ligne dédiée pour un ajustement facile aux doigts.
- **Placement Délimité Précis (v1.8.12)** : Possibilité de déposer des stickers en dessous de la barre d'annotations même si celle-ci a été déplacée au milieu/haut de l'écran.
- **Verrouillage Strict (Lock Safety)** : Interdiction d'ajouter ou modifier des stickers si le cadenas est verrouillé avec message d'information explicite.
- **Édition Temps Réel** : Modification dynamique en mode déverrouillé (couleurs texte/fond et taille).

### ⚡ Performance "Zero-Copy" & Cache Setlists (v1.0.1 - v1.1.1)
- **Mise en Cache Automatique** : Les setlists sont pré-chargées en tâche de fond dans un stockage ultra-rapide.
- **Résilience du Cache** : Tolérance aux fichiers inaccessibles sans interruption du flux principal.
- **Chargement Direct** : Suppression des copies disques lors de l'ouverture d'un PDF pour une transition instantanée entre morceaux.
- **Indicateur de Statut** : Visualisation en temps réel de l'état du cache (⏳/⚡) dans l'éditeur de setlist.

### 🎵 Gestion des Partitions & Échange Bluetooth (v1.0.5 - v1.8.20)
- **Échange Bluetooth Unitaire & Groupé (v1.8.20)** : Option "Échanger" disponible directement dans le menu contextuel (3 points) de chaque partition ainsi qu'en mode sélection multiple.
- **Stockage Public Configurable** : Définissez vos propres répertoires pour les partitions et les fichiers audio.
- **Super-Détection & Scan Récursif** : Scan intelligent tolérant et détection automatique des fichiers déjà présents dans l'arborescence racine/sous-dossiers pour éviter la duplication.
- **Import Hybride** : Choisissez entre "Copier" (interne) ou "Lier" (externe avec icône 🔗 et option de rapatriement rapide).

### 🏷️ Système d'Étiquettes (Chips)
- **Gestion Globale** : Créez et personnalisez vos étiquettes avec des couleurs (Palette + Sliders RGB).
- **Filtrage Rapide** : Carrousel horizontal pour filtrer instantanément par catégorie.

### 📋 Gestion de Setlists (v0.2.0+)
- **Mode Édition Avancé** : Réordonnez vos partitions par **glisser-déposer**.
- **Gestion des Statuts** : Marquez vos listes comme `À venir`, `Active` ou `Terminée` avec filtrage sur l'accueil.
- **Lecture en Continu** : Transition automatique entre les morceaux d'une même liste.
- **Mode Concert (Verrouillage)** : Bouton de verrouillage persistant pour désactiver toute modification accidentelle sur scène.

### ⏱️ Métronome Pro & Audio Sync
- **Haute Précision** : Boucle temporelle basée sur `Stopwatch` sans dérive CPU.
- **Bip de Pré-compte & Synchronisation** : Son distinctif (880Hz) et calage précis du démarrage audio sur le temps fort.

### 🛡️ Gestion des Sauvegardes
- **Sauvegarde Automatique & Rétention** : Sauvegarde automatique configurable avec rétention paramétrable.
- **Restauration en un Clic** : Restaurez n'importe quelle version précédente depuis l'historique UI.

---

## 📜 Historique Récent des Versions

- **v1.8.7** : **"Performance & Precision Zoom"** — Version actuelle avec retouches d'optimisation de zoom et retours tactiles.
- **v1.8.4** : **"Safe Gesture Placement"** — Isolation anti-drop accidentel lors de l'utilisation du sélecteur d'annotations.
- **v1.8.3** : **"Dynamic Annotation Editing"** — Modification en direct de la taille, couleur de texte et fond des stickers posés.
- **v1.8.2** : **"Safe Boundaries & Native Execution"** — Blocage des retours d'écran intempestifs aux extrémités de partition.
- **v1.8.1** : **"Gesture Tuning & Annotation Clamping"** — Intégration du tiroir à stickers unifié et limitation du panoramique.
- **v1.8.0** : **"Safe Zooming & Fluid Image Zoom"** — Refonte de la couche tactile MAUI pour le zoom d'images et PDF.
- **v1.1.0** : **"Super-Detection Edition"** — Scan récursif et tolérant pour détection des doublons d'import.
- **v1.0.0** : **"Public Library Edition"** — Refonte du stockage et optimisation PDF.js.

---

## 🛠️ Stack Technique

- **Framework** : .NET 10 (MAUI)
- **Base de données** : SQLite (via `sqlite-net-pcl`)
- **Lecteur PDF** : PDF.js (injecté via WebView)
- **Lecteur Audio** : `Plugin.Maui.Audio` & `CommunityToolkit.Maui.MediaElement`
- **Compatibilité** : Android 12.0+ (API 31) minimum (Target SDK 36.0)
- **Logiciel de compilation** : Visual Studio 2022 / .NET CLI (`dotnet build`)

---

## 📂 Structure du Projet

- `/Models` : Entités (`Score`, `Setlist`, `Tag`, `ScoreTag`, `BackupFile`, etc.).
- `/Services` : Logique métier (`DatabaseService`, `SettingsService`, etc.).
- `/Converters` : Convertisseurs XAML pour l'affichage dynamique.
- `/Resources/Raw/pdfjs` : Moteur de rendu PDF interne.
- `ViewerPage.xaml(.cs)` : Lecteur principal de partitions (PDF & Images, annotations, audio & métronome).

---

## 📥 Installation & Compilation

1. Clonez le dépôt.
2. Ouvrez la solution dans Visual Studio 2022 ou utilisez la CLI.
3. Ciblez le framework `net10.0-android36.0` (ou `net10.0-windows10.0.19041.0`).
4. Lancez ou générez l'application :

```powershell
# Compilation & Exécution Debug Android
dotnet build -t:Run -f net10.0-android36.0

# Publication Release APK
dotnet publish -f net10.0-android36.0 -c Release
```

---

**Développé par Audiothor** — *MusicScoreManager v1.8.23 "Translucent Score Highlighter (Stabilo)"*
