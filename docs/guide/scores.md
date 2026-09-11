# 🎵 Menu Partitions

Le menu **Partitions** constitue la porte d'entrée de votre bibliothèque musicale. Il regroupe l'ensemble de vos partitions sous forme d'une grille de cartes modernes, réactives et optimisées pour les grands écrans comme pour les smartphones.

---

## 🖥️ Interface & Éléments de l'En-tête

En haut de l'écran des partitions se trouvent plusieurs commandes essentielles :

1. **Barre de Recherche en Temps Réel** :
   - Saisissez quelques lettres pour filtrer instantanément la liste par **titre de partition**, **nom de compositeur** ou nom de fichier.
   - Effacement rapide d'une touche pour retrouver la vue intégrale.

2. **Bouton Multi-sélection (☑️)** :
   - Active le mode de sélection par lots pour appliquer des actions sur plusieurs partitions à la fois.
   - Affiche une barre d'actions en bas de l'écran :
     * **🏷️ Assigner des étiquettes** : modale pour ajouter ou remplacer simultanément des tags sur toutes les partitions cochées.
     * **📡 Partager en Wi-Fi Direct** : transmettez tout le paquet sélectionné en une seule fois sans fil.
     * **📦 Exporter (.msmscores)** : génère une archive compressée unique regroupant tous les fichiers choisis (avec choix des annotations et des pistes audio).
     * **🗑️ Supprimer** : suppression groupée avec confirmation de sécurité.

3. **Bouton de Filtrage par Étiquettes (🏷️)** :
   - Ouvre le volet de sélection des tags avec recherche interne et tri des tags (A-Z, Z-A, les plus/moins utilisés).
   - Affiche les étiquettes actives sous forme de carrousel horizontal avec pastilles de couleur et boutons de retrait rapide.
   - Boutons *« Tout effacer »* et *« Appliquer »*.

4. **Bouton de Tri Rapide (🔃)** :
   - Permet de réordonner instantanément votre bibliothèque selon :
     * **Date d'ajout (Plus récent d'abord)** : tri par défaut pour repérer immédiatement vos derniers imports.
     * **Date d'ajout (Plus ancien d'abord)**.
     * **Titre (A-Z) / Titre (Z-A)**.
     * **Date de modification (Plus récent)**.
     * **Évaluation (Meilleures notes)** : vos partitions notées de 1 à 5 étoiles classées par ordre décroissant.
     * **Compositeur (A-Z)** : ordre alphabétique des compositeurs. Un paramètre dédié dans *Paramètres > Partitions* permet de choisir si les partitions sans compositeur renseigné apparaissent au tout début ou à la fin.
     * **Sans étiquette d'abord** : place en tête les partitions qui n'ont encore aucun tag assigné pour vous aider à les classer facilement.

5. **Bouton Ajouter (➕)** :
   - **Importation de PDF** : Contrôle strict de l'intégrité binaire (`%PDF-`) empêchant tout plantage.
   - **Importation et conversion d'Images (v2.0.2)** : L'application manipulant **exclusivement des fichiers PDF en interne**, la sélection d'un ou plusieurs fichiers images (PNG, JPEG, GIF, WEBP, BMP) affiche un message d'information listant les fichiers images et demandant la confirmation de conversion en PDF :
     * **Refuser** : Les images sont ignorées (non converties, non ajoutées). Les éventuels PDF sélectionnés en même temps continuent leur import normal.
     * **Accepter** : Les images sont converties en PDF (au choix de l'utilisateur : fusionnées en un seul PDF multi-pages ou générées en partitions PDF individuelles).

---

## 📥 Processus d'Importation & Choix de Stockage

Lors de l'ajout de partitions depuis votre appareil, l'application propose deux modes de gestion :

- **Copier vers la bibliothèque (Conseillé)** :  
  Le fichier PDF est copié dans le dossier interne dédié de Music Score Manager. Vos partitions restent accessibles en permanence même si le fichier original est déplacé ou supprimé de son dossier de téléchargement.
- **Lier le fichier original (Externe)** :  
  L'application conserve le chemin d'accès absolu vers le fichier sans le dupliquer pour économiser l'espace mémoire (signalé par un badge `🔗`).

---

## 🗂️ Anatomie d'une Carte de Partition

Chaque partition est représentée par une carte visuelle comprenant :
- **Titre principal de l'œuvre**.
- **Sous-titre personnalisable** (configurable dans les paramètres : compositeur, date d'ajout ou combinaison des deux).
- **Pastilles d'étiquettes colorées** pour repérer les catégories au premier coup d'œil.
- **🔗 Pastille de liaison externe** si le fichier est stocké hors du dossier interne.
- **⚠️ Pastille d'alerte rouge (!) et Titre rouge** :  
  Si le fichier PDF associé a été déplacé, renommé sur le stockage ou est introuvable, une pastille rouge d'avertissement avec point d'exclamation apparaît à gauche du bouton ⋮ pour vous prévenir qu'elle ne pourra pas s'ouvrir en concert.

---

## ⋮ Menu Contextuel d'une Partition (3 petits points)

En cliquant sur le bouton **⋮** d'une carte de partition, une carte moderne et ergonomique s'affiche :

1. **📖 Ouvrir la partition** :
   - Ouvre immédiatement la partition dans le visualiseur plein écran (mode concert).

2. **✏️ Éditer la partition** :
   - Ouvre la page d'édition détaillée ([`ScoreEditPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ScoreEditPage.xaml)) :
     * **Titre** et **Compositeur / Arrangeur**.
     * **Tonalité musicale** (solfège classique *Do, Ré, Mi...* et international *A, B, C...*).
     * **Tempo de référence (BPM)** : valeur initiale pour le métronome.
     * **Note (1 à 5 étoiles)**.
     * **Étiquettes (tags)** : sélection dynamique.
     * **Accordéon Métronome** : chiffrage de mesure (2/4, 3/4, 4/4, 6/8...), subdivision, pré-compte et coupure/activation du son par défaut.
     * **Accordéon Pistes audio** : association de fichiers audio d'accompagnement (MP3, WAV, etc.), pré-écoute et réglage du volume.
     * **Informations techniques** : chemin d'accès, taille, date, et bouton pour réassigner le fichier en cas de déplacement.

3. **📋 Ajouter dans un Setlist** :
   - Permet d'insérer instantanément la partition dans la setlist de votre choix.
   - **Placement prioritaire en 1ère position** (décalant les autres morceaux vers le bas).
   - Gestion sécurisée des doublons et des setlists verrouillées.

4. **📑 Modifier l'assemblage PDF** :
   - Ouvre la partition directement dans l'**Atelier d'Assemblage PDF** pour réorganiser les pages, insérer une page blanche, supprimer des pages inutiles ou effectuer des rotations.

5. **📡 Envoyer en Wi-Fi Direct** :
   - Ouvre une boîte modale d'options de partage vous permettant de cocher/décocher :
     * ☑️ *Inclure les annotations manuscrites* dessinées sur le document.
     * ☑️ *Inclure les pistes audio rattachées* (MP3/WAV).
   - Lance la recherche immédiate des appareils à proximité en Wi-Fi Direct P2P.

6. **📦 Exporter la partition (.msmscore)** :
   - Génère une archive autonome transportable avec modale d'options (annotations, pistes audio).

7. **🏷️ Renommer la partition** :
   - Boîte de dialogue rapide pour changer le titre affiché sans entrer dans l'édition complète.

8. **🗑️ Supprimer la partition** :
   - Supprime la partition de votre bibliothèque avec demande de confirmation sécurisée.
