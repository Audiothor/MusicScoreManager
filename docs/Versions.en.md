# 📋 Version History — Music Score Manager

This document contains the official changelog of all versions of **Music Score Manager**, their release dates, and an indexed summary of modifications.

---

## Versions Index

- [v2.4.9 — 07/10/2026](#v249--07102026)
- [v2.4.8 — 01/10/2026](#v248--01102026)
- [v2.4.7 — 28/09/2026](#v247--28092026)
- [v2.4.6 — 25/09/2026](#v246--25092026)
- [v2.4.5 — 23/09/2026](#v245--23092026)
- [v2.4.4 — 21/09/2026](#v244--21092026)
- [v2.4.3 — 19/09/2026](#v243--19092026)
- [v2.4.2 — 17/09/2026](#v242--17092026)
- [v2.4.1 — 16/09/2026](#v241--16092026)
- [v2.4.0 — 14/09/2026](#v240--14092026)

---

## v2.4.9 — 07/10/2026

### 1. Tag Display Bug Fix ("Untagged first" sort)
- **1.1.** Fixed an issue where sorting scores by "Untagged first" in the Scores menu caused all tag badges to disappear across the entire list.
- **1.2.** Replaced fixed item measurement `ItemSizingStrategy="MeasureFirstItem"` with `ItemSizingStrategy="MeasureAllItems"` in the scores `CollectionView`, ensuring dynamic sizing for each score item.
- **1.3.** Added the reactive `HasTags` property and `INotifyPropertyChanged` notification on the `Score` model to bind tag visibility dynamically.

### 2. Page Bookmark / Marker System
- **2.1.** **On/Off Page Toggle:** Added an On/Off toggle in the central viewer menu (default: Off) to place a unique marker on the current page.
- **2.2.** **Discreet and Modern Visual Marker:** Discreet `🔖` page badge positioned in the top-left corner of the score view with automatic alphabetical uppercase naming (`A`, `B`, `C`...).
- **2.3.** **Interactive Dialog Modal:** Tapping the bookmark opens a modern dialog allowing:
  - Renaming the marker (1 to 3 characters max, unique within the score).
  - Deleting the marker from the page.
  - Navigating to the previous or next marker.
  - Viewing all bookmarks defined on the score to jump directly to any marked page.
- **2.4.** **Bluetooth Pedal & MIDI Support:** Added two new pedal actions `NextBookmark` and `PreviousBookmark` for hands-free navigation between markers.
- **2.5.** **SQLite Persistence:** Created the `ScoreBookmark` table with automatic cascade deletion when a score is deleted.

### 3. Viewer Central Menu Ergonomic Redesign
- **3.1.** **Instant Priority Access:** Added a top fixed header with immediate access to "🏠 Home" and "✕ Close" buttons without any scrolling.
- **3.2.** **Grouped Card Layout:**
  - *Bookmark Card:* Marker toggle and Previous/Next navigation buttons.
  - *Display & Navigation Card:* Quick buttons for rotation (+90°), zoom reset (100%), direct page jump, and rotation scope switches.
  - *Tools & Modules Card:* Toggles for metronome (with ⚙️ direct settings button), audio player, and annotations.
  - *Score Management Card:* Edit score and PDF assembly buttons.
- **3.3.** **Adaptive Sizing:** Compact auto-sized menu height avoiding unnecessary scrollbars on desktop/tablet while ensuring smooth scrolling on mobile in landscape mode.

---

## v2.4.8 — 01/10/2026

### 1. Modern Metronome Settings Dialog
- **1.1.** Sleek modern settings dialog for metronome tempo and visual/audio options.
- **1.2.** Sound volume control per score with slider and persistence.
- **1.3.** Removed camera and location Android permissions.

---

## v2.4.7 — 28/09/2026

### 1. Dialog Design Overhaul
- **1.1.** Modern visual styling and smooth animations for import dialogs and system messages.

---

## v2.4.6 — 25/09/2026

### 1. Mobile Responsive Annotation Toolbar
- **1.1.** Responsive adaptation of annotations, pencil, highlighter, and sticker toolbars in portrait and landscape modes.

---

## v2.4.5 — 23/09/2026

### 1. Import Preferences
- **1.1.** Default import mode choice (Local copy vs External link) with persistent preference.

---

## v2.4.4 — 21/09/2026

### 1. Cloud & Drive Import Robustness
- **1.1.** Compatibility improvements for importing scores directly from Google Drive and cloud providers.

---

## v2.4.3 — 19/09/2026

### 1. Central Menu Compactness
- **1.1.** Optimized height without vertical stretching on large screens.

---

## v2.4.2 — 17/09/2026

### 1. Sticker Usability & Documentation
- **1.1.** Sticker visual enhancements and documentation updates.

---

## v2.4.1 — 16/09/2026

### 1. Annotation Options Toolbar
- **1.1.** Compact color palette and brush size selector for drawings and highlighters.

---

## v2.4.0 — 14/09/2026

### 1. Mobile Annotations & Page Number Toggle
- **1.1.** Complete annotation support (pencil, highlighter, stickers).
- **1.2.** Double-tap quick actions and page number visibility toggle.
