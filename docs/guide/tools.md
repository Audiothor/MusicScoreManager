<h1 style="text-align: center;">Outils</h1>

Le menu **Outils** regroupe la suite d'utilitaires spécialisés de Music Score Manager pour manipuler, partager, nettoyer et sécuriser vos partitions sans dépendre d'un ordinateur ou d'Internet.

---

## 1. Scanner de Partitions Papier (Capture Photo Haute Fidélité)

Le **Scanner de Partitions** ([`ScoreScannerPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ScoreScannerPage.xaml)) permet de numériser directement vos partitions papier depuis votre pupitre sans scanner externe :

- **Capture Photo Instantanée & Import Galerie** :
  - Prenez directement en photo chaque page de votre partition grâce à l'appareil photo (`Prendre une photo`) ou importez vos photos déjà enregistrées (`Importer une image`).
- **Filtres d'Amélioration Haute Visibilité** :
  - **Partition N&B optimisée (Haut contraste)** : applique un algorithme de seuillage adaptatif pour éliminer les ombres du pupitre, blanchir le fond et rendre les portées et les notes ultra-nettes.
  - **Niveaux de gris** : adoucit les dégradés tout en conservant une taille de fichier compacte.
  - **Couleur originale** : conserve l'aspect brut du document original et des annotations manuscrites colorées.
- **Attribution des Métadonnées & Dossier / Catégorie** :
  - Renseignez directement le **Titre** et le **Compositeur / Arrangeur**.
  - **Sélection du dossier / catégorie** : choisissez le dossier de classement (par défaut "Général") ou créez un nouveau dossier à la volée avec le bouton `➕`.
- **Gestion Dynamique des Pages Numérisées** :
  - Aperçu miniature de chaque page avec numéro d'ordre.
  - Rotation individuelle de 90° (`↻`) et suppression ciblée (`🗑️`).
  - Bouton `Supprimer tout` pour réinitialiser la session de numérisation.
- **Compilation Immédiate en Partition PDF** :
  - Les boutons **Générer la partition** (en haut et en bas) compilent instantanément vos pages traitées dans un PDF multi-pages, créent la fiche dans votre bibliothèque et l'ouvrent immédiatement dans le visualiseur !

---

## 2. Créateur & Assemblage PDF (Atelier Studio)

L'**Atelier d'Assemblage PDF** ([`PdfAssemblerPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/PdfAssemblerPage.xaml)) est un studio complet de retouche et de composition de documents PDF multi-pages.

### Fonctionnalités disponibles :

- **Créer un PDF à partir de photos ou d'images** :
  - Sélectionnez plusieurs photos de partitions prises avec votre appareil photo ou stockées dans votre galerie (`.jpg`, `.png`).
  - L'application compile les images en conservant 100% de leur résolution et de leur rapport d'aspect.
- **Visualisation par miniatures de pages** :
  - Chaque page du document est représentée par une miniature interactive avec son numéro d'ordre.
- **Réorganisation des pages** :
  - Utilisez les boutons **Monter (▲)** et **Descendre (▼)** sous chaque page pour réordonner vos feuillets.
- **Rotation individuelle par page** :
  - Bouton **↻ Pivoter** sur chaque miniature pour tourner une page spécifique de 90° en 90° (0°, 90°, 180°, 270°). Idéal pour les pages scannées à l'envers ou en orientation paysage.
- **Duplication de page** :
  - Clonez une page en un clic (très utile pour répéter un refrain ou un Da Capo sans avoir à tourner la page en arrière pendant le jeu).
- **Suppression de page (🗑️)** :
  - Retirez les pages de garde blanches ou les publicités présentes dans les téléchargements.
- **Inverser l'ordre de toutes les pages** :
  - Remet à l'endroit un document scanné de la dernière à la première page.
- **Enregistrement & Remplacement** :
  - Enregistrez le document sous forme d'une nouvelle partition ou remplacez directement le fichier original.

---

## 3. Transfert Wi-Fi Direct (P2P) & Diffusion Groupe

