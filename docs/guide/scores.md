# 🎵 Menu Partitions

Le menu **Partitions** constitue la porte d'entrée de votre bibliothèque musicale. Il regroupe l'ensemble de vos partitions sous forme d'une grille de cartes modernes, réactives et optimisées pour les grands écrans comme pour les smartphones.

---

## 🖥️ Interface & Éléments de l'En-tête

En haut de l'écran des partitions se trouvent plusieurs commandes essentielles :

1. **Barre de Recherche en Temps Réel** :
   - Saisissez quelques lettres pour filtrer instantanément la liste par **titre de partition** ou par **nom de compositeur**.
   - Effacement rapide d'une touche pour retrouver la vue intégrale.

2. **Bouton Multi-sélection (☑️)** :
   - Active le mode de sélection par lots pour appliquer des actions sur plusieurs partitions à la fois.
   - Affiche une barre d'actions en bas de l'écran :
     * **🏷️ Assigner des étiquettes** : ajoutez ou retirez des tags simultanément sur toutes les partitions cochées.
     * **📡 Partager en Wi-Fi Direct** : transmettez tout le paquet sélectionné en une seule fois.
     * **📦 Exporter (.msmscores)** : génère une archive compressée unique regroupant tous les fichiers choisis.
     * **🗑️ Supprimer** : suppression groupée avec confirmation de sécurité.

3. **Bouton de Filtrage par Étiquettes (🏷️)** :
   - Ouvre le volet de sélection des tags pour n'afficher que les partitions associées à une ou plusieurs étiquettes (ex : *Jazz*, *Trompette*, *Concert d'Été*).

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
   - Lance le sélecteur de fichiers de votre appareil pour importer un ou plusieurs fichiers PDF.

---

## 📥 Processus d'Importation & Choix de Stockage

Lors de l'ajout de partitions PDF depuis votre appareil, l'application vous propose un choix stratégique pour chaque fichier ou pour l'ensemble du lot :

- **Copier vers la bibliothèque (Conseillé)** :  
  Le fichier PDF est copié dans le dossier interne dédié de Music Score Manager. Vos partitions restent accessibles en permanence même si le fichier original est déplacé ou supprimé de son dossier de téléchargement.
- **Lier le fichier original (Externe)** :  
  L'application conserve le chemin d'accès absolu vers le fichier sans le dupliquer pour économiser l'espace mémoire.

---

## 🗂️ Anatomie d'une Carte de Partition

Chaque partition est représentée par une carte visuelle comprenant :
- **Titre principal de l'œuvre**.
- **Sous-titre personnalisable** (configurable dans les paramètres : compositeur, date d'ajout ou combinaison des deux).
- **Évaluation par étoiles** (de 1 à 5 étoiles jaunes).
- **Pastilles d'étiquettes colorées** pour repérer les catégories au premier coup d'œil.
- **⚠️ Indicateur visuel d'erreur (point d'exclamation rouge)** :  
  Si le fichier PDF associé a été déplacé, renommé sur le stockage ou est introuvable, une pastille rouge d'avertissement apparaît à gauche du bouton 3 points ⋮ pour vous prévenir qu'elle ne pourra pas s'ouvrir en concert.

---

## ⋮ Menu Contextuel d'une Partition (3 petits points)

En cliquant sur le bouton **⋮** d'une carte de partition, un menu complet s'affiche :

1. **📖 Ouvrir la partition** :
   - Ouvre immédiatement la partition dans le visualiseur plein écran (mode concert).

2. **✏️ Éditer la partition** :
   - Ouvre la page d'édition détaillée ([`ScoreEditPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ScoreEditPage.xaml)) :
     * **Titre** de la partition.
     * **Compositeur / Arrangeur**.
     * **Tempo de référence (BPM)** : configurera automatiquement le métronome intégré sur ce morceau.
     * **Tonalité musicale** (ex : *Do majeur*, *Sib mineur*).
     * **Note (1 à 5 étoiles)**.
     * **Sélection et ajout d'étiquettes (tags)**.
     * **Chemin du fichier PDF associé** avec bouton pour relier un nouveau fichier en cas de besoin.
     * **Bannière d'alerte rouge** explicite si le fichier PDF est manquant.

3. **📑 Modifier l'assemblage PDF** :
   - Ouvre la partition directement dans l'**Atelier d'Assemblage PDF** pour réorganiser les pages, insérer une page blanche, supprimer des pages inutiles ou effectuer des rotations.

4. **📡 Partager en Wi-Fi Direct** :
   - Ouvre un **menu modal d'options de partage** vous demandant si vous souhaitez :
     * ☑️ *Inclure les annotations manuscrites* dessinées sur le document.
     * ☑️ *Inclure les pistes audio rattachées* (MP3/WAV).
   - Lance la recherche immédiate des appareils à proximité en Wi-Fi Direct P2P.

5. **📦 Exporter la partition (.msmscore)** :
   - Génère une archive autonome contenant la partition, ses métadonnées et (selon vos choix) ses annotations et son audio, prête à être partagée par e-mail, messagerie ou clé USB.

6. **🏷️ Renommer la partition** :
   - Raccourci rapide pour changer le titre affiché sans entrer dans l'édition complète.

7. **🗑️ Supprimer la partition** :
   - Supprime la partition de votre bibliothèque avec demande de confirmation (suppression de la référence et du fichier local associé si copié).
