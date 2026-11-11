<h1 style="text-align: center;">Tools</h1>

The **Tools** menu hosts Music Score Manager's powerful suite of built-in utilities designed to manipulate, share, clean up, and safeguard your music catalog without relying on a computer or internet connection.

---

## 1. Paper Score Scanner (High-Definition Camera Capture)

The **Score Scanner** allows you to digitize physical sheet music directly from your music stand without an external scanner:

- **Instant Camera Capture & Gallery Import**:  
  Take photos of each page of your score using your camera (`Take Photo`) or import previously captured photos (`Pick Image`).

- **High-Visibility Enhancement Filters**:  
  - **Optimized B&W Score (High Contrast)**: applies an adaptive thresholding algorithm to eliminate music stand shadows, whiten the background, and make staves and musical notes razor-sharp.  
  - **Grayscale**: smooths tonal gradients while maintaining a compact file size.  
  - **Original Color**: preserves the raw appearance of the original document and colored handwritten markings.

- **Metadata & Folder / Category Assignment**:  
  - Enter the **Title** and **Composer / Arranger** directly.  
  - **Folder / Category Selection**: choose the destination folder (defaults to "Default") or create a new folder on the fly with the `➕` button.

- **Dynamic Page Management**:  
  - Thumbnail preview of every page with sequence number.  
  - Individual 90° rotation (`↻`) and targeted deletion (`🗑️`).  
  - `Delete All` button to reset the scanning session.

- **Immediate Multi-Page PDF Generation**:  
  - The **Generate Score** buttons (at top and bottom) compile your processed pages into a multi-page PDF, register it in your library, and open it immediately in the viewer!

---

## 2. PDF Creator & Assembler Studio

The **PDF Assembler Studio** is a complete multi-page PDF document manipulation and composition studio.

### Key Capabilities:

- **Build PDFs from Photos or Images**:  
  Select multiple photos of sheet music from your camera or gallery (`.jpg`, `.png`). The app compiles images while preserving 100% of their resolution and aspect ratio.

- **Interactive Page Thumbnails**:  
  Each page of the document is represented by an interactive thumbnail with its order number.

- **Reorder Pages**:  
  Use the **Move Up (▲)** and **Move Down (▼)** buttons under each thumbnail to reorganize your sheets.

- **Individual Page Rotations**:  
  Tap **↻ Rotate** on any specific thumbnail to turn that page in 90° increments (0°, 90°, 180°, 270°). Perfect for pages scanned sideways or upside down.

- **Duplicate Pages**:  
  Clone any page with a single tap (ideal for repeating a chorus or Da Capo section without flipping backwards while playing).

- **Delete Pages (🗑️)**:  
  Remove cover pages, blank sheets, or download advertisements.

- **Reverse All Pages**:  
  Fixes documents scanned from the last page to the first page in one tap.

- **Save & Replace**:  
  Save the document as a brand-new score or overwrite the original file directly.

---

## 3. Wi-Fi Direct Transfer (P2P) & Group Broadcast

This tool handles direct wireless transfers between musicians, **with no Wi-Fi router or 4G/5G mobile connection required**.  
It simply requires Wi-Fi to be enabled on your devices (no internet connection needed!).  
Received scores are automatically routed to your default `"Default"` folder, preserving your personal directory structure without interference from the sender's hierarchy.

#### A. Direct Tablet-to-Tablet Transfer (P2P)

##### 1. Preparation

<u>- CASE 1: BOTH MUSICIANS ARE CONNECTED TO THE SAME WI-FI NETWORK (at home, conservatory network, etc.)</u>  
Both devices simply connect to the shared local Wi-Fi network.

<u>- CASE 2: NO EXISTING WI-FI NETWORK AVAILABLE (backstage, outdoors, festival, church)</u>  
**=> Create a direct Wi-Fi link between the two tablets:**  
1. **On Tablet 1 (either device):**  
   Open Android settings / notification drawer and turn on **« Wi-Fi Hotspot »** (or *Mobile Hotspot / Tethering*).  
2. **On Tablet 2:**  
   Enable **Wi-Fi** and connect to the Wi-Fi hotspot created by Tablet 1. *(Both tablets are now directly linked on a local network)*.

##### 2. Execute Transfer in Music Score Manager

1. **On the receiving tablet:**  
   Open **Tools** > **Wi-Fi Direct Transfer (P2P)**.  
   Click the large green button **« 🟢 Activate Reception (Make Visible) »**.

2. **On the sending tablet:**  
   - On the score or setlist to share, tap the **⋮** (3 dots) menu > **« Send via Wi-Fi Direct »** (or check multiple scores in multi-selection mode).  
   - The recipient tablet appears immediately in the discovered devices list.  
   - Choose whether to include handwritten annotations and attached audio tracks.  
   - Tap its name to begin sending.

3. **On the receiving tablet:**  
   - A confirmation dialog appears: tap **« Accept »**.  
   - Scores, annotations, and audio tracks are received and integrated directly into your library in seconds!

#### B. Multi-Musician Group Broadcast via QR Code

1. **The Sender / Band Leader**:  
   Enables **Group Broadcast Mode**. The app starts a secure local server and displays a **high-resolution QR Code** on screen.

2. **Band Members**:  
   Open their camera or the **Tools > Receive** tab in the app and scan the QR Code.  
   All musicians download the score or complete setlist simultaneously to their tablets in a few seconds!

---

## 4. Package Imports (Setlists, Scores, and Bundles)

This page allows you to load archives shared by other musicians:

- **Supported formats**: `.msmsetlist` (complete setlist), `.msmscore` (individual score with annotations/audio), and `.msmscores` (batch archive of multiple scores).
- **Automated import**: extracts PDF documents, recreates musical metadata (tempo, composer, key, rating), restores annotation layers, and links audio tracks into your local library in a single operation.
- **Neutral organization**: scores extracted from external packages are placed in the default `"Default"` folder to respect your personal directory structure.

---

## 5. Duplicate Finder

This utility audits your entire sheet music library to free up storage space and keep your catalog tidy:

- **SHA-256 Binary Hash Analysis**: compares the exact binary content of your PDFs (even if two files have different names).
- **Recursive Directory Scan**: automatically inspects all category sub-directories (`📁 [Category] Path`).
- **Duplicate Grouping**: displays matching files side by side with their metadata, dates, and file paths.
- **Safe Deletion**: delete superfluous copies while keeping the reference original safe.

---

## 6. Backup & Restore Management

This page ensures the absolute safety of your musical work:

- **Instant Database Snapshot**: create a timestamped backup of your SQLite database (titles, metadata, setlists, links, annotations, tags).
- **Database Backup History**: detailed list of local database backup points with date, time, and file size, and 1-click restore.
- **Complete Data Backup (.zip)**: all-in-one compressed export combining the SQLite database (`scores.db3`), all physical PDF scores with their category folder structures, and all attached audio backing tracks.
- **Custom Destination**: saved by default to the `Download` folder and customizable to export directly to external storage (USB drive, SD card) or cloud storage (Google Drive, pCloud, OneDrive).
- **Blocking Progress Dialog**: step-by-step progress tracking with exact percentage and clean cancellation option.
- **Complete Restore (Erase / Replace)**: full re-import of a `.zip` archive with solemn confirmation, safe temporary extraction, complete replacement, and automatic library integrity verification.
