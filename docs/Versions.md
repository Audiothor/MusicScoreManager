# 📋 Historique des Versions — Music Score Manager

Ce document consigne l'historique officiel de toutes les versions de l'application **Music Score Manager**, leur date de sortie et la synthèse indexée des modifications apportées.

---

## Sommaire des Versions

- [v2.7.2 — 15/10/2026](#v272--15102026)
- [v2.7.1 — 15/10/2026](#v271--15102026)
- [v2.7.0 — 15/10/2026](#v270--15102026)
- [v2.6.5 — 15/10/2026](#v265--15102026)
- [v2.6.4 — 15/10/2026](#v264--15102026)
- [v2.6.3 — 15/10/2026](#v263--15102026)
- [v2.6.2 — 14/10/2026](#v262--14102026)
- [v2.6.1 — 14/10/2026](#v261--14102026)
- [v2.6.0 — 14/10/2026](#v260--14102026)
- [v2.5.8 — 13/10/2026](#v258--13102026)
- [v2.5.7 — 13/10/2026](#v257--13102026)
- [v2.5.6 — 12/10/2026](#v256--12102026)
- [v2.5.5 — 11/10/2026](#v255--11102026)
- [v2.5.4 — 10/10/2026](#v254--10102026)
- [v2.5.3 — 09/10/2026](#v253--09102026)
- [v2.5.2 — 07/10/2026](#v252--07102026)
- [v2.5.1 — 07/10/2026](#v251--07102026)
- [v2.5.0 — 07/10/2026](#v250--07102026)

---

## v2.7.2 — 15/10/2026

### 1. Performance Majeure : Optimisation de l'Ouverture des Partitions (Temps divisé par 4 à 8)
- **1.1. Élimination du gel WebView Chromium / V8 au démarrage :** Le composant `WebView` (utilisé uniquement en solution de repli non-native) n'est plus instancié statiquement dans le layout XAML de `ViewerPage`. Il est désormais instancié à la demande (*lazy-loading*), évitant le chargement à froid du moteur Chromium Android (gain direct de 1,5 à 2,5 secondes à chaque ouverture de partition).
- **1.2. Suppression de l'extraction redondante de PDF.js :** La décompression systématique des fichiers d'archive PDF.js depuis l'APK vers le stockage local lors de l'ouverture a été supprimée du chemin critique et n'est invoquée que si le moteur natif Android `PdfRenderer` n'est pas supporté.
- **1.3. Réutilisation du nombre de pages déjà indexé :** Réutilisation immédiate de `_score.PageCount` stocké en base de données SQLite plutôt que de réanalyser et réouvrir le fichier PDF sur disque, éliminant un accès I/O bloquant (gain de 200 à 400 ms).
- **1.4. Chargement non bloquant des rotations et métadonnées :** Le chargement des rotations individuelles de page s'exécute désormais en parallèle en tâche de fond au lieu de sérialiser et bloquer le premier rendu visuel de la partition.
- **1.5. Persistance du cache mémoire bitmap entre ouvertures :** Suppression du vidage forcé du cache (`PdfService.ClearMemoryCache()`) à la fermeture du visualiseur. Les pages déjà rendues restent en cache mémoire LRU pour une réouverture quasi-instantanée (~0 à 50 ms).

---

## v2.7.1 — 15/10/2026

### 1. Scanner de Partitions : Correction de l'Affichage du Mode de Rendu
- **1.1. Suppression de la troncature verticale du texte :** Élimination de la contrainte fixe de hauteur (`HeightRequest="34"`) et application d'un espacement vertical adéquat (`Padding="10,2"`, `MinimumHeightRequest="44"`). Le libellé complet du mode sélectionné (« *Partition N&B optimisée (Haut contraste)* ») s'affiche désormais intégralement avec toutes ses hampes et jambages sans aucun rognage.
- **1.2. Optimisation des listes déroulantes de l'accordeur :** Harmonisation des contraintes de hauteur minimale sur les sélecteurs de diapason, de note de référence et de hauteur d'octave pour garantir un affichage textuel parfait sur tous les ratios d'écran et densités DPI.

---

## v2.7.0 — 15/10/2026

### 1. Scanner de Partitions Papier via Photo (Onglet Outils)
- **1.1. Module complet de numérisation embarqué (`ScoreScannerPage`) :** Permet de photographier directement des partitions physiques avec l'appareil photo ou d'importer des images de la galerie/stockage pour les convertir immédiatement en partitions PDF multi-pages.
- **1.2. Filtre de contraste et nettoyage de partition :** Traitement d'image en temps réel (Mode « Partition N&B optimisée » blanchissant les fonds de papier jaunis ou ombragés et noircissant les portées pour une netteté de lecture maximale, Niveaux de gris, ou Couleur originale).
- **1.3. Outils par page :** Rotation individuelle à 90°, réorganisation par montée/descente des pages et suppression ciblée.
- **1.4. Intégration directe en bibliothèque :** Saisie du titre et du compositeur, conversion PDF instantanée, enregistrement en base SQLite et invite d'ouverture directe dans le lecteur de partitions.

### 2. Accordeur : Choix Déroulants Ergonomiques (ViewerPage)
- **2.1. Sélection déroulante du diapason :** Remplacement de la rangée de boutons par un sélecteur déroulant (`Picker`) proposant 440 Hz (Standard international, par défaut), 442 Hz (Orchestre symphonique), 415 Hz (Musique baroque) et 432 Hz (Fréquence alternative).
- **2.2. Sélection déroulante de la note de référence :** Remplacement des boutons horizontaux par une liste déroulante claire (« Détection auto » par défaut, puis toutes les notes de la gamme chromatique ou cordes de l'instrument).
- **2.3. Sélection déroulante de la hauteur (octave) :** Choix déroulant fluide (hauteurs 1 à 7, hauteur 4 sélectionnée par défaut).

### 3. Affichage du Titre de la Partition en Haut de l'Écran (ViewerPage)
- **3.1. Option globale dans les Paramètres :** Ajout de l'interrupteur « Afficher le titre de la partition tout en haut » dans les paramètres de partitions (`SettingsScoresPage`, désactivé par défaut).
- **3.2. Contrôle instantané dans la Boîte à outils :** Ajout de la tuile « Titre de partition » dans le tiroir d'outils haut-droit (`ViewerPage`) permettant d'activer ou désactiver l'affichage du titre en direct pour la partition en cours.
- **3.3. Bandeau discret et élégant :** Affichage centré en haut de l'écran avec fond semi-transparent et texte net sans gêner la lecture de la partition.

### 4. Simplification de l'Arborescence des Paramètres
- **4.1. Suppression du sous-menu « Organisation » :** Élimination de la section intermédiaire dans les Paramètres Partitions.
- **4.2. Accès direct aux Étiquettes :** Déplacement de « Gérer la liste des étiquettes » directement sur la page racine des Paramètres (`SettingsPage`), immédiatement sous « Paramètres Partitions ».

### 5. Harmonisation Graphique des Étiquettes
- **5.1. Design pilule unifié dans la Gestion des étiquettes (`TagsPage`) :** Les étiquettes dans la liste de gestion adoptent exactement le même design sous forme de badges arrondis colorés que ceux affichés à côté des titres dans la bibliothèque des partitions.

---

## v2.6.5 — 15/10/2026

### 1. Accordeur : Correction de la Grille d'Étalonnage du Diapason (A4)
- **1.1. Restauration de l'affichage des 4 fréquences :** Attribution explicite des colonnes de grille (`Grid.Column="0"` à `"3"`) et du conteneur parent (`Grid.Column="1"`). Les 4 options de diapason (`440 Hz`, `442 Hz`, `415 Hz` et `432 Hz`) sont désormais parfaitement visibles et alignées horizontalement à droite du libellé « Diapason : », sans aucune superposition.
- **1.2. Alignement du sélecteur d'octaves (Hauteur) :** Décalage propre du défilement des boutons d'octave en `Grid.Column="1"` pour éliminer tout chevauchement avec le libellé « Hauteur : ».

---

## v2.6.4 — 15/10/2026

### 1. Accordeur Amovible & Déplaçable
- **1.1. Boîte flottante déplaçable :** Ajout d'une poignée de déplacement tactile (`⠿ Déplacer`) et du support panoramique complet (`PanGestureRecognizer`) permettant de repositionner librement l'accordeur n'importe où sur l'écran pour ne pas masquer la partition pendant l'accordage.
- **1.2. Isolation tactile complète (Anti Pass-Through) :** Élimination définitive des interactions accidentelles avec la partition sous-jacente (les clics simples et doubles à l'intérieur de la boîte sont interceptés et n'ouvrent plus le menu central ni ne tournent les pages).

### 2. Étalonnage du Diapason (Fréquences de Référence)
- **2.1. Correction de sélection :** Résolution du bug de conversion de paramètre : l'ensemble des 4 fréquences de référence (`440 Hz`, `442 Hz`, `415 Hz`, `432 Hz`) sont désormais toutes sélectionnables instantanément avec retour visuel d'activation immédiat.
- **2.2. Synchronisation en temps réel :** Le changement de fréquence de référence recalcule à la volée la hauteur de toutes les notes cibles et le son généré par le diapason audio.

### 3. Hauteur de Note & Mode Chromatique Universel
- **3.1. Affichage explicite de la hauteur d'octave :** En mode Chromatique, le sélecteur d'octaves (1 à 7) est toujours accessible et chaque note affiche sa hauteur réelle (par ex. `Do 5 (C5)` ou `Do 3 (C3)`).
- **3.2. Feedback visuel et tonal instantané :** Dès la sélection d'une note ou d'une octave, l'écran affiche immédiatement la note cible (`C5`), son nom complet (`Do 5`) et sa fréquence précise (`523.3 Hz`), et le bouton audio synthétise la tonalité exacte correspondante.

### 4. Mise en Conformité Politique de Confidentialité
- **4.1. Déclaration du Microphone (`RECORD_AUDIO`) :** Formalisation claire dans la politique de confidentialité (versions française et anglaise) : le flux microphone est traité strictement en mémoire vive locale (RAM) pour le calcul de hauteur fondamentale ($F_0$), avec zéro enregistrement sur disque et zéro transmission réseau.

---

## v2.6.3 — 15/10/2026

### 1. Accordeur : Étalonnage du Diapason (Fréquence de Référence A4)
- **1.1. Sélecteur de diapason instantané :** Ajout de 4 options d'accordage de référence :
  - `440 Hz` : Standard international de concert (par défaut).
  - `442 Hz` : Fréquence d'orchestre symphonique et ensembles modernes.
  - `415 Hz` : Tempérament et accordage musique baroque (demi-ton inférieur).
  - `432 Hz` : Fréquence alternative & acoustique naturelle (Verdi).
- **1.2. Recalcul automatique du tempérament égal :** Le changement de fréquence de référence ajuste immédiatement la fréquence cible de toutes les cordes et notes, ainsi que le signal sonore de référence généré.

### 2. Mode Chromatique Universel : Choix Manuel de la Note et de l'Octave
- **2.1. Sélection de note ou détection automatique :** En mode Chromatique, l'utilisateur peut laisser le mode `[Auto]` ou choisir manuellement la note souhaitée parmi les 12 demi-tons (Do, Do♯, Ré, Ré♯, Mi, Fa, Fa♯, Sol, Sol♯, La, La♯, Si).
- **2.2. Sélecteur d'octaves (1 à 7) :** En sélection manuelle d'une note chromatique, une rangée d'octaves (1 à 7) s'affiche pour verrouiller la hauteur exacte (par ex. Do 5 à 523.25 Hz pour vérifier son instrument).
- **2.3. Diapason sonore synchronisé :** La touche de lecture audio joue le son sinusoïdal pur à la fréquence exacte de la note et de l'octave sélectionnées.

### 3. Jauge Visuelle Tricolore & Guidage Intuitif
- **3.1. Précision visuelle en 3 zones distinctes :**
  - 🟢 **Vert (≤ 4 cents) :** « PARFAIT ! Accordé » avec graduation lumineuse centrale.
  - 🟠 **Orange (5 à 15 cents) :** Accordage proche nécessitant un ajustement fin (« ▲ Un peu trop bas - Monter légèrement » ou « ▼ Un peu trop haut - Descendre légèrement »).
  - 🔴 **Rouge (> 15 cents) :** Écart notable (« ▲ Trop bas - Tendre / Monter » ou « ▼ Trop haut - Descendre / Desserrer »).
- **3.2. Clarté et feedback immédiat sur scène :** L'aiguille et le rail dynamique 5-zones offrent un guidage instinctif d'un coup d'œil sans quitter des yeux son instrument.

---

## v2.6.2 — 14/10/2026

### 1. Barre d'Annotations : Ergonomie Épurée & Suppression du Cadenas
- **1.1. Suppression du cadenas de verrouillage/déverrouillage :** La distinction manuelle verrouillé/déverrouillé est désormais inutile : l'ouverture de la barre d'annotations active directement le mode édition, et sa fermeture par la croix `✕` reverrouille et protège automatiquement les annotations contre les touches involontaires.
- **1.2. Gain d'espace et lisibilité :** La suppression du bouton cadenas aère la barre d'outils d'annotation au bas de l'écran, évitant toute confusion chez le musicien.

### 2. Accordeur Multi-Instruments Intégré (« Tuner »)
- **2.1. Nouvel outil accessible depuis le tiroir d'outils :** 5e tuile interactive avec icône `🎛️` et badge d'état (`ON` / `OFF`) dans la boîte à outils.
- **2.2. Prise en charge de 9 accordages d'instruments :** Accordeur Chromatique Universel, Guitare (E A D G B E), Basse 4 cordes (E A D G), Basse 5 cordes (B E A D G), Violon (G D A E), Alto (C G D A), Violoncelle (C G D A), Contrebasse (E A D G) et Ukulélé (G C E A).
- **2.3. Sélection ergonomique de la note ou détection automatique :** Boutons-pilules tactiles pour sélectionner la corde cible souhaitée ou laisser l'algorithme détecter automatiquement la note jouée.
- **2.4. Détection de hauteur en temps réel de haute précision :** Algorithme par autocorrélation calculant la fréquence fondamentale $F_0$, l'écart en cents ($-50$ à $+50$), la note et solfège (Do à Si), l'octave et le conseil d'accordage (Tendre / Desserrer / Parfait).
- **2.5. Contrôle du microphone explicite & sécurisé :** Bouton d'activation/désactivation du micro bien visible pour garantir l'intimité et n'écouter que lorsque nécessaire.
- **2.6. Diapason sonore de référence :** Bouton pour écouter une onde sinusoïdale de référence synthétisée à la fréquence exacte de la note sélectionnée.
- **2.7. Fermeture instantanée :** Bouton `✕` en haut à droite permettant de fermer immédiatement l'accordeur et de revenir instantanément à la partition sans interrompre le jeu.

---

## v2.6.1 — 14/10/2026

### 1. Boîte à Outils : Clarté & Numéros de Page
- **1.1. Renommage précis de la tuile Annotations :** Remplacement de « Barre d'annotations » par « Annotations » dans les 8 langues prises en charge, clarifiant qu'il s'agit du contrôle d'affichage des annotations sur la partition.
- **1.2. Ajout de la tuile interactive « Numéros de page » :** 4e tuile d'action dans le tiroir d'outils avec icône `📄` et badge synchronisé (`ON` / `OFF`) pour masquer ou afficher à la volée le compteur de page en bas à droite.

### 2. Métronome Flottant Déplaçable
- **2.1. Glisser-déposer libre du métronome :** La vignette du métronome peut désormais être déplacée n'importe où sur l'écran pendant le jeu via glisser-déposer (`PanGestureRecognizer`).
- **2.2. Détection intelligente Tap vs Drag :** Un tap court continue d'ouvrir immédiatement le panneau de réglage du tempo et volume, tandis qu'un glissement déplace l'encadré sans déclencher l'ouverture involontaire de la modale.
- **2.3. Poignée tactile et contraste renforcé :** Intégration d'un témoin de déplacement `⠿`, d'une bordure fine et d'une ombre douce garantissant une parfaite visibilité sur partitions blanches ou sombres.

### 3. Paramètres Partitions : Option Globale d'Affichage de l'Icône Outils
- **3.1. Nouveau réglage dans Affichage :** Ajout de l'interrupteur « Afficher l'icône de boîte à outils (haut-droite) » dans **Paramètres > Paramètres partitions > Affichage** (désactivé par défaut).
- **3.2. Préservation permanente de l'accès tactile :** Même lorsque l'icône visuelle est masquée pour épurer la partition, la zone tactile du coin supérieur droit reste 100% opérationnelle pour ouvrir le tiroir d'outils à tout moment.

---

## v2.6.0 — 14/10/2026

### 1. Boîte à Outils (« Tools Hub ») & Tiroir d'Actions Flottant
- **1.1. Nouveau tiroir flottant ergonomique :** Regroupement des 3 outils principaux (Métronome, Lecteur audio, Annotations) dans un popover moderne avec fond flouté, ombre douce et fermeture instantanée au clic extérieur.
- **1.2. Tuiles d'activation interactive avec état en temps réel :** Chaque outil dispose d'une tuile cliquable avec indicateur coloré et badge d'état (`ON` / `OFF`) synchronisé instantanément avec la partition.
- **1.3. Zone tactile dédiée coin haut-droit :** Réservation d'une zone tactile permanente (70×55 px) dans l'angle supérieur droit ouvrant immédiatement la boîte à outils sur tout type de document (PDF natif ou Image, zoomé ou non).
- **1.4. Option d'affichage du bouton `[ 🛠️ ]` :** Nouveau paramètre par partition (`ShowToolsButton`) permettant d'afficher ou masquer l'icône visuelle sans jamais désactiver la zone tactile secrète.
- **1.5. Réorganisation spatiale de la partition :** Déplacement du métronome en haut à gauche et du lecteur audio en haut à droite pour une lisibilité optimale et un équilibre visuel parfait.
- **1.6. Épuration du menu central :** Retrait de la section 3 (interrupteurs d'outils) et compactage de la hauteur du menu central pour une navigation plus légère et directe.

---

## v2.5.8 — 13/10/2026

### 1. Conformité Android 15 Edge-to-Edge & Résolution des API Obsolètes
- **1.1. Thème dédié Android 15 (`values-v35/styles.xml`) :** Conformité stricte aux directives Android 15 sans aucun paramètre de couleur de fenêtre obsolète, et prise en charge des découpes d'écran (`android:windowLayoutInDisplayCutoutMode` fixé sur `always`).
- **1.2. Suppression des paramètres de fenêtres obsolètes :** Élimination définitive des attributs XML dépréciés `android:windowLightStatusBar` et `android:windowLightNavigationBar` qui déclenchaient l'avertissement de l'analyseur statique Google Play Console.
- **1.3. Élimination complète des avertissements de dépendances (0 avertissement, 0 erreur) :** Rétablissement de la compatibilité ascendante complète (minSdkVersion 21.0 / Android 5.0+) avec résolution des conflits de versions transitive NuGet.
- **1.4. Élimination par R8 des appels obsolètes :** Règle `-assumenosideeffects` sur `Window.setStatusBarColor` et `Window.setNavigationBarColor` pour épurer les appels obsolètes dans le bytecode DEX.
- **1.5. Maintien absolu de la stabilité runtime :** Préservation intégrale du pont JNI (.NET MAUI / Mono) avec `-dontobfuscate` actif, empêchant tout plantage à l'ouverture de l'application.

---

## v2.5.7 — 13/10/2026

### 1. Hotfix Stabilité Runtime & Restauration du Pont JNI
- **1.1. Résolution définitive du crash au lancement :** Rétablissement de la politique de protection des classes Java/AndroidX (`-dontobfuscate` et conservation stricte de `androidx.**` et `com.google.android.material.**`). Le renommage des classes Java opéré en v2.5.6 rompait la résolution JNI native du runtime .NET MAUI, provoquant des erreurs fatales `ClassNotFoundException` à l'ouverture.
- **1.2. Sécurisation du thème XML :** Restauration de l'attribut `colorPrimaryDark` sous `Maui.MainTheme` assurant l'initialisation sans faille des composants MaterialComponents.
- **1.3. Préservation de l'optimisation DEX sans régression :** Maintien du code shrinking R8 (`AndroidEnableCodeShrinking`) pour réduire la taille du paquet tout en garantissant une stabilité d'exécution irréprochable sur l'ensemble des appareils Android.

---

## v2.5.6 — 12/10/2026

### 1. Conformité Définitive Google Play Console & Android 15
- **1.1. Obscurcissement R8 effectif (DEX Code Shrinking) :** Résolution de la directive `-dontobfuscate` injectée automatiquement par le SDK .NET pour Android (`proguard_xamarin.cfg`). Suppression programmée de cette directive au build MSBuild (`_EnableR8Obfuscation`) avant la compilation Dalvik/Dex, et configuration de règles R8 ciblées sur les bibliothèques AndroidX et Google Material (`allowobfuscation`, `allowoptimization`). Renommage effectif des classes et méthodes Java/Android dans le fichier de mapping, satisfaisant pleinement le seuil d'obscurcissement minimum exigé par Google Play Vitals (> 25 %).
- **1.2. Élimination totale des attributs XML obsolètes :** Suppression complète des attributs `android:statusBarColor`, `android:navigationBarColor` et `colorPrimaryDark` de `styles.xml`. Sous Android 15 (SDK 35), l'affichage de bord à bord est ainsi appliqué de manière native et propre, sans conflit avec les analyseurs statiques du Play Store.
- **1.3. Affichage Bord-à-Bord universel & gestion des découpes d'écran :** Configuration de `android:windowLayoutInDisplayCutoutMode` sur `shortEdges` dans le thème de l'application, garantissant que l'affichage s'étend de manière optimale sur tous les appareils équipés d'encoches, poinçons ou découpes d'écran, tout en préservant le confort de lecture des partitions.
- **1.4. Génération des symboles de débogage natifs :** Emballage systématique de l'archive des symboles natifs (.so) non tronqués (`com.audiothor.musicscoremanager-v2.5.6-native-symbols.zip`) pour le téléversement sur Google Play Console et la résolution des rapports de plantages natifs/ANR.

---

## v2.5.5 — 11/10/2026

### 1. Conformité aux Standards Google Play & Android 15
- **1.1. Optimisation et Obscurcissement du code DEX (R8) :** Activation explicite du rétrécissement de code (`AndroidEnableCodeShrinking`) et du compilateur R8 (`AndroidLinkTool=r8`) pour tous les paquets Android Release (AAB/APK). Intégration d'un fichier de règles ProGuard/R8 dédié (`Platforms/Android/proguard.cfg`) protégeant le runtime .NET MAUI/Mono, et génération automatique de la table de correspondance `mapping.txt` (`AndroidCreateProguardMappingFile` / `AndroidEnableProguardMapping`), garantissant le dépassement du seuil de 25 % d'obscurcissement imposé par Google Play Vitals.
- **1.2. Support de l'affichage Bord-à-Bord moderne (Edge-to-Edge) :** Appel natif de `AndroidX.Activity.EdgeToEdge.Enable(this)` dans `MainActivity.OnCreate()` assurant la compatibilité complète avec les exigences d'Android 15 (SDK 35+) et la rétrocompatibilité sur les versions antérieures.
- **1.3. Élimination des paramètres et API de fenêtre dépréciés :** Suppression des définitions de couleurs opaques pour la barre d'état et la barre de navigation dans `styles.xml`. Définition de barres système transparentes (`android:statusBarColor` et `android:navigationBarColor` à `@android:color/transparent`) avec gestion dynamique du contraste (`windowLightStatusBar` et `windowLightNavigationBar`), répondant strictement aux directives de Google Play Console.

---

## v2.5.4 — 10/10/2026

### 1. Profils de Pédales Dédiés par Setlist (`SetlistEditPage`, `ViewerPage`)
- **1.1. Sélecteur de profil dans l'éditeur de setlist :** Ajout d'une nouvelle option située sous « Mode continu » dans `SetlistEditPage`. Elle affiche par défaut le profil actif des paramètres généraux. L'utilisateur peut sélectionner n'importe quel profil configuré (profils d'usine ou personnalisés) et enregistrer ce choix pour la setlist.
- **1.2. Application prioritaire en lecture de concert :** Lors de la lecture des partitions de cette setlist (`ViewerPage`), le profil choisi pour cette setlist est automatiquement appliqué en substitution du profil par défaut. Toutes les interactions au pied ou commandes MIDI s'adaptent ainsi sur mesure au répertoire joué.
- **1.3. Restauration automatique du profil par défaut :** À la fermeture du visualiseur de partitions, le profil de substitution est immédiatement libéré et le profil par défaut configuré dans les paramètres redevient actif.
- **1.4. Persistance et synchronisation :** Sauvegarde transparente dans la base de données SQLite via le champ `PedalProfileId`, prise en charge lors de la duplication de setlists, des exports/imports (.msmsetlist) et des transferts Wi-Fi Direct.

### 2. Clarté des Paramètres Pédales & MIDI (`SettingsPedalsPage`)
- **2.1. Intitulé explicite :** Renommage de l'en-tête « Profil de pédale actif » en « Profil de pédale actif par défaut » pour expliciter qu'il s'agit du comportement par défaut global de l'application.
- **2.2. Traductions internationales :** Prise en charge intégrale dans les 8 langues de l'application (Français, Anglais, Allemand, Espagnol, Italien, Néerlandais, Polonais, Portugais).

---

## v2.5.3 — 09/10/2026

### 1. Précision du Positionnement des Annotations en Mode Paysage 2 Pages (`ViewerPage`)
- **1.1. Correction du calcul de projection de coordonnées (`PageToScreen` / `ScreenToPage`) :** Correction du calcul de la largeur relative unitaire (`pageWRel`) par page qui était faussé en mode 2 pages (lors d'une dernière page impaire sans page droite ou lors d'asymétries). Chaque page d'une planche double dispose désormais systématiquement d'un gabarit de 50% de la planche (`_canvasWRel / 2.0`), garantissant un alignement pixel-perfect au millimètre près des annotations créées en mode portrait lorsqu'on bascule la tablette en mode paysage 2 pages.
- **1.2. Résolution des courses de dimensions lors de la rotation d'écran :** Synchronisation immédiate des dimensions réelles de l'écran dans `RenderPdfCurrentAsync` au moment d'appliquer les limites du canevas (`UpdateCanvasBounds`), éliminant tout décalage d'aspect ratio résiduel causé par l'ancienne orientation portrait.

---

## v2.5.2 — 07/10/2026

### 1. Fluidité Extrême du Défilement des Partitions (Proposition 2)
- **1.1. Stabilisation du gabarit des cellules :** Définition d'une hauteur minimale uniforme de cellule (`MinimumHeightRequest="78"`) dans `ScoresPage.xaml`.
- **1.2. Maintien intégral de la visibilité des étiquettes :** Conservation de l'ensemble des étiquettes associées à chaque partition, désormais disposées dans un conteneur horizontal direct (`HorizontalStackLayout`) remplaçant l'ancien calcul Flexbox multi-passes.
- **1.3. Réactivation de la stratégie `MeasureFirstItem` :** Mesure unique de la première cellule à l'affichage et recyclage instantané à 60/120 images par seconde sans recalcul lourd de hauteur pendant le défilement.
- **1.4. Optimisation du cache natif Android RecyclerView :** Configuration dans `MauiProgram.cs` d'un cache mémoire étendu de 25 cellules hors-écran (`SetItemViewCacheSize(25)`) éliminant toute saccade même avec 400+ partitions.

### 2. Menu Central du Visualiseur (`ViewerPage`)
- **2.1. Correction du rognage inférieur des boutons :** Ajustement des marges internes du menu central (`CentralMenuOverlay`) et de son conteneur défilable. Les boutons d'action « ✏️ Modifier » et « 📑 Assemblage » disposent désormais d'un espacement net et de coins arrondis intacts sans coupure visuelle au bas de la fenêtre.

### 3. Optimisation de l'Ouverture des Partitions
- **3.1. Indexation SQLite des fichiers audio :** Ajout de l'attribut `[Indexed]` sur la clé étrangère `ScoreId` du modèle `ScoreAudioFile`, éliminant les analyses séquentielles (*table scan*) de la base de données.
- **3.2. Élimination des requêtes redondantes à l'ouverture :** Suppression du re-chargement complet synchrone du score dans `OnAppearing` (les métadonnées étant déjà chargées) et remplacement par une récupération ciblée et légère.
- **3.3. Initialisation différée (Lazy-Loading) des annotations :** Déport de la construction lourde des catégories de stickers et palettes uniquement au moment où l'utilisateur ouvre le panneau d'annotations/stickers, accélérant l'affichage immédiat du PDF.
- **3.4. Régulation du calcul de pages en tâche de fond :** Temporisation du calcul asynchrone des pages manquantes dans `ScoresPage` pour ne plus encombrer la base SQLite lors de la sélection d'un morceau.

---

## v2.5.1 — 07/10/2026

### 1. Ergonomie du Menu Central du Visualiseur (`ViewerPage`)
- **1.1. Suppression du bouton redondant « Fermer » :** Retrait du bouton en bas du menu rapide central, la fermeture s'effectuant de manière immédiate via la croix `✕` de l'en-tête supérieur.
- **1.2. Épuration de la ligne Métronome :** Retrait de l'icône roue crantée `⚙️` située entre « Afficher métronome » et l'interrupteur On/Off pour une disposition harmonieuse et parfaitement alignée avec les lignes Audio et Annotations.

### 2. Lisibilité de la Barre de Navigation Inférieure (`AppShell`)
- **2.1. Agrandissement des caractères :** Légère augmentation de la taille de police et renforcement des intitulés du menu principal (*Partitions*, *Setlists*, *Outils*, *Paramètres*, *Quitter*) pour un meilleur confort de lecture sur scène.

### 3. Correctif d'État du Surlignage Stabilo
- **3.1. Réinitialisation complète à la fermeture :** Correction du problème où la fermeture de la palette d'options du stabilo via sa croix laissait l'outil actif en arrière-plan, obligeant l'utilisateur à cliquer deux fois sur l'icône stabilo pour la rouvrir. La fermeture de la boîte désactive désormais proprement le mode et réinitialise l'état visuel du bouton.
- **3.2. Uniformisation :** Application du même comportement aux palettes du crayon et du texte.

---

## v2.5.0 — 07/10/2026
- [v2.4.9 — 07/10/2026](#v249--07102026)
- [v2.4.8 — 01/10/2026](#v248--01102026)
- [v2.4.7 — 28/09/2026](#v247--28092026)
- [v2.4.6 — 25/09/2026](#v246--25092026)
- [v2.4.5 — 23/09/2026](#v245--23092026)
- [v2.4.4 — 21/09/2026](#v244--21092026)
- [v2.4.3 — 19/09/2026](#v243--19092026)
- [v2.4.2 — 17/09/2026](#v242--17092026)
- [v2.4.1 — 16/09/2026](#v241--16092026)
- [v2.4.0 — 14/09/2026](#v240--14092026)
- [v2.3.0 — 30/09/2026](#v230--30092026)
- [v2.2.2 — 29/09/2026](#v222--29092026)
- [v2.2.1 — 29/09/2026](#v221--29092026)
- [v2.2.0 — 29/09/2026](#v220--29092026)
- [v2.1.13 — 29/09/2026](#v2113--29092026)
- [v2.1.12 — 25/09/2026](#v2112--25092026)
- [v2.1.11 — 22/09/2026](#v2111--22092026)
- [v2.1.10 — 22/09/2026](#v2110--22092026)
- [v2.1.9 — 22/09/2026](#v219--22092026)
- [v2.1.8 — 20/09/2026](#v218--20092026)
- [v2.1.7 — 20/09/2026](#v217--20092026)
- [v2.1.6 — 19/09/2026](#v216--19092026)
- [v2.1.5 — 18/09/2026](#v215--18092026)
- [v2.1.4 — 18/09/2026](#v214--18092026)
- [v2.1.3 — 18/09/2026](#v213--18092026)
- [v2.1.2 — 17/09/2026](#v212--17092026)
- [v2.1.1 — 17/09/2026](#v211--17092026)
- [v2.1.0 — 17/09/2026](#v210--17092026)
- [v2.0.4 — 13/09/2026](#v204--13092026)
- [v2.0.3 — 13/09/2026](#v203--13092026)
- [v2.0.2 — 12/09/2026](#v202--12092026)
- [v2.0.1 — 11/09/2026](#v201--11092026)
- [v2.0.0 — 10/09/2026](#v200--10092026)
- [v1.9.8.0 — 08/09/2026](#v1980--08092026)

---

## v2.5.0 — 07/10/2026

### 1. Refonte Complète de la Gestion des Profils de Pédales Bluetooth & Événements MIDI
- **1.1. Suppression des anciennes sections :** Suppression totale de l'ancien panneau de diagnostic/testeur et de l'accordéon des raccourcis dans les paramètres principaux afin de clarifier l'interface.
- **1.2. Maintien intégral de la sécurité scénique :** Conservation du chapitre « Sécurité scène & sensibilité » avec protection anti-double saut de page paramétrable (300 à 800 ms) et réglage précis du seuil d'appui long.
- **1.3. Gestion universelle et ouverte des profils :**
  - Maintien des préréglages constructeurs (PageFlip Dragonfly, Firefly, Butterfly, AirTurn Duo/Quad 500, Donner, Joyo, Harley Benton, iRig BlueTurn, Coda STOMP, etc.).
  - **Modification sans restriction :** L'utilisateur peut désormais modifier directement n'importe quel profil de base constructeur sans devoir obligatoirement créer un clone.
  - **Champ Description & Commentaires :** Ajout pour chaque profil d'un espace pour consigner les astuces de réglage matériel (ex: commutateur REPEAT sur OFF, sélection du Mode 3 pour PageFlip Dragonfly).
  - Création de profil en 1 clic demandant le nom et basculant instantanément vers l'écran d'édition.
  - Suppression de profil personnalisé et possibilité de restaurer l'ensemble des profils d'usine par défaut.

### 2. Édition Granulaire des Pédales et Boutons (`PedalProfileEditPage`)
- **2.1. Trois opérations intuitives :**
  - **Ajouter une pédale / bouton :** Demande du libellé personnalisé (ex: « Pédale gauche », « Bouton 1 »), puis transition directe vers la page de configuration.
  - **Éditer une pédale :** Ajustement des paramètres du bouton sélectionné via une page dédiée.
  - **Supprimer une pédale :** Suppression directe du bouton du profil sélectionné.
- **2.2. Vue récapitulative élégante :** Cartes stylisées affichant le rôle, le badge de signal capté, le type d'appui (Simple vs Long) et l'action assignée.

### 3. Page de Configuration d'un Bouton (`PedalButtonConfigPage`)
- **3.1. Nom / rôle :** Édition du libellé personnalisé.
- **3.2. Détection automatique du signal en direct :** Écoute active Bluetooth HID et MIDI affichant en direct le code brut (Hex/Décimal) et le nom de touche, avec flash visuel vert de confirmation.
- **3.3. Type d'appui :** Choix clair entre « ⚡ Appui simple » et « ⏱️ Appui long ».
- **3.4. 13 Actions Scéniques Strictes :**
  1. *Page précédente*
  2. *Page suivante*
  3. *Aller au début du morceau*
  4. *Aller à la fin du morceau*
  5. *Aller au marqueur précédent*
  6. *Aller au marqueur suivant*
  7. *Aller au début du morceau précédent du setlist*
  8. *Aller au début du morceau suivant du setlist*
  9. *Ouvrir le menu de liste de morceaux de la setlist (top drawer)*
  10. *Activer le mode nuit (implémentation future)*
  11. *Zoom +*
  12. *Zoom -*
  13. *Afficher / retirer les annotations*
- **3.5. Prise en charge dans le visualiseur :** Câblage complet des actions dans `ViewerPage` (zoom par palier, masquage des traits d'annotations, contrôle du tiroir setlist).

---

## v2.4.9 — 07/10/2026

### 1. Correctif d'affichage des étiquettes (Tri « Sans étiquette d'abord »)
- **1.1.** Correction du problème où, lors de la sélection du tri « Sans étiquette d'abord » dans le menu Partitions, aucune étiquette n'apparaissait sur l'ensemble de la liste de partitions.
- **1.2.** Remplacement de la stratégie de mesure fixe `ItemSizingStrategy="MeasureFirstItem"` par `ItemSizingStrategy="MeasureAllItems"` dans la `CollectionView` des partitions, garantissant une hauteur adaptée à chaque cellule.
- **1.3.** Ajout de la propriété réactive `HasTags` et notification de changement de propriété (`INotifyPropertyChanged`) sur le modèle `Score` pour lier dynamiquement la visibilité des badges d'étiquettes.

### 2. Système de Marqueurs de Page (Signets / Bookmarks)
- **2.1.** **Activation par page :** Ajout dans le menu central d'une bascule On/Off (désactivée par défaut) permettant de positionner un marqueur unique sur la page actuellement affichée.
- **2.2.** **Affichage discret et moderne :** Symbole graphique élégant et discret de signet `🔖` positionné en haut à gauche de la page affichée, avec lettrage alphabétique majuscule automatique par défaut (`A`, `B`, `C`...).
- **2.3.** **Boîte de dialogue interactive :** Clic sur le marqueur ouvrant une modale moderne permettant :
  - De renommer le marqueur (1 à 3 caractères maximum, unique au sein de la partition).
  - De supprimer le marqueur de la page.
  - De naviguer au marqueur précédent ou suivant.
  - D'accéder à la liste complète des marqueurs de la partition pour s'y rendre immédiatement par simple clic sur un badge.
- **2.4.** **Support Pédale Bluetooth & Commandes MIDI :** Intégration de deux nouvelles actions configurables `NextBookmark` (Marqueur suivant) et `PreviousBookmark` (Marqueur précédent) dans le service de pédales, permettant de sauter de signet en signet sans lâcher son instrument.
- **2.5.** **Persistance SQLite :** Création de la table `ScoreBookmark` avec cascade de suppression automatique lors de la suppression d'une partition.

### 3. Refonte Ergonomique du Menu Central du Visualiseur
- **3.1.** **Accès prioritaire immédiat :** Création d'un bandeau d'en-tête supérieur fixe regroupant le bouton rouge « 🏠 Accueil » et le bouton « ✕ Fermer », directement visibles et cliquables sans aucun défilement.
- **3.2.** **Organisation en cartes thématiques :**
  - *Carte Marqueur :* Bascule On/Off du marqueur de la page courante et boutons Précédent / Suivant.
  - *Carte Affichage & Navigation :* Boutons rapides de rotation (+90°), réinitialisation du zoom (100%) et saut direct de page, accompagnés des bascules de portée de rotation.
  - *Carte Outils & Modules :* Toggles pour le métronome (avec son bouton d'accès direct aux réglages ⚙️), le lecteur audio et l'affichage des annotations.
  - *Carte Gestion :* Boutons de modification de la partition et d'assemblage PDF.
- **3.3.** **Adaptabilité dynamique :** Calcul automatique de la hauteur naturelle du menu pour supprimer tout ascenseur vertical inutile sur les écrans spacieux, tout en conservant un défilement fluide sur petits écrans en mode paysage.

---

## v2.4.8 — 01/10/2026

### 1. Dialogue moderne de réglage du métronome
- **1.1.** Nouvelle boîte de dialogue moderne et épurée pour les paramètres complets du métronome.
- **1.2.** Contrôle du volume sonore du métronome par partition avec slider et mémorisation.
- **1.3.** Suppression des permissions inutiles (caméra et localisation) sur Android.

---

## v2.4.7 — 28/09/2026

### 1. Design immersif des boîtes de dialogue
- **1.1.** Refonte visuelle et animations fluides des dialogues d'importation et messages système.

---

## v2.4.6 — 25/09/2026

### 1. Adaptation responsive mobile
- **1.1.** Adaptation responsive des barres d'outils d'annotation, crayons, stabilo et stickers pour smartphones en orientation portrait et paysage.

---

## v2.4.5 — 23/09/2026

### 1. Préférences d'importation
- **1.1.** Mémorisation et choix du mode d'importation par défaut (Copie locale vs Lien externe).

---

## v2.4.4 — 21/09/2026

### 1. Importation Cloud
- **1.1.** Robustesse et compatibilité totale avec les fichiers provenant de Google Drive et fournisseurs Cloud.

---

## v2.4.3 — 19/09/2026

### 1. Optimisation du menu central
- **1.1.** Ajustement de la compacité sans étirement vertical sur tablette et grand écran.

---

## v2.4.2 — 17/09/2026

### 1. Correctifs et ergonomie
- **1.1.** Améliorations de l'affichage des stickers et documentation officielle.

---

## v2.4.1 — 16/09/2026

### 1. Barres d'options d'annotations
- **1.1.** Compacité accrue des sélecteurs de couleur et réglages de taille pour crayons et stabilo.

---

## v2.4.0 — 14/09/2026

### 1. Refonte mobile et annotations
- **1.1.** Support complet des annotations crayon, stabilo et stickers.
- **1.2.** Double-tap pour actions rapides et bascule des numéros de page.

---

## v2.3.0 — 30/09/2026

### 1. Ordonnancement personnalisé des stickers favoris
- **1.1.** **Réorganisation libre des stickers favoris :** Ajout de la réorganisation personnalisée des stickers favoris dans la page des paramètres d'annotations (`SettingsAnnotationsPage`).
- **1.2.** **Contrôles dédiés :** Boutons Monter / Descendre et poignées de réordonnancement pour agencer ses symboles musicaux préférés exactement selon ses habitudes de jeu.
- **1.3.** **Persistance du tri :** Enregistrement de l'ordre d'affichage dans la base de données locale (`FavoriteSticker.OrderIndex`) appliqué instantanément dans la palette du visualiseur.
- **1.4.** **Ressources Google Play Store :** Intégration des visuels promotionnels officiels (icône haute résolution, image vedette 1024x500 et captures d'écran des 8 fonctionnalités clés).

---

## v2.2.2 — 29/09/2026

### 1. Vélocité de l'index alphabétique et remontée haut de page
- **1.1.** **Navigation instantanée A-Z :** Optimisation du calcul de défilement de l'index alphabétique latéral pour un alignement au pixel près sur la première partition correspondante.
- **1.2.** **Bouton flottant retour en haut (FAB) :** Défilement fluide et immédiat vers le début de la bibliothèque sans latence ni à-coups graphiques.

---

## v2.2.1 — 29/09/2026

### 1. Correctifs de réactivité de l'index alphabétique
- **1.1.** **Suppression des micro-sauts :** Correction du repositionnement instable lors d'un tap rapide sur les lettres extrêmes (A, W, Z).
- **1.2.** **Amélioration du ciblage :** Prise en compte immédiate des partitions accentuées et des caractères spéciaux dans le calcul d'indexation.

---

## v2.2.0 — 29/09/2026

### 1. Fluidité absolue sur bibliothèques volumineuses (300+ partitions)
- **1.1.** **Virtualisation haute performance :** Rendu ultrarapide de la liste des partitions sans ralentissement de l'interface même avec des centaines de partitions enregistrées.
- **1.2.** **Bandeau d'index alphabétique latéral A-Z :** Barre de navigation alphabétique tactile sur le côté droit de l'écran pour atteindre directement la section souhaitée par simple toucher ou glissement.
- **1.3.** **Bouton flottant de retour en haut :** Apparition dynamique d'un bouton flottant élégant dès que la liste est défilée pour remonter instantanément au sommet.
- **1.4.** **Configuration dans les réglages :** Option dédiée dans les paramètres des partitions pour activer ou masquer l'index alphabétique selon les préférences de l'utilisateur.

---

## v2.1.13 — 29/09/2026

### 1. Précision du surlignage et élimination des traces tactiles
- **1.1.** **Tracé du stabilo :** Correction du rendu des traits de surlignage pour préserver la netteté et la transparence optimale au-dessus des portées musicales.
- **1.2.** **Suppression des artefacts tactiles :** Élimination définitive des micro-points résiduels causés par les contacts d'appui court lors de l'activation des outils de dessin.

---

## v2.1.12 — 25/09/2026

### 1. Gestion avancée et tri configurable des Setlists
- **1.1.** **Tri par défaut personnalisable :** Ajout d'une option de tri par défaut des setlists dans les paramètres (par nom alphabétique, par date d'événement ou par date de création).
- **1.2.** **Relégation automatique des setlists terminées :** Option permettant de basculer automatiquement les concerts et répétitions passés en fin de liste pour conserver les programmes à venir au premier plan.
- **1.3.** **Distingo visuel clair :** Mise en valeur des setlists actives et atténuation des programmes archivés.

---

## v2.1.11 — 22/09/2026

### 1. Harmonisation multilingue et stabilité générale
- **1.1.** **Correction des libellés multilingues :** Traduction intégrale des dialogues de sauvegarde, d'échange Wi-Fi Direct, de gestion des sauvegardes et des modules de pédales dans les 8 langues prises en charge.
- **1.2.** **Stabilité du moteur de rendu :** Robustesse renforcée lors du basculement rapide entre différentes partitions.

---

## v2.1.10 — 22/09/2026

### 1. Optimisation mémoire et performances
- **1.1.** **Gestion du cache RAM :** Libération optimisée des images et des flux mémoires lors de longues répétitions ou concerts.
- **1.2.** **Stabilité applicative :** Traitement des exceptions potentielles lors des transitions d'écrans.

---

## v2.1.9 — 22/09/2026

### 1. Internationalisation multilingue intégrale (i18n)
- **1.1.** **8 langues supportées :** Déploiement complet des traductions natives en Français 🇫🇷, Anglais 🇬🇧, Allemand 🇩🇪, Espagnol 🇪🇸, Italien 🇮🇹, Polonais 🇵🇱, Néerlandais 🇳🇱 et Portugais 🇵🇹.
- **1.2.** **Couverture exhaustive :** Traduction intégrale des menus, réglages d'application, catégories de stickers musicaux, dialogues d'importation et page À propos.
- **1.3.** **Application instantanée :** Changement de langue à chaud dans les paramètres avec rafraîchissement immédiat de l'interface utilisateur.

---

## v2.1.8 — 20/09/2026

### 1. Maintien de l'écran allumé pendant le jeu
- **1.1.** **Option « Garder l'écran allumé » (Keep Screen Awake) :** Empêche la mise en veille automatique de la tablette ou de l'écran pendant la consultation d'une partition.
- **1.2.** **Renommage du menu Organisation :** Clarification des intitulés des menus de paramètres pour une navigation plus intuitive.

---

## v2.1.7 — 20/09/2026

### 1. Centralisation de la gestion des étiquettes
- **1.1.** **Module Étiquettes dans Outils :** Regroupement de la création, du renommage, de la colorimétrie et de la suppression des étiquettes dans le menu Outils.
- **1.2.** **Raccourci direct depuis les réglages de partition :** Accès immédiat à la gestion globale des étiquettes sans quitter le contexte de personnalisation de la partition.

---

## v2.1.6 — 19/09/2026

### 1. Calque dynamique d'annotations On/Off
- **1.1.** **Bascule instantanée d'affichage :** Ajout d'une option d'affichage/masquage rapide des annotations dans le menu central du visualiseur pour afficher la partition vierge ou annotée en un clic.
- **1.2.** **Contrôle dans la fiche partition :** Possibilité de choisir si les annotations sont visibles par défaut pour chaque partition individuellement.

---

## v2.1.5 — 18/09/2026

### 1. Pré-roll audio et décompte synchronisé
- **1.1.** **Décompte rythmique de haute précision :** Métronome visuel et sonore synchronisé (1 à 4 mesures paramétrables) avant le départ de la bande audio d'accompagnement.
- **1.2.** **Répétition systématique à la reprise :** Réactivation automatique du pré-roll lors de la reprise après une pause pour un calage musical parfait en répétition.

---

## v2.1.4 — 18/09/2026

### 1. Lecteur audio flottant et repositionnable
- **1.1.** **Mini-lecteur audio flottant :** Lecteur de bande-son d'accompagnement draggable n'obstruant pas la lecture de la partition.
- **1.2.** **Démarrage automatique configurable :** Option permettant de charger et démarrer automatiquement la piste audio dès l'ouverture de la partition.
- **1.3.** **Contrôle réactif dans le menu :** Bascule directe de l'état audio depuis le panneau central.

---

## v2.1.3 — 18/09/2026

### 1. Navigation sécurisée vers l'atelier PDF
- **1.1.** **Élimination des conditions de course :** Routage Shell MAUI fiabilisé lors de l'ouverture de l'assembleur et découpeur PDF depuis le visualiseur et les fiches partitions.

---

## v2.1.2 — 17/09/2026

### 1. Protection anti-tourne rapide et réglages pédales ergonomiques
- **1.1.** **Protection anti-double tourne (Cooldown) :** Temporisation paramétrable (anti-rebond de 200 ms à 1000 ms) empêchant les sauts involontaires de multiples pages lors d'un appui nerveux sur la pédale.
- **1.2.** **Accordéon repliable pour raccourcis :** Interface épurée dans les réglages de pédale avec sections repliables par fonction.

---

## v2.1.1 — 17/09/2026

### 1. Sous-titres dynamiques personnalisables
- **1.1.** **Sous-titres réorganisables par glisser-déposer :** Choix libre des éléments affichés sous le titre de la partition (compositeur, tonalité, tempo, étiquettes, nombre total de pages).
- **1.2.** **Optimisation du mode paysage :** Présentation ajustée des métadonnées pour éviter tout chevauchement en affichage horizontal.

---

## v2.1.0 — 17/09/2026

### 1. Moteur de rendu PDF natif ultra-rapide
- **1.1.** **Moteur natif matériel :** Intégration directe d'Android `PdfRenderer` et de Windows `Windows.Data.Pdf` pour un temps de chargement des pages divisé par trois.
- **1.2.** **Mode double page en paysage :** Affichage simultané de deux pages côte à côte en orientation horizontale avec pagination synchronisée.
- **1.3.** **Mise en cache RAM dynamique :** Préchargement intelligent des pages adjacentes pour une tourne instantanée à 0 ms de délai perçu.

---

## v2.0.4 — 13/09/2026

### 1. Importation d'images sécurisée et améliorations setlists
- **1.1.** **Modale bloquante de conversion d'images en PDF :** Dialogue clair guidant l'utilisateur pour convertir les photos et captures de partitions en PDF avant intégration.
- **1.2.** **Filtrage multi-étiquettes :** Recherche croisée par étiquettes lors de l'ajout de partitions à une setlist.
- **1.3.** **Autorisation des doublons dans une setlist :** Possibilité d'intégrer plusieurs fois le même morceau dans un programme (rappels, interludes).

---

## v2.0.3 — 13/09/2026

### 1. Fiabilisation de la conversion Image en PDF
- **1.1.** **Gestion sécurisée des chemins temporaires :** Résolution des erreurs d'accès lors de la conversion d'images issues d'applications tierces ou de galeries photos.
- **1.2.** **Messages conviviaux :** Reformulation des alertes d'importation avec explications claires et rassurantes.

---

## v2.0.2 — 12/09/2026

### 1. Détection et proposition de conversion automatique d'images
- **1.1.** Détection proactive des formats graphiques (PNG, JPEG, WebP) à l'importation avec proposition immédiate de conversion en document PDF vectoriel standardisé.

---

## v2.0.1 — 11/09/2026

### 1. Améliorations du tiroir de setlist en direct
- **1.1.** **Colorimétrie d'avancement :** Distinction visuelle nette entre les morceaux déjà joués, le morceau actuellement en cours et les morceaux restants à jouer.
- **1.2.** **Recentrage automatique :** Défilement automatique du bandeau pour maintenir la partition en cours au centre de l'écran.
- **1.3.** **Bascule de l'overlay :** Option pour afficher ou masquer le bandeau de progression selon les besoins scéniques.

---

## v2.0.0 — 10/09/2026

### 1. Refonte majeure de l'expérience scénique (Setlist Live Mode)
- **1.1.** **Bandeau déroulant de setlist en direct (Live Progress Drawer) :** Tiroir supérieur rétractable dans le visualiseur permettant de voir l'ensemble du set musical sans interrompre la lecture.
- **1.2.** **Navigation tactile directe :** Saut instantané d'un morceau à l'autre d'un simple toucher sur le bandeau pendant le concert ou la répétition.

---

## v1.9.8.0 — 08/09/2026

### 1. Support complet des Pédales Bluetooth et Contrôleurs MIDI
- **1.1.** **Moniteur d'événements en direct (Live Monitor) :** Affichage en temps réel des frappes de pédalier (touches clavier Bluetooth) et des messages MIDI (Control Change, Program Change, Note On) pour faciliter la configuration.
- **1.2.** **Profils matériels préconfigurés :** Profils prêts à l'emploi pour AirTurn (Duo 500, QUAD 500, PEDpro), PageFlip (Firefly, Butterfly, Dragonfly), Coda STOMP, Donner, Joyo, Harley Benton, iRig BlueTurn et claviers standards.
- **1.3.** **Gestion des profils personnalisés :** Création, duplication, renommage et suppression de profils sur-mesure adaptés à tout matériel.
- **1.4.** **Double action Appui Court / Appui Long :** Configuration indépendante de deux actions distinctes par pédale, avec réglette de sensibilité réglable (200 ms à 1000 ms).
- **1.5.** **Palette complète d'actions musicales :** Tourne de page suivante/précédente, morceau suivant/précédent de setlist, métronome On/Off, lecture/pause audio, zoom 100%, saut direct de page, menu central et verrouillage des annotations.

