# 🎬 Guide & TODO : Vidéo de Démonstration (Scrcpy, YouTube & Google Play)

Ce guide complet récapitule l'ensemble du scénario, des commandes techniques, et des étapes pas-à-pas pour enregistrer, publier et intégrer votre vidéo promotionnelle de **Music Score Manager**.

---

## 📋 Checklist Globale

- [ ] **Phase 1 : Préparation des fichiers et de l'appareil**
- [ ] **Phase 2 : Enregistrement de la démonstration avec `scrcpy`**
- [ ] **Phase 3 : Vérification / Montage rapide de la vidéo**
- [ ] **Phase 4 : Création de la chaîne et mise en ligne sur YouTube**
- [ ] **Phase 5 : Ajout du lien dans la Google Play Console**

---

## 🎯 1. Analyse & Objectifs

- **Durée cible** : **1 min 30 à 2 min 15 max** (rythme dynamique, pas de temps mort).
- **Format** : Paysage (16:9) ou Portrait selon la tablette, format vidéo standard YouTube (pas de Shorts).
- **Public cible** : Musiciens amateurs et professionnels cherchant une liseuse de partitions fluide, complète et sans latence.

---

## ⏱️ 2. Scénario Minuté (Storyboard)

| Timing | Action à l'écran | Ce qui est mis en valeur |
| :--- | :--- | :--- |
| **0:00 - 0:15** | **Bibliothèque** : Défilement fluide de quelques partitions existantes, démonstration d'un tri ou d'une recherche rapide. | Ergonomie, rapidité d'affichage, cartes modernes. |
| **0:15 - 0:40** | **Importation double (PDF + JPEG)** :<br>1. Clic sur `+` $\rightarrow$ Importation d'un fichier PDF.<br>2. Clic sur `+` $\rightarrow$ Importation d'une image/scan JPEG.<br>3. Les 2 partitions apparaissent en haut de la liste. | Polyvalence des formats et rapidité de conversion/import. |
| **0:40 - 1:00** | **Édition & Étiquetage** :<br>1. Menu `⋮` sur la partition $\rightarrow$ *Éditer les détails*.<br>2. Saisie/choix du compositeur et assignation de 2 étiquettes (ex: *Classique*, *Piano*).<br>3. Sauvegarde $\rightarrow$ Les pastilles colorées s'affichent instantanément sur la carte. | Classification intuitive par étiquettes et dossiers. |
| **1:00 - 1:40** | **Stage Viewer & Annotations** :<br>1. Clic sur la partition pour l'ouvrir en plein écran.<br>2. Surligneur (Stabilo) : Tracé fluide d'un passage (opacité claire 28%). Montrer rapidement le sélecteur d'opacité.<br>3. Crayon : Note manuscrite rapide ou doigté.<br>4. Poser un **Sticker musical** (ex: respiration `comma`, nuance `p`/`f` ou dièse).<br>5. Affichage rapide de la **Boîte à outils** (métronome visuel ou accordeur). | Zéro latence, richesse des outils d'annotation, confort de lecture. |
| **1:40 - 2:15** | **Setlist & Déroulement Scène** :<br>1. Retour bibliothèque $\rightarrow$ Onglet **Setlists**.<br>2. Ajout de la partition à un programme de concert.<br>3. Lancement direct : ouverture du premier morceau en scène.<br>4. **Appui discret en haut au centre** : Déroulement du *Live Progress Drawer* (tiroir d'avancement néon avec statut *Now Playing*). | Conçu pour la scène sans temps mort, sécurité en concert. |
| **2:15 - 2:25** | Retour à l'accueil / fondu avec le logo et le nom de l'application. | Conclusion professionnelle. |

---

## 🛠️ 3. Préparation & Enregistrement avec `scrcpy`

### A. Préparation de la tablette / téléphone
1. **Activer le mode « Ne pas déranger »** : Bloque les notifications personnelles, messages WhatsApp, popups, etc.
2. Nettoyer l'écran d'accueil et masquer la barre de navigation si nécessaire.
3. Déposer au préalable 1 PDF musical propre et 1 photo/scan JPEG bien cadré dans le dossier `Téléchargements` de l'appareil.

### B. Commande d'enregistrement recommandée
Ouvrez un terminal PowerShell et lancez :

```powershell
scrcpy --record demo_musicscoremanager.mp4 --video-codec=h264 --video-bit-rate=16M --max-fps=60 --show-touches
```

- `--show-touches` : **Indispensable** ! Affiche un point tactile sous chaque contact de doigt pour que le spectateur voie vos gestes.
- `--video-bit-rate=16M` : Débit vidéo optimal pour un rendu 1080p très net sur YouTube.
- `--record demo_musicscoremanager.mp4` : Enregistre directement le fichier vidéo MP4 dans le répertoire courant.

> **Astuce** : Pour terminer l'enregistrement, fermez la fenêtre `scrcpy` ou faites `Ctrl+C` dans le terminal.

---

## 📺 4. Mise en Ligne sur YouTube

### A. Création de la chaîne
1. Rendez-vous sur [youtube.com](https://www.youtube.com) et connectez-vous avec votre compte Google (Audiothor).
2. Cliquez sur votre icône de profil en haut à droite $\rightarrow$ **Créer une chaîne**.
3. Choisissez le nom : **Audiothor** ou **Music Score Manager**.

### B. Téléversement de la vidéo
1. Accédez à [studio.youtube.com](https://studio.youtube.com) ou cliquez sur le bouton `+` (Mettre en ligne une vidéo).
2. Sélectionnez le fichier `demo_musicscoremanager.mp4`.
3. Renseignez les métadonnées :
   - **Titre** : `Music Score Manager - Présentation & Démo Complète (Android)`
   - **Description type** :
     ```text
     Découvrez Music Score Manager, l'application complète de gestion et lecture de partitions musicales numériques pour Android.
     
     🎵 Fonctionnalités clés :
     - Import rapide de partitions PDF et images (JPG/PNG)
     - Visualiseur de scène zéro latence & mode 2 pages paysage
     - Suite complète d'annotations manuscrites (surligneur, crayon, symboles musicaux)
     - Gestion des Setlists avec tiroir d'avancement en temps réel
     - Outils intégrés : Métronome visuel/audio, accordeur, transpositeur
     
     📥 Télécharger sur Google Play : [VOTRE_LIEN_GOOGLE_PLAY]
     📖 Documentation officielle : https://audiothor.github.io/MusicScoreManager/
     💻 Projet Open Source (GPLv3) : https://github.com/Audiothor/MusicScoreManager
     ```
   - **Audience** : Cochez impérativement **« Non, elle n'est pas conçue pour les enfants »** (afin de conserver l'intégration libre et les contrôles vidéo standards).
   - **Visibilité** : **Public** (fortement conseillé pour le référencement naturel) ou **Non répertoriée** (si vous souhaitez la réserver aux utilisateurs ayant le lien).
4. **Miniature (Thumbnail)** : Ajoutez une capture nette d'une partition avec une annotation colorée et le logo.

---

## 📱 5. Ajout sur la Google Play Console

1. Connectez-vous sur la [Google Play Console](https://play.google.com/console).
2. Sélectionnez l'application **Music Score Manager**.
3. Dans le menu de gauche, rendez-vous dans :  
   **Présence sur le Play Store** $\rightarrow$ **Fiche principale du Play Store**.
4. Descendez jusqu'à la section **Graphismes**.
5. Dans le champ **Vidéo promotionnelle**, collez l'URL complète de votre vidéo YouTube (ex: `https://www.youtube.com/watch?v=XXXXXXXXXXX`).
   > ⚠️ **Important** : N'utilisez pas de lien court `youtu.be/...` ou de lien `shorts/...`, utilisez l'URL de visionnage classique.
6. Cliquez sur **Enregistrer** en bas à droite.

Votre vidéo apparaîtra immédiatement en tête de votre fiche de présentation sur le Google Play Store !
