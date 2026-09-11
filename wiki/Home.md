> **🇫🇷 Français** | [🇬🇧 English Version](Home-EN)

# 🏛️ Bienvenue sur le Wiki Technique de Music Score Manager

Bienvenue dans l'espace communautaire et d'ingénierie avancée de **Music Score Manager**.

Alors que le [Site de Documentation Officiel (MkDocs)](https://audiothor.github.io/MusicScoreManager/) et le [README.md](https://github.com/Audiothor/MusicScoreManager/blob/main/README.md) fournissent le manuel d'utilisation utilisateur écran par écran, ce **Wiki** est dédié aux **cas d'usage scéniques complexes**, à la **compatibilité matérielle approfondie**, à **l'architecture interne du logiciel** et au **guide de développement pour contributeurs**.

---

## 🧭 Sommaire Général du Wiki

### 1. [🦶 Compatibilité Matériel, Pédaliers Bluetooth & MIDI](Hardware-Compatibility-&-Pedals)
- Matrice comparative complète des pédaliers de scène testés (AirTurn, PageFlip, Joyo, Donner, Thomann, iRig, STOMP).
- Contrôleurs MIDI : mapping des Control Changes (CC 64, 66, 67), notes MIDI et Program Changes.
- Recommandations ergonomiques de tablettes (tailles d'écran, ratios 4:3 vs 16:9) et pinces pupitres.

### 2. [🎭 Recettes de Scène & Scénarios Concrets (Stage Cookbook)](Stage-Cookbook-&-Scenarios)
- **Scénario Régie Groupe / Chorale** : Diffusion sans fil d'une setlist complète à 20 musiciens en 30 secondes via QR Code sans box Wi-Fi.
- **Scénario Récital Piano / Orgue** : Affichage 2 pages en mode paysage et stratégie d'insertion de pages blanches pour caler les tournes sur les temps morts.
- **Scénario Musiques Actuelles / Répétitions** : Enchaînement de playbacks audio synchronisés au métronome.
- **Guide de numérisation** : Recommandations de scan et traitement d'image pour un rendu optimal sans alourdir la mémoire.

### 3. [🔬 Architecture Interne & Spécifications des Protocoles](Architecture-&-Internals)
- Schéma relationnel de la base de données locale SQLite (`scores.db3`) et fonctionnement du mode WAL.
- Spécification des archives autonomes compressées (`.msmscore`, `.msmscores`, `.msmsetlist`) et manifestes JSON.
- Protocole de transfert sans fil P2P (UDP Beacon, TCP streaming binaire, serveur local HTTP).
- Moteur de coordonnées géométriques normalisées (0.0 à 1.0) des annotations sous PDF.js.

### 4. [🛠️ Guide Développeur & Contribution](Developer-&-Contribution-Guide)
- Mise en place de l'environnement .NET 10 MAUI, Android SDK et Windows SDK.
- Guide d'internationalisation : comment ajouter une 9ème langue via les dictionnaires JSON sans toucher au code C#.
- Workflow de build, exécution en débogage via ADB et pipeline de signature Release.

### 5. [🚑 Dépannage Technique & FAQ Système (Troubleshooting)](Troubleshooting-&-OS-FAQ)
- Android : Neutraliser l'économie d'énergie agressive qui déconnecte les pédales Bluetooth en plein concert.
- Gestion des permissions de stockage Scoped Storage (Android 11 à 16) pour les transferts USB sur PC.
- Sauvegarde et migration complète d'une bibliothèque d'un ancien appareil vers une nouvelle tablette.

---

## ⚡ En Savoir Plus

- Code source : [https://github.com/Audiothor/MusicScoreManager](https://github.com/Audiothor/MusicScoreManager)
- Documentation bilingue en ligne : [https://audiothor.github.io/MusicScoreManager/](https://audiothor.github.io/MusicScoreManager/)
- Licence : **GNU General Public License v3.0 (GPLv3)**
