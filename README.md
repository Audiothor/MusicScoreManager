<p align="center">
  <img src="Resources/Splash/app_splash_padded.png" width="400" />
</p>

# Music Score Manager v1.0.3

**Music Score Manager** est une application mobile multiplateforme construite avec **.NET MAUI** (ciblant principalement Android) conçue pour les musiciens afin de gérer, organiser et visualiser leurs partitions (PDF et Images) de manière efficace, particulièrement en situation de concert.

---

## 🚀 Fonctionnalités Clés (v1.0.1)

### ⚡ Performance "Zero-Copy" (Nouveauté v1.0.1)
- **Mise en Cache Automatique** : Les setlists sont pré-chargées en tâche de fond dans un stockage ultra-rapide.
- **Chargement Direct** : Suppression des copies disques lors de l'ouverture d'un PDF, rendant la transition entre morceaux instantanée.
- **Indicateur de Statut** : Visualisation en temps réel de l'état du cache (⏳/⚡) dans l'éditeur de setlist.

### 🎵 Gestion des Partitions & Stockage (Refonte v1.0)
- **Stockage Public Unique** : Définissez vos propres répertoires pour les partitions et l'audio.
- **Import Intelligent** : Choisissez entre "Copier" (interne) ou "Lier" (externe).
- **Indicateurs Visuels** : Icône 🔗 pour repérer les fichiers hors bibliothèque.
- **Rapatriement Rapide** : Importez physiquement un fichier lié en un clic.

### 🏷️ Système d'Étiquettes (Chips)
- **Gestion Globale** : Créez et personnalisez vos étiquettes avec des couleurs (Palette + Sliders RGB).
- **Filtrage Rapide** : Carrousel horizontal pour filtrer instantanément par catégorie.

### 📋 Gestion de Setlists (Nouveauté v0.2.0)
- **Mode Édition Avancé** : Réordonnez vos partitions par **glisser-déposer**.
- **Gestion des Statuts** : Marquez vos listes comme `À venir`, `Active` ou `Terminée` avec filtrage sur l'accueil.
- **Lecture en Continu** : Transition automatique entre les morceaux d'une même liste.
- **Mode Concert (Verrouillage)** : Bouton de verrouillage persistant pour désactiver toute modification accidentelle sur scène.

### 📖 Lecteur Ultra-Performant (v1.0)
- **Accès Disque Direct** : Chargement instantané des PDF, même volumineux (plus de Base64).
- **Consommation Mémoire** : Optimisée pour les tablettes d'entrée de gamme.
- **Moteur PDF.js** & Rendu Image natif.
- **Navigation Tactile** :
    - **Zone Gauche** : Page précédente (ou morceau précédent de la setlist).
    - **Zone Droite** : Page suivante (ou morceau suivant de la setlist).
    - **Zone Bas** : Menu contextuel.
### ⏱️ Métronome Pro & Audio Sync
- **Haute Précision** : Nouvelle boucle temporelle sans dérive CPU.
- **Bip de Pré-compte** : Son distinctif (880Hz) généré dynamiquement.
- **Synchronisation Parfaite** : L'audio démarre précisément sur le premier temps fort après le pré-compte.

### 🛡️ Gestion des Sauvegardes
- **Sauvegarde Automatique** : Déclenchement au lancement selon un intervalle paramétrable (ex: tous les 30 jours).
- **Règle de Rétention** : Garde uniquement un nombre défini de copies (ex: 6) pour économiser l'espace.
- **Restauration en un clic** : Restaurez n'importe quelle version précédente depuis l'historique avec avertissement de sécurité.

---

## 🛠️ Stack Technique

- **Framework** : .NET 10 (MAUI)
- **Base de données** : SQLite (via sqlite-net-pcl)
- **Lecteur PDF** : PDF.js (injecté via WebView)
- **Compatibilité** : Android 12.0+ (API 31) minimum
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

**Développé par Audiothor** - *Version 1.0.0 "Public Library Edition"*
