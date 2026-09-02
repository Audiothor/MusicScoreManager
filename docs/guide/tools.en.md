# 🛠️ Tools Menu

The **Tools** menu hosts Music Score Manager's powerful suite of built-in utilities designed to manipulate, share, clean up, and safeguard your music catalog completely offline.

---

## 📑 1. PDF Creator & Assembler Studio

The **PDF Assembler Studio** is an all-in-one document manipulation studio built right into the app.

### Key Capabilities:
- **Build PDFs from Photos or Scans**:
  - Select photos of sheet music from your gallery or camera (`.jpg`, `.png`).
  - Compiles them into a crisp, high-resolution PDF document preserving 100% of their aspect ratio.
- **Interactive Page Thumbnails**:
  - View miniature previews of every page with its sequence index number.
- **Rearrange Pages**:
  - Use **Move Up (▲)** and **Move Down (▼)** buttons to reorder pages intuitively.
- **Individual Page Rotations**:
  - Tap **↻ Rotate** on any specific page thumbnail to turn it in 90° steps (0°, 90°, 180°, 270°). Perfect for correcting sideways scans.
- **Duplicate Pages**:
  - Clone any page with a single tap (ideal for repeated chorus sections or Da Capo jumps without turning back).
- **Delete Pages (🗑️)**:
  - Remove cover pages, blank sheets, or unwanted download advertisements.
- **Reverse All Pages**:
  - Quickly fixes sheet music scanned in backwards order.
- **Save as New or Replace**:
  - Save as a brand new score or overwrite the original PDF directly in your library.

---

## 📡 2. Wi-Fi Direct Transfer (P2P) & Group Broadcast

The **Wi-Fi Direct Transfer** module powers direct wireless sharing between devices **with no external internet, router, or mobile data required**.

### A. Direct Tablet-to-Tablet Transfer (P2P):
1. **Receiver**:
   - Opens **Tools > Wi-Fi Direct Transfer**, enables Wi-Fi, and taps **Receive**. The device enters discoverable mode.
2. **Sender**:
   - Picks a score or setlist to share.
   - Selects whether to bundle personal annotations and audio tracks.
   - Selects the target recipient from the discovered devices list. The binary stream transfers in seconds over local TCP sockets.

### B. Multi-Musician Group Broadcast (QR Code):
1. **The Sender**:
   - Switches to **Group Broadcast Mode**. The app initiates a secure local hotspot with an embedded HTTP server and displays a **high-resolution QR Code** on screen.
2. **Group Musicians**:
   - Open their camera or the app's **Tools > Receive** screen to scan the QR Code.
   - All band members download the score or complete setlist simultaneously!

---

## 🏷️ 3. Tag Management

The **Tag Management** screen gives you full control over library categorization:
- **Create New Tags** with custom names (e.g., *Choir*, *Sight Reading*, *Christmas Concert*, *Lead Sheet*).
- **Vibrant Color Palette**: Assign a distinct color badge to each tag for instant visual spotting in score lists.
- **Edit & Rename** existing tags.
- **Delete Tags**: Safely removes tags without affecting any underlying sheet music files.

---

## 📦 4. Package & Setlist Imports

The **Package Import** tool handles external archives shared by colleagues:
- **Supported Formats**: `.msmsetlist` (complete setlist), `.msmscore` (individual score with annotations/audio), and `.msmscores` (batch archive of multiple pieces).
- **Automated Extraction**: Extracts PDFs, recreates musical metadata (composer, key, tempo, rating), restores drawings/annotations, and attaches audio files automatically.

---

## 🔍 5. Duplicate Finder

The **Duplicate Finder** audits your library storage to free up disk space:
- **SHA-256 Cryptographic Hash Check**: Compares the exact binary contents of PDF files, catching duplicates even if filenames are different.
- **Side-by-Side Duplicate Groups**: Displays matching files with their file sizes, dates, and paths.
- **Safe Cleaning**: Delete redundant copies while keeping the original library entry intact.

---

## 💾 6. Database Backups & Restoration

The **Backups** utility ensures your annotations and organization are never lost:
- **Instant Manual Snapshot**: Create a dated backup of your SQLite database (scores, setlists, tags, links, annotations, settings).
- **Backup History**: View all restore points saved on device storage with exact timestamps and file sizes.
- **One-Click Restoration**: Revert back to any previous state in case of a mistake or device replacement.
- **External Export**: Copy backup archives to an external USB thumb drive, micro-SD card, or cloud folder.
