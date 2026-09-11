> [🇫🇷 Version Française](Home) | **🇬🇧 English**

# 🏛️ Welcome to the Music Score Manager Technical Wiki

Welcome to the technical and community engineering hub for **Music Score Manager**.

While the [Official Documentation Website (MkDocs)](https://audiothor.github.io/MusicScoreManager/en/) and the [README.md](https://github.com/Audiothor/MusicScoreManager/blob/main/README.md) provide comprehensive screen-by-screen end-user guidance, this **Wiki** is dedicated to **advanced live stage scenarios**, **in-depth hardware compatibility**, **software internals & protocols**, and the **developer contribution guide**.

---

## 🧭 Wiki Table of Contents

### 1. [🦶 Hardware Compatibility, Bluetooth Pedals & MIDI](Hardware-Compatibility-&-Pedals-EN)
- Complete comparative matrix of field-tested stage pedals (AirTurn, PageFlip, Joyo, Donner, Thomann, iRig, STOMP).
- MIDI Controllers: Control Change mapping (CC 64, 66, 67), MIDI note events, and Program Changes.
- Ergonomic tablet recommendations (screen sizes, 4:3 vs 16:10 ratios) and heavy-duty music stand clamps.

### 2. [🎭 Stage Cookbook & Real-World Scenarios](Stage-Cookbook-&-Scenarios-EN)
- **Choir / Big Band Broadcast Scenario**: Distribute a full concert program with annotations to 20 musicians in 30 seconds via QR Code with zero internet/router.
- **Solo Piano / Organ Recital Scenario**: 2-page landscape reading and strategic blank page insertion to place page turns during rests.
- **Modern Music / Rehearsal Scenario**: Backing audio track playback synchronized with a visual and audible metronome pre-count.
- **Sheet Music Digitization Guide**: Scanner DPI, color vs grayscale, and compression guidelines for lightweight, fast-rendering files.

### 3. [🔬 Software Architecture & Protocols](Architecture-&-Internals-EN)
- SQLite database schema (`scores.db3`) and benefits of WAL (Write-Ahead Logging) mode.
- Standalone container archive specifications (`.msmscore`, `.msmscores`, `.msmsetlist`) and JSON manifest schemas.
- Peer-to-Peer network protocols (UDP discovery beacons, TCP binary stream, local embedded HTTP broadcast server).
- Normalized geometric coordinate engine (0.0 to 1.0) under PDF.js.

### 4. [🛠️ Developer & Contribution Guide](Developer-&-Contribution-Guide-EN)
- Setting up the .NET 10 MAUI, Android SDK 36, and Windows SDK environment.
- Internationalization tutorial: how to add a 9th language via JSON dictionaries without modifying any C# code.
- Build workflow, live USB on-device debugging via ADB, and Release Keystore signing.

### 5. [🚑 Troubleshooting & OS FAQ](Troubleshooting-&-OS-FAQ-EN)
- Android: Neutralizing aggressive battery optimization (*Doze Mode*) that drops Bluetooth pedals mid-song.
- Managing Scoped Storage permissions (Android 11 to 16) to access PDFs over USB from a PC or Mac.
- Full backup and library migration workflow when moving to a new tablet.

---

## ⚡ Learn More

- Source Code: [https://github.com/Audiothor/MusicScoreManager](https://github.com/Audiothor/MusicScoreManager)
- Bilingual Online Docs: [https://audiothor.github.io/MusicScoreManager/en/](https://audiothor.github.io/MusicScoreManager/en/)
- License: **GNU General Public License v3.0 (GPLv3)**
