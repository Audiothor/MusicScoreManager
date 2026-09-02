# 📋 Menu Setlists

Le menu **Setlists** est spécialement conçu pour planifier, structurer et exécuter vos programmes de concert, auditions, offices liturgiques et répétitions générales sans temps mort.

---

## 📋 Présentation & Organisation de la Liste

L'onglet Setlists affiche la liste de tous vos programmes créés :
- **Titre du programme** (ex : *Festival d'Automne 2026*, *Répétition Générale*).
- **Nombre de partitions incluses** dans le programme.
- **Date de dernière modification**.
- **Indicateur d'erreur visuel** : Si une des partitions contenues dans la setlist a son fichier PDF manquant ou corrompu, une alerte visuelle s'affiche immédiatement.

---

## ➕ Création & Édition d'une Setlist

1. **Créer une nouvelle setlist** :
   - Cliquez sur le bouton **➕** en haut à droite.
   - Donnez un nom explicite à votre programme.
2. **Ajouter des morceaux** :
   - Sélectionnez des partitions parmi l'ensemble de votre bibliothèque grâce à la sélection multiple.
3. **Ordonner le programme** :
   - Utilisez les flèches **Monter (▲)** et **Descendre (▼)** pour définir précisément l'ordre d'exécution de vos morceaux.
   - Retirez un morceau du programme en un clic (sans le supprimer de votre bibliothèque générale).
   - Enregistrez vos modifications.

---

## ▶️ Démarrage Direct en Concert (Clic sur la Carte)

Dans Music Score Manager, le passage sur scène doit être immédiat :
- **Un simple toucher sur la carte d'une setlist** lance **directement la lecture de la première partition** dans le visualiseur plein écran.
- Si la setlist est vide (aucun morceau associé), un message d'information vous avertit gentiment et vous invite à lui assigner des partitions.
- Pendant la lecture, la setlist active le **mode enchaînement continu** : lorsque vous tournez la dernière page d'un morceau, l'application bascule automatiquement sur la première page du morceau suivant du programme !

---

## ⋮ Menu Contextuel d'une Setlist (3 petits points)

Chaque setlist dispose de son propre menu d'options via le bouton **⋮** :

1. **▶️ Démarrer la setlist** :
   - Lance le concert directement sur le premier morceau en mode plein écran (équivalent au toucher direct sur la carte).

2. **✏️ Éditer la setlist** :
   - Ouvre la fenêtre d'édition ([`SetlistEditPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SetlistEditPage.xaml)) pour modifier le titre, ajouter ou supprimer des morceaux et changer l'ordre de passage.

3. **📑 Dupliquer la setlist** :
   - **Fonctionnalité gain de temps** : crée instantanément une copie complète de la setlist avec l'ensemble des morceaux conservés dans le même ordre (nommée automatiquement *« Nom - Copie »*). Idéal pour préparer deux versions d'un concert (ex : version courte et version longue).

4. **📡 Envoyer en Wi-Fi Direct** :
   - Permet de transmettre la setlist entière ainsi que tous ses fichiers de partitions associés vers les tablettes des autres musiciens.
   - Ouvre le **menu d'options** :
     * ☑️ *Inclure les annotations manuscrites*.
     * ☑️ *Inclure les pistes audio rattachées*.

5. **📦 Exporter (.msmsetlist)** :
   - Génère un paquet autonome `.msmsetlist` contenant le programme complet, les métadonnées de l'ordre de passage, les PDF et (en option) les annotations et pistes audio.

6. **🏷️ Renommer** :
   - Modifie rapidement l'intitulé de la setlist.

7. **🗑️ Supprimer** :
   - Supprime la setlist (vos partitions restent intactes dans votre bibliothèque).
