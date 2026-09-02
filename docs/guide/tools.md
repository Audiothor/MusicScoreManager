# 🛠️ Menu Outils

Le menu **Outils** regroupe la suite d'utilitaires spécialisés de Music Score Manager pour manipuler, partager, nettoyer et sécuriser vos partitions sans dépendre d'un ordinateur ou d'Internet.

---

## 📑 1. Créateur & Assemblage PDF (Atelier Studio)

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

## 📡 2. Transfert Wi-Fi Direct (P2P) & Diffusion Groupe

La page [`WifiTransferPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/WifiTransferPage.xaml) gère les échanges sans fil directs entre musiciens, **sans box internet ni routeur 4G/5G requis**.

### A. Échange Direct de Tablette à Tablette (P2P) :
1. **Destinataire** :
   - Ouvre **Outils > Transfert Wi-Fi Direct**, active le Wi-Fi de sa tablette et appuie sur **Recevoir**. Son appareil devient visible sur le réseau local ad-hoc.
2. **Émetteur** :
   - Sélectionne la partition ou la setlist à envoyer (depuis les menus 3 points ⋮ ou en multi-sélection).
   - Choisit d'inclure ou non les annotations manuscrites et les pistes audio.
   - Sélectionne le destinataire dans la liste des appareils détectés. Le transfert binaire TCP s'exécute à très grande vitesse.

### B. Mode Diffusion de Groupe par QR Code (Multi-Musiciens) :
1. **L'émetteur** :
   - Active le **Mode Diffusion Groupe**. L'application démarre un point d'accès Wi-Fi sécurisé et un serveur local HTTP embarqué, puis affiche un **QR Code haute résolution** sur son écran.
2. **Les membres du groupe** :
   - Ouvrent leur appareil photo ou l'onglet **Outils > Réception** de l'application et scannent le QR Code.
   - Tous les musiciens téléchargent simultanément la partition ou la setlist complète sur leur tablette en quelques secondes !

---

## 🏷️ 3. Gestion des Étiquettes (Tags)

La page [`TagsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/TagsPage.xaml) vous donne le plein contrôle sur la taxonomie de votre bibliothèque :
- **Créer une nouvelle étiquette** avec un nom personnalisé (ex : *Chorale*, *Solfège*, *Concert Noël*, *Guitare Acoustique*).
- **Palette de couleurs vives** : assignez une pastille colorée unique à chaque étiquette pour une identification visuelle instantanée dans la liste des partitions.
- **Modifier ou renommer** un tag existant.
- **Supprimer une étiquette** : supprime l'étiquette de l'application sans endommager les partitions qui la possédaient.

---

## 📦 4. Imports de Paquets & Setlists

La page [`ImportPackagePage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ImportPackagePage.xaml) vous permet de charger des archives partagées par des tiers :
- **Formats reconnus** : `.msmsetlist` (setlist complète), `.msmscore` (partition individuelle avec annotations/audio) et `.msmscores` (paquet groupé de plusieurs partitions).
- **Importation automatisée** : extrait les documents PDF, recrée les métadonnées musicales (tempo, compositeur, tonalité, note), réinjecte les calques d'annotations et rattache les fichiers audio dans votre bibliothèque locale en une seule opération.

---

## 🔍 5. Gestion des Doublons

La page [`DuplicatesPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/DuplicatesPage.xaml) inspecte votre répertoire de partitions pour libérer de l'espace de stockage et clarifier votre bibliothèque :
- **Analyse d'empreinte SHA-256** : compare le contenu binaire exact de vos PDF (même si deux fichiers portent des noms différents).
- **Regroupement par doublons** : affiche les fichiers en double côte à côte avec leurs métadonnées, dates et chemins.
- **Suppression sécurisée** : supprimez les copies superflues en conservant la version originale de référence.

---

## 💾 6. Gestion des Sauvegardes & Restauration

La page [`BackupsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/BackupsPage.xaml) assure la pérennité totale de votre travail musical :
- **Sauvegarde manuelle immédiate** : créez un instantané horodaté de votre base de données SQLite (partitions, setlists, liaisons, annotations, tags, paramètres).
- **Historique des sauvegardes** : liste détaillée des points de sauvegarde enregistrés sur votre appareil avec date, heure et poids.
- **Restauration en un clic** : rétablissez n'importe quel état antérieur en cas de mauvaise manipulation ou de réinstallation.
- **Export & Sauvegarde externe** : enregistrez votre archive de sauvegarde sur une clé USB, carte micro-SD ou dossier cloud personnel.
