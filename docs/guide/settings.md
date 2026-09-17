# ⚙️ Paramètres

Le menu **Paramètres** ([`SettingsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsPage.xaml)) vous permet d'adapter précisément Music Score Manager à vos habitudes de jeu, votre instrument et la taille de votre écran.

---

## 🎵 1. Paramètres Partitions

La page [`SettingsScoresPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsScoresPage.xaml) configure le comportement global de votre bibliothèque de partitions :

- **Tri par défaut à l'ouverture** :
  - Définissez quel ordre de tri s'applique dès le lancement de l'application (*Date d'ajout plus récent, plus ancien, Titre A-Z / Z-A, Date de modification, Évaluation par étoiles, Compositeur A-Z, ou Sans étiquette d'abord*).
- **Partitions sans compositeur en premier (Switch On/Off)** :
  - Lors d'un tri par compositeur, choisissez si les morceaux dont le champ compositeur est vide apparaissent au tout début (On) ou à la fin de l'ordre alphabétique (Off, comportement par défaut).
- **Informations sous le titre de la partition** :
  - Personnalisez le sous-titre figurant sur les cartes de la bibliothèque (*Date d'ajout*, *Compositeur seul*, ou *Compositeur et date d'ajout*).
- **Affichage du numéro de la page actuelle (Switch On/Off)** :
  - Affiche ou masque la pastille discrète indiquant la page en cours (ex : *Page 3 / 12*) en bas à droite de l'écran pendant le jeu.
- **Affichage 2 pages en mode paysage (Switch On/Off)** :
  - Sur tablette ou écran large orienté à l'horizontale, affiche deux pages juxtaposées avec synchronisation géométrique des annotations et adaptation d'échelle.
- **Taille d'affichage du numéro de page** :
  - Curseur réglable de **10 px à 40 px** avec aperçu direct de la taille pour adapter la lisibilité du compteur à votre distance de lecture du pupitre.
- **Gestes ergonomiques de tourne de page** :
  - *Aller vers la page suivante* : Glisser vers la gauche, Taper à droite, ou Glisser vers le haut.
  - *Aller vers la page précédente* : Glisser vers la droite, Taper à gauche, ou Glisser vers le bas.

---

## 📋 2. Paramètres Setlists

La page [`SettingsSetlistsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsSetlistsPage.xaml) configure la lecture des concerts :

- **Lecture en continu par défaut (Switch On/Off)** :
  - Définit si toute nouvelle setlist créée doit automatiquement enchaîner sur le morceau suivant lorsqu'on tourne la dernière page d'un morceau.
- **Retour à la setlist en fin de partition** :
  - Si la lecture en continu est désactivée, tourner la page sur la dernière page du morceau quitte le visualiseur pour vous ramener automatiquement à la liste de la setlist.
- **Afficher le déroulement de la setlist (Switch On/Off)** :
  - Active ou désactive le déclenchement du volet de suivi de programme au toucher tout en haut au centre de l'écran (activé par défaut).

---

## 🏷️ 3. Paramètres Étiquettes

- Raccourci vers la console de gestion des étiquettes ([`TagsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/TagsPage.xaml)), palette de couleurs et mélangeur RVB personnalisé.

---

## ✍️ 4. Paramètres Annotations

La page [`SettingsAnnotationsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsAnnotationsPage.xaml) est divisée en deux chapitres clairs :

- **Chapitre 1 : Gestion des stickers Favoris** :
  - Champ de saisie pour créer vos stickers personnalisés avec texte libre (ex : *Vibrato*, *Attention*, *Solo*, *Respirer*).
  - Liste de vos favoris avec suppression unitaire `✕`.
- **Chapitre 2 : Sélection des catégories de stickers actives** :
  - Cochez ou décochez les catégories de stickers que vous souhaitez voir apparaître dans le tiroir d'annotations (*Favoris, Doigtés, Nuances, Articulations, Répétitions, Notes, Silences, Altérations, Symboles*).

---

## 🦶 5. Paramètres Pédaliers Bluetooth & Événements MIDI

La page [`SettingsPedalsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsPedalsPage.xaml) assure le contrôle mains-libres complet :

- **Interrupteur Général & Testeur en Direct** :
  - Voyant d'écoute en temps réel (🟢).
  - Diagnostic immédiat affichant le nom de la touche pressée, le code hexadécimal, la source (Bluetooth HID ou MIDI), le badge *« ⏱️ Pression Longue »* et l'action exécutée.
- **Profils de Pédales Préconfigurés** :
  - *Standard (Touches clavier)*, *PageFlip Dragonfly (4 pédales)*, *PageFlip Firefly & Butterfly*, *AirTurn Duo 500 & PEDpro*, *AirTurn Quad 500*, *Joyo JSP-01*, *Thomann / Harley Benton*, *Donner Wireless*, *IK Multimedia iRig BlueTurn*, *Coda STOMP*, *Contrôleur MIDI Avancé (USB / Bluetooth : CC 64, CC 66, CC 67, Notes C1-F1, Program Change)*.
- **Profils Personnalisés & Mode Apprentissage (*Learn Mode*)** :
  - Création, duplication, réinitialisation et capture automatique par appui sur la pédale.
- **Palette d'Actions Assignables (Appui Court & Appui Long)** :
  - Tourner les pages, changer de morceau dans la setlist, aller au début/fin, défiler, métronome On/Off/Mute, audio Play/Pause, reset zoom 100%, saut direct, verrouiller/déverrouiller annotations, annuler/rétablir, menu central, quitter.
- **Sensibilité d'Appui Long** :
  - Réglette réglable de 200 ms à 1000 ms pour calibrer le déclenchement des actions sur pression prolongée.

---

## 🌐 6. Paramètres Application (Général)

La page [`SettingsAppPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsAppPage.xaml) gère le système et l'environnement :

- **🌐 Choix de la Langue de l'Interface** :
  - Modal interactif proposant **8 langues intégrées** :
    * 🇫🇷 **Français**, 🇬🇧 **English**, 🇩🇪 **Deutsch**, 🇪🇸 **Español**, 🇮🇹 **Italiano**, 🇵🇱 **Polski**, 🇳🇱 **Nederlands**, 🇵🇹 **Português**.
  - Traduction instantanée dynamique de l'ensemble de l'interface.
- **📊 Statistiques de la Bibliothèque & Métriques de Stockage** :
  - Nombre total de partitions PDF enregistrées.
  - Espace disque total occupé par vos partitions PDF (en Mo/Go).
  - **Espace disponible sur le stockage** : Capacité mémoire libre restante sur l'appareil.
  - **Taille de la base de données** : Espace occupé par SQLite (`scores.db3` + journaux WAL/SHM).
- **📁 Emplacements des Répertoires Personnalisables** :
  - Dossier racine des partitions, dossier des pistes audio, dossier des exports.
  - Prise en charge des répertoires publics Android pour liaison facile par câble USB à un PC.

---

## ❓ 7. Aide Intégrée

La page [`HelpPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/HelpPage.xaml) intègre des fiches d'aide et conseils scéniques hors-ligne.

---

## ℹ️ 8. À propos

La page [`AboutPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/AboutPage.xaml) fournit les informations légales et techniques :

- Numéro de version dynamique.
- Licence libre **GNU General Public License v3.0 (GPLv3)**.
- Liste exhaustive des frameworks tiers utilisés (.NET MAUI, CommunityToolkit, SQLite, Mozilla PDF.js, SkiaSharp, ZXing).
- Liens directs vers le code source GitHub et la Politique de Confidentialité.
