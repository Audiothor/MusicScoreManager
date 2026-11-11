# Politique de Confidentialité / Privacy Policy

*Dernière mise à jour : 11 novembre 2026 (v3.2.8) / Last updated: November 11, 2026*

---

## 🇫🇷 Version Française (Conforme RGPD & Google Play)

### 1. Introduction & Responsable du Traitement
L'application **Music Score Manager** (identifiant de package : `com.audiothor.musicscoremanager`), développée par **Audiothor**, est un logiciel libre et open source conçu pour respecter rigoureusement la vie privée de ses utilisateurs. Cette politique de confidentialité explique en toute transparence le fonctionnement de l'application vis-à-vis des données personnelles et des autorisations système.

- **Éditeur / Développeur :** Audiothor
- **Licence :** GNU General Public License v3.0 (GPLv3)
- **Code source officiel :** [https://github.com/Audiothor/MusicScoreManager](https://github.com/Audiothor/MusicScoreManager)
- **Contact & Support :** [https://github.com/Audiothor/MusicScoreManager/issues](https://github.com/Audiothor/MusicScoreManager/issues) ou sur Discord : [https://discord.gg/EtZf8VyMz](https://discord.gg/EtZf8VyMz)

### 2. Collecte et Traitement des Données Personnelles
- **Aucune collecte de données personnelles :** Music Score Manager ne collecte, n'enregistre, ne transmet, ne vend ni ne monétise aucune donnée personnelle ou d'identification (aucun nom, adresse e-mail, numéro de téléphone, localisation GPS, adresse IP, identifiant publicitaire Google AAID/GAID ni empreinte matérielle).
- **Aucun compte utilisateur requis :** L'utilisation de l'application ne nécessite aucune création de compte ni authentification.
- **Stockage 100 % local :** Toutes vos données (partitions PDF, images numérisées, fichiers audio d'accompagnement, métadonnées, annotations manuscrites, étiquettes, listes de morceaux / setlists et sauvegardes complètes) sont stockées exclusivement en local sur votre appareil ou dans les répertoires que vous avez explicitement choisis.
- **Zéro dépendance Cloud :** Aucun fichier ni donnée n'est envoyé vers des serveurs distants.

### 3. Autorisations Requises et Justification Technique
L'application demande uniquement les autorisations matérielles et système strictement indispensables à l'exécution de ses fonctionnalités :
- **Fichiers & Stockage (`MANAGE_EXTERNAL_STORAGE`, `READ_EXTERNAL_STORAGE`, `WRITE_EXTERNAL_STORAGE`)** : Utilisé exclusivement pour vous permettre d'importer, lire et afficher vos partitions (PDF, images), associer vos fichiers audio locaux et créer/restaurer des sauvegardes complètes (.zip).
- **Appareil Photo (`CAMERA`)** : Utilisé **exclusivement et à la demande** par la fonction *Scanner de Partitions Papier* (`ScoreScannerPage`). Il permet aux musiciens de prendre en photo les feuillets physiques d'une partition pour les numériser. Le traitement d'image et la génération PDF s'effectuent **à 100 % localement sur l'appareil**. Aucune photo n'est jamais transmise via Internet.
- **Microphone (`RECORD_AUDIO`)** : Utilisé **exclusivement et à la demande** pour l'outil d'accordeur intégré (*Tuner*). Le flux audio capté par le microphone est analysé en temps réel dans la mémoire vive volatile (RAM) de l'appareil par un algorithme DSP (YIN) pour déterminer la hauteur des notes ($F_0$). **Aucun enregistrement audio n'est conservé, écrit sur le stockage ou téléversé.** L'accès au microphone s'arrête instantanément dès la fermeture de l'accordeur.
- **Réseau Local Wi-Fi & P2P (`INTERNET`, `ACCESS_WIFI_STATE`, `CHANGE_WIFI_STATE`, `ACCESS_NETWORK_STATE`)** : Utilisé **strictement en réseau local** pour l'échange direct de partitions sans fil de tablette à tablette (*Wi-Fi Direct P2P*) et la diffusion par QR Code entre musiciens à proximité. Aucune donnée n'est transmise via Internet.
- **Bluetooth & MIDI (`BLUETOOTH`, `BLUETOOTH_CONNECT`, `BLUETOOTH_SCAN`)** : Utilisé pour la communication avec vos pédaliers tourne-page sans fil et contrôleurs MIDI. Seuls les signaux de commande sont interceptés localement.
- **Aucune géolocalisation** : L'application n'utilise, ne demande et n'a accès à aucune autorisation de localisation (`LOCATION`).

### 4. Services Tiers et Publicités
- **Aucun traceur publicitaire :** L'application est 100 % exempte de publicités.
- **Aucun outil d'analyse ou de télémétrie tiers :** Aucun SDK de tracking ou d'analyse comportementale (aucun Google Analytics, Firebase Analytics, Crashlytics, Facebook SDK, etc.).

### 5. Confidentialité des Enfants (Conformité COPPA & RGPD)
Ne collectant aucune donnée personnelle, Music Score Manager est en totale conformité avec la réglementation COPPA et l'article 8 du RGPD européen. L'application peut être utilisée en toute sécurité par des mineurs de tout âge.

### 6. Droits des Utilisateurs & Suppression des Données
Toutes les données étant conservées localement sur votre terminal, vous disposez d'un contrôle total. La désinstallation de l'application ou l'effacement du stockage de l'application via les paramètres de votre appareil supprime définitivement et immédiatement toutes les données locales associées.

---

## 🇬🇧 English Version (GDPR, CCPA & Google Play Compliant)

### 1. Introduction & Data Controller
**Music Score Manager** (package ID: `com.audiothor.musicscoremanager`), developed by **Audiothor**, is an open-source application built with a strict commitment to user privacy and data sovereignty.

- **Developer / Publisher:** Audiothor
- **License:** GNU General Public License v3.0 (GPLv3)
- **Official Repository:** [https://github.com/Audiothor/MusicScoreManager](https://github.com/Audiothor/MusicScoreManager)
- **Contact & Support:** [https://github.com/Audiothor/MusicScoreManager/issues](https://github.com/Audiothor/MusicScoreManager/issues) or Discord: [https://discord.gg/EtZf8VyMz](https://discord.gg/EtZf8VyMz)

### 2. Collection and Use of Personal Information
- **Zero Personal Data Collection:** Music Score Manager does NOT collect, record, transmit, store, sell, or monetize any personal information or device identifiers (no name, email address, phone number, GPS coordinates, IP address, Google Advertising ID [AAID/GAID], or hardware fingerprints).
- **No User Account Required:** The application requires no account registration, no login credentials, and no remote authentication.
- **100% Local Storage:** All of your sheet music (PDFs, images), audio recordings, annotations, setlists, tags, and database backups remain strictly stored on your local device.
- **Zero Cloud Dependencies:** No files or database entries are ever uploaded to any cloud service or remote server.

### 3. Device Permissions & Technical Usage
The application requests only the permissions strictly necessary for its musical capabilities:
- **Files & Storage (`MANAGE_EXTERNAL_STORAGE`, `READ_EXTERNAL_STORAGE`, `WRITE_EXTERNAL_STORAGE`):** Used strictly to allow you to select, open, render, annotate, and manage your sheet music files and local audio tracks, and to create/restore full backups (.zip).
- **Camera (`CAMERA`):** Used **strictly on-demand** by the *Paper Score Scanner* feature (`ScoreScannerPage`) to photograph paper sheet music. Image processing and PDF creation are performed **100% locally on the device**. No photos or videos are ever uploaded or transmitted across any network.
- **Microphone (`RECORD_AUDIO`):** Used **strictly on-demand** for the built-in Chromatic Tuner. The incoming audio stream is analyzed in real time in volatile system memory (RAM) via a DSP pitch-detection algorithm (YIN) to determine musical pitch ($F_0$). **No audio is ever recorded, written to disk, or uploaded.** Microphone access ceases immediately when the tuner is closed.
- **Local Network & Wi-Fi (`INTERNET`, `ACCESS_WIFI_STATE`, `CHANGE_WIFI_STATE`, `ACCESS_NETWORK_STATE`):** Used **strictly on local networks** for peer-to-peer wireless sharing (Wi-Fi Direct P2P) and QR Code broadcasting between nearby musicians. No data is sent over the internet.
- **Bluetooth & MIDI (`BLUETOOTH`, `BLUETOOTH_CONNECT`, `BLUETOOTH_SCAN`):** Used for wireless Bluetooth page turners and MIDI controllers. Only hardware command signals are intercepted locally.
- **Zero Location Tracking:** The application does not use, request, or require any location (`LOCATION`) permissions.

### 4. Third-Party Services & Advertising
- **No Advertisements:** The application is 100% ad-free.
- **No Third-Party Trackers or Analytics:** The application contains zero analytics or telemetry SDKs (no Google Analytics, Firebase, Crashlytics, Facebook SDK, etc.).

### 5. Children's Privacy (COPPA & GDPR Article 8)
Because the app collects no personal information whatsoever, it is fully compliant with COPPA and European GDPR Article 8. It is completely safe for musicians of all ages, including music school and conservatory students.

### 6. User Rights & Data Deletion
Because all data resides exclusively on your local device, you retain 100% continuous ownership and sovereignty over your data. Clearing app storage in your device settings or uninstalling the app permanently and immediately removes all locally stored data.
