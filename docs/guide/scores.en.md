# 🎵 Scores Menu

The **Scores** menu is the primary gateway to your digital sheet music library. It displays all your scores in a responsive, modern card grid optimized for large tablets and phones alike.

---

## 🖥️ Top Header Elements & Controls

At the top of the scores view, you have immediate access to several critical tools:

1. **Real-time Search Bar**:
   - Type a few letters to instantly filter your library by **piece title** or **composer name**.
   - One-tap clear button to quickly return to your full catalog.

2. **Batch Multi-selection Button (☑️)**:
   - Activates bulk selection mode to perform operations across multiple scores simultaneously.
   - Reveals an action bar at the bottom:
     * **🏷️ Assign Tags**: Add or remove tags from all selected scores at once.
     * **📡 Share via Wi-Fi Direct**: Send the entire batch over local peer-to-peer Wi-Fi in a single transfer.
     * **📦 Export Package (.msmscores)**: Generates a single compressed archive bundling all selected scores.
     * **🗑️ Batch Delete**: Remove selected items with safety confirmation.

3. **Tag Filter Button (🏷️)**:
   - Opens the tag selection panel to display only pieces matching one or several categories (e.g., *Jazz*, *Brass Section*, *Summer Festival*).

4. **Sort Order Selector (🔃)**:
   - Instantly reorders your library by:
     * **Date added (Newest first)**: Default sorting to easily find your latest imports.
     * **Date added (Oldest first)**.
     * **Title (A-Z) / Title (Z-A)**.
     * **Modification date (Recently edited)**.
     * **Rating (Highest rated)**: Pieces with 1 to 5 stars listed in descending order.
     * **Composer (A-Z)**: Alphabetical composer order. A dedicated toggle in *Settings > Scores* controls whether pieces without a composer are shown at the very top or at the end.
     * **Untagged pieces first**: Prioritizes scores with no assigned tags so you can quickly organize them.

5. **Add Score Button (➕)**:
   - Opens your device's native file picker to import one or multiple PDF documents.

---

## 📥 Import Process & Storage Strategy

When selecting PDF files, Music Score Manager offers a storage choice for individual files or for the whole batch:

- **Copy to app library (Recommended)**:  
  Copies the PDF into Music Score Manager's sandboxed storage directory. Scores stay accessible even if the original download folder is cleaned up or files are moved.
- **Link external original file**:  
  Retains the absolute path on storage without duplicating the file, saving device storage space.

---

## 🗂️ Score Card Breakdown

Every score is displayed as an informative card:
- **Piece Title**.
- **Customizable Subtitle** (configurable in Settings: composer name, date added, or both).
- **Star Rating** (from 1 to 5 yellow stars).
- **Color-coded Tag Badges** for quick visual classification.
- **⚠️ Missing File Warning Indicator (Red Exclamation Mark)**:  
  If a PDF file was moved or deleted from device storage, a bright red warning badge appears to the left of the 3-dots ⋮ button, notifying you before a concert that the score cannot be rendered.

---

## ⋮ Context Menu (3 dots)

Tapping the **⋮** button on any score card opens the full context actions menu:

1. **📖 Open score**:
   - Opens the score directly in the full-screen stage performance viewer.

2. **✏️ Edit score**:
   - Opens the detailed metadata editor:
     * **Title**.
     * **Composer / Arranger**.
     * **Target Tempo (BPM)**: Automatically configures the metronome when opening this score.
     * **Musical Key** (e.g., *C Major*, *Bb Minor*).
     * **Star Rating (1 to 5 stars)**.
     * **Tag Assignments & Creation**.
     * **Associated PDF File Path** with file replacement button.
     * **Red Warning Banner** if the PDF file is missing.

3. **📑 Modify PDF assembly**:
   - Launches the piece directly inside the **PDF Assembler Studio** to rearrange pages, insert blank sheets, rotate pages, or remove unwanted sheets.

4. **📡 Send via Wi-Fi Direct**:
   - Opens a **Sharing Options modal** asking you to choose:
     * ☑️ *Include handwritten annotations & drawings*.
     * ☑️ *Include attached audio backing tracks* (MP3/WAV).
   - Starts searching for nearby devices using high-speed Wi-Fi Direct P2P.

5. **📦 Export score (.msmscore)**:
   - Produces a self-contained archive file containing the PDF, metadata, annotations, and audio.

6. **🏷️ Rename score**:
   - Fast shortcut to rename the piece without opening the full editor.

7. **🗑️ Delete score**:
   - Removes the piece from your library with a confirmation prompt.
