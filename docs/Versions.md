# 📋 Historique des Versions — Music Score Manager

Ce document consigne l'historique officiel de toutes les versions de l'application **Music Score Manager**, leur date de sortie et la synthèse indexée des modifications apportées.

---

## Sommaire des Versions

- [v2.5.1 — 07/10/2026](#v251--07102026)
- [v2.5.0 — 07/10/2026](#v250--07102026)

---

## v2.5.1 — 07/10/2026

### 1. Ergonomie du Menu Central du Visualiseur (`ViewerPage`)
- **1.1. Suppression du bouton redondant « Fermer » :** Retrait du bouton en bas du menu rapide central, la fermeture s'effectuant de manière immédiate via la croix `✕` de l'en-tête supérieur.
- **1.2. Épuration de la ligne Métronome :** Retrait de l'icône roue crantée `⚙️` située entre « Afficher métronome » et l'interrupteur On/Off pour une disposition harmonieuse et parfaitement alignée avec les lignes Audio et Annotations.

### 2. Lisibilité de la Barre de Navigation Inférieure (`AppShell`)
- **2.1. Agrandissement des caractères :** Légère augmentation de la taille de police et renforcement des intitulés du menu principal (*Partitions*, *Setlists*, *Outils*, *Paramètres*, *Quitter*) pour un meilleur confort de lecture sur scène.

### 3. Correctif d'État du Surlignage Stabilo
- **3.1. Réinitialisation complète à la fermeture :** Correction du problème où la fermeture de la palette d'options du stabilo via sa croix laissait l'outil actif en arrière-plan, obligeant l'utilisateur à cliquer deux fois sur l'icône stabilo pour la rouvrir. La fermeture de la boîte désactive désormais proprement le mode et réinitialise l'état visuel du bouton.
- **3.2. Uniformisation :** Application du même comportement aux palettes du crayon et du texte.

---

## v2.5.0 — 07/10/2026
- [v2.4.9 — 07/10/2026](#v249--07102026)
- [v2.4.8 — 01/10/2026](#v248--01102026)
- [v2.4.7 — 28/09/2026](#v247--28092026)
- [v2.4.6 — 25/09/2026](#v246--25092026)
- [v2.4.5 — 23/09/2026](#v245--23092026)
- [v2.4.4 — 21/09/2026](#v244--21092026)
- [v2.4.3 — 19/09/2026](#v243--19092026)
- [v2.4.2 — 17/09/2026](#v242--17092026)
- [v2.4.1 — 16/09/2026](#v241--16092026)
- [v2.4.0 — 14/09/2026](#v240--14092026)
- [v2.3.0 — 30/09/2026](#v230--30092026)
- [v2.2.2 — 29/09/2026](#v222--29092026)
- [v2.2.1 — 29/09/2026](#v221--29092026)
- [v2.2.0 — 29/09/2026](#v220--29092026)
- [v2.1.13 — 29/09/2026](#v2113--29092026)
- [v2.1.12 — 25/09/2026](#v2112--25092026)
- [v2.1.11 — 22/09/2026](#v2111--22092026)
- [v2.1.10 — 22/09/2026](#v2110--22092026)
- [v2.1.9 — 22/09/2026](#v219--22092026)
- [v2.1.8 — 20/09/2026](#v218--20092026)
- [v2.1.7 — 20/09/2026](#v217--20092026)
- [v2.1.6 — 19/09/2026](#v216--19092026)
- [v2.1.5 — 18/09/2026](#v215--18092026)
- [v2.1.4 — 18/09/2026](#v214--18092026)
- [v2.1.3 — 18/09/2026](#v213--18092026)
- [v2.1.2 — 17/09/2026](#v212--17092026)
- [v2.1.1 — 17/09/2026](#v211--17092026)
- [v2.1.0 — 17/09/2026](#v210--17092026)
- [v2.0.4 — 13/09/2026](#v204--13092026)
- [v2.0.3 — 13/09/2026](#v203--13092026)
- [v2.0.2 — 12/09/2026](#v202--12092026)
- [v2.0.1 — 11/09/2026](#v201--11092026)
- [v2.0.0 — 10/09/2026](#v200--10092026)
- [v1.9.8.0 — 08/09/2026](#v1980--08092026)

---

## v2.5.0 — 07/10/2026

### 1. Refonte Complète de la Gestion des Profils de Pédales Bluetooth & Événements MIDI
- **1.1. Suppression des anciennes sections :** Suppression totale de l'ancien panneau de diagnostic/testeur et de l'accordéon des raccourcis dans les paramètres principaux afin de clarifier l'interface.
- **1.2. Maintien intégral de la sécurité scénique :** Conservation du chapitre « Sécurité scène & sensibilité » avec protection anti-double saut de page paramétrable (300 à 800 ms) et réglage précis du seuil d'appui long.
- **1.3. Gestion universelle et ouverte des profils :**
  - Maintien des préréglages constructeurs (PageFlip Dragonfly, Firefly, Butterfly, AirTurn Duo/Quad 500, Donner, Joyo, Harley Benton, iRig BlueTurn, Coda STOMP, etc.).
  - **Modification sans restriction :** L'utilisateur peut désormais modifier directement n'importe quel profil de base constructeur sans devoir obligatoirement créer un clone.
  - **Champ Description & Commentaires :** Ajout pour chaque profil d'un espace pour consigner les astuces de réglage matériel (ex: commutateur REPEAT sur OFF, sélection du Mode 3 pour PageFlip Dragonfly).
  - Création de profil en 1 clic demandant le nom et basculant instantanément vers l'écran d'édition.
  - Suppression de profil personnalisé et possibilité de restaurer l'ensemble des profils d'usine par défaut.

### 2. Édition Granulaire des Pédales et Boutons (`PedalProfileEditPage`)
- **2.1. Trois opérations intuitives :**
  - **Ajouter une pédale / bouton :** Demande du libellé personnalisé (ex: « Pédale gauche », « Bouton 1 »), puis transition directe vers la page de configuration.
  - **Éditer une pédale :** Ajustement des paramètres du bouton sélectionné via une page dédiée.
  - **Supprimer une pédale :** Suppression directe du bouton du profil sélectionné.
- **2.2. Vue récapitulative élégante :** Cartes stylisées affichant le rôle, le badge de signal capté, le type d'appui (Simple vs Long) et l'action assignée.

### 3. Page de Configuration d'un Bouton (`PedalButtonConfigPage`)
- **3.1. Nom / rôle :** Édition du libellé personnalisé.
- **3.2. Détection automatique du signal en direct :** Écoute active Bluetooth HID et MIDI affichant en direct le code brut (Hex/Décimal) et le nom de touche, avec flash visuel vert de confirmation.
- **3.3. Type d'appui :** Choix clair entre « ⚡ Appui simple » et « ⏱️ Appui long ».
- **3.4. 13 Actions Scéniques Strictes :**
  1. *Page précédente*
  2. *Page suivante*
  3. *Aller au début du morceau*
  4. *Aller à la fin du morceau*
  5. *Aller au marqueur précédent*
  6. *Aller au marqueur suivant*
  7. *Aller au début du morceau précédent du setlist*
  8. *Aller au début du morceau suivant du setlist*
  9. *Ouvrir le menu de liste de morceaux de la setlist (top drawer)*
  10. *Activer le mode nuit (implémentation future)*
  11. *Zoom +*
  12. *Zoom -*
  13. *Afficher / retirer les annotations*
- **3.5. Prise en charge dans le visualiseur :** Câblage complet des actions dans `ViewerPage` (zoom par palier, masquage des traits d'annotations, contrôle du tiroir setlist).

---

## v2.4.9 — 07/10/2026

### 1. Correctif d'affichage des étiquettes (Tri « Sans étiquette d'abord »)
- **1.1.** Correction du problème où, lors de la sélection du tri « Sans étiquette d'abord » dans le menu Partitions, aucune étiquette n'apparaissait sur l'ensemble de la liste de partitions.
- **1.2.** Remplacement de la stratégie de mesure fixe `ItemSizingStrategy="MeasureFirstItem"` par `ItemSizingStrategy="MeasureAllItems"` dans la `CollectionView` des partitions, garantissant une hauteur adaptée à chaque cellule.
- **1.3.** Ajout de la propriété réactive `HasTags` et notification de changement de propriété (`INotifyPropertyChanged`) sur le modèle `Score` pour lier dynamiquement la visibilité des badges d'étiquettes.

### 2. Système de Marqueurs de Page (Signets / Bookmarks)
- **2.1.** **Activation par page :** Ajout dans le menu central d'une bascule On/Off (désactivée par défaut) permettant de positionner un marqueur unique sur la page actuellement affichée.
- **2.2.** **Affichage discret et moderne :** Symbole graphique élégant et discret de signet `🔖` positionné en haut à gauche de la page affichée, avec lettrage alphabétique majuscule automatique par défaut (`A`, `B`, `C`...).
- **2.3.** **Boîte de dialogue interactive :** Clic sur le marqueur ouvrant une modale moderne permettant :
  - De renommer le marqueur (1 à 3 caractères maximum, unique au sein de la partition).
  - De supprimer le marqueur de la page.
  - De naviguer au marqueur précédent ou suivant.
  - D'accéder à la liste complète des marqueurs de la partition pour s'y rendre immédiatement par simple clic sur un badge.
- **2.4.** **Support Pédale Bluetooth & Commandes MIDI :** Intégration de deux nouvelles actions configurables `NextBookmark` (Marqueur suivant) et `PreviousBookmark` (Marqueur précédent) dans le service de pédales, permettant de sauter de signet en signet sans lâcher son instrument.
- **2.5.** **Persistance SQLite :** Création de la table `ScoreBookmark` avec cascade de suppression automatique lors de la suppression d'une partition.

### 3. Refonte Ergonomique du Menu Central du Visualiseur
- **3.1.** **Accès prioritaire immédiat :** Création d'un bandeau d'en-tête supérieur fixe regroupant le bouton rouge « 🏠 Accueil » et le bouton « ✕ Fermer », directement visibles et cliquables sans aucun défilement.
- **3.2.** **Organisation en cartes thématiques :**
  - *Carte Marqueur :* Bascule On/Off du marqueur de la page courante et boutons Précédent / Suivant.
  - *Carte Affichage & Navigation :* Boutons rapides de rotation (+90°), réinitialisation du zoom (100%) et saut direct de page, accompagnés des bascules de portée de rotation.
  - *Carte Outils & Modules :* Toggles pour le métronome (avec son bouton d'accès direct aux réglages ⚙️), le lecteur audio et l'affichage des annotations.
  - *Carte Gestion :* Boutons de modification de la partition et d'assemblage PDF.
- **3.3.** **Adaptabilité dynamique :** Calcul automatique de la hauteur naturelle du menu pour supprimer tout ascenseur vertical inutile sur les écrans spacieux, tout en conservant un défilement fluide sur petits écrans en mode paysage.

---

## v2.4.8 — 01/10/2026

### 1. Dialogue moderne de réglage du métronome
- **1.1.** Nouvelle boîte de dialogue moderne et épurée pour les paramètres complets du métronome.
- **1.2.** Contrôle du volume sonore du métronome par partition avec slider et mémorisation.
- **1.3.** Suppression des permissions inutiles (caméra et localisation) sur Android.

---

## v2.4.7 — 28/09/2026

### 1. Design immersif des boîtes de dialogue
- **1.1.** Refonte visuelle et animations fluides des dialogues d'importation et messages système.

---

## v2.4.6 — 25/09/2026

### 1. Adaptation responsive mobile
- **1.1.** Adaptation responsive des barres d'outils d'annotation, crayons, stabilo et stickers pour smartphones en orientation portrait et paysage.

---

## v2.4.5 — 23/09/2026

### 1. Préférences d'importation
- **1.1.** Mémorisation et choix du mode d'importation par défaut (Copie locale vs Lien externe).

---

## v2.4.4 — 21/09/2026

### 1. Importation Cloud
- **1.1.** Robustesse et compatibilité totale avec les fichiers provenant de Google Drive et fournisseurs Cloud.

---

## v2.4.3 — 19/09/2026

### 1. Optimisation du menu central
- **1.1.** Ajustement de la compacité sans étirement vertical sur tablette et grand écran.

---

## v2.4.2 — 17/09/2026

### 1. Correctifs et ergonomie
- **1.1.** Améliorations de l'affichage des stickers et documentation officielle.

---

## v2.4.1 — 16/09/2026

### 1. Barres d'options d'annotations
- **1.1.** Compacité accrue des sélecteurs de couleur et réglages de taille pour crayons et stabilo.

---

## v2.4.0 — 14/09/2026

### 1. Refonte mobile et annotations
- **1.1.** Support complet des annotations crayon, stabilo et stickers.
- **1.2.** Double-tap pour actions rapides et bascule des numéros de page.

---

## v2.3.0 — 30/09/2026

### 1. Ordonnancement personnalisé des stickers favoris
- **1.1.** **Réorganisation libre des stickers favoris :** Ajout de la réorganisation personnalisée des stickers favoris dans la page des paramètres d'annotations (`SettingsAnnotationsPage`).
- **1.2.** **Contrôles dédiés :** Boutons Monter / Descendre et poignées de réordonnancement pour agencer ses symboles musicaux préférés exactement selon ses habitudes de jeu.
- **1.3.** **Persistance du tri :** Enregistrement de l'ordre d'affichage dans la base de données locale (`FavoriteSticker.OrderIndex`) appliqué instantanément dans la palette du visualiseur.
- **1.4.** **Ressources Google Play Store :** Intégration des visuels promotionnels officiels (icône haute résolution, image vedette 1024x500 et captures d'écran des 8 fonctionnalités clés).

---

## v2.2.2 — 29/09/2026

### 1. Vélocité de l'index alphabétique et remontée haut de page
- **1.1.** **Navigation instantanée A-Z :** Optimisation du calcul de défilement de l'index alphabétique latéral pour un alignement au pixel près sur la première partition correspondante.
- **1.2.** **Bouton flottant retour en haut (FAB) :** Défilement fluide et immédiat vers le début de la bibliothèque sans latence ni à-coups graphiques.

---

## v2.2.1 — 29/09/2026

### 1. Correctifs de réactivité de l'index alphabétique
- **1.1.** **Suppression des micro-sauts :** Correction du repositionnement instable lors d'un tap rapide sur les lettres extrêmes (A, W, Z).
- **1.2.** **Amélioration du ciblage :** Prise en compte immédiate des partitions accentuées et des caractères spéciaux dans le calcul d'indexation.

---

## v2.2.0 — 29/09/2026

### 1. Fluidité absolue sur bibliothèques volumineuses (300+ partitions)
- **1.1.** **Virtualisation haute performance :** Rendu ultrarapide de la liste des partitions sans ralentissement de l'interface même avec des centaines de partitions enregistrées.
- **1.2.** **Bandeau d'index alphabétique latéral A-Z :** Barre de navigation alphabétique tactile sur le côté droit de l'écran pour atteindre directement la section souhaitée par simple toucher ou glissement.
- **1.3.** **Bouton flottant de retour en haut :** Apparition dynamique d'un bouton flottant élégant dès que la liste est défilée pour remonter instantanément au sommet.
- **1.4.** **Configuration dans les réglages :** Option dédiée dans les paramètres des partitions pour activer ou masquer l'index alphabétique selon les préférences de l'utilisateur.

---

## v2.1.13 — 29/09/2026

### 1. Précision du surlignage et élimination des traces tactiles
- **1.1.** **Tracé du stabilo :** Correction du rendu des traits de surlignage pour préserver la netteté et la transparence optimale au-dessus des portées musicales.
- **1.2.** **Suppression des artefacts tactiles :** Élimination définitive des micro-points résiduels causés par les contacts d'appui court lors de l'activation des outils de dessin.

---

## v2.1.12 — 25/09/2026

### 1. Gestion avancée et tri configurable des Setlists
- **1.1.** **Tri par défaut personnalisable :** Ajout d'une option de tri par défaut des setlists dans les paramètres (par nom alphabétique, par date d'événement ou par date de création).
- **1.2.** **Relégation automatique des setlists terminées :** Option permettant de basculer automatiquement les concerts et répétitions passés en fin de liste pour conserver les programmes à venir au premier plan.
- **1.3.** **Distingo visuel clair :** Mise en valeur des setlists actives et atténuation des programmes archivés.

---

## v2.1.11 — 22/09/2026

### 1. Harmonisation multilingue et stabilité générale
- **1.1.** **Correction des libellés multilingues :** Traduction intégrale des dialogues de sauvegarde, d'échange Wi-Fi Direct, de gestion des sauvegardes et des modules de pédales dans les 8 langues prises en charge.
- **1.2.** **Stabilité du moteur de rendu :** Robustesse renforcée lors du basculement rapide entre différentes partitions.

---

## v2.1.10 — 22/09/2026

### 1. Optimisation mémoire et performances
- **1.1.** **Gestion du cache RAM :** Libération optimisée des images et des flux mémoires lors de longues répétitions ou concerts.
- **1.2.** **Stabilité applicative :** Traitement des exceptions potentielles lors des transitions d'écrans.

---

## v2.1.9 — 22/09/2026

### 1. Internationalisation multilingue intégrale (i18n)
- **1.1.** **8 langues supportées :** Déploiement complet des traductions natives en Français 🇫🇷, Anglais 🇬🇧, Allemand 🇩🇪, Espagnol 🇪🇸, Italien 🇮🇹, Polonais 🇵🇱, Néerlandais 🇳🇱 et Portugais 🇵🇹.
- **1.2.** **Couverture exhaustive :** Traduction intégrale des menus, réglages d'application, catégories de stickers musicaux, dialogues d'importation et page À propos.
- **1.3.** **Application instantanée :** Changement de langue à chaud dans les paramètres avec rafraîchissement immédiat de l'interface utilisateur.

---

## v2.1.8 — 20/09/2026

### 1. Maintien de l'écran allumé pendant le jeu
- **1.1.** **Option « Garder l'écran allumé » (Keep Screen Awake) :** Empêche la mise en veille automatique de la tablette ou de l'écran pendant la consultation d'une partition.
- **1.2.** **Renommage du menu Organisation :** Clarification des intitulés des menus de paramètres pour une navigation plus intuitive.

---

## v2.1.7 — 20/09/2026

### 1. Centralisation de la gestion des étiquettes
- **1.1.** **Module Étiquettes dans Outils :** Regroupement de la création, du renommage, de la colorimétrie et de la suppression des étiquettes dans le menu Outils.
- **1.2.** **Raccourci direct depuis les réglages de partition :** Accès immédiat à la gestion globale des étiquettes sans quitter le contexte de personnalisation de la partition.

---

## v2.1.6 — 19/09/2026

### 1. Calque dynamique d'annotations On/Off
- **1.1.** **Bascule instantanée d'affichage :** Ajout d'une option d'affichage/masquage rapide des annotations dans le menu central du visualiseur pour afficher la partition vierge ou annotée en un clic.
- **1.2.** **Contrôle dans la fiche partition :** Possibilité de choisir si les annotations sont visibles par défaut pour chaque partition individuellement.

---

## v2.1.5 — 18/09/2026

### 1. Pré-roll audio et décompte synchronisé
- **1.1.** **Décompte rythmique de haute précision :** Métronome visuel et sonore synchronisé (1 à 4 mesures paramétrables) avant le départ de la bande audio d'accompagnement.
- **1.2.** **Répétition systématique à la reprise :** Réactivation automatique du pré-roll lors de la reprise après une pause pour un calage musical parfait en répétition.

---

## v2.1.4 — 18/09/2026

### 1. Lecteur audio flottant et repositionnable
- **1.1.** **Mini-lecteur audio flottant :** Lecteur de bande-son d'accompagnement draggable n'obstruant pas la lecture de la partition.
- **1.2.** **Démarrage automatique configurable :** Option permettant de charger et démarrer automatiquement la piste audio dès l'ouverture de la partition.
- **1.3.** **Contrôle réactif dans le menu :** Bascule directe de l'état audio depuis le panneau central.

---

## v2.1.3 — 18/09/2026

### 1. Navigation sécurisée vers l'atelier PDF
- **1.1.** **Élimination des conditions de course :** Routage Shell MAUI fiabilisé lors de l'ouverture de l'assembleur et découpeur PDF depuis le visualiseur et les fiches partitions.

---

## v2.1.2 — 17/09/2026

### 1. Protection anti-tourne rapide et réglages pédales ergonomiques
- **1.1.** **Protection anti-double tourne (Cooldown) :** Temporisation paramétrable (anti-rebond de 200 ms à 1000 ms) empêchant les sauts involontaires de multiples pages lors d'un appui nerveux sur la pédale.
- **1.2.** **Accordéon repliable pour raccourcis :** Interface épurée dans les réglages de pédale avec sections repliables par fonction.

---

## v2.1.1 — 17/09/2026

### 1. Sous-titres dynamiques personnalisables
- **1.1.** **Sous-titres réorganisables par glisser-déposer :** Choix libre des éléments affichés sous le titre de la partition (compositeur, tonalité, tempo, étiquettes, nombre total de pages).
- **1.2.** **Optimisation du mode paysage :** Présentation ajustée des métadonnées pour éviter tout chevauchement en affichage horizontal.

---

## v2.1.0 — 17/09/2026

### 1. Moteur de rendu PDF natif ultra-rapide
- **1.1.** **Moteur natif matériel :** Intégration directe d'Android `PdfRenderer` et de Windows `Windows.Data.Pdf` pour un temps de chargement des pages divisé par trois.
- **1.2.** **Mode double page en paysage :** Affichage simultané de deux pages côte à côte en orientation horizontale avec pagination synchronisée.
- **1.3.** **Mise en cache RAM dynamique :** Préchargement intelligent des pages adjacentes pour une tourne instantanée à 0 ms de délai perçu.

---

## v2.0.4 — 13/09/2026

### 1. Importation d'images sécurisée et améliorations setlists
- **1.1.** **Modale bloquante de conversion d'images en PDF :** Dialogue clair guidant l'utilisateur pour convertir les photos et captures de partitions en PDF avant intégration.
- **1.2.** **Filtrage multi-étiquettes :** Recherche croisée par étiquettes lors de l'ajout de partitions à une setlist.
- **1.3.** **Autorisation des doublons dans une setlist :** Possibilité d'intégrer plusieurs fois le même morceau dans un programme (rappels, interludes).

---

## v2.0.3 — 13/09/2026

### 1. Fiabilisation de la conversion Image en PDF
- **1.1.** **Gestion sécurisée des chemins temporaires :** Résolution des erreurs d'accès lors de la conversion d'images issues d'applications tierces ou de galeries photos.
- **1.2.** **Messages conviviaux :** Reformulation des alertes d'importation avec explications claires et rassurantes.

---

## v2.0.2 — 12/09/2026

### 1. Détection et proposition de conversion automatique d'images
- **1.1.** Détection proactive des formats graphiques (PNG, JPEG, WebP) à l'importation avec proposition immédiate de conversion en document PDF vectoriel standardisé.

---

## v2.0.1 — 11/09/2026

### 1. Améliorations du tiroir de setlist en direct
- **1.1.** **Colorimétrie d'avancement :** Distinction visuelle nette entre les morceaux déjà joués, le morceau actuellement en cours et les morceaux restants à jouer.
- **1.2.** **Recentrage automatique :** Défilement automatique du bandeau pour maintenir la partition en cours au centre de l'écran.
- **1.3.** **Bascule de l'overlay :** Option pour afficher ou masquer le bandeau de progression selon les besoins scéniques.

---

## v2.0.0 — 10/09/2026

### 1. Refonte majeure de l'expérience scénique (Setlist Live Mode)
- **1.1.** **Bandeau déroulant de setlist en direct (Live Progress Drawer) :** Tiroir supérieur rétractable dans le visualiseur permettant de voir l'ensemble du set musical sans interrompre la lecture.
- **1.2.** **Navigation tactile directe :** Saut instantané d'un morceau à l'autre d'un simple toucher sur le bandeau pendant le concert ou la répétition.

---

## v1.9.8.0 — 08/09/2026

### 1. Support complet des Pédales Bluetooth et Contrôleurs MIDI
- **1.1.** **Moniteur d'événements en direct (Live Monitor) :** Affichage en temps réel des frappes de pédalier (touches clavier Bluetooth) et des messages MIDI (Control Change, Program Change, Note On) pour faciliter la configuration.
- **1.2.** **Profils matériels préconfigurés :** Profils prêts à l'emploi pour AirTurn (Duo 500, QUAD 500, PEDpro), PageFlip (Firefly, Butterfly, Dragonfly), Coda STOMP, Donner, Joyo, Harley Benton, iRig BlueTurn et claviers standards.
- **1.3.** **Gestion des profils personnalisés :** Création, duplication, renommage et suppression de profils sur-mesure adaptés à tout matériel.
- **1.4.** **Double action Appui Court / Appui Long :** Configuration indépendante de deux actions distinctes par pédale, avec réglette de sensibilité réglable (200 ms à 1000 ms).
- **1.5.** **Palette complète d'actions musicales :** Tourne de page suivante/précédente, morceau suivant/précédent de setlist, métronome On/Off, lecture/pause audio, zoom 100%, saut direct de page, menu central et verrouillage des annotations.

