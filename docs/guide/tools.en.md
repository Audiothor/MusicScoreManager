# 🛠️ Tools Menu

The **Tools** menu hosts Music Score Manager's powerful suite of built-in utilities designed to manipulate, share, clean up, and safeguard your music catalog completely offline.

---

## 📸 1. Score Scanner Studio (High-Definition Paper Scanning)

The **Score Scanner** ([`ScoreScannerPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ScoreScannerPage.xaml)) digitizes physical sheet music directly from your music stand without any external hardware:

- **Instant Camera Capture & Gallery Import**:
  - Snap photos of your paper scores directly using your tablet or phone camera (`Take Photo`), or import previously captured images (`Pick Image`).
- **High-Visibility Enhancement Filters**:
  - **B&W High Contrast (Optimized Sheet Music)**: Applies smart adaptive thresholding to remove stand shadows, whiten the paper background, and make staves and musical notes razor-sharp.
  - **Grayscale**: Smooths tonal gradients while maintaining a compact file size.
  - **Original Color**: Preserves the natural warm paper color and handwritten pencil or highlighter annotations.
- **Musical Metadata & Folder / Category Assignment**:
  - Fill in the **Score Title** and **Composer / Arranger** right away.
  - **Folder / Category Selection**: Pick the target folder (defaults to "Général") or tap `➕` to create a new folder on the fly.
- **Dynamic Multi-Page Management**:
  - Thumbnail preview of every page with index counters.
  - Individual 90° rotation (`↻`) and targeted deletion (`🗑️`).
  - `Delete All` button to reset the current scanning session if needed.
- **Instant Multi-Page PDF Generation**:
  - Tap **Generate Score** (available at the top header and bottom of the page) to compile all processed pages into a multi-page PDF, register it in your library, and open it immediately in the performance viewer!

---

## 📑 2. PDF Creator & Assembler Studio

The **PDF Assembler Studio** ([`PdfAssemblerPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/PdfAssemblerPage.xaml)) is an all-in-one document manipulation studio built right into the app.

### Key Capabilities:
- **Build PDFs from Photos or Scans**:
  - Select photos of sheet music from your gallery or camera (`.jpg`, `.png`).
  - Compiles them into a crisp, high-resolution PDF document preserving 100% of their aspect ratio.
- **Modify Existing Scores or External PDFs**:
  - Load any piece from your library or any external PDF document directly into the studio.
- **Merge Additional Sheets or Photos**:
  - Insert additional photos (`➕ Photos`) or merge pages from another PDF file (`➕ PDF`).
- **Insert Blank Pages (`📄 Blank Page`)**:
  - Strategic feature to synchronize page turns on physical music stands and dual-page landscape views.
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
- **Rotate Entire Document**:
  - Single-tap 90° rotation across all pages in the document.
- **High-Definition Page Preview**:
  - Tap the magnifying glass icon or the thumbnail to preview any page in full screen with interactive zoom and rotation.
- **Save as New or Overwrite**:
  - Save as a brand new score or overwrite the original PDF directly in your library while preserving all metadata and ratings.

---

## 📡 3. Wi-Fi Direct Transfer (P2P) & Group Broadcast

The **Wi-Fi Direct Transfer** module ([`WifiTransferPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/WifiTransferPage.xaml)) powers direct wireless sharing between devices **with no external internet, router, or mobile data required**.  
Received scores and setlists are automatically placed into the default "Général" folder, preserving each musician's customized directory structure without imposing the sender's hierarchy.

### A. Direct Tablet-to-Tablet Transfer (P2P):
1. **Receiver**:
   - Opens **Tools > Wi-Fi Direct Transfer**, enables Wi-Fi, and taps **« 🟢 Receive Mode (Make Visible) »**. The device enters discoverable mode.
2. **Sender**:
   - Picks a score or setlist to share.
   - Selects whether to bundle personal annotations and audio tracks.
   - Selects the target recipient from the discovered devices list. The binary stream transfers in seconds over local TCP sockets.
3. **Acceptance**:
   - Receiver taps **Accept** on the confirmation prompt, and items integrate immediately into the library.

### B. Multi-Musician Group Broadcast (QR Code):
1. **The Band Leader / Conductor (Sender)**:
   - Selects the setlist or pieces to share and taps **« 📲 Group Broadcast Mode (QR Code) »**.
   - The app starts an ad-hoc local server and displays a **large QR Code** on screen with a live connected musicians counter.
2. **Group Musicians (Receivers)**:
   - Open **Tools > Wi-Fi Direct Transfer** and scan the QR Code on the leader's tablet.
   - All band members download the score or complete setlist in parallel simultaneously without needing an internet router!

---

## 🏷️ 4. Tag Management

The **Tag Management** screen ([`TagsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/TagsPage.xaml) & [`TagEditPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/TagEditPage.xaml)) gives you full control over library categorization:
- **Create New Tags** with custom names (e.g., *Choir*, *Sight Reading*, *Christmas Concert*, *Lead Sheet*).
- **RGB Color Palette & Sliders**: Choose predefined colors or mix custom colors using Red, Green, and Blue sliders with real-time badge preview.
- **Search, Edit & Rename** existing tags.
- **Delete Tags**: Safely removes tags without deleting any underlying sheet music files.

---

## 📦 5. Package & Setlist Imports

The **Package Import** tool ([`ImportPackagePage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ImportPackagePage.xaml)) handles external archives:
- **Supported Formats**: `.msmsetlist` (complete setlist), `.msmscore` (individual score with annotations/audio), and `.msmscores` (batch archive of multiple pieces).
- **Automated Extraction**: Extracts PDFs, recreates musical metadata (composer, key, tempo, rating), restores drawings/annotations, and attaches audio files automatically.
- **Neutral Destination**: Files extracted from imported packages are stored in the default "Général" category to maintain clean organization.

---

## 🔍 6. Duplicate Finder

The **Duplicate Finder** ([`DuplicatesPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/DuplicatesPage.xaml)) audits your library storage to free up disk space:
- **SHA-256 Cryptographic Hash Check**: Compares the exact binary contents and byte length of PDF files, catching duplicates even if filenames or titles differ.
- **Recursive Folder Scanning**: Recursively inspects all category sub-directories (`📁 [Category] Path`).
- **Side-by-Side Duplicate Groups**: Displays matching files with their file sizes, dates, and wasted storage space.
- **Safe Cleaning**: Delete redundant copies while keeping the original library entry intact.

---

## 💾 7. Database Backups & Restoration

The **Backups** utility ([`BackupsPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/BackupsPage.xaml)) ensures your annotations and organization are never lost:
- **Instant Database Snapshot**: Create a dated backup of your SQLite database (`scores.db3` containing scores, setlists, tags, links, annotations).
- **Database Backup History**: View and restore database snapshots in 1 click.
- **Automated Backup Scheduling**:
  - Configurable interval in days (e.g., every 30 days).
  - Maximum retention limit with automated rotation (e.g., keep the 6 most recent backups).
- **Complete Data Backup (.zip)**: All-in-one compressed `.zip` export packaging the SQLite database, all physical PDF scores with their category folder structures, and all attached audio backing tracks.
- **Customizable Destination**: Defaults to the device's `Download` directory and can be redirected to external storage (USB drive, SD card) or cloud storage (Google Drive, pCloud, OneDrive).
- **Blocking Progress Overlay**: Step-by-step progress tracking with exact percentage and safe cancellation support.
- **Complete Restore (Erase / Replace)**: Full re-import from a backup `.zip` with solemn confirmation, safe cache staging extraction, complete replacement, and post-restore integrity validation.
