# 📖 Visualiseur Plein Écran (Mode Scène)

Le **Visualiseur** ([`ViewerPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ViewerPage.xaml)) est l'espace où la musique prend vie. Conçu pour éliminer toute distraction visuelle, il offre une lisibilité maximale, un rendu synchrone ultra-fluide et des commandes gestuelles ultra-rapides.

---

## 👆 Gestes & Navigation sur Scène

- **Tourner la page suivante** :
  - Tapez sur la moitié droite de l'écran ou glissez votre doigt de droite à gauche (configurable dans les paramètres : glissement horizontal, taper, ou glissement vertical).
- **Page précédente** :
  - Tapez sur la moitié gauche ou glissez de gauche à droite.
- **Enchaînement automatique dans une setlist** :
  - Lorsque vous atteignez la dernière page d'un morceau appartenant à une setlist, la tourne de page suivante ouvre instantanément le premier feuillet du morceau suivant.
- **Zoom tactile (Pinch-to-zoom) & Pan** :
  - Écartez deux doigts pour agrandir une portée ou une mesure. Vous pouvez déplacer la page librement même en zoomant.
- **Affichage 2 pages en mode paysage** :
  - Sur tablette en orientation horizontale, activez dans les paramètres l'affichage double-page côte à côte (comme un vrai recueil de partitions ouvert).
  - Les annotations sont synchronisées géométriquement avec une fidélité absolue sur la page de gauche et la page de droite en tenant compte des marges réelles (*letterbox/pillarbox*).
- **Indicateur de Page Cliquable** :
  - Badge de numérotation en bas à droite (`ex: 2/8`). Un clic ouvre la boîte de saut direct vers la page de votre choix.

---

## 🦶 Contrôle Mains-Libres : Pédaliers Bluetooth & Événements MIDI

Music Score Manager intègre un moteur de commande matérielle universel :
- **Pédaliers Bluetooth HID & Claviers** : Compatibilité totale avec AirTurn, PageFlip Dragonfly/Firefly, Joyo, Donner, Thomann/Harley Benton, IK Multimedia iRig BlueTurn, Coda STOMP, touches fléchées, etc.
- **Contrôleurs MIDI (USB-OTG & Bluetooth MIDI)** : Pédales sustain (CC 64), sostenuto (CC 66), soft pedal (CC 67), notes MIDI et Program Changes.
- **Actions Déclenchables au Pied** (au choix sur appui court ou appui long avec réglette de sensibilité réglable de 200 à 1000 ms) :
  - Page suivante / précédente
  - Morceau suivant / précédent dans la setlist
  - Début / fin du morceau
  - Défilement haut / bas
  - Démarrer / couper le métronome et son
  - Play / Pause de la bande audio d'accompagnement
  - Rétablir le zoom à 100%
  - Ouvrir le saut direct de page
  - Verrouiller / déverrouiller les annotations
  - Annuler (Undo) / Rétablir (Redo)
  - Ouvrir le menu central / Quitter le lecteur

---

## 🎛️ Menu Central Rapide (Double-Tap)

Faites un **double-clic ou double-tap au centre de l'écran** pour afficher le menu d'actions rapides sans quitter votre partition :

1. **↻ Rotation (+90°)** :
   - Fait pivoter la partition de 90° dans le sens des aiguilles d'une montre.
   - **Option « Appliquer à cette page uniquement »** : permet de pivoter uniquement une page spécifique scannée en paysage.
   - **Interrupteur « Mémoriser les rotations »** : enregistre l'orientation dans la base de données locale.
2. **🔍 Rétablir la taille d'origine (100%)** :
   - Réinitialise immédiatement le zoom et recadre parfaitement la page sur l'écran.
3. **📄 Page** : Saut numérique de page.
4. **⏱️ Afficher / Masquer le Métronome** : Active ou masque l'overlay métronome.
5. **🎧 Afficher / Masquer le Lecteur Audio** : Active ou masque la barre audio.
6. **📑 Modifier l'assemblage PDF** : Bascule directement dans l'Atelier d'Assemblage PDF.
7. **✏️ Modifier la partition** : Accès direct à l'édition des métadonnées musicales.
8. **Retour à l'accueil / Fermer** : Sortie propre du visualiseur.

---

## ✍️ Boîte à Outils d'Annotation Complète

La barre d'annotation peut être translatée verticalement sur l'écran par glissement (*pan*), avec boîtes d'options secondaires collées au-dessus :

- **🖌️ Surligneur Fluo (Stabilo)** :
  - Rendu translucide naturel à bords biseautés droits (`PenLineCap.Flat`).
  - 4 teintes fluo (jaune, vert, bleu, rose) et 3 largeurs (5 mm, 10 mm, 18 mm).
- **T Texte Typographié** :
  - Saisie textuelle directe sur la partition, 6 couleurs et 5 tailles de police.
- **✏️ Crayon à Main Levée** :
  - Tracé opaque à 100%, 6 couleurs et 5 épaisseurs de trait calibrées de 1 mm à 5 mm.
- **❏ Stickers Musicaux & Favoris** :
  - Plus de 100 stickers organisés par catégories : Favoris, Doigtés, Nuances, Articulations, Répétitions, Notes (𝅝, 𝅗𝅥, ♩, ♪, ♫...), Silences (𝄻, 𝄼, 𝄽, 𝄾...), Altérations (♯, ♭, ♮, 𝄪, 𝄫) et Symboles.
  - Réglette grand format ergonomique pour ajuster la taille du sticker au doigt.
- **↩️ Annuler (Undo) / ↪️ Rétablir (Redo)** :
  - Historique dynamique complet pour annuler ou rétablir chaque modification.
- **🔒/🔓 Cadenas de Verrouillage de Sécurité** :
  - Verrouillage automatique systématique à l'ouverture pour sanctuariser la partition contre toute fausse manipulation.
  - Déverrouillage automatique au choix d'un outil d'édition.
  - Re-verrouillage automatique à la fermeture de la barre (bouton ✕).
- **✧ Nettoyage Général** : Réinitialise toutes les annotations de la page.
- **🗑️ Suppression Ciblée** : Sélection tactile d'une annotation existante pour la supprimer au doigt ou par double-tap.

---

## ⏱️ Métronome Haute Précision Intégré

- **Moteur Thread-Safe Régulé** : Zéro dérive CPU grâce à la double boucle `Stopwatch` + `SpinWait`.
- **Zéro Latence Audio** : Échantillons déclenchés via `SoundPool` natif Android.
- **Affichage Visuel & LED** : Pulsation lumineuse synchronisée sur les temps forts et temps faibles.
- **Coupure du Clic Sonore** : Possibilité d'activer ou de couper le son à la volée tout en conservant le repère visuel.
- **Pré-compte Synchronisé** : Décompte de mesures avant démarrage de la lecture audio.

---

## 🎧 Lecteur Audio Synchronisé (Accompagnement & Playback)

- Prise en charge des fichiers MP3, WAV, AAC, FLAC rattachés.
- Barre de transport compacte avec bouton Lecture/Pause.
- Slider de progression avec affichage du temps écoulé et temps total.
