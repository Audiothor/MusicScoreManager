<h1 style="text-align: center;">Music Score Manager</h1>
<p style="text-align: center;">
  <img src="../site/assets/images/MusicScoreManager.png" alt="Music Score Manager" style="max-width: 100%; height: auto;">
</p>

Bienvenue sur la documentation complète et détaillée de **Music Score Manager**, l'application ***GRATUITE et SANS PUBLICITE*** multiplateforme (Android / Windows) conçue sur mesure pour les musiciens solistes, ensembles, chorales et orchestres.
Il a été développé pour répondre aux besoins des musiciens issus de conservatoires, harmonies, groupes de musique et autodidactes.

---

## Pourquoi utiliser Music Score Manager ?

1. **Visualiseur de partitions**
    Affichage rapide, multipages, zooms, editions du compositeurs, ajout d'étiquettes, filtres nombreux...
2. **Gestion Puissante des Setlists**
    Ordonancement, parametres de lectures de liste…
3. **Partages de partitions et de Setlists immédiats (sans internet)**
    100% autonome sur scène : Envoi de partitions uniques ou de programmes complets incluant les annotations manuscrites et les fichiers audio d'accompagnement.
    Mode Leader avec QR Code pour distribuer un programme à tout un pupitre en quelques secondes.
4. **Prise en Charge Totale des Pédales Bluetooth / Tourne-Pages**
    Mains libres sur l'instrument : Compatible avec tous les pédaliers Bluetooth du marché (AirTurn, PageFlip, Donner, Coda, CubeSuite...).
    Mappage personnalisé complet : Affectation libre des touches physiques pour tourner les pages, naviguer, déclencher le métronome ou piloter le lecteur audio.
5. **Atelier d'Annotations Musicales & Bibliothèque de Stickers (+100 symboles)**
    Outils réalistes : Surligneur fluo biseauté translucide (qui ne masque pas les portées ni les paroles), crayon fin à opaque, et saisie de texte libre.
    Plus de 100 stickers musicaux professionnels classés par catégories : nuances, doigtés d'instruments, signes de reprise (Coda, Segno, Da Capo), respirations,     coups d'archet, etc. et paramétrage de ses favoris
    Historique Undo / Redo illimité pour corriger sans stress.
6. **Métronome Haute Précision**
    Avec son du métronome ou pas
    Signal visuel LED pulsé synchronisé sur les temps forts et faibles.
    Option silencieuse à la volée : Coupure instantanée du son tout en conservant le repère visuel de la pulsation LED pendant le concert.
7. **Lecteur Audio Multipiste Synchronisé aux Morceaux**
    Rapprochement d'une ou plusieurs pistes d'accompagnement (MP3, WAV, AAC, FLAC, OGG) à chaque partition.
    Barre de transport compacte intégrée au visualiseur pour lancer les bandes orchestre, playbacks ou enregistrements de témoins sans quitter la partition des yeux.
    Réglage du pré-compte et du volume d'écoute relatif.
8. **Import Intelligent & Fusion d'Images en PDF Haute Définition**
    Conversion d'images et photos : Photographiez une partition papier avec votre tablette ; l'application propose soit de générer des pages individuelles, soit de     les fusionner automatiquement en 1 seule partition PDF multi-pages, avec modal bloquant et barre d'état en direct.
