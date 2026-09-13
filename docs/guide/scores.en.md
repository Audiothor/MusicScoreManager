# 🎵 Scores Menu

The **Scores** menu is the primary gateway to your digital sheet music library. It displays all your scores in a responsive, modern card grid optimized for large tablets and phones alike.

---

## 🖥️ Top Header Elements & Controls

At the top of the scores view, you have immediate access to several critical tools:

1. **Real-time Search Bar**:
   - Type a few letters to instantly filter your library by **piece title**, **composer name**, or file name.
   - Fast one-tap clear button to quickly return to your full catalog.

2. **Batch Multi-selection Button (☑️)**:
   - Activates bulk selection mode to perform operations across multiple scores simultaneously.
   - Reveals an action bar at the bottom:
     * **🏷️ Assign Tags**: Modal overlay to add or replace tags across all selected scores at once.
     * **📡 Share via Wi-Fi Direct**: Send the entire batch over local peer-to-peer Wi-Fi without internet.
     * **📦 Export Package (.msmscores)**: Generates a single compressed archive bundling all selected scores (with options to include annotations and audio backing tracks).
     * **🗑️ Batch Delete**: Secure bulk deletion with confirmation prompt.

3. **Tag Filter Button (🏷️)**:
   - Opens the tag selection modal with internal tag search and tag sort (A-Z, Z-A, most/least used).
   - Displays active filters in a horizontal carousel with color pills and quick-remove buttons.
   - *« Clear all »* and *« Apply »* buttons.

4. **Sort Order Selector (🔃)**:
   - Instantly reorders your library by:
     * **Date added (Newest first)**: Default sorting to easily find your latest imports.
     * **Date added (Oldest first)**.
     * **Title (A-Z) / Title (Z-A)**.
     * **Modification date (Recently edited)**.
     * **Rating (Highest rated)**: Pieces with 1 to 5 stars listed in descending order.
     * **Composer (A-Z)**: Alphabetical composer order. A dedicated toggle in *Settings > Scores* controls whether pieces without a composer appear at the very top or at the end.
     * **Untagged pieces first**: Prioritizes scores with no assigned tags so you can quickly organize them.

5. **Add Score Button (➕)**:
   - **PDF Import**: Strict binary `%PDF-` verification prevents corrupt or misnamed files from crashing the viewer.
    - **Photos / Images Import & Conversion (v2.0.3)**: Selecting one or more image files (PNG, JPEG, GIF, WEBP, BMP) informs the user that the selected image files will be converted into PDF format:
      * **Cancel**: Image files are ignored. Any accompanying PDF files in the same batch continue their normal import.
      * **Continue**: Images are seamlessly converted to high-definition PDF (users choose between merging into a single multi-page PDF score or creating individual PDF scores). The new scores immediately appear in the library.

---

## 📥 Import Process & Storage Strategy

When importing scores, the app offers two flexible storage modes:

- **Copy to app library (Recommended)**:  
  Copies the PDF into Music Score Manager's sandboxed storage directory. Scores stay accessible even if the original download folder is cleaned up or files are moved.
- **Link external original file (External)**:  
  Retains the absolute path on storage without duplicating the file, marked by a blue `🔗` badge.

---

## 🗂️ Score Card Breakdown

Every score is displayed as an informative card:
- **Piece Title**.
- **Customizable Subtitle** (configurable in Settings: composer name, date added, or both).
- **Color-coded Tag Badges** for instant visual classification.
- **🔗 External Linked File Badge** when stored in custom external folders.
- **⚠️ Missing File Warning Indicator (Red Exclamation Mark (!))**:  
  If a PDF file was moved or deleted from storage, a bright red warning badge appears to the left of the 3-dots ⋮ button, notifying you before a concert that the score cannot be opened.

---

## ⋮ Context Menu (3 dots)

Tapping the **⋮** button on any score card opens the full context actions menu:

1. **📖 Open score**:
   - Opens the score directly in the full-screen stage performance viewer.

2. **✏️ Edit score**:
   - Opens the detailed metadata editor ([`ScoreEditPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ScoreEditPage.xaml)):
     * **Title** & **Composer / Arranger**.
     * **Musical Key** (both standard notation *A, B, C...* and classical solfège *Do, Ré, Mi...*).
     * **Target Tempo (BPM)**: Automatically initializes the metronome when opening this score.
     * **Star Rating (1 to 5 stars)**.
     * **Dynamic Tag Selection**.
     * **Metronome Accordion**: Time signature (2/4, 3/4, 4/4, 6/8...), subdivision, pre-count measures, and default mute/sound toggle.
     * **Audio Tracks Accordion**: Attach backing audio tracks (MP3, WAV, etc.), preview listening, and relative volume adjustment.
     * **Technical File Details**: Full path, file size, modification date, and button to re-link files if moved.

3. **📋 Add to Setlist**:
   - Inserts the score immediately into the chosen setlist.
   - **High-priority 1st position placement** (automatically shifts other songs down).
   - Handles deduplication and confirmation if the setlist is locked.

4. **📑 Modify PDF assembly**:
   - Launches the piece directly inside the **PDF Assembler Studio** to rearrange pages, insert blank sheets, rotate pages, or remove unwanted sheets.

5. **📡 Send via Wi-Fi Direct**:
   - Opens the sharing options modal asking you to choose:
     * ☑️ *Include handwritten annotations & drawings*.
     * ☑️ *Include attached audio backing tracks* (MP3/WAV).
   - Starts high-speed P2P Wi-Fi Direct transfer to nearby devices.

6. **📦 Export score (.msmscore)**:
   - Produces a self-contained archive file containing the PDF, metadata, and optional annotations/audio.

7. **🏷️ Rename score**:
   - Fast dialog shortcut to rename the piece without opening the full editor.

8. **🗑️ Delete score**:
   - Removes the piece from your library with a safety confirmation prompt.
