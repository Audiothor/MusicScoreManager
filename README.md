<p align="center">
  <img src="Resources/Splash/app_splash_padded.png" width="400" />
</p>

# Music Score Manager v1.9.5.5

**Music Score Manager** est une application mobile multiplateforme construite avec **.NET MAUI** (ciblant principalement Android) conçue pour les musiciens afin de gérer, organiser, annoter et visualiser leurs partitions (PDF standardisés) de manière efficace, particulièrement en situation de concert.

---

## 🚀 Fonctionnalités Clés

### ▶️ Démarrage Direct de Setlist & Clarté des Menus (v1.9.5.5)
- **Démarrage Instantané de Setlist au Clic** : Cliquer sur une setlist dans la liste lance directement la lecture de sa première partition en mode concert (avec message d'information si la setlist ne contient encore aucun morceau).
- **Option « Démarrer la setlist » (3 points ⋮)** : Remplacement de l'ancien libellé par *« ▶️ Démarrer la setlist »* pour une intention d'action immédiate, tout en conservant l'option dédiée *« ✏️ Éditer la setlist »*.
- **Libellé « Éditer la partition »** : Harmonisation du menu contextuel des partitions pour remplacer *« Éditer les informations »* par *« ✏️ Éditer la partition »*.

### 🎛️ Options de Partage & Export de Partition (v1.9.5.4)
- **Menu d'Options Détaillé (Annotations & Audio)** : Comme pour les setlists, l'envoi Wi-Fi Direct ou l'export de partition propose désormais un menu d'options permettant de cocher/décocher l'inclusion des annotations manuscrites et des pistes audio rattachées.
- **Harmonisation Complète** : Fonctionne aussi bien pour le partage d'une partition individuelle que pour l'envoi/export groupé en multi-sélection.

### 🛡️ Contrôle Strict & Intégrité des Fichiers PDF (v1.9.5.3)
- **Validation Binaire de l'En-tête & Structure PDF** : Vérification systématique lors de tout import ou assemblage que le fichier est un véritable document PDF intègre (magic header `%PDF-`, terminaison, descripteurs natifs de rendu) même s'il porte l'extension `.pdf`.
- **Protection Anti-Crash** : Rejet automatique des fichiers corrompus, incomplets ou renommés artificiellement avec avertissement explicite à l'utilisateur, empêchant toute instabilité du visualiseur de partitions.

### ⚠️ Indicateurs Visuels de Fichier Manquant (v1.9.5.2)
- **Badge d'Erreur Rouge (!)** : Pastille rouge vive avec point d'exclamation positionnée à gauche du bouton 3 points (⋮) dans la bibliothèque de partitions dès qu'un fichier PDF est introuvable ou déplacé.
- **Alerte dans l'Édition de Partition** : Bannière d'avertissement rouge explicite à l'ouverture de la page d'édition pour informer immédiatement que le fichier PDF associé est manquant.
- **Indication dans les Setlists** : Badge d'alerte rouge affiché sur les partitions d'une setlist dont le fichier PDF est manquant avec blocage préventif de l'ouverture.

### 📋 Duplication de Setlists & Tris Intelligents (v1.9.5.1)
- **Duplication de Setlist en un Clic** : Dupliquez instantanément n'importe quelle setlist avec l'intégralité de ses morceaux et de leur ordre depuis le menu d'options (3 points ⋮).
- **Tri Compositeur Intelligent avec Option On/Off** : Tri alphabétique des compositeurs avec affichage automatique des partitions sans compositeur à la fin (par défaut), ou au début via le nouveau paramètre *Partitions sans compositeur en premier*.
- **Tri Sans Étiquette** : Option de tri direct *Sans étiquette d'abord* pour identifier immédiatement les partitions non classées et leur attribuer des étiquettes.

### 🌟 Ergonomie, Métadonnées & Expérience Partition (v1.9.4.0)
- **Menu Central & Rétablissement du Zoom (100%)** : Ajout d'un bouton « 🔍 Rétablir la taille d'origine (100%) » dans le menu central au double-tap. Détection du double-tap optimisée et fonctionnelle en toutes circonstances (même en plein zoom ou dézoom).
- **Sous-titres de Partition Configurables** : Nouveau paramètre dans *Paramètres > Partitions* permettant de choisir les informations affichées sous le titre de chaque partition dans la bibliothèque (*Date d'ajout*, *Compositeur*, ou *Compositeur et date d'ajout*).
- **Popup Moderne & Ergonomique (3 points ⋮)** : Carte sombre moderne avec coins arrondis, ombre portée, en-tête complet (icône 🎵 ou 📋, titre, sous-titre, bouton fermeture ✕) et boutons larges bien espacés évitant les erreurs de manipulation (*Ouvrir*, *Éditer les informations*, *Envoyer en Wi-Fi Direct*, *Exporter*, *Renommer*, *Supprimer*).
- **Page d'Édition Métadonnées Enrichie** : Nouveaux champs pour *Compositeur*, *Tempo (BPM)*, *Tonalité* (supportant la notation classique *Do, Ré, Mi...* et anglo-saxonne *A, B, C...*), et *Évaluation (étoiles)*. Section *Étiquettes* réordonnée au-dessus du tempo.
- **Affichage Épuré du Chemin de Fichier** : Affichage précis du dossier parent (`dirname`) entre parenthèses à côté du libellé pour une clarté optimale.
- **Accordéons Ergonomiques** : Sections *Métronome* et *Fichiers audio* fermées par défaut pour alléger l'interface d'édition.
- **Tri des Partitions par Défaut (Récent)** : Tri automatique des partitions avec les plus récemment ajoutées en premier (`Date d'ajout (Récent)`), entièrement personnalisable dans les paramètres.
- **Ouverture & Fermeture Ultra-Fluides (< 50ms)** : Double-buffering off-screen canvas éliminant le balayage visuel, fond natif Android noir `#000000` sans flash blanc, et nettoyage asynchrone évitant tout gel UI lors de la fermeture du viewer.

### 📑 Atelier d'Assemblage & Nouvelle Ergonomie de l'Onglet Outils (v1.9.5.0)
- **Ergonomie Unifiée (Modèle Paramètres)** : Remplacement complet des anciens accordéons par un hub épuré sous forme de liste de cartes avec chevrons `›`. Chaque outil s'ouvre désormais dans sa propre sous-page dédiée avec en-tête `← Retour` assurant un confort visuel maximal, sans défilement surchargé.
- **Modification Complète de Partitions Existantes** : Chargez n'importe quelle partition PDF de votre bibliothèque ou fichier externe pour en modifier l'assemblage complet : réorganisation de l'ordre des pages (▲ / ▼), rotation individuelle (⟳ 90°), rotation globale de tout le document, inversion complète de l'ordre des pages, duplication (📑) et suppression de pages (🗑️).
- **Insertion de Pages Blanches & Fusion Multi-PDF** : Insérez à volonté des pages blanches (idéal pour synchroniser vos tournes de pages) ou fusionnez des pages provenant d'autres fichiers PDF / photos.
- **Visualisation / Zoom Haute Définition par Page** : Prévisualisez chaque page en plein écran (bouton `🔍` ou tap sur miniature) avec navigation précédent/suivant et rotation interactive pour vérifier la netteté et la mise en page.
- **Remplacement direct ou Nouvelle copie** : Choisissez d'écraser la partition existante en conservant vos réglages ou de l'enregistrer comme nouvelle copie autonome.
- **Raccourcis Directs dans l'Application** : Accédez à l'atelier d'assemblage en un clic depuis le menu contextuel (⋮) de la bibliothèque de partitions, la page d'édition des métadonnées ou directement depuis le menu central du lecteur de partitions.
- **Sous-pages Dédiées** :
  - `📑 Créateur & Assemblage PDF` (`PdfAssemblerPage`)
  - `📡 Transfert Wi-Fi Direct (P2P)` (`WifiTransferPage`)
  - `🏷️ Gestion des étiquettes` (`TagsPage`)
  - `📦 Imports de paquets & setlists` (`ImportPackagePage`)
  - `🔍 Gestion des doublons` (`DuplicatesPage`)
  - `💾 Gestion des sauvegardes` (`BackupsPage`)
- **Détection & Fusion Intelligente à l'Import** : Lors de l'import (`+` dans Partitions), si plusieurs photos/images sont sélectionnées, l'application propose automatiquement de les fusionner en 1 seule partition PDF multi-pages (tri naturel des pages) ou de les convertir individuellement.
- **Conversion Automatique en PDF** : Toute image importée est proprement convertie au format PDF standardisé pour unifier l'expérience de lecture, de zoom et d'annotations.

### 📡 Envoi & Diffusion Sans Fil Wi-Fi Direct & QR Code (v1.9.5.0)
- **Transfert Wi-Fi Direct Ultra-Rapide (P2P)** : Échangez instantanément des partitions ou des setlists entières d'une tablette à une autre en streaming TCP binaire direct sans aucune connexion Internet ni box requise.
- **Diffusion de Groupe par QR Code (1-à-plusieurs)** : Le chef de pupitre ou leader génère un point d'accès/serveur local temporaire et affiche un QR Code à l'écran : tous les musiciens du groupe le scannent simultanément pour télécharger le programme en parallèle.
- **Zéro Configuration Manuelle** : L'application gère de façon transparente la découverte UDP balise, l'ouverture et la fermeture des sockets sans nécessiter de manipulations techniques dans les paramètres Android.
- **Export & Import de Packages Autonomes (`.msmsetlist`, `.msmscore`, `.msmscores`)** : Les exports et imports de fichiers physiques restent 100% opérationnels pour l'archivage ou le partage USB.
- **Boîte de Dialogue avec Options d'Envoi** : Choisissez précisément avant l'envoi d'inclure ou non vos annotations (doigtés, surlignages, textes) et les pistes audio (MP3/WAV) rattachées.

#### 📖 Guide pas à pas : Envoi direct à une personne (1-à-1)
1. **Sur la tablette émettrice** :
   - Sélectionnez la partition (menu `⋮` › *Envoyer en Wi-Fi Direct*) ou cochez plusieurs partitions via le mode multi-sélection (bouton `☑`), ou ouvrez une Setlist (`⋮` › *Envoyer en Wi-Fi Direct*).
   - Cochez les options souhaitées (*Inclure les annotations*, *Inclure les pistes audio*).
   - L'écran affiche la recherche automatique des tablettes à proximité.
2. **Sur la tablette réceptrice** :
   - Ouvrez l'onglet **Outils** › **Transfert Wi-Fi Direct (P2P)** › cliquez sur **« 🟢 Mode Réception (Se rendre visible) »** (ou depuis le bouton de transfert de la page Partitions).
3. **Transmission instantanée** :
   - L'émetteur voit apparaître le nom de la tablette réceptrice et clique sur **« Envoyer »**.
   - Un message de confirmation s'affiche sur la tablette réceptrice : cliquez sur **« Accepter »**.
   - Le transfert s'effectue à haute vitesse (Mo/s) et les partitions, annotations et audios sont intégrés directement dans la bibliothèque !

#### 📖 Guide pas à pas : Diffusion à tout un groupe en parallèle (1-à-Plusieurs)
1. **Sur la tablette du chef de pupitre / leader (Émetteur)** :
   - Sélectionnez les partitions ou la setlist à diffuser › cliquez sur *Envoyer*.
   - Cliquez sur le bouton rose **« 📲 Mode Diffusion Groupe (QR Code) »**.
   - Un grand QR Code s'affiche à l'écran avec l'adresse locale du serveur de partage et un compteur de musiciens connectés en direct.
2. **Sur les tablettes de tous les musiciens (Récepteurs)** :
   - Chaque musicien ouvre **Outils** › **Transfert Wi-Fi Direct (P2P)**.
   - Il scanne le QR Code affiché sur la tablette du leader (ou saisit l'adresse `http://...`).
   - Le téléchargement et l'intégration s'exécutent simultanément en parallèle pour tous les musiciens du groupe, sans nécessiter de box Internet !


### ⏱️ Métronome Temps Réel Haute Précision & Contrôle du Son (v1.8.22 - v1.8.26)
- **Régularité Absolue & Moteur Thread-Safe (v1.8.26)** : Verrouillage atomique du thread d'horloge pour empêcher tout conflit de tempo. Cadence rigoureusement métronomique et fluide avec double temporisation fine (`Thread.Sleep` + `SpinWait`) et synchronisation visuelle LED découplée.
- **Contrôle On/Off du Son dans le Menu Central (v1.8.26)** : Possibilité d'activer ou de couper le son du métronome à la volée directement depuis le menu central de la partition, tout en conservant l'option assignée par défaut à la partition.
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
- **Intégration des Étiquettes dans l'onglet Outils (v1.8.30)** : Fusion complète de la gestion des étiquettes (création, recherche, modification, suppression) sous forme de chapitre dédié dans **Outils** aux côtés de la **Gestion des sauvegardes**.
- **Menu Principal Optimisé à 5 Onglets Stricts (v1.8.30)** : Barre de navigation épurée (`[Partitions] [Setlists] [Outils] [Paramètres] [Quitter]`) éliminant définitivement l'apparition du menu « More / Plus » sur les interfaces mobiles et tablettes.
- **Page « À propos » Dédiée (v1.8.29)** : Présentation officielle de l'application avec logo, version dynamique, détails de la licence GNU GPLv3, liste exhaustive des licences des frameworks utilisés (.NET MAUI, CommunityToolkit, SQLite, PDF.js) et lien direct vers le projet Open Source sur GitHub.
- **Moteur de Localisation Multilingue (v1.8.29)** : Chapitre *Langue* ajouté dans les *Paramètres Application* (avant Bibliothèque) avec sélecteur intuitif. Prise en charge initiale de 4 langues (🇫🇷 Français, 🇬🇧 Anglais, 🇩🇪 Allemand, 🇪🇸 Espagnol). Architecture structurée avec fichiers de traduction JSON indépendants (`Resources/Raw/Languages/*.json`) pour une maintenance et un ajout de nouvelles langues facilités.
- **Menu Quitter Intégré & Suppression de la Croix (v1.8.28)** : Ajout de l'onglet `Quitter` (`🚪`) directement dans la barre de navigation principale à droite de Paramètres pour une fermeture propre et immédiate de l'application. Suppression de l'ancienne croix discrète sur la page Partitions.
- **Message d'État de Chargement Évolué (v1.8.28)** : Affichage explicite de *"Chargement des partitions en cours..."* lors de l'initialisation de l'application ou d'une recherche, et *"Aucune partition trouvée."* uniquement lorsqu'aucune partition n'est présente dans la base.
- **Accès Direct au Saut de Page** : Clic direct sur l'indicateur de numérotation de page (`1/5`) pour ouvrir le prompt de changement de page.

### 🎨 Édition & Système d'Annotations Dynamiques (v1.8.1 - v1.8.27)
- **Mode Dessin & Annotations au Crayon (v1.8.27)** : Nouvel outil `✎` interactif permettant de dessiner librement à main levée au doigt sur la partition. Tracé opaque (100% au premier plan pour masquer les éléments désirés). Palette de 6 couleurs (Noir, Rouge, Bleu, Vert, Jaune, Blanc) et 5 épaisseurs de trait (1mm, 2mm, 3mm, 4mm, 5mm). Support complet de la sélection, suppression au double-tap et de l'historique Undo/Redo.
- **Historique Dynamique Undo / Redo (v1.8.25 - v1.8.27)** : Nouveaux boutons `↩` (Annuler) et `↪` (Rétablir) intégrés directement dans la barre d'outils d'annotations. Permet d'annuler et rétablir instantanément n'importe quel dessin au crayon, surlignage, ajout ou suppression de sticker.
- **Surlignage Stabilo à Bords Droits / Biseautés (v1.8.25)** : Extrémités de sélection nettes et droites (`PenLineCap.Flat`) remplaçant l'effet arrondi pour un rendu de surlignage authentique et précis sur les portées et textes.
- **Pipeline Tactile Natif & Surlignage Stabilo Temps Réel (v1.8.24)** : Capture matérielle directe des événements tactiles Android. Tracé instantané et fluide du surlignage au doigt (`🖍`) sur PDF et Image sans blocage ni latence. Restauration complète du zoom/dézoom multi-touch (Pinch à 2 doigts) et du déplacement (Pan) sur les partitions PDF.
- **Surlignage Translucide Tactile / Stabilo (v1.8.23 - v1.8.25)** : Outil `🖍` avec rendu translucide naturel (sans masquer les notes et annotations sous-jacentes). Palette ergonomique de 4 couleurs fluo et 3 épaisseurs de trait (5 mm, 10 mm, 18 mm).
- **Harmonisation Typographique & Rendu Anti-Rognage des Stickers (v1.8.31)** : Échelle typographique affinée (taille de base 15px au lieu de 24px) pour des annotations musicales et textuelles nettes et parfaitement proportionnées. Marges dynamiques de sécurité évitant toute troncature/coupure de texte sur Android. Refonte visuelle du tiroir avec boutons de catégories en pilules et pastilles stickers bold parfaitement centrées.
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

### 🎵 Gestion des Partitions & Échange Wi-Fi Direct (v1.0.5 - v1.9.5)
- **Échange Wi-Fi Direct Unitaire & Groupé (v1.9.5)** : Option d'envoi disponible directement dans le menu contextuel (3 points) de chaque partition ainsi qu'en mode sélection multiple.
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

- **v1.9.5.5** : **"Direct Setlist Start & Score Context Menu Refinement"** — Démarrage instantané de la lecture des partitions au clic sur une setlist (avec gestion de setlist vide), nouveau libellé *« Démarrer la setlist »* dans les options et libellé *« Éditer la partition »* dans le menu 3 points des partitions.
- **v1.9.5.4** : **"Score Transfer & Export Options Modal (Annotations & Audio)"** — Ajout du menu modal d'options (annotations, pistes audio) lors de l'envoi Wi-Fi Direct ou de l'export d'une ou plusieurs partitions, calqué sur le comportement des setlists.
- **v1.9.5.3** : **"Strict PDF Binary Header & Structure Validation"** — Contrôle d'intégrité binaire et structurel à l'import de PDF (détection magic header, validation anti-fichiers corrompus ou frauduleux) empêchant tout crash lors de la lecture.
- **v1.9.5.2** : **"Missing File Visual Indicators & Warning Banners"** — Ajout d'une pastille d'alerte rouge avec point d'exclamation (!) à gauche des 3 points dans la bibliothèque, bannière d'avertissement dans l'édition de partition et badge d'erreur dans les setlists si un PDF est manquant.
- **v1.9.5.1** : **"Setlist Duplication, Smart Composer & Untagged Sort"** — Ajout de la duplication instantanée de setlists avec conservation de l'ordre des morceaux, tri intelligent par compositeur avec paramètre personnalisable (sans compositeur au début/fin), et nouveau tri direct des partitions sans étiquette.
- **v1.9.5.0** : **"Wi-Fi Direct P2P, QR Group Broadcast & PDF Studio Edition"** — Intégration du transfert sans fil Wi-Fi Direct ultra-rapide (streaming binaire TCP) et de la diffusion simultanée de groupe par QR Code (serveur local HTTP), nouveau popup moderne unifié pour les setlists et partitions avec mise en avant de l'édition d'informations, atelier d'assemblage PDF et migration complète sous licence GNU GPLv3.
- **v1.9.4.0** : **"Ergonomic UI & Performance Edition"** — Rétablissement du zoom 100% dans le menu central, détection double-tap unifiée même zoomé, sous-titres de partitions personnalisables en paramètres, popup moderne et ergonomique pour les options de partition, nouveaux champs d'édition (compositeur, tempo, tonalité, rating), tri par défaut automatique (récent), et moteur d'ouverture/fermeture PDF ultra-rapide (< 50ms).
- **v1.9.3.0** : **"Smart Image-to-PDF & Assembler Edition"** — Fusion multi-images automatique en PDF à l'import, atelier créateur et assembleur de pages PDF dans l'onglet Outils, uniformisation de la bibliothèque exclusivement au format PDF standardisé.
- **v1.9.0.0** : **"Full Setlist & Package Transfer Edition"** — Envoi P2P de setlists complètes avec options d'annotations/audio, export/import d'archives autonomes `.msmsetlist`, `.msmscore`, `.msmscores`.
- **v1.8.31** : **"Harmonized Typography & Sticker Rendering"** — Pastilles stickers centrées, marges anti-rognage et échelle typographique fine.
- **v1.8.30** : **"Unified Tools & 5-Tabs Layout"** — Intégration des étiquettes dans Outils et barre de navigation épurée à 5 onglets.
- **v1.8.27** : **"Pencil Drawing & Undo/Redo Engine"** — Outil dessin à main levée, surlignage stabilo biseauté et historique complet Annuler/Rétablir.

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

## 📄 Licence

Ce projet est sous licence libre **GNU General Public License v3.0 (GPLv3)**.  
Consultez le fichier [LICENSE](LICENSE) pour prendre connaissance de l'intégralité des termes et conditions.

```
Music Score Manager
Copyright (C) 2026 Audiothor

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
```

---

## 🔒 Politique de Confidentialité / Privacy Policy

L'application **Music Score Manager** respecte rigoureusement la vie privée de ses utilisateurs :
- **0 collecte de données personnelles** (pas d'identifiants, pas de tracking, pas de télémétrie).
- **Stockage 100% local** sur l'appareil.
- **Conformité Google Play Store** : Consultez le document complet dans [PRIVACY_POLICY.md](PRIVACY_POLICY.md).

---

**Développé par Audiothor** — *MusicScoreManager v1.9.5.5 "Direct Setlist Start & Score Context Menu Refinement"*