La page ([`WifiTransferPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/WifiTransferPage.xaml)) gère les échanges sans fil directs entre musiciens, **sans box internet ni routeur 4G/5G requis**.
La manipulation nécessite juste d'activer le Wi-Fi (sans connexion Internet nécessaire !).
Les partitions reçues sont automatiquement dirigées vers votre dossier "Général", afin de préserver votre propre organisation personnelle de répertoires sans être polluée par celle de l'expéditeur.

#### A. Échange Direct de Tablette à Tablette (P2P)

<u>CAS OÙ LES PERSONNES SONT SUR UN MÊME RÉSEAU WI-FI</u>

1. **Le Destinataire** :
   - Ouvre **Outils > Transfert Wi-Fi Direct**, active le Wi-Fi de sa tablette et appuie sur **Recevoir**. Son appareil devient visible sur le réseau local ad-hoc.
2. **L'Émetteur** :
   - Sélectionne la partition ou la setlist à envoyer (depuis les menus 3 points ⋮ ou en multi-sélection).
   - Choisit d'inclure ou non les annotations manuscrites et les pistes audio.
   - Sélectionne le destinataire dans la liste des appareils détectés. Le transfert binaire TCP s'exécute à très grande vitesse.

<u>CAS OÙ LES PERSONNES NE SONT PAS SUR UN MÊME RÉSEAU WI-FI</u>

#### 1. Créer le lien Wi-Fi entre les 2 tablettes

1. **Sur la tablette 1 (n'importe laquelle des deux) :**
   - Ouvrez les réglages Android / le volet supérieur de notifications.
   - Activez le **« Point d'accès Wi-Fi »** (ou *Partage de connexion*).
2. **Sur la tablette 2 :**
   - Activez le **Wi-Fi** et connectez-vous au réseau Wi-Fi créé par la tablette 1. *(Les deux tablettes sont désormais reliées en réseau direct)*.

---

#### 2. Réaliser le transfert dans Music Score Manager

1. **Sur la tablette qui reçoit :**
   - Ouvrez l'onglet **Outils** > **Transfert Wi-Fi Direct (P2P)**.
   - Cliquez sur le gros bouton vert **« 🟢 Activer la Réception (Se rendre visible) »**.

2. **Sur la tablette qui envoie :**
   - Sur la partition ou la setlist à partager, appuyez sur le menu **⋮** (3 points) > **« Envoyer en Wi-Fi Direct »** (ou cochez plusieurs partitions avec le bouton de sélection).
   - L'autre tablette apparaît instantanément dans la liste des appareils détectés.
   - Cliquez sur son nom pour lancer l'envoi.

3. **Sur la tablette qui reçoit :**
   - Une boîte de dialogue apparaît : cliquez sur **« Accepter »**.
   - Les partitions, annotations et pistes audio sont reçues et intégrées directement dans la bibliothèque en quelques secondes !

#### B. Mode Diffusion de Groupe par QR Code (Multi-Musiciens)

1. **L'émetteur** :
   - Active le **Mode Diffusion Groupe**. L'application démarre un point d'accès Wi-Fi sécurisé et un serveur local HTTP embarqué, puis affiche un **QR Code haute résolution** sur son écran.
2. **Les membres du groupe** :
   - Ouvrent leur appareil photo ou l'onglet **Outils > Réception** de l'application et scannent le QR Code.
   - Tous les musiciens téléchargent simultanément la partition ou la setlist complète sur leur tablette en quelques secondes !

---

## 4. Gestion des Étiquettes (Tags)

La page ([`TagsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/TagsPage.xaml) & [`TagEditPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/TagEditPage.xaml)) vous donne le plein contrôle sur l'organisation de votre bibliothèque :

- **Créer une nouvelle étiquette** avec un nom personnalisé (ex : *Chorale*, *Solfège*, *Concert Noël*, *Guitare Acoustique*).
- **Palette de couleurs vives & curseurs RVB** : assignez une pastille colorée unique à chaque étiquette avec aperçu en temps réel pour une identification visuelle immédiate dans la liste des partitions.
- **Modifier ou renommer** un tag existant.
- **Supprimer une étiquette** : supprime l'étiquette de l'application sans endommager les partitions qui la possédaient.

---

## 5. Imports de fichiers (Setlists, Partitions et paquets)

La page ([`ImportPackagePage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ImportPackagePage.xaml)) vous permet de charger des fichiers partagés par des tiers :

- **Formats reconnus** : `.msmsetlist` (setlist complète), `.msmscore` (partition individuelle avec annotations/audio) et `.msmscores` (paquet groupé de plusieurs partitions).
- **Importation automatisée** : extrait les documents PDF, recrée les métadonnées musicales (tempo, compositeur, tonalité, note), réinjecte les calques d'annotations et rattache les fichiers audio dans votre bibliothèque locale en une seule opération.
- **Classement neutre** : les partitions issues de paquets externes sont placées dans le dossier par défaut "Général" pour respecter votre propre arborescence.

---

## 6. Gestion des Doublons

La page ([`DuplicatesPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/DuplicatesPage.xaml)) inspecte l'intégralité de votre bibliothèque de partitions pour libérer de l'espace de stockage et clarifier votre catalogue :

- **Analyse d'empreinte SHA-256** : compare le contenu binaire exact de vos PDF (même si deux fichiers portent des noms différents).
- **Analyse récursive des dossiers** : examine automatiquement l'ensemble des sous-répertoires de catégories (`📁 [Catégorie] Chemin`).
- **Regroupement par doublons** : affiche les fichiers en double côte à côte avec leurs métadonnées, dates et chemins.
- **Suppression sécurisée** : supprimez les copies superflues en conservant la version originale de référence.

---

## 7. Gestion des Sauvegardes & Restauration

La page ([`BackupsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/BackupsPage.xaml)) assure la pérennité totale de votre travail musical :

- **Sauvegarde manuelle rapide (Base de données)** : créez un instantané horodaté de votre base de données SQLite (titres, métadonnées, setlists, liaisons, annotations, tags).
- **Historique des sauvegardes de la base de données** : liste détaillée des points de sauvegarde de la base de données enregistrés localement avec date, heure et poids, et restauration en un clic.
- **Sauvegarde complète des données (.zip)** : export tout-en-un compressé réunissant la base de données SQLite (`scores.db3`), l'intégralité des partitions PDF physiques avec leur arborescence de répertoires, ainsi que tous les fichiers audio associés.
- **Destination personnalisée** : proposé par défaut dans le dossier Téléchargements (`Download`) et modifiable pour exporter directement vers un stockage externe (clé USB, carte SD) ou un cloud synchronisé (Google Drive, pCloud, OneDrive).
- **Dialogue de progression bloquant** : suivi étape par étape avec pourcentage exact et possibilité d'annulation propre.
- **Restauration complète (Erase / Replace)** : réimportation intégrale d'une archive `.zip` avec confirmation solennelle, extraction sécurisée en zone temporaire, remplacement complet et vérification automatique de l'intégrité de la bibliothèque.
