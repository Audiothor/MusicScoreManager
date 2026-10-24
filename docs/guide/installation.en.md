# 🚀 Chapter 1: App Description, Installation & Prerequisites

## 1.1 General Overview & Philosophy

**Music Score Manager** is a cross-platform application built with **.NET 10 MAUI**, primarily designed for **Android** tablets and smartphones (as well as **Windows** workstations). Built by musicians for musicians, it meets the highest standards of musical practice: sectional rehearsals, personal practice, music lessons, dress rehearsals, and live concert stage performances.

The app completely replaces heavy physical sheet music binders and generic PDF viewers that are ill-suited for the stage:

- **100% Offline & Network-Independent**: The entire score library, database, metadata, annotations, and audio files are stored locally on the device. No internet access is ever required on stage.
- **Instant Responsiveness (< 50 ms)**: Optimized zero-copy architecture with offscreen double-buffered canvas rendering for smooth, stutter-free page turns without latency.
- **Stage-Optimized Dark Mode**: Native dark interface (`#121212`) prevents stage glare and eliminates white flashes when loading scores.
- **Complete Hands-Free Control**: Native support for wireless Bluetooth page-turner pedals (HID and keyboard profiles) as well as MIDI controllers (USB-OTG and Bluetooth MIDI).
- **Professional Annotation Suite**: Stylus, freehand pencil, translucent chiseled highlighters, typed text, and over 100 built-in musical stickers.

---

## 1.2 Top Highlights of Music Score Manager

1. **Secure & Immersive Stage Viewer**: Strict editing lock during performance (`🔒`), screen keep-awake, 2-page landscape view, and night color inversion for pit orchestras.
2. **Built-in Multi-Instrument Chromatic Tuner (YIN Algorithm)**: Floating, draggable stage tuner powered by the benchmark YIN DSP engine ($\theta = 0.15$), 1200 Hz low-pass filter, and harmonic correction for all instruments (guitars, basses, strings, full saxophone family, brass, flute, oboe A 440 Hz reference, vocals).
3. **Physical Folder & Category Architecture**: Dual display mode (List View ⇄ Folders View), automatic disk-level sub-directory sync, and 1-tap bulk category assignment.
4. **High-Definition Paper Score Scanner**: Direct music stand digitization with camera or gallery import, high-contrast B&W enhancement filters, and immediate multi-page PDF generation.
5. **Powerful Setlist Management with Live Progress Drawer**: Automatic seamless transition between songs, 1-tap duplication (`⧉`), dedicated Bluetooth pedal profiles per setlist, and collapsible progress drawer.
6. **P2P Wireless Sharing via Wi-Fi Direct & Hotspot (No Internet)**: Direct tablet-to-tablet transfers in backstage without a router, transferring complete packages including annotations and audio files, with Leader QR Code mode.
7. **Universal Bluetooth Page-Turner Pedal & MIDI Support**: Compatible with all major market pedals (AirTurn, PageFlip, Donner, Joyo, USB/Bluetooth MIDI controllers...) with auto-learn and anti-double-jump debounce protection.
8. **Musical Annotation Suite & 100+ Stickers**: Translucent chiseled highlighter, fine/opaque pencil, free text, tactile size slider, 100+ categorized musical symbols, and unlimited Undo/Redo.
9. **Zero-Drift High-Precision Metronome**: Real-time thread-safe regulated clock (`Stopwatch` + `SpinWait`), low-latency Android audio (`SoundPool`), synchronized LED pulse, and on-the-fly sound mute.
10. **Synchronized Audio Backing Track Player & PDF Assembler Studio**: Link accompaniment audio tracks (MP3, WAV, AAC, FLAC), and built-in studio to reorder, rotate, clone, or insert blank pages in your PDFs.
11. **Dynamic Categorization & Multi-Tag Filtering**: Customizable colored tags, instant title/composer search, and multi-tag filtering with fast category switching.

---

## 1.3 System & Hardware Requirements

