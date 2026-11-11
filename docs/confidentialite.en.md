# Privacy Policy

*Last updated: November 11, 2026 (v3.2.8)*

---

## 🔒 Privacy Commitment (GDPR, CCPA & Google Play Compliant)

**Music Score Manager** (application package ID: `com.audiothor.musicscoremanager`), developed by **Audiothor**, is an open-source project engineered with an absolute commitment to user privacy and data sovereignty.

This Privacy Policy explains in complete transparency our principles regarding your data, along with a comprehensive and accurate technical justification for every device permission requested by the application.

---

### 1. Data Controller & Contact Information

- **Developer / Publisher:** Audiothor (Independent Open-Source Developer)
- **License:** GNU General Public License v3.0 (GPLv3)
- **Official Source Code:** [https://github.com/Audiothor/MusicScoreManager](https://github.com/Audiothor/MusicScoreManager)
- **Contact & Support:**
  - GitHub Issue Tracker: [https://github.com/Audiothor/MusicScoreManager/issues](https://github.com/Audiothor/MusicScoreManager/issues)
  - Discord Community: [https://discord.gg/EtZf8VyMz](https://discord.gg/EtZf8VyMz)

---

### 2. Personal Data Collection & Processing

- **Zero Personal Data Collection:** Music Score Manager does NOT collect, record, transmit, store, sell, lease, or monetize **any personal or personally identifiable information (PII)** (no names, email addresses, phone numbers, GPS coordinates, IP addresses, Google Advertising IDs [AAID/GAID], or device hardware fingerprints).
- **No User Account Required:** The application requires no account registration, no login credentials, and no remote authentication. All features are fully functional offline immediately upon installation.
- **100% Local Storage:** All of your musical assets (PDF sheet music files, scanned images, audio backing tracks, musical metadata, handwritten drawings, highlights, musical stickers, concert setlists, tags, and database backups) remain **strictly and exclusively stored on your local device** (or removable media like SD cards or USB flash drives that you explicitly select).
- **Zero Cloud Dependencies:** No files or database entries are ever uploaded to any cloud service, remote server, or third-party host.

---

### 3. Device Permissions & Technical Justifications

Music Score Manager requests only the hardware and operating system permissions strictly necessary to perform its musical functions:

#### A. Files, Media & Storage (`MANAGE_EXTERNAL_STORAGE`, `READ_EXTERNAL_STORAGE`, `WRITE_EXTERNAL_STORAGE`)
- **Purpose:** Allows the application to browse your device directories to import sheet music (PDFs, images), link local audio accompaniment tracks (MP3, WAV, AAC, FLAC), save vector annotations, and create or restore full data backup archives (`.zip`, `.msmscore`, `.msmsetlist`).
- **Privacy Safeguard:** File access is strictly confined to user-initiated music file operations. No files are ever sent outside the device.

#### B. Camera (`CAMERA`)
- **Purpose:** Used **exclusively on-demand** by the *Paper Score Scanner* feature (`ScoreScannerPage`). It allows musicians to take photos of physical paper sheet music directly from their music stand to digitize them.
- **Privacy Safeguard:** Image processing (cropping, contrast enhancement, adaptive black-and-white thresholding, and PDF generation) is executed **100% locally in device memory** using the `SixLabors.ImageSharp` library. No photos or video frames are ever recorded without user interaction, transmitted off the device, or uploaded over the internet.

#### C. Microphone (`RECORD_AUDIO`)
- **Purpose:** Used **exclusively on-demand** by the integrated Chromatic Tuner tool (*Tuner* — `TunerService`). It captures live acoustic instrument audio (guitar, strings, woodwinds, brass, vocals).
- **Privacy Safeguard:** The incoming audio stream is analyzed in real time in volatile system memory (RAM) via a DSP pitch-detection algorithm (YIN algorithm, $\theta = 0.15$) to compute frequency in Hertz and pitch deviation in cents. **No audio is ever recorded, written to disk, archived, transmitted, or uploaded.** Microphone listening terminates immediately when the tuner panel is closed or when the microphone button is toggled off.

#### D. Local Network & Wi-Fi (`INTERNET`, `ACCESS_WIFI_STATE`, `CHANGE_WIFI_STATE`, `ACCESS_NETWORK_STATE`)
- **Purpose:** Used **strictly and exclusively on the local network** for direct tablet-to-tablet wireless sheet music exchange (*Wi-Fi Direct P2P*) and QR Code multi-musician group broadcasting in rehearsal rooms or backstage.
- **Privacy Safeguard:** Transfers occur directly between peers via local TCP sockets or an embedded ad-hoc local HTTP server, **with zero internet connection required**. The app never connects to external web servers or cloud endpoints.

#### E. Bluetooth & MIDI Controllers (`BLUETOOTH`, `BLUETOOTH_CONNECT`, `BLUETOOTH_SCAN`)
- **Purpose:** Enables discovery and communication with wireless Bluetooth page-turner foot pedals (HID keyboard profiles) and hardware MIDI controllers (USB-OTG and Bluetooth MIDI).
- **Privacy Safeguard:** Only keystroke events and MIDI control messages (CC, Note, PC) are intercepted locally to trigger page turns or metronome controls. No peripheral information or telemetry is collected or sent.

#### F. Zero Location Tracking
- The application does not request, require, access, or process **any geographic location data** (`ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION`).

---

### 4. Advertisements, Trackers & Third-Party SDKs

- **100% Ad-Free:** The application contains zero banner ads, interstitials, promotional videos, or affiliate links.
- **Zero Telemetry & Analytics:** No analytics, crash telemetry, or user profiling SDKs are embedded in the codebase (complete absence of Google Analytics, Firebase Analytics, Crashlytics, Facebook SDK, Adjust, AppsFlyer, etc.).
- **Zero Commercial Sharing:** No data is sold, leased, shared, or disclosed to data brokers, advertising networks, or commercial third parties.

---

### 5. Children's Privacy (COPPA & GDPR Article 8 Compliance)

Music Score Manager is designed for musicians of all ages, including music school and conservatory students.
Because the application collects no personal information whatsoever, it is fully compliant with the United States **COPPA** (*Children's Online Privacy Protection Act*) and Article 8 of the European **GDPR**. It can be safely used by children and minors without requiring prior parental consent, as no personal data is collected or processed.

---

### 6. User Sovereignty, Rights & Data Deletion

In accordance with the **General Data Protection Regulation (GDPR — Regulation EU 2016/679)** and international privacy laws (including the *California Consumer Privacy Act* — CCPA/CPRA):
- **Right to Access & Portability:** You maintain 100% continuous ownership over your data. You can export your sheet music, annotations, setlists, and complete backups at any time in standard open formats (`.zip`, `.pdf`).
- **Right to Erasure:** Because all data is stored locally on your device, deleting a score, setlist, or backup from the app permanently and immediately removes it from storage. Clearing the application's storage in your device settings or uninstalling the app permanently deletes all associated application data.

---

### 7. Policy Updates

Should technical or regulatory requirements necessitate changes to this policy, any updates will be documented with a revised effective date in this document, on the official GitHub repository, and in the application release notes.

---

### 8. Questions & Contact

For any privacy-related inquiries, questions, or feedback:
- **Official GitHub Repository:** [https://github.com/Audiothor/MusicScoreManager](https://github.com/Audiothor/MusicScoreManager)
- **Submit an Issue:** [https://github.com/Audiothor/MusicScoreManager/issues](https://github.com/Audiothor/MusicScoreManager/issues)
- **Official Discord Community:** [https://discord.gg/EtZf8VyMz](https://discord.gg/EtZf8VyMz)