9. **Atelier d'Assemblage PDF Embarqué**
    Pas besoin d'ordinateur ou d'outil externe : réorganisez les pages d'une partition directement dans l'application par simple glisser-déposer tactile.
    Rotation individuelle des pages à 90°, 180° ou 270° (très pratique pour les pages numérisées à l'envers ou en format paysage).
    Suppression de pages blanches et insertion de nouvelles pages à la volée.
10. **Classification Dynamique & Filtrage Multi-Étiquettes (Mode ET / OU)**


Et beaucoup d'autres fonctionnalités...

```mermaid
graph TD
    A[Music Score Manager] --> C1[1. Description, Installation & Prérequis]
    A --> C2[2. Partitions & Visualiseur Scénique]
    A --> C3[3. Setlists & Déroulement Concert]
    A --> C4[4. Outils Avancés & P2P]
    A --> C5[5. Paramètres & Personnalisation]
    A --> C6[6. Détails Techniques & Confidentialité]
```

1. **[Chapitre 1 : Description Complète de l'Application, Installation & Prérequis](guide/installation.md)**  
   Philosophie, points forts, 100% hors-ligne, zéro latence, Dark Mode scénique, prérequis système (Android 12+, Windows 10/11) et procédures d'installation / compilation.

2. **[Chapitre 2 : Partitions — Actions, Bibliothèque & Visualiseur de Scène](guide/scores.md)**  
   Recherche en direct, filtrage par étiquettes, tri multi-critères, mode multi-sélection, import PDF / images (fusion intelligente en PDF multi-pages), menu contextuel (⋮), métadonnées complètes ([`ScoreEditPage`](guide/scores.md#menu-contextuel-dune-partition-3-petits-points)) et visualiseur plein écran ([`ViewerPage`](guide/viewer.md)) avec mode 2 pages paysage, annotations complètes (surligneur Stabilo biseauté, crayon, texte, stickers), métronome régulé thread-safe, lecteur audio et menu central au double-tap.

3. **[Chapitre 3 : Setlists — Organisation des Programmes & Déroulement Scénique](guide/setlists.md)**  
   Création de programmes, filtres par statut (Active, À venir, Terminée), démarrage immédiat au tap, duplication de setlist, verrouillage concert et le **Volet Déroulement de la Setlist en Direct (v2.0.1.1)** avec centrage automatique et statuts colorés.

4. **[Chapitre 4 : Outils — Boîte à Utilitaires Avancés](guide/tools.md)**  
   Atelier d'assemblage PDF ([`PdfAssemblerPage`](guide/tools.md#1-createur-assemblage-pdf-atelier-studio)), transfert Wi-Fi Direct P2P et diffusion groupe QR Code ([`WifiTransferPage`](guide/tools.md#2-transfert-wi-fi-direct-p2p-diffusion-groupe)), gestionnaire d'étiquettes avec sélecteur RVB ([`TagsPage`](guide/tools.md#3-gestion-des-etiquettes-tags)), imports de paquets (`.msmsetlist`, `.msmscore`, `.msmscores`), détection de doublons par empreinte SHA-256 ([`DuplicatesPage`](guide/tools.md#5-gestion-des-doublons)) et sauvegardes / restaurations ([`BackupsPage`](guide/tools.md#6-gestion-des-sauvegardes-restauration)).

5. **[Chapitre 5 : Paramètres — Configuration & Personnalisation](guide/settings.md)**  
   Paramètres Partitions (tris, gestes, affichage 2 pages, compteur de page), Paramètres Setlists (lecture continue, déroulement scénique), Paramètres Annotations (stickers favoris et sélection des catégories actives), Paramètres Pédaliers Bluetooth & Événements MIDI ([`SettingsPedalsPage`](guide/settings.md#5-parametres-pedaliers-bluetooth-evenements-midi) avec diagnostic direct, profils constructeurs, mode apprentissage et réactivité d'appui long), et Paramètres Application (8 langues intégrées, espace disque disponible, taille SQLite et dossiers publics).

6. **[Chapitre 6 : Détails Techniques, Confidentialité, Droits & Liens GitHub](confidentialite.md)**  
   Architecture logicielle (.NET 10 MAUI, SQLite WAL, Mozilla PDF.js, SoundPool), permissions Android transparentes, politique de confidentialité stricte (0 donnée collectée, 0 télémétrie, 100% stockage local), licence libre GNU General Public License v3.0 (GPLv3) et liens officiels vers le projet GitHub.

---

## 🧭 Accès Rapide aux Guides Détaillés

- [🚀 Chapitre 1 : Description & Installation](guide/installation.md)
- [🎵 Guide Complet du Menu Partitions](guide/scores.md)
- [📖 Guide Détaillé du Visualiseur Plein Écran](guide/viewer.md)
- [📋 Guide Complet du Menu Setlists](guide/setlists.md)
- [🛠️ Guide Complet du Menu Outils](guide/tools.md)
- [⚙️ Guide Complet du Menu Paramètres](guide/settings.md)
- [🚪 Guide du Menu Quitter](guide/quit.md)
- [🔒 Politique de Confidentialité & Mentions Légales](confidentialite.md)
