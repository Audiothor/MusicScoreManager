# Music Score Manager v0.1.6

**Music Score Manager** is a cross-platform mobile application built with **.NET MAUI** designed for musicians to manage, organize, and view their sheet music (PDFs and Images) efficiently. 

It features a robust tagging system, setlist management, and a high-performance internal viewer optimized for live performance.

---

## 🚀 Fonctionnalités (v0.1.4)

### 🎵 Gestion des Partitions
- **Import intelligent** : Support des fichiers PDF et Images (JPG, PNG).
- **Organisation visuelle** : Affichage sous forme de cartes avec titres et types.
- **Menu contextuel (⋮)** : Accès rapide à l'édition des propriétés, au transfert (en cours) et à la suppression.
- **Tri avancé** : Filtrage par titre (A-Z/Z-A) ou par date d'ajout.

### 🏷️ Système d'Étiquettes (Chips)
- **Gestion Globale** : Créez, modifiez et supprimez des étiquettes avec des couleurs personnalisées.
- **Affichage moderne** : Les étiquettes apparaissent sous forme de "Chips" colorées de style Android.
- **Filtrage Rapide** : Un carrousel horizontal sur l'accueil permet de filtrer instantanément vos partitions par catégorie (Jazz, Rock, Classique, etc.).
- **Édition visuelle** : Affectation des étiquettes aux partitions via un panneau de sélection tactile.

### 📋 Setlists
- **Listes ordonnées** : Créez des listes de morceaux pour vos concerts ou répétitions.
- **Gestion complète** : Ajout, renommage et suppression de setlists avec tri par date ou nom.

### 📖 Lecteur Interne Optimisé
- **Moteur PDF.js** : Intégration d'un lecteur PDF haute performance.
- **Zones tactiles invisibles** :
    - **Gauche** : Page précédente.
    - **Droite** : Page suivante.
    - **Bas** : Menu contextuel (Retour accueil, Saut de page).
- **Plein écran** : Immersion totale dans la partition.

### 🛠️ Outils & Paramètres
- **Backup & Restauration** : Préparé pour la sauvegarde de la base de données SQLite.
- **Personnalisation** : 
    - Activation/Désactivation du numéro de page actuel.
    - Réglage dynamique de la taille du numéro de page.
    - Sauvegarde persistante des préférences utilisateur.

---

## 🛠️ Stack Technique

- **Framework** : .NET 10 (MAUI)
- **Base de données** : SQLite (via sqlite-net-pcl)
- **Lecteur PDF** : PDF.js (injecté via WebView)
- **Langages** : C#, XAML, JavaScript, CSS, HTML5

---

## 📥 Installation & Configuration

### Prérequis
- **Visual Studio 2022** (version 17.8+) avec la charge de travail **Développement .NET MAUI**.
- **SDK Android** (API 34/35+ recommandé).

### Plateforme Android
1. Clonez le dépôt :
   ```bash
   git clone https://github.com/Audiothor/MusicScoreManager.git
   ```
2. Ouvrez `MusicScoreManager.sln` dans Visual Studio.
3. Assurez-vous que le framework cible est défini sur `net10.0-android`.
4. Connectez un appareil physique ou lancez un émulateur.
5. Appuyez sur **F5** pour compiler et déployer.

*Note : Pour les PDF, l'application utilise des ressources statiques situées dans `Resources/Raw/pdfjs/`. Celles-ci sont automatiquement déployées avec l'APK.*

---

## 📂 Structure du Projet

- `/Models` : Définition des entités (Score, Setlist, Tag, ScoreTag).
- `/Services` : Logique métier (DatabaseService, ImportService).
- `/Resources/Raw/pdfjs` : Moteur de rendu PDF interne.
- `AppShell.xaml` : Architecture de navigation (TabBar).

---

## 🗺️ Roadmap
- [ ] Partage de partitions entre appareils via Wi-Fi/Bluetooth.
- [ ] Interface détaillée pour la gestion interne des Setlists.
- [ ] Synchronisation Cloud (OneDrive/Google Drive).
- [ ] Version iOS et Windows Desktop finalisée.

---

**Développé par Audiothor** - *Version 0.1.6*
