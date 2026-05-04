<p align="center">
  <img src="Resources/Splash/app_splash_padded.png" width="400" />
</p>

# Music Score Manager v0.2.1

**Music Score Manager** est une application mobile multiplateforme construite avec **.NET MAUI** (ciblant principalement Android) conçue pour les musiciens afin de gérer, organiser et visualiser leurs partitions (PDF et Images) de manière efficace, particulièrement en situation de concert.

---

## 🚀 Fonctionnalités Clés (v0.2.0)

### 🎵 Gestion des Partitions
- **Import intelligent** : Support des fichiers PDF et Images (JPG, PNG).
- **Organisation visuelle** : Affichage sous forme de cartes avec titres et types.
- **Filtrage & Tri** : Recherche textuelle, filtrage par étiquettes et tris (A-Z, Date).
- **Édition complète** : Modification des titres, des chemins et des étiquettes avec gestion des erreurs et navigation fluide.

### 🏷️ Système d'Étiquettes (Chips)
- **Gestion Globale** : Créez et personnalisez vos étiquettes avec des couleurs (Palette + Sliders RGB).
- **Filtrage Rapide** : Carrousel horizontal pour filtrer instantanément par catégorie.

### 📋 Gestion de Setlists (Nouveauté v0.2.0)
- **Mode Édition Avancé** : Réordonnez vos partitions par **glisser-déposer**.
- **Gestion des Statuts** : Marquez vos listes comme `À venir`, `Active` ou `Terminée` avec filtrage sur l'accueil.
- **Lecture en Continu** : Transition automatique entre les morceaux d'une même liste.
- **Mode Concert (Verrouillage)** : Bouton de verrouillage persistant pour désactiver toute modification accidentelle sur scène.

### 📖 Lecteur Interne Optimisé
- **Moteur PDF.js** & Rendu Image natif.
- **Navigation Tactile** :
    - **Zone Gauche** : Page précédente (ou morceau précédent de la setlist).
    - **Zone Droite** : Page suivante (ou morceau suivant de la setlist).
    - **Zone Bas** : Menu contextuel.
- **Plein écran automatique** : Masquage de la barre de navigation pour une immersion totale.

### 🛡️ Gestion des Sauvegardes
- **Sauvegarde Automatique** : Déclenchement au lancement selon un intervalle paramétrable (ex: tous les 30 jours).
- **Règle de Rétention** : Garde uniquement un nombre défini de copies (ex: 6) pour économiser l'espace.
- **Restauration en un clic** : Restaurez n'importe quelle version précédente depuis l'historique avec avertissement de sécurité.

---

## 🛠️ Stack Technique

- **Framework** : .NET 10 (MAUI)
- **Base de données** : SQLite (via sqlite-net-pcl)
- **Lecteur PDF** : PDF.js (injecté via WebView)
- **Logiciel de compilation** : Visual Studio 2022 (v17.8+)

---

## 📂 Structure du Projet

- `/Models` : Entités (Score, Setlist, Tag, BackupFile).
- `/Services` : Logique métier (DatabaseService, ImportService).
- `/Resources/Raw/pdfjs` : Moteur de rendu PDF interne.
- `AppShell.xaml` : Navigation principale par onglets.

---

## 📥 Installation

1. Clonez le dépôt.
2. Ouvrez la solution dans Visual Studio 2022.
3. Ciblez le framework `net10.0-android36.0` (ou supérieur).
4. Déployez sur votre tablette ou smartphone Android.

---

**Développé par Audiothor** - *Version 0.2.0 "Concert Ready"*
