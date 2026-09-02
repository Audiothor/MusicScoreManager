# 📖 Visualiseur Plein Écran (Mode Scène)

Le **Visualiseur** ([`ViewerPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ViewerPage.xaml)) est l'espace où la musique prend vie. Conçu pour éliminer toute distraction visuelle, il offre une lisibilité maximale et des commandes gestuelles ultra-rapides.

---

## 👆 Gestes & Navigation sur Scène

- **Tourner la page suivante** :
  - Tapez sur la moitié droite de l'écran ou glissez votre doigt de droite à gauche (configurable dans les paramètres : glissement horizontal, taper, ou glissement vertical).
- **Page précédente** :
  - Tapez sur la moitié gauche ou glissez de gauche à droite.
- **Enchaînement automatique dans une setlist** :
  - Lorsque vous atteignez la dernière page d'un morceau appartenant à une setlist, la tourne de page suivante ouvre instantanément le premier feuillet du morceau suivant.
- **Zoom tactile (Pinch-to-zoom)** :
  - Écartez deux doigts pour agrandir une portée, une mesure serrée ou une nuance subtile. Vous pouvez déplacer la page librement même en zoomant.
- **Affichage 1 ou 2 pages en mode paysage** :
  - Sur tablette en orientation horizontale, activez dans les paramètres l'affichage double-page côte à côte (comme un vrai recueil de partitions ouvert).

---

## 🎛️ Menu Central Rapide (Double-Tap)

Faites un **double-clic ou double-tape au centre de l'écran** pour afficher le menu d'actions rapides sans quitter votre partition :

1. **↻ Rotation (+90°)** :
   - Fait pivoter la partition de 90° dans le sens des aiguilles d'une montre.
   - **Option « Appliquer à cette page uniquement »** : cochez cette case si seule une page spécifique a été scannée à l'envers ou en paysage. Si décoché, la rotation s'applique à l'intégralité du document.
   - La rotation choisie est **automatiquement mémorisée** pour les prochaines ouvertures.

2. **🔍 Rétablir la taille d'origine (100%)** :
   - Réinitialise immédiatement le zoom et recadre parfaitement la page sur l'écran sans changer de feuillet.

3. **📑 Modifier l'assemblage PDF** :
   - Bascule directement la partition en cours dans l'Atelier d'Assemblage PDF pour réorganiser, ajouter ou supprimer des pages à la volée.

4. **⏱️ Afficher / Masquer le Métronome** :
   - Déploie le panneau de métronome intégré.

5. **🎧 Afficher / Masquer le Lecteur Audio** :
   - Déploie la barre de lecture audio en bas de l'écran.

---

## ✍️ Boîte à Outils d'Annotation Complète

Activez la barre d'annotation pour marquer vos partitions comme sur papier :

- **✏️ Crayon à main levée** :
  - Pour noter doigtés, coups d'archet, respirations et annotations personnelles.
  - Choix de plusieurs couleurs et réglage fin de l'épaisseur du trait.
- **🖍️ Surligneur Stabilo biseauté** :
  - Trait large semi-transparent laissant les notes et portées parfaitement lisibles.
- **🧹 Gomme de précision** :
  - Effacez vos traits au toucher.
- **⭐ Stickers & Symboles Musicaux** :
  - Insérez des symboles expressifs (points d'orgue, nuances *p*, *f*, *ff*, dièses, bémols, bécarres, flèches, textes d'attention).
  - Gestion de stickers favoris personnalisables dans les paramètres.
- **↩️ Annuler (Undo) / ↪️ Rétablir (Redo)** :
  - Historique complet de vos tracés pour annuler une fausse manipulation en un éclair.
- **🗑️ Effacer toutes les annotations de la page** :
  - Remet la page dans son état d'origine.

---

## ⏱️ Métronome Haute Précision Intégré

- **Zéro latence audio** : battements ultra-réguliers grâce au moteur audio natif optimisé.
- **Réglage du tempo** :
  - Molette ou curseur de 30 à 300 BPM.
  - Boutons fins **-1 / +1** et **-5 / +5 BPM**.
  - **Tap Tempo** : tapez le rythme en cadence avec votre doigt pour détecter automatiquement le BPM.
- **Métronome visuel** : flashs lumineux synchronisés sur le tempo pour répétition silencieuse.
- **Pré-compte** : son distinctif pour annoncer le premier temps de la mesure (2/4, 3/4, 4/4, 6/8, etc.).
- **Synchronisation automatique** : si la partition contient un tempo de référence renseigné lors de son édition, le métronome s'initialise automatiquement sur cette valeur !

---

## 🎧 Lecteur Audio Synchronisé (Accompagnement & Playback)

- Associez une piste d'accompagnement sonore (fichier MP3, WAV, M4A) à n'importe quelle partition.
- Barre de lecture compacte en bas de l'écran avec bouton Lecture/Pause.
- Barre de progression temporelle pour avancer ou reculer rapidement dans le morceau.
- Contrôle de volume dédié indépendant du volume général de la tablette.
