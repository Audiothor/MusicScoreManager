> **🇫🇷 Français** | [🇬🇧 English Version](Troubleshooting-&-OS-FAQ-EN)

# 🚑 Dépannage Technique & FAQ Système (Troubleshooting)

Cette section recense les solutions aux problèmes fréquemment rencontrés sur les appareils Android et Windows en situation réelle.

---

## 1. Android : Le pédalier Bluetooth se déconnecte après 10 minutes de jeu

### Cause :
Les surcouches constructeurs (Samsung OneUI, Xiaomi MIUI/HyperOS, Huawei EMUI) intègrent des optimiseurs de batterie très agressifs (*Doze Mode*) qui endorment les périphériques Bluetooth ou coupent les services en arrière-plan lorsqu'aucun appui sur l'écran tactile n'a eu lieu pendant plusieurs minutes.

### Solution :
1. Sur votre tablette Android, ouvrez les **Paramètres Android**.
2. Allez dans **Applications** › **Music Score Manager**.
3. Appuyez sur **Batterie** (ou *Optimisation de la batterie*).
4. Sélectionnez impérativement **« Non restreinte »** (ou désactivez l'optimisation).
5. Assurez-vous également que la mise en veille automatique de l'écran est désactivée ou configurée sur une durée supérieure à votre prestation.

---

## 2. Impossible de voir les fichiers PDF sur un ordinateur (PC/Mac) connecté en USB

### Cause :
Depuis Android 11, le système applique la politique du *Scoped Storage* : les fichiers stockés dans le dossier privé interne de l'application (`/data/user/0/...`) sont invisibles via l'explorateur de fichiers Windows ou le Finder Mac lorsqu'on branche un câble USB.

### Solution :
Dans Music Score Manager :
1. Ouvrez l'onglet **Paramètres** › **Paramètres Application**.
2. Dans la section *Emplacements des Répertoires*, cliquez sur **« Modifier »** pour le *Dossier des partitions*.
3. Sélectionnez un dossier public accessible de votre appareil (par exemple dans `Documents/MusicScores` ou `Download/Scores`).
4. Rebranchez la tablette en USB à votre ordinateur en choisissant le mode *Transfert de fichiers (MTP)* : vos partitions et dossiers sont désormais parfaitement visibles et synchronisables sur votre PC !

---

## 3. L'écran de la tablette s'éteint pendant que je joue un morceau

### Cause :
Le délai de mise en veille d'Android est généralement configuré sur 1 à 2 minutes par défaut.

### Solution :
- Pendant l'affichage d'une partition dans le **Visualiseur Plein Écran**, Music Score Manager active automatiquement l'instruction native `DeviceDisplay.Current.KeepScreenOn = true`. L'écran reste allumé en permanence tant que vous êtes dans le lecteur.
- Si vous constatez une extinction sur certains modèles spécifiques, vérifiez dans les paramètres de votre tablette (*Affichage > Mise en veille de l'écran*) et passez la temporisation à 10 minutes ou « Jamais ».

---

## 4. Légère latence du son de métronome sur des appareils anciens

### Cause :
Certains processeurs d'entrée de gamme peinent à décoder des fichiers audio compressés en temps réel.

### Solution :
- Music Score Manager utilise le sous-système matériel **`SoundPool`** d'Android, qui pré-décode les bips en mémoire vive non compressée (PCM 16-bit) dès le lancement de l'application.
- Pour une précision absolue, privilégiez le repère visuel (pulsation LED) qui est cadencé par une horloge nanoseconde matérielle insensible aux ralentissements de la carte son.

---

## 5. Comment transférer toute ma bibliothèque sur une nouvelle tablette ?

Vous venez d'acheter une nouvelle tablette et souhaitez retrouver l'intégralité de vos partitions, setlists et annotations :

1. **Sur l'ancienne tablette** :
   - Allez dans **Outils** › **💾 Gestion des sauvegardes**.
   - Cliquez sur **« Sauvegarder la base de données »**.
   - Branchez l'ancienne tablette à votre PC en USB (ou utilisez une clé USB-OTG) et copiez :
     * Le dossier de vos fichiers PDF et audio.
     * Le fichier de sauvegarde le plus récent situé dans le dossier des sauvegardes.
2. **Sur la nouvelle tablette** :
   - Installez **Music Score Manager**.
   - Copiez vos fichiers PDF et audio au même emplacement ou dans votre dossier public préféré.
   - Copiez le fichier de sauvegarde dans le dossier de sauvegardes de l'application.
   - Ouvrez **Outils** › **Gestion des sauvegardes**, repérez la sauvegarde et cliquez sur **« Restaurer »**.
   - Votre bibliothèque complète (morceaux, annotations, tags et setlists) est immédiatement restaurée !

---

## 6. Mon pédalier fonctionne mais n'envoie pas la bonne commande

### Solution :
1. Ouvrez **Paramètres** › **Paramètres Pédales & MIDI**.
2. Observez le **Testeur en direct** avec le voyant vert 🟢.
3. Actionnez la pédale : l'écran affiche exactement le code capturé (ex: `Key: ArrowDown`, `Keycode: 20`).
4. Si le modèle de votre pédale dispose de plusieurs modes physiques (interrupteur sur la tranche de la pédale), basculez-le sur le **Mode 1** ou **Mode 2**.
5. Si nécessaire, utilisez le bouton **« ➕ Nouveau profil »** et le **Mode Apprentissage ("Learn")** pour assigner précisément la pédale à l'action souhaitée.
