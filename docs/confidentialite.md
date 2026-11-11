# Politique de Confidentialité / Privacy Policy

*Dernière mise à jour : 11 novembre 2026 (v3.2.8)*

---

## 🇫🇷 Engagement de Confidentialité (RGPD & Google Play)

L'application **Music Score Manager** (identifiant de package : `com.audiothor.musicscoremanager`), développée par **Audiothor**, est un projet open-source conçu avec une exigence absolue de respect de la vie privée et de protection des données personnelles.

La présente politique de confidentialité décrit en toute transparence les principes appliqués à vos données ainsi que la justification technique rigoureuse de chaque autorisation système requise.

---

### 1. Responsable du Traitement & Contact

- **Éditeur / Développeur :** Audiothor (Développeur indépendant de logiciel libre)
- **Licence :** GNU General Public License v3.0 (GPLv3)
- **Code source officiel :** [https://github.com/Audiothor/MusicScoreManager](https://github.com/Audiothor/MusicScoreManager)
- **Contact & Support :**
  - Fil d'assistance GitHub : [https://github.com/Audiothor/MusicScoreManager/issues](https://github.com/Audiothor/MusicScoreManager/issues)
  - Communauté Discord : [https://discord.gg/EtZf8VyMz](https://discord.gg/EtZf8VyMz)

---

### 2. Collecte et Traitement des Données Personnelles

- **Zéro collecte de données personnelles :** Music Score Manager ne collecte, n'enregistre, ne transmet, ne vend, ne loue ni ne monétise **aucune donnée personnelle ou directement identifiable** (aucun nom, adresse e-mail, numéro de téléphone, localisation géographique, adresse IP, identifiant publicitaire Google AAID/GAID ni empreinte matérielle).
- **Aucun compte utilisateur ni inscription :** L'application ne requiert aucune création de compte, aucun mot de passe ni aucune authentification distante. Toutes les fonctionnalités sont immédiatement et intégralement accessibles hors-ligne dès l'installation.
- **Stockage 100 % local :** L'intégralité de vos contenus musicaux (partitions PDF, images numérisées, fichiers audio d'accompagnement, métadonnées, annotations manuscrites, surlignages, stickers musicaux, listes de concert / setlists, étiquettes et sauvegardes de base de données) est stockée **exclusivement et localement sur votre appareil** ou sur le support de stockage externe (clé USB, carte micro-SD) que vous avez expressément choisi.
- **Zéro dépendance Cloud :** Aucun fichier ni donnée n'est téléversé vers un serveur distant, un service d'hébergement ou un cloud tiers.

---

### 3. Autorisations Requises & Justification Technique

Music Score Manager demande uniquement les autorisations matérielles et système strictement indispensables à l'exécution de ses fonctionnalités musicales :

#### A. Fichiers, Médias & Stockage (`MANAGE_EXTERNAL_STORAGE`, `READ_EXTERNAL_STORAGE`, `WRITE_EXTERNAL_STORAGE`)
- **Finalité :** Permet à l'application de parcourir vos dossiers pour importer vos partitions (documents PDF, images), associer des fichiers audio d'accompagnement locaux (MP3, WAV, AAC, FLAC), sauvegarder vos annotations et exporter ou restaurer vos archives de sauvegarde complètes (`.zip`, `.msmscore`, `.msmsetlist`).
- **Protection :** L'accès aux fichiers est cantonné aux opérations musicales demandées par l'utilisateur. Aucun fichier n'est transmis en dehors de l'appareil.

#### B. Appareil Photo (`CAMERA`)
- **Finalité :** Utilisé **exclusivement et à la demande** par la fonction *Scanner de Partitions Papier* (`ScoreScannerPage`). Il permet aux musiciens de prendre en photo les feuillets physiques d'une partition directement depuis leur pupitre pour les numériser.
- **Protection :** Le traitement de l'image (recadrage, rehaussement de contraste, seuillage noir et blanc, génération du document PDF) s'effectue **à 100 % localement en mémoire** via la bibliothèque `SixLabors.ImageSharp`. Aucune photo ni aucun flux vidéo n'est jamais transmis, enregistré à l'insu de l'utilisateur ou transféré via Internet.

#### C. Microphone (`RECORD_AUDIO`)
- **Finalité :** Utilisé **exclusivement et à la demande** par l'outil d'accordeur chromatique intégré (*Tuner* - `TunerService`). Il permet de capter en direct le son produit par un instrument acoustique (guitare, cordes, bois, cuivres, chant).
- **Protection :** Le flux sonore capté est analysé en temps réel dans la mémoire vive volatile (RAM) de l'appareil par un algorithme DSP de détection de hauteur (algorithme YIN, $\theta = 0.15$) pour calculer la fréquence en Hertz et l'écart en cents. **Aucun enregistrement audio n'est conservé, écrit sur le disque, archivé, transmis ou téléversé.** L'écoute microphone s'arrête instantanément dès la fermeture de la boîte de l'accordeur ou la coupure manuelle du micro.

#### D. Réseau Local Wi-Fi & P2P (`INTERNET`, `ACCESS_WIFI_STATE`, `CHANGE_WIFI_STATE`, `ACCESS_NETWORK_STATE`)
- **Finalité :** Utilisé **strictement et exclusivement en réseau local direct** pour la fonctionnalité d'échange de partitions sans fil de tablette à tablette (*Wi-Fi Direct P2P*) et la diffusion groupée par QR Code entre musiciens à proximité (en répétition ou en coulisses).
- **Protection :** Ces échanges fonctionnent de pair à pair via des sockets TCP ou un serveur HTTP local embarqué temporaire, **sans nécessiter de connexion Internet**. L'application n'établit aucune connexion avec des serveurs externes.

#### E. Bluetooth & Contrôleurs MIDI (`BLUETOOTH`, `BLUETOOTH_CONNECT`, `BLUETOOTH_SCAN`)
- **Finalité :** Permet la détection et la communication avec vos pédaliers sans fil tourne-page Bluetooth (profils HID et touches clavier) ainsi que vos contrôleurs MIDI physiques (USB-OTG et Bluetooth BLE).
- **Protection :** Seuls les signaux d'événements de touches et messages de contrôle MIDI (CC, Notes, PC) sont interceptés localement pour commander la tourne de page ou le métronome. Aucune donnée relative à vos périphériques n'est transmise.

#### F. Absence Totale de Géolocalisation
- L'application n'utilise, ne demande et n'a accès à **aucune autorisation de localisation géographique** (`ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION`).

---

### 4. Publicités, Traceurs & Services Tiers

- **100 % Exempt de Publicité :** L'application ne contient aucune bannière publicitaire, aucun interstitiel, aucune vidéo promotionnelle ni aucun lien affilié.
- **Zéro Télémétrie & Zéro Outil d'Analyse :** Aucun kit de développement (SDK) de suivi comportemental, d'analyse statistique ou de profilage utilisateur n'est présent dans le code source (absence totale de Google Analytics, Firebase Analytics, Crashlytics, Facebook SDK, Adjust, AppsFlyer, etc.).
- **Zéro Partage Commercial :** Aucune donnée n'est vendue, louée, partagée ou cédée à des courtiers de données (*data brokers*), des régies publicitaires ou des entreprises tierces.

---

### 5. Protection de l'Enfance (Conformité COPPA & RGPD Mineurs)

Music Score Manager s'adresse à un public de musiciens de tous âges, y compris les élèves d'écoles de musique et de conservatoires.
Ne collectant aucune donnée personnelle, l'application est en totale conformité avec la réglementation américaine **COPPA** (*Children's Online Privacy Protection Act*) et l'article 8 du **RGPD** européen. Elle peut être utilisée en toute sécurité par des mineurs sans nécessiter de consentement parental préalable pour la collecte de données, celle-ci étant inexistante.

