# ⚙️ Menu Paramètres

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
  - Sur tablette ou écran large orienté à l'horizontale, affiche deux pages juxtaposées pour limiter de moitié la fréquence des tournes de page.
- **Taille d'affichage du numéro de page** :
  - Curseur réglable de **10 px à 40 px** pour adapter la lisibilité du compteur à votre distance de lecture du pupitre.
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

---

## 🏷️ 3. Paramètres Étiquettes

- Raccourci vers le module de création, d'attribution de couleurs et d'organisation globale des étiquettes (tags) de la bibliothèque.

---

## ✍️ 4. Paramètres Annotations & Stickers Favoris

La page [`SettingsAnnotationsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsAnnotationsPage.xaml) personnalise vos outils de dessin :

- **Gestion des Stickers Personnalisés** :
  - Ajoutez vos propres abréviations, textes récurrents ou symboles spécifiques (ex : *Vibrato*, *Solo*, *Sourdine*, *Tacet*, *Reprise*).
- **Liste des favoris enregistrés** :
  - Consultez, modifiez ou supprimez vos stickers favoris qui apparaîtront directement dans la palette de tampons du visualiseur.

---

## 🌐 5. Paramètres Application (Général)

La page [`SettingsAppPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsAppPage.xaml) gère le système et l'environnement :

- **🌐 Choix de la Langue de l'Interface** :
  - Modal interactif proposant **8 langues intégrées** :
    * 🇫🇷 **Français**
    * 🇬🇧 **English**
    * 🇩🇪 **Deutsch**
    * 🇪🇸 **Español**
    * 🇮🇹 **Italiano**
    * 🇵🇱 **Polski**
    * 🇳🇱 **Nederlands**
    * 🇵🇹 **Português**
  - L'application traduit instantanément tous les menus, boutons et messages sans nécessiter de redémarrage.
- **📊 Statistiques de la Bibliothèque** :
  - Compteur en direct du nombre total de partitions, documents PDF et images gérés par l'application.
- **📁 Emplacements des Répertoires** :
  - Visualisez et modifiez les dossiers de stockage locaux :
    * *Dossier des partitions* : répertoire où sont stockés vos PDF importés.
    * *Dossier des pistes audio* : emplacement de vos fichiers d'accompagnement MP3/WAV.
    * *Dossier des exports* : répertoire de destination de vos sauvegardes et paquets exportés.

---

## ❓ 6. Aide Intégrée

La page [`HelpPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/HelpPage.xaml) intègre un visualiseur d'aide hors-ligne accessible directement dans l'application sans quitter votre instrument.

---

## ℹ️ 7. À propos

La page [`AboutPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/AboutPage.xaml) fournit les informations légales et techniques :
- **Numéro de version actuelle** (ex : *v1.9.5.7*).
- **Licence libre GNU General Public License v3.0 (GPLv3)**.
- **Crédits & Bibliothèques tierces utilisées** (.NET MAUI, CommunityToolkit, SQLite, Mozilla PDF.js).
- **Bouton vers le code source GitHub**.
- **Bouton vers la Politique de Confidentialité**.
