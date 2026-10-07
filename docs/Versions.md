# 📋 Historique des Versions — Music Score Manager

Ce document consigne l'historique officiel de toutes les versions de l'application **Music Score Manager**, leur date de sortie et la synthèse indexée des modifications apportées.

---

## Sommaire des Versions

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
