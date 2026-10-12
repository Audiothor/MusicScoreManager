<h1 style="text-align: center;">Paramètres</h1>

Le menu **Paramètres**  vous permet d'adapter précisément Music Score Manager à vos habitudes de jeu, votre instrument et la taille de votre écran.

---

## 1. Paramètres Partitions

La page configure le comportement global de votre bibliothèque de partitions :

GENERAL

- **Tri par défaut à l'ouverture** :
  - Définissez quel ordre de tri s'applique dès le lancement de l'application (*Date d'ajout plus récent, plus ancien, Titre A-Z / Z-A, Date de modification, Évaluation par étoiles, Compositeur A-Z, ou Sans étiquette d'abord*).
- **Partitions sans compositeur en premier (Switch On/Off)** :
  - Lors d'un tri par compositeur, choisissez si les morceaux dont le champ compositeur est vide apparaissent au tout début (On) ou à la fin de l'ordre alphabétique (Off, comportement par défaut).

AFFICHAGE

- **Informations sous le titre de la partition** :
  
  - Personnalisez et réordonnez les informations affichées sous chaque titre dans la bibliothèque par **glisser-déposer (Drag & Drop)** ou boutons ▲ / ▼ (*Date d'ajout*, *Compositeur*, *Nombre de pages*), avec activation individuelle par cases à cocher et encadré d'aperçu en direct.

- **Affichage du numéro de la page actuelle (Switch On/Off)** :
  
  - Affiche ou masque la pastille discrète indiquant la page en cours (ex : *Page 3 / 12*) en bas à droite de l'écran pendant le jeu.

- **Affichage 2 pages en mode paysage (Switch On/Off)** :
  
  - Sur tablette ou écran large orienté à l'horizontale, affiche deux pages juxtaposées avec synchronisation géométrique des annotations et adaptation d'échelle.

- **Garder l'écran allumé pendant la lecture (Switch On/Off)** :
  
  - Permet d'empecher l'assombrissement et la mise en veille automatique pendant la consultation d'une partition

- **Taille d'affichage du numéro de page** :
  
  - Curseur réglable de **10 px à 40 px** avec aperçu direct de la taille pour adapter la lisibilité du compteur à votre distance de lecture du pupitre.

ERGONOMIE

- **Gestes ergonomiques de tourne de page** :
  - *Aller vers la page suivante* : Glisser vers la gauche, Taper à droite, ou Glisser vers le haut.
  - *Aller vers la page précédente* : Glisser vers la droite, Taper à gauche, ou Glisser vers le bas.

---

## 2. Paramètres Setlists

La page configure la lecture des concerts :

- **Lecture en continu par défaut (Switch On/Off)** :
  - Définit si toute nouvelle setlist créée doit automatiquement enchaîner sur le morceau suivant lorsqu'on tourne la dernière page d'un morceau.
- **Retour à la setlist en fin de partition** :
  - Si la lecture en continu est désactivée, tourner la page sur la dernière page du morceau quitte le visualiseur pour vous ramener automatiquement à la liste de la setlist.
- **Afficher le déroulement de la setlist (Switch On/Off)** :
  - Active ou désactive le déclenchement du volet de suivi de programme au toucher tout en haut au centre de l'écran (activé par défaut).

---

## 3. Paramètres Annotations

La page est divisée en deux chapitres clairs :

- **Chapitre 1 : Gestion des stickers Favoris** :
  
  - Champ de saisie pour créer vos stickers personnalisés avec texte libre (ex : *Vibrato*, *Attention*, *Solo*, *Respirer*,*Intro Hélène*...).
  - Liste de vos favoris avec suppression unitaire `✕`.

- **Chapitre 2 : Sélection des catégories de stickers actives** :
  
  - Cochez ou décochez les catégories de stickers que vous souhaitez voir apparaître dans le tiroir d'annotations (*Favoris, Doigtés, Nuances, Articulations, Répétitions, Notes, Silences, Altérations, Symboles*).

---

## 4. Paramètres Pédaliers Bluetooth & Événements MIDI

Sur scène comme en répétition, la lecture mains-libres est indispensable. Music Score Manager intègre un moteur universel capable de dialoguer aussi bien avec les **pédaliers Bluetooth (HID)** qu'avec les **contrôleurs et pédaliers MIDI (USB et Bluetooth BLE)**, tout en protégeant le musicien contre les fausses manipulations lors des performances en direct.

---

#### A. Périphériques Pris en Charge & Connexions

* **Pédaliers Bluetooth HID (Mode Clavier) :**
  * Compatible avec tous les pédaliers du marché : *AirTurn (Duo 500, Quad, PEDpro)*, *PageFlip (Dragonfly, Firefly, Butterfly)*, *Donner*, *Joyo*, *Thomann / Harley Benton*, *IK Multimedia iRig BlueTurn*, *Coda STOMP*, etc.
  * Il suffit d'appairer le pédalier dans les réglages Bluetooth de votre tablette.
* **Contrôleurs & Pédaliers MIDI (USB-OTG & Bluetooth BLE) :**
  * Connexion filaire directe sans pilote via adaptateur USB-OTG (Boss, Behringer FCB1010, Morningstar, Line 6 Helix, etc.) ou sans fil via adaptateur Bluetooth MIDI.
  * Interception native des messages :
    * **Control Change (CC)** : Pédale de sustain standard (`CC 64`), `CC 66`, ou tout CC personnalisé de 0 à 127.
    * **Notes MIDI** : Déclenchement d'actions depuis une touche de synthétiseur ou une note de pédalier basse.
    * **Program Change (PC)** : Synchronisation automatique des changements de morceaux avec vos multi-effets ou claviers.

---

#### B. Gestion des Profils de Pédales

Pour s'adapter à vos différents contextes de jeu (orchestre classique, groupe de rock, piano solo), l'application repose sur un système de profils interchangeables :

* **Profil Actif par Défaut :** Le profil sélectionné dans les paramètres généraux s'applique automatiquement à toute la bibliothèque.
* **Profils d'Usine Intégrés :** Des profils préconfigurés sont fournis pour les plus grands modèles du marché (Dragonfly 4 pédales, Duo 2 pédales, Flèches standard, etc.).
* **Profils Personnalisés Illimités :** Créez, nommez, dupliquez et modifiez autant de profils que vous le souhaitez avec vos propres annotations d'utilisation.
* **⭐ Profils Dédiés par Setlist :** Vous pouvez associer un profil spécifique à une setlist particulière (depuis l'éditeur de setlist). Lors de la lecture de cette setlist en concert, ce profil prend la priorité absolue, puis le profil par défaut est automatiquement restauré dès la sortie du lecteur.

---

#### C. Configuration et Apprentissage Automatique des Boutons

L'éditeur de profil permet d'assigner les pédales en quelques secondes grâce à l'apprentissage en direct :

1. **Détection Automatique (Mode « En écoute ») :**
   * Appuyez sur la pédale physique ou la commande MIDI : l'application capture instantanément le signal, identifie son type (Clavier HID, CC, Note MIDI) et affiche son code exact.
2. **Appui Simple (⚡) vs Appui Long (⏱️) :**
   * Chaque pédale physique peut accomplir **deux actions différentes** selon la durée de maintien (ex. : *Appui simple = Page suivante*, *Appui long = Morceau suivant de la setlist*).
3. **Catalogue des Actions Assignables :**
   * **Tourne & Navigation :** Page suivante / précédente, Défilement haut / bas, Première / dernière page, Morceau suivant / précédent de la setlist.
   * **Répétition & Outils Scéniques :** Démarrer / arrêter le métronome, Couper / activer le son du métronome, Lecture / pause de la piste audio, Recommencer le morceau.
   * **Lecture & Affichage :** Signet / marqueur suivant / précédent, Zoom avant / arrière / réinitialisation, Mode Nuit (inversion des couleurs).
   * **Annotations :** Verrouiller / déverrouiller le calque de dessin, Masquer / afficher les annotations, Annuler / rétablir le dernier tracé.

---

#### D. Sécurité Scène & Confort Anti-Rebond

Pour garantir une sérénité absolue sous la chaussure pendant le direct :

* **Protection Anti-Double Saut de Page :** Élimine les rebonds mécaniques de certains switches ou les pressions involontaires saccadées. L'application ignore les frappes trop rapprochées grâce à un délai de sécurité réglable (de 200 ms à 800 ms).
* **Sensibilité de l'Appui Long :** Un curseur de temporisation (200 ms à 1 000 ms) permet de régler avec exactitude le temps nécessaire pour valider un appui long sans risque de faux positif.

---

#### E. Conseil Pratique Android (Clavier Virtuel)

> 💡 **Astuce pour les pédaliers Bluetooth HID :**  
> Lorsqu'un pédalier est connecté en Bluetooth, Android le considère comme un clavier physique et peut masquer le clavier virtuel à l'écran.  
> Pour continuer à annoter ou chercher des morceaux au texte :  
> Rendez-vous dans les réglages Android de la tablette > **Système** > **Langues et saisie** > **Clavier physique**, et cochez l'option **« Afficher le clavier virtuel à l'écran »**.

---

## 5. Paramètres Application (Général)

La page gère le système et l'environnement global de l'application.

- **🌐 Choix de la Langue de l'Interface** :
  
  - Modal interactif proposant **8 langues intégrées** :
    * 🇫🇷 **Français**, 🇬🇧 **English**, 🇩🇪 **Deutsch**, 🇪🇸 **Español**, 🇮🇹 **Italiano**, 🇵🇱 **Polski**, 🇳🇱 **Nederlands**, 🇵🇹 **Português**.
  - Traduction instantanée dynamique de l'ensemble de l'interface.
    Nota : la langue prise par l'application est par défault celle qui est actuellement utilisée par l'OS de votre tablette/téléphone.
    Si la langue n'est pas prise en charge, elle se positionnera automatiquement sur l'anglais.

- **📊 Statistiques de la Bibliothèque & Métriques de Stockage** :
  
  - Nombre total de partitions PDF enregistrées.
  - Espace disque total occupé par vos partitions PDF (en Mo).
  - **Espace disponible sur le stockage** : Capacité mémoire libre restante sur l'appareil. (en Go)
  - **Taille de la base de données** : Espace occupé par SQLite (`scores.db3` + journaux WAL/SHM).

- **📁 Emplacements des Répertoires Personnalisables** :
  
  - Dossier racine des partitions, dossier des pistes audio, dossier des exports.
  - Prise en charge des répertoires publics Android pour liaison facile par câble USB à un PC.

---

## 6. Aide Intégrée

La page intègre des fiches d'aide et conseils scéniques hors-ligne plus succintes que celles du site web.

---

## 7. À propos

La page fournit les informations légales et techniques :

- Numéro de version dynamique.
- Auteur
- Licence libre **GNU General Public License v3.0 (GPLv3)**.
- Liste exhaustive des frameworks tiers utilisés (.NET MAUI, CommunityToolkit, SQLite, Mozilla PDF.js, SkiaSharp, ZXing).
- Liens directs vers le code source GitHub et la Politique de Confidentialité.
- Lien vers le forum d'échange Discord pour tous bugs, remarques, améiorations ou messages