| Component | Minimum Requirement | Recommended for Stage |
| :--- | :--- | :--- |
| **Operating System** | **Android 12.0** (API 31) or higher | **Android 13 / 14 / 15 / 16** (Target SDK 36) |
| **Desktop Alternative** | **Windows 10** (build 19041+) | **Windows 11** with touch screen or stylus |
| **Display** | 8-inch touch screen | **10.5 to 13.3-inch tablet** (high-res, 4:3 or 16:10 ratio) |
| **Storage** | 200 MB free space for the app | 4 GB+ depending on your PDF and audio library size |
| **RAM** | 3 GB RAM | 4 GB to 8 GB RAM |
| **Pedal Controllers** | Touch screen | **Bluetooth HID Pedal** (AirTurn, PageFlip, Donner...) or **MIDI Controller** |
| **Network** | No internet connection required | Active Wi-Fi adapter only for direct P2P sharing between musicians |

---

## 1.4 Installation & Deployment

### 📱 For Android

#### Method 1: Installing via the Google Play Store (Recommended)

This is the easiest, fastest, and most secure way to install and keep **Music Score Manager** up to date:

1. Open the **Google Play Store** on your Android tablet or smartphone.
2. Search for **Music Score Manager** (by *Audiothor*) or click the direct store link:  
   👉 [**Music Score Manager on Google Play Store**](https://play.google.com/store/apps/details?id=com.audiothor.musicscoremanager)
3. Tap **Install**.
4. The application will be installed within seconds and will automatically receive future official updates.

#### Method 2: Manual Installation via APK Package (Offline / Without Google Play)

For musicians without Google Play Services or those looking for an autonomous offline install:

1. Download the latest signed APK file (`MusicScoreManager-vX.Y.Z.apk`) from official [GitHub Releases](https://github.com/Audiothor/MusicScoreManager/releases).
2. On your Android device, allow installing apps from unknown sources for your browser or file manager (*Settings > Security > Install unknown apps*).
3. Open the downloaded APK file and confirm installation.
4. On first launch, grant the required permissions for media storage, Bluetooth (for page turners), and local device discovery (for Wi-Fi Direct).

---

### 💻 For Windows (PC, Laptops & Surface Tablets)

**Music Score Manager** runs natively on **Windows 10** (build 19041+) and **Windows 11** with full support for touch screens, styluses, mouse, and keyboard shortcuts:

#### Method 1: Direct Download of the Windows Version

1. Go to [GitHub Releases](https://github.com/Audiothor/MusicScoreManager/releases) and download the latest Windows package (e.g., `MusicScoreManager-Windows-vX.Y.Z.zip` or the installer).
2. Extract the ZIP archive to your preferred directory (e.g., `C:\Program Files\MusicScoreManager` or your user folder).
3. Double-click `MusicScoreManager.exe` to launch the application.  
   *(Optional: right-click the executable to create a Desktop shortcut or pin it to your Taskbar).*
4. **Windows SmartScreen Note**: If a blue screen appears stating "Windows protected your PC", click **"More info"** and then **"Run anyway"**.

---

### 🛠️ For Developers: Building from Source (.NET MAUI)

For developers who want to compile and customize the application for Android or Windows:

1. Install the **.NET 10 SDK** and .NET MAUI workloads:
   ```powershell
   dotnet workload install maui
   dotnet workload install maui-android
   dotnet workload install maui-windows
   ```
2. Clone the official Git repository:
   ```powershell
   git clone https://github.com/Audiothor/MusicScoreManager.git
   cd MusicScoreManager
   ```
3. Build according to the desired target:
   ```powershell
   # For Android: deploy directly to USB-connected Android tablet
   dotnet build -t:Run -f net10.0-android

   # For Android: generate signed Release APK package
   dotnet publish -f net10.0-android -c Release

   # For Windows: compile Windows executable
   dotnet build -f net10.0-windows10.0.19041.0 -c Release

   # For Windows: publish standalone binary
   dotnet publish -f net10.0-windows10.0.19041.0 -c Release
   ```
