# 📋 Setlists

Le menu **Setlists** est spécialement conçu pour planifier, structurer et exécuter vos programmes de concert, auditions, offices liturgiques et répétitions générales sans temps mort.

---

## 📋 Présentation & Organisation de la Liste

L'onglet Setlists affiche la liste de tous vos programmes créés :

- **Barre supérieure** : Recherche instantanée, tri multi-critères (Nom A-Z/Z-A, Date de création, Statut) et bouton de création rapide (**➕**).
- **Filtres par Statut en Carrousel Horizontal** :
  - *Toutes*
  - *Active* (badge vert) : programmes en cours de tournée ou répétés actuellement.
  - *À venir* (badge orange) : concerts en préparation future.
  - *Terminée* (badge gris) : archives de prestations passées.
- **Cartes de Setlists** :
  - Nom du programme, date de création, badge de statut et cadenas de verrouillage `🔒`.
  - Glissement latéral (Swipe) pour accéder immédiatement à *Renommer* et *Supprimer*.

---

## ▶️ Démarrage Direct en Concert (Clic sur la Carte)

Dans Music Score Manager, le passage sur scène est instantané :

- **Un simple toucher sur la carte d'une setlist** lance **directement la lecture de la première partition** dans le visualiseur plein écran.
- Si la setlist est vide (aucun morceau associé), un message d'information vous avertit et vous invite à lui assigner des partitions.
- Pendant la lecture, la setlist active le **mode enchaînement continu** : lorsque vous tournez la dernière page d'un morceau, l'application bascule automatiquement sur la première page du morceau suivant du programme !

---

## 📋 Volet Déroulement de la Setlist en Direct (v2.0.1.1)

Pendant la lecture des morceaux d'une setlist dans le visualiseur :

- **Ouverture Discrète depuis le Haut** : Un simple appui tout en haut au centre de l'écran (barre tactile transparente) fait descendre le volet d'avancement fixé au sommet de l'écran.
- **Centrage Automatique Immédiat** : À l'ouverture, la boîte défile automatiquement pour centrer le morceau actuellement joué dans la liste.
- **Palette Visuelle Dédiée par Statut** :
  - **Partition en cours** : Fond contrasté bleu nuit, contour lumineux cyan, badge `▶` vert éclatant, titre blanc éclatant et libellé *« En cours »*.
  - **Partitions déjà passées** : Badge coché `✓` vert sauge (`#52B788`), texte gris bleuté apaisé (`#8A95A5`) et libellé *« Passé »*.
  - **Partitions à venir** : Numéro bleu pastel (`#74C0FC`), texte blanc cassé doux (`#EAF2FF`), compositeur azuréen (`#A5C8E4`) et libellé *« À venir »* (`#4DABF7`).
- **Navigation & Fermeture** :
  - Cliquez sur n'importe quel morceau pour y sauter directement et instantanément.
  - Fermez le volet via la croix `✕`, en tapant sur le fond semi-transparent ou en cliquant sur le morceau courant.

---

## ⋮ Menu Contextuel d'une Setlist (3 petits points)

Chaque setlist dispose de son propre menu d'options via le bouton **⋮** :

1. **▶️ Démarrer la setlist** :
   
   - Lance le concert directement sur le premier morceau en mode plein écran.

2. **✏️ Éditer la setlist** :
   
   - Ouvre la fenêtre d'édition ([`SetlistEditPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SetlistEditPage.xaml)) pour modifier le titre, le statut, ajouter des morceaux via la bibliothèque, réordonner les pièces à l'aide des flèches **▲ / ▼** (ou par glisser-déposer), et supprimer des morceaux.

3. **📡 Envoyer en Wi-Fi Direct** :
   
   - Permet de transmettre la setlist entière ainsi que tous ses fichiers de partitions associés vers les tablettes des autres musiciens en streaming direct sans Internet.
   - Boîte de dialogue d'options :
     * ☑️ *Inclure les annotations manuscrites*.
     * ☑️ *Inclure les pistes audio rattachées*.

4. **📦 Exporter (.msmsetlist)** :
   
   - Génère un paquet autonome `.msmsetlist` contenant le programme complet, les métadonnées de l'ordre de passage, les PDF et (en option) les annotations et pistes audio.

5. **📑 Dupliquer la setlist** :
   
   - Crée instantanément une copie complète de la setlist avec l'ensemble des morceaux conservés dans le même ordre.

6. **🏷️ Renommer** :
   
   - Modifie rapidement l'intitulé de la setlist.

7. **🔒 Verrouiller / Déverrouiller** :
   
   - Sanctuarise la setlist pour empêcher toute modification accidentelle en plein concert.

8. **🗑️ Supprimer** :
   
   - Supprime la setlist (vos partitions restent intactes dans votre bibliothèque).
