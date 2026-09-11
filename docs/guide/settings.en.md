# ⚙️ Settings Menu

The **Settings** menu ([`SettingsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsPage.xaml)) allows you to precisely configure Music Score Manager for your instruments, stage performance habits, and display size.

---

## 🎵 1. Score Settings

The [`SettingsScoresPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsScoresPage.xaml) page configures global sheet music library behaviors:

- **Default Sort on Launch**:
  - Set which order applies automatically (*Date added newest/oldest, Title A-Z/Z-A, Modification date, Star rating, Composer A-Z, or Untagged first*).
- **Scores Without Composer First (Toggle On/Off)**:
  - When sorting by composer, choose whether pieces lacking a composer appear at the very top (On) or at the end of the alphabetical list (Off, default).
- **Score Subtitle Display**:
  - Choose which details appear beneath each title (*Date added*, *Composer only*, or *Composer and date added*).
- **Current Page Number Display (Toggle On/Off)**:
  - Show or hide the bottom-right page index badge (e.g., *Page 3 / 12*).
- **2-Page Landscape Display (Toggle On/Off)**:
  - Enables side-by-side dual page reading on horizontal tablets with complete geometric annotation projection.
- **Page Number Font Size**:
  - Adjustable slider from **10 px to 40 px** with live text size preview.
- **Ergonomic Page Turn Gestures**:
  - *Next page gesture*: Swipe left, Tap right, or Swipe up.
  - *Previous page gesture*: Swipe right, Tap left, or Swipe down.

---

## 📋 2. Setlist Settings

The [`SettingsSetlistsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsSetlistsPage.xaml) page governs live concert behaviors:

- **Continuous Reading by Default (Toggle On/Off)**:
  - Automatically loads the next piece in the setlist when turning past the final page of a score.
- **Return to Setlist on Score End**:
  - If continuous reading is disabled, turning past the last page exits back to the setlist view.
- **Show Live Setlist Progress (Toggle On/Off)**:
  - Enables or disables opening the top progress drawer when tapping the top-center edge of the screen (enabled by default).

---

## 🏷️ 3. Tag Settings

- Direct access to the tag manager ([`TagsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/TagsPage.xaml)), RGB color editor, and category classifications.

---

## ✍️ 4. Annotation Settings

The [`SettingsAnnotationsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsAnnotationsPage.xaml) page is divided into two distinct chapters:

- **Chapter 1: Custom Favorite Stickers**:
  - Add customized text stickers with any label (e.g., *Vibrato*, *Solo*, *Mute*, *Breathe*, *Look at Conductor*).
  - List of active favorites with single-tap deletion `✕`.
- **Chapter 2: Active Sticker Categories Selection**:
  - Check or uncheck categories to display only the stickers relevant to your playing (*Favorites, Fingerings, Dynamics, Articulations, Repeats, Notes, Rests, Accidentals, Symbols*).

---

## 🦶 5. Bluetooth Pedals & MIDI Controller Settings

The [`SettingsPedalsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsPedalsPage.xaml) page delivers complete hands-free stage control:

- **Master Toggle & Live Signal Monitor**:
  - Real-time listening indicator (🟢).
  - Live diagnostic card displaying key name, hex code, event source (Bluetooth HID or MIDI), *« ⏱️ Long Press »* badge, and triggered action.
- **Factory Pre-Configured Profiles**:
  - *Standard (Keyboard arrows/PageUp/Down)*, *PageFlip Dragonfly (4 pedals)*, *PageFlip Firefly & Butterfly*, *AirTurn Duo 500 & PEDpro*, *AirTurn Quad 500*, *Joyo JSP-01*, *Thomann / Harley Benton PageTurn*, *Donner Wireless*, *IK Multimedia iRig BlueTurn*, *Coda STOMP*, and *Advanced MIDI Controller (USB / Bluetooth: CC 64 Sustain, CC 66 Sostenuto, CC 67 Soft, Notes C1-F1, Program Change)*.
- **Custom Profiles & Learn Mode**:
  - Create new custom profiles, duplicate, rename, or reset to factory defaults.
  - **Auto-Learn Mode**: Click learn and simply press the pedal or MIDI button to automatically map the incoming command.
- **Assignable Stage Actions (Short & Long Press)**:
  - Next / previous page, next / previous song in setlist, start / end of piece, scroll up / down, metronome start/stop/mute, audio play/pause/restart, reset zoom 100%, direct page jump, lock/unlock annotations, undo/redo, central menu, exit viewer.
- **Long-Press Sensitivity**:
  - Fine-grained slider from 200 ms to 1000 ms to eliminate accidental triggers on stage.

---

## 🌐 6. App Settings (General)

The [`SettingsAppPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SettingsAppPage.xaml) page handles system settings:

- **🌐 Multi-Language Selector**:
  - Interactive modal with **8 built-in languages**:
    * 🇫🇷 **Français**, 🇬🇧 **English**, 🇩🇪 **Deutsch**, 🇪🇸 **Español**, 🇮🇹 **Italiano**, 🇵🇱 **Polski**, 🇳🇱 **Nederlands**, 🇵🇹 **Português**.
  - Immediate dynamic translation with no app restart needed.
- **📊 Library Metrics & Storage Statistics**:
  - Total number of registered PDF scores.
  - Total disk space occupied by sheet music PDFs (in MB/GB).
  - **Available storage space**: Remaining free capacity on device memory or SD card.
  - **SQLite Database Size**: Space used by `scores.db3` and WAL transaction logs.
- **📁 Customizable Storage Folders**:
  - Scores root folder, audio root folder, and exports root folder.
  - Fully compatible with public Android folders for easy USB file transfer with a PC or Mac.

---

## ❓ 7. Built-in Help

The [`HelpPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/HelpPage.xaml) page includes offline guides and stage troubleshooting tips.

---

## ℹ️ 8. About

The [`AboutPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/AboutPage.xaml) page provides software credits and legal licenses:
- Dynamic build version string.
- Free software license **GNU General Public License v3.0 (GPLv3)**.
- Third-party open-source component credits (.NET MAUI, CommunityToolkit, SQLite, Mozilla PDF.js, SkiaSharp, ZXing).
- Direct links to GitHub repository and Privacy Policy.
