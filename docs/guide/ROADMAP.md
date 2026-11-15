**Music Score Manager ROADMAP** :

------------------------------------------------------------------------------
**Urgent bloquant ou bugs :**
Aucun pour le moment


------------------------------------------------------------------------------
**Faisables rapidement (fort impact, faible complexité) :**

* **Sauvegarde/Restauration Cloud via Android (SAF) :** Export/import d'un `.zip` via le sélecteur natif Android (compatible Google Drive, pCloud, USB) tout en gardant le statut « 100 % local / zéro collecte ».
-> RETENU

* **Liens de saut de page (*Da Capo*, *Coda*) :** Ajout de pastilles cliquables entre deux pages via votre base SQLite existante.
-> RETENU

* **Recadrage automatique des marges blanches :** Détection de la zone utile du PDF pour agrandir les portées sur tablette.
-> A CONFIRMER l'utilité suivant retours des utilisateurs

* **Pilotage MIDI (*Program Change*) :** Envoi d'un changement de programme vers un clavier ou multi-effets à l'ouverture d'une partition via l'API native `Android.Media.Midi`.
-> RETENU : à voir l'interêt et comment mettre en oeuvre simplement

------------------------------------------------------------------------------
**Plus complexes (chantiers lourds ou bloquants) :**

* **Demi-tours de page (*Half-page turns*) :** Refonte du lecteur pour afficher simultanément deux demi-pages différentes et recalibrer le placement des annotations.
-> A ETUDIER : si les utilisateurs sont fortement demandeurs

* **Portage Windows :** Réécriture nécessaire de toutes les briques liées au matériel Android (Wi-Fi Direct P2P, scanner caméra, gestion des fichiers).
-> A ETUDIER : actuellement une partie de l'application fonctionne complètement sous Windows.

* **Synchronisation Cloud temps réel :** Gestion complexe des conflits de fusion SQLite/PDF hors ligne et obligation de modifier la déclaration de confidentialité Google Play.
-> NON RETENU POUR LE MOMENT : les utilisateurs préfèrent avoir une solution local