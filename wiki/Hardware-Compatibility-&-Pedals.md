# 🦶 Compatibilité Matériel, Pédaliers Bluetooth & Contrôleurs MIDI

Sur scène, le contrôle mains-libres est un élément vital de l'expérience musicale. Cette page répertorie tous les périphériques testés, leurs spécificités de commutation, et les réglages matériels recommandés pour une fiabilité totale en direct.

---

## 1. Matrice des Pédaliers Bluetooth Testés & Recommandés

| Modèle de Pédalier | Type de Switch | Profil Intégré MSM | Mode Bluetooth Conseillé | Remarque Scénique |
| :--- | :--- | :--- | :--- | :--- |
| **PageFlip Dragonfly** (4 pédales) | Mécanique quasi-silencieux | `PageFlip Dragonfly` | Mode 1 (Flèches gauche/droite) | **Recommandé** : 4 boutons permettant d'assigner Page Précédente/Suivante + Morceau Précédent/Suivant de setlist. |
| **PageFlip Firefly & Butterfly** | Mécanique doux | `PageFlip Firefly & Butterfly` | Mode 1 (Flèches) ou Mode 2 (PageUp/Down) | Très robuste, autonomie sur piles AA de plusieurs mois. |
| **AirTurn Duo 500 & BT500S-2** | Bouton silencieux à membrane | `AirTurn Duo 500 & PEDpro` | Mode 2 (Clavier HID standard) | Switches 100% silencieux idéaux pour la musique de chambre et le récital acoustique. |
| **AirTurn Quad 500** (4 pédales) | Bouton silencieux à membrane | `AirTurn Quad 500` | Mode 2 (Touches fléchées + 1/2) | Contrôle complet : 2 pédales de tourne + 2 pédales pour métronome/audio. |
| **AirTurn PEDpro** | Capteur capacitif plat | `AirTurn Duo 500 & PEDpro` | Mode 2 | Ultra-plat, transport facile en housse d'instrument. |
| **Joyo JSP-01 Wireless** | Switch mécanique avec clic doux | `Joyo JSP-01` | Mode 1 (Flèches) | Excellent rapport qualité/prix, batterie rechargeable USB-C. |
| **Thomann / Harley Benton PageTurn** | Switch mécanique | `Thomann / Harley Benton` | Mode 1 (Flèches) | Pédalier en métal robuste pour concerts amplifiés. |
| **Donner Wireless Page Turner** | Switch mécanique | `Donner Wireless` | Mode 1 ou Mode 3 | Très répandu, affichage LED de batterie. |
| **IK Multimedia iRig BlueTurn** | Boutons rétroéclairés souples | `iRig BlueTurn` | Mode Page Up/Down | Boutons rétroéclairés très pratiques sur scènes très sombres. |
| **Coda Music STOMP** | Switch métallique type guitare | `Coda STOMP` | Mode 1 | Boîtier en aluminium indestructible pour pedalboard de guitariste/bassiste. |

---

## 2. Pédales Silencieuses vs Mécaniques : Le Choix Selon Votre Discipline

- **Musique Classique, Chœur, Acoustique & Théâtre** :
  - Privilégiez impérativement des switches **silencieux** (ex: *AirTurn Duo 500*, *AirTurn PEDpro* ou *PageFlip Dragonfly*).
  - Un clic mécanique sur scène de musique de chambre ou en église s'entend distinctement par le public et les micros d'enregistrement.
- **Musiques Actuelles, Jazz, Rock, Église Amplifiée** :
  - Les switches **mécaniques** avec retour tactile franc (ex: *Joyo*, *Thomann*, *Donner*, *Coda STOMP*) sont appréciés car ils permettent de sentir le déclenchement sous la chaussure même dans un environnement bruyant.

---

## 3. Contrôleurs MIDI (USB-OTG & Bluetooth MIDI)

Music Score Manager intègre un parseur d'événements MIDI natif permettant de convertir n'importe quel signal en action scénique :

### A. Messages MIDI Pris en Charge
1. **Control Change (CC)** :
   - `CC 64` (Sustain Pedal) : valeur > 63 = pressé, valeur 0 = relâché.
   - `CC 66` (Sostenuto).
   - `CC 67` (Soft Pedal / Una Corda).
   - Tout CC personnalisé de 0 à 127.
2. **Notes MIDI (Note On / Note Off)** :
   - Clavier maître ou pédalier basse MIDI : assignation possible des notes (ex: C1 à F1) pour déclencher la page suivante ou démarrer le métronome.
3. **Program Change (PC)** :
   - Pratique pour synchroniser le changement de morceau d'une setlist avec le changement de patch d'un synthétiseur ou multi-effet de guitare.

### B. Connexions Recommandées
- **Connexion Filaire USB-OTG** : Branchez un adaptateur USB-C vers USB-A sur votre tablette Android, puis connectez votre pédalier MIDI (Boss, Behringer FCB1010, Morningstar, Line 6 Helix). Zéro latence et aucune batterie à surveiller.
- **Connexion Bluetooth MIDI (BLE)** : Utilisable via des adaptateurs type *CME Widi Master* ou contrôleurs BLE natifs.

---

## 4. Astuce Majeure Android : Masquer le Clavier Virtuel

Lorsqu'un pédalier Bluetooth fonctionne en **mode Clavier HID**, Android peut considérer qu'un clavier physique est branché et masquer le clavier virtuel à l'écran, ou au contraire afficher une barre de saisie inutile :
1. Dans Android, allez dans *Paramètres > Système > Langues et saisie > Clavier physique*.
2. Activez l'option **« Afficher le clavier virtuel à l'écran »** afin de pouvoir continuer à taper du texte dans la barre de recherche ou les annotations sans avoir à éteindre votre pédalier.

---

## 5. Recommandations de Tablettes pour le Pupitre

| Modèle / Gamme | Taille & Ratio | Confort de Lecture | Autonomie Scène |
| :--- | :--- | :--- | :--- |
| **Tablettes 12.4" à 13.3" (ex: Samsung Galaxy Tab S8+/S9+/S10+, Lenovo P12 Pro)** | 12.4" à 12.7" (16:10) | **Exceptionnel** : Taille quasi équivalente à une vraie feuille A4 physique. Idéal pour les conducteurs d'orchestre et pianistes. | 8 à 11 heures |
| **Tablettes 11" (ex: Samsung Tab S9, Xiaomi Pad 6)** | 11.0" (16:10) | **Très Bon** : Très bon compromis entre compacité dans la housse d'instrument et lisibilité des portées. | 9 à 12 heures |
| **Tablettes 8" à 10"** | 8.4" à 10.1" | **Correct pour chanteurs/guitaristes** : Convient aux grilles d'accords et paroles, mais un peu juste pour les partitions pour piano à 3 portées. | 7 à 9 heures |

> **Conseil Pupitre** : Pour fixer solidement une tablette de 12 pouces sur un pied de micro, privilégiez des pinces à mâchoires métalliques type *K&M (König & Meyer)* ou *Hercules Stands*, évitant tout risque de chute causé par les vibrations de scène.