---

### 6. Souveraineté, Droits des Utilisateurs & Suppression des Données

Conformément au **Règlement Général sur la Protection des Données (RGPD - Règlement UE 2016/679)** et aux législations internationales équivalentes (notamment le *California Consumer Privacy Act* - CCPA/CPRA) :
- **Droit d'accès et de portabilité :** Vos données vous appartiennent intégralement. Vous pouvez exporter à tout moment vos partitions, annotations, setlists et sauvegardes complètes au format standard ouvert `.zip` ou `.pdf`.
- **Droit à l'effacement :** Toutes les données étant stockées localement sur votre terminal, la suppression d'une partition, d'une setlist ou d'une sauvegarde dans l'application entraîne sa suppression physique immédiate. L'effacement du stockage de l'application dans les paramètres de votre appareil ou la désinstallation de l'application détruit instantanément et définitivement l'ensemble des données associées.

---

### 7. Modifications de la Politique de Confidentialité

Si des évolutions techniques ou réglementaires futures devaient impacter cette politique, toute modification sera consignée avec une date de révision mise à jour dans ce document, sur le dépôt GitHub officiel et dans les notes de version de l'application.

---

### 8. Questions & Contact

Pour toute demande, remarque ou question relative à la confidentialité de l'application :
- **Dépôt officiel GitHub :** [https://github.com/Audiothor/MusicScoreManager](https://github.com/Audiothor/MusicScoreManager)
- **Signalement d'une issue :** [https://github.com/Audiothor/MusicScoreManager/issues](https://github.com/Audiothor/MusicScoreManager/issues)
- **Serveur Discord officiel :** [https://discord.gg/EtZf8VyMz](https://discord.gg/EtZf8VyMz)
