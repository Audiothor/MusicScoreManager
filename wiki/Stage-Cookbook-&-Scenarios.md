> **🇫🇷 Français** | [🇬🇧 English Version](Stage-Cookbook-&-Scenarios-EN)

# 🎭 Recettes de Scène & Scénarios Concrets (Stage Cookbook)

Ce guide pratique propose des protocoles éprouvés en répétition et en concert pour tirer le meilleur parti de Music Score Manager selon votre formation musicale.

---

## 🎺 Scénario 1 : Régie Groupe / Chorale / Big Band (Diffusion sans box)

### Le Défi
En répétition ou avant de monter sur scène, le chef de pupitre ou chef d'orchestre souhaite distribuer le programme de concert ordonné (15 morceaux) avec les annotations de doigtés et coups d'archet à **20 musiciens**. Aucun réseau Wi-Fi public n'est disponible et la 4G/5G ne capte pas dans la salle de spectacle en sous-sol.

### Le Protocole Music Score Manager (Temps total : 45 secondes)
1. **Sur la tablette du Leader (Émetteur)** :
   - Ouvrez l'onglet **Setlists**, repérez le programme du concert (ex: *« Concert Philharmonique Juin »*).
   - Cliquez sur les trois petits points `⋮` › **« 📡 Envoyer en Wi-Fi Direct »**.
   - Cochez **« Inclure les annotations manuscrites »** (pour partager les coups d'archet et nuances ajoutés lors des répétitions).
   - Cliquez sur le bouton rose **« 📲 Mode Diffusion Groupe (QR Code) »**.
   - La tablette lance un mini-serveur local autonome et affiche un grand **QR Code** à l'écran avec un compteur de musiciens connectés (`0 connecté(s)`).
2. **Sur les tablettes des musiciens (Récepteurs)** :
   - Tous les musiciens ouvrent l'application › onglet **Outils** › **« 📡 Transfert Wi-Fi Direct (P2P) »**.
   - Ils cliquent sur **« Scanner un QR Code »** et pointent la caméra vers la tablette du leader.
3. **Résultat** :
   - Le compteur du leader s'incrémente en direct : `1... 5... 12... 20 musiciens connectés`.
   - L'ensemble des partitions PDF, métadonnées, ordre de setlist et calques d'annotations sont téléchargés en parallèle en quelques secondes.
   - Les musiciens cliquent sur le setlist reçu et sont instantanément prêts à jouer !

---

## 🎹 Scénario 2 : Récital de Piano ou d'Orgue (Mode 2 Pages & Synchronisation des Tournes)

### Le Défi
Sur une œuvre dense pour piano (ex: Sonate de Beethoven ou Prélude de Chopin), tourner la page pendant un trait rapide à deux mains est impossible. Le pianiste souhaite afficher **2 pages côte à côte en mode paysage** et s'assurer que chaque tourne tombe exactement pendant un silence ou une mesure jouée à une seule main.

### Le Protocole d'Assemblage Intelligent
1. Dans l'onglet **Outils**, ouvrez **« 📑 Créateur & Assemblage PDF »**.
2. Cliquez sur **« 📖 Modifier une partition »** et chargez la pièce.
3. Repérez où tombent les paires de pages (Page 1-2, Page 3-4, etc.) :
   - Si la tourne entre la Page 2 et la Page 3 tombe en plein milieu d'une mesure virtuose, mais que la fin de la Page 1 contient un silence d'une mesure entière.
4. Cliquez sur **« 📄 Page Blanche »** et déplacez cette page blanche en **Position 1** :
   - Désormais, l'écran affichera :
     * *Écran 1* : [Page Blanche] + [Page 1 de musique]
     * *Écran 2* : [Page 2] + [Page 3]
   - La première tourne s'effectue désormais à la fin de la Page 1 (sur le silence !), et les deux pages suivantes sont visibles d'un seul coup d'œil.
5. Cliquez sur **« Écraser la partition existante »** pour enregistrer.
6. Dans **Paramètres > Pédaliers**, réglez la sensibilité d'appui long sur **450 ms** : cela évite de déclencher un saut de page accidentel en relâchant la pédale un peu trop lentement sous le stress de la scène.

---

## 🎸 Scénario 3 : Concert Pop/Rock avec Playbacks & Métronome

### Le Défi
Le groupe joue avec des bandes d'orchestration (cordes et synthétiseurs pré-enregistrés) et le batteur doit être calé au clic. Le chanteur et le guitariste ont besoin de la grille d'accords et des paroles synchronisées.

### Le Protocole
1. **Édition de la partition** :
   - Dans **Partitions**, cliquez sur `⋮` › **« ✏️ Éditer la partition »**.
   - Renseignez le **Tempo BPM** exact du morceau (ex: `128`).
   - Déroulez l'accordéon **Métronome** : définissez le chiffrage (`4/4`) et le pré-compte (`2 mesures` = 8 bips de décompte).
   - Déroulez l'accordéon **Fichiers audio** : importez le fichier MP3 ou WAV de la bande d'accompagnement.
2. **Sur Scène** :
   - Lancez le morceau. Le métronome visuel (LED) et sonore émet les 2 mesures de pré-compte.
   - À la fin du pré-compte, la piste audio démarre automatiquement au premier temps.
   - En cas d'imprévu sur scène (soliste qui souhaite prolonger un solo), appuyez sur le bouton assigné sur votre pédalier Bluetooth pour mettre en pause la bande audio sans couper le visualiseur de partition.

---

## 📑 Scénario 4 : Bonnes Pratiques de Numérisation & Poids des Fichiers

Un problème récurrent avec les classeurs numériques est la lenteur provoquée par des scans PDF de 150 Mo issus de photocopieurs mal configurés.

### Recommandations pour des Fichiers Parfaits :
- **Résolution Idéale** : **150 à 200 DPI** (dots per inch). C'est amplement suffisant pour une netteté cristalline sur tablette sans alourdir le processeur graphique.
- **Mode Couleur** :
  - Utilisez le mode **Niveaux de gris (Grayscale)** ou **Noir & Blanc (Seuil)**.
  - Évitez le mode couleur RVB 24-bit pour des partitions imprimées en noir et blanc (la couleur multiplie la taille du fichier par 3 ou 4 sans aucun bénéfice visuel).
- **Format** : Fichiers PDF standardisés. Une partition de piano de 6 pages doit idéalement peser entre **500 Ko et 2 Mo**.
- **Scan Smartphone Direct** :
  - Prenez des photos bien éclairées sans reflets.
  - Dans Music Score Manager, cliquez sur `+` › sélectionnez les photos : l'application les compile automatiquement en un PDF léger et standardisé.
