# 📋 Version History — Music Score Manager

This document contains the official changelog of all versions of **Music Score Manager**, their release dates, and an indexed summary of modifications.

---

## Versions Index

- [v2.5.1 — 07/10/2026](#v251--07102026)
- [v2.5.0 — 07/10/2026](#v250--07102026)

---

## v2.5.1 — 07/10/2026

### 1. Central Quick Menu Polish (`ViewerPage`)
- **1.1. Removed redundant bottom "Close" button:** The modal already features an instant close cross `✕` in the top header.
- **1.2. Cleaner Metronome Row:** Removed the gear icon `⚙️` between "Show Metronome" and the On/Off switch for a streamlined, uniform layout matching the Audio and Annotations rows.

### 2. Improved Bottom Navigation Tab Bar Readability (`AppShell`)
- **2.1. Increased font size & contrast:** Slightly enlarged text size and bold emphasis for main tab labels (*Scores*, *Setlists*, *Tools*, *Settings*, *Quit*) for higher stage legibility.

### 3. Highlighter (Stabilo) Mode State Fix
- **3.1. Clean reset on modal close:** Fixed an issue where closing the highlighter options palette with the `✕` cross left the tool internally active, requiring the user to tap the highlighter icon twice to re-open it. Closing the overlay now cleanly resets the mode and toolbar button state.
- **3.2. Consistency:** Applied the same clean reset to draw and text option palettes.

---

## v2.5.0 — 07/10/2026
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
- [v2.3.0 — 30/09/2026](#v230--30092026)
- [v2.2.2 — 29/09/2026](#v222--29092026)
- [v2.2.1 — 29/09/2026](#v221--29092026)
- [v2.2.0 — 29/09/2026](#v220--29092026)
- [v2.1.13 — 29/09/2026](#v2113--29092026)
- [v2.1.12 — 25/09/2026](#v2112--25092026)
- [v2.1.11 — 22/09/2026](#v2111--22092026)
- [v2.1.10 — 22/09/2026](#v2110--22092026)
- [v2.1.9 — 22/09/2026](#v219--22092026)
- [v2.1.8 — 20/09/2026](#v218--20092026)
- [v2.1.7 — 20/09/2026](#v217--20092026)
- [v2.1.6 — 19/09/2026](#v216--19092026)
- [v2.1.5 — 18/09/2026](#v215--18092026)
- [v2.1.4 — 18/09/2026](#v214--18092026)
- [v2.1.3 — 18/09/2026](#v213--18092026)
- [v2.1.2 — 17/09/2026](#v212--17092026)
- [v2.1.1 — 17/09/2026](#v211--17092026)
- [v2.1.0 — 17/09/2026](#v210--17092026)
- [v2.0.4 — 13/09/2026](#v204--13092026)
- [v2.0.3 — 13/09/2026](#v203--13092026)
- [v2.0.2 — 12/09/2026](#v202--12092026)
- [v2.0.1 — 11/09/2026](#v201--11092026)
- [v2.0.0 — 10/09/2026](#v200--10092026)
- [v1.9.8.0 — 08/09/2026](#v1980--08092026)

---

## v2.5.0 — 07/10/2026

### 1. Complete Redesign of Bluetooth Pedals & MIDI Events Management
- **1.1. Removal of deprecated sections:** Completely eliminated the legacy diagnostic tester and accordion shortcuts list from the main settings to streamline the UI.
- **1.2. Preserved Stage Safety:** Kept the "Stage Security & Sensitivity" section intact with anti-double page turn cooldown (300 to 800 ms) and long press threshold slider.
- **1.3. Universal Profile System:**
  - Preserved and enhanced all factory presets (PageFlip Dragonfly, Firefly, Butterfly, AirTurn Duo/Quad 500, Donner, Joyo, Harley Benton, iRig BlueTurn, Coda STOMP, etc.).
  - **Unrestricted Customization:** Users can directly modify built-in hardware presets without being required to create a new profile.
  - **Profile Description & Comments:** Added custom description field for hardware notes and physical switch advice (e.g., REPEAT OFF switch, Mode 3 on PageFlip Dragonfly).
  - 1-click profile creation prompting for a name and transitioning immediately to edit mode.
  - Profile deletion and factory defaults restoration.

### 2. Granular Pedal & Button Management (`PedalProfileEditPage`)
- **2.1. Three intuitive operations:**
  - **Add pedal / button:** Prompts for custom label (e.g., "Left Pedal", "Button 1"), then opens configuration page.
  - **Edit pedal:** Edit selected button parameters via dedicated configuration page.
  - **Delete pedal:** Directly remove button binding from profile.
- **2.2. Polished Summary Cards:** Displays button label, captured signal badge, press type badge (Simple vs Long), and mapped action.

### 3. Dedicated Pedal Button Configuration (`PedalButtonConfigPage`)
- **3.1. Button Label:** Custom name / role entry.
- **3.2. Live Signal Auto-Detection:** Real-time Bluetooth HID and MIDI listener capturing raw code (Hex/Dec) and key name with green flash confirmation.
- **3.3. Press Type Selection:** Seamless choice between "⚡ Simple Press" and "⏱️ Long Press".
- **3.4. 13 Strict Stage Actions:**
  1. *Previous page*
  2. *Next page*
  3. *Go to beginning of piece*
  4. *Go to end of piece*
  5. *Previous bookmark*
  6. *Next bookmark*
  7. *Go to beginning of previous setlist song*
  8. *Go to beginning of next setlist song*
  9. *Open setlist songs menu (top drawer)*
  10. *Toggle night mode (future implementation)*
  11. *Zoom +*
  12. *Zoom -*
  13. *Toggle annotations visibility*
- **3.5. Viewer Execution:** Full wiring of all new actions in `ViewerPage` (step zoom, toggle annotations drawing, setlist drawer toggle).

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

---

## v2.3.0 — 30/09/2026

### 1. Custom Favorite Stickers Ordering
- **1.1.** **Custom Favorite Reordering:** Added drag-and-drop / Move Up / Move Down controls in annotation settings (`SettingsAnnotationsPage`).
- **1.2.** **Tailored Ergonomics:** Easily reorder frequently used musical stickers to suit personal performance habits.
- **1.3.** **Database Persistence:** Saved order in SQLite database (`FavoriteSticker.OrderIndex`) applied instantly to the viewer toolbar.
- **1.4.** **Google Play Store Assets:** Added official promotional graphics (high-res icon, 1024x500 feature graphic, and 8 key feature screenshots).

---

## v2.2.2 — 29/09/2026

### 1. Alphabetical Index Velocity & Smooth Scroll-To-Top
- **1.1.** **Instant A-Z Navigation:** Optimized scroll position computation for pixel-perfect alignment to the first matching score.
- **1.2.** **Floating Action Button (Scroll-To-Top):** Smooth and immediate scrolling back to the library start without visual stutter.

---

## v2.2.1 — 29/09/2026

### 1. Alphabetical Index Responsiveness Fixes
- **1.1.** **Elimination of Scroll Jumps:** Resolved jumpy behavior when quickly tapping outer letters (A, W, Z).
- **1.2.** **Accented Title Targeting:** Full support for accented letters and special characters in indexation calculations.

---

## v2.2.0 — 29/09/2026

### 1. Absolute Fluidity on Large Libraries (300+ scores)
- **1.1.** **High-Performance Virtualization:** Ultra-fast rendering of score collections without UI lag, even with hundreds of imported files.
- **1.2.** **Lateral A-Z Alphabetical Index Bar:** Quick touch-and-drag index on the right edge of the screen to jump directly to any letter.
- **1.3.** **Floating Scroll-To-Top Button:** Elegant floating action button appearing on scroll to return to top in one tap.
- **1.4.** **Dedicated Settings Toggle:** Preference to enable or disable the alphabetical index bar in Score Settings.

---

## v2.1.13 — 29/09/2026

### 1. Highlighter Precision & Elimination of Touch Artifacts
- **1.1.** **Highlighter Rendering:** Refined highlighter drawing engine preserving optical clarity and transparency over music staves.
- **1.2.** **Touch Artifact Elimination:** Completely removed residual dots caused by short touch contacts when activating drawing tools.

---

## v2.1.12 — 25/09/2026

### 1. Advanced Setlist Management & Default Sorting
- **1.1.** **Configurable Default Sort:** Added default setlist sorting options in settings (by name, concert date, or creation date).
- **1.2.** **Automatic Relegation of Past Setlists:** Option to move completed/past events to the bottom of the list.
- **1.3.** **Clear Visual Distinction:** Highlighting active setlists while dimming past performances.

---

## v2.1.11 — 22/09/2026

### 1. Multilingual Harmonization & General Stability
- **1.1.** **I18n Translation Fixes:** Full translations across backup dialogs, Wi-Fi Direct transfers, and pedal modules in all 8 languages.
- **1.2.** **Rendering Engine Robustness:** Hardened lifecycle handling during rapid switching between scores.

---

## v2.1.10 — 22/09/2026

### 1. Memory Management & Performance Optimizations
- **1.1.** **RAM Cache Management:** Cleaned up image buffers and memory streams during long rehearsal sessions.
- **1.2.** **Application Resilience:** Safe handling of potential edge-case navigation exceptions.

---

## v2.1.9 — 22/09/2026

### 1. Complete Multilingual Internationalization (i18n)
- **1.1.** **8 Supported Languages:** Native translations in French 🇫🇷, English 🇬🇧, German 🇩🇪, Spanish 🇪🇸, Italian 🇮🇹, Polish 🇵🇱, Dutch 🇳🇱, and Portuguese 🇵🇹.
- **1.2.** **Comprehensive Coverage:** Translated menus, application preferences, musical sticker categories, import prompts, and About page.
- **1.3.** **Instant Hot-Switch:** Dynamic language switching in settings with immediate UI update without app restart.

---

## v2.1.8 — 20/09/2026

### 1. Keep Screen Awake During Reading
- **1.1.** **"Keep Screen Awake" Option:** Prevents the device screen from sleeping while reading a score.
- **1.2.** **Organization Menu Rename:** Clarified settings navigation labels for an intuitive user experience.

---

## v2.1.7 — 20/09/2026

### 1. Streamlined Tag Management
- **1.1.** **Tags Module in Tools:** Centralized tag creation, renaming, color picking, and deletion in the Tools menu.
- **1.2.** **Direct Shortcut from Score Settings:** Instant access to global tag management without losing score editing context.

---

## v2.1.6 — 19/09/2026

### 1. Dynamic On/Off Annotations Overlay
- **1.1.** **Quick Visibility Toggle:** Instant show/hide toggle for annotations in the viewer central menu to view original or annotated score.
- **1.2.** **Per-Score Preference:** Set default annotation visibility per score in the score editing page.

---

## v2.1.5 — 18/09/2026

### 1. Audio Pre-Roll & Synchronized Metronome Pre-Count
- **1.1.** **High-Precision Pre-Count:** Synchronized visual and audible metronome countdown (1 to 4 bars) before backing track playback.
- **1.2.** **Systematic Pre-Count on Resume:** Re-triggers the countdown on resume after pause for perfect rhythm alignment.

---

## v2.1.4 — 18/09/2026

### 1. Floating Movable Audio Player
- **1.1.** **Movable Mini Player:** Draggable backing track audio player that can be repositioned anywhere without blocking notes.
- **1.2.** **Auto-Enable Setting:** Option to automatically load and start audio playback when opening a score.
- **1.3.** **Reactive Menu Control:** Direct audio switch in the viewer central menu.

---

## v2.1.3 — 18/09/2026

### 1. Reliable Shell Navigation to PDF Assembler
- **1.1.** **Race Condition Elimination:** Hardened MAUI Shell routing when navigating to the PDF assembler and splitter tool.

---

## v2.1.2 — 17/09/2026

### 1. Anti-Fast-Page-Turn Protection & Pedal Ergonomics
- **1.1.** **Cooldown / Anti-Bounce Protection:** Configurable debounce delay (200 ms to 1000 ms) preventing accidental double page turns.
- **1.2.** **Collapsible Pedal Shortcuts Accordion:** Streamlined settings interface with expandable sections per function.

---

## v2.1.1 — 17/09/2026

### 1. Customizable Dynamic Subtitles
- **1.1.** **Draggable Subtitle Elements:** Choose and reorder elements displayed under score title (composer, key, tempo, tags, page count).
- **1.2.** **Landscape Optimization:** Clean metadata presentation without text overlapping in horizontal orientation.

---

## v2.1.0 — 17/09/2026

### 1. Ultra-Fast Native PDF Rendering Engine
- **1.1.** **Hardware Native Rendering:** Direct integration with Android `PdfRenderer` and Windows `Windows.Data.Pdf` for 3x faster page loading.
- **1.2.** **Two-Page Landscape Mode:** Display two pages side by side in landscape mode with synchronized page flipping.
- **1.3.** **Dynamic RAM Caching:** Smart pre-caching of adjacent pages for perceived 0 ms page turn delay.

---

## v2.0.4 — 13/09/2026

### 1. Secure Image Import & Setlist Enhancements
- **1.1.** **Modal Conversion Dialog:** Clear dialog guiding users to convert photos and sheet music images to PDF.
- **1.2.** **Multi-Tag Filtering:** Cross-tag search when adding scores to a setlist.
- **1.3.** **Duplicate Scores in Setlists:** Support for adding the same score multiple times (for encores or repeated pieces).

---

## v2.0.3 — 13/09/2026

### 1. Robust Image-to-PDF Conversion
- **1.1.** **Safe Temporary Paths:** Resolved file access errors when converting images from external apps or photo galleries.
- **1.2.** **Friendly Prompts:** Rephrased import prompts with clear, reassuring guidance.

---

## v2.0.2 — 12/09/2026

### 1. Proactive Image Conversion Prompts
- **1.1.** Automatic detection of image formats (PNG, JPEG, WebP) upon import with instant prompt to convert to standardized PDF.

---

## v2.0.1 — 11/09/2026

### 1. Setlist Live Progress Drawer Refinements
- **1.1.** **Progress Color Coding:** Clear distinction between completed songs, current song, and upcoming songs.
- **1.2.** **Auto-Centering:** Automatically keeps the active score centered in the drawer.
- **1.3.** **Overlay Toggle:** Option to display or hide the setlist progress bar based on performance needs.

---

## v2.0.0 — 10/09/2026

### 1. Major Stage Experience Overhaul (Setlist Live Mode)
- **1.1.** **Setlist Live Progress Drawer:** Retractable top drawer in the viewer showing the full concert set without leaving the score.
- **1.2.** **Direct Touch Navigation:** Tap any song in the drawer to switch to it instantly during rehearsals or live shows.

---

## v1.9.8.0 — 08/09/2026

### 1. Bluetooth Pedals & MIDI Controller Support
- **1.1.** **Live Monitor:** Real-time display of incoming Bluetooth keypresses and MIDI events (Control Change, Program Change, Note On).
- **1.2.** **Pre-configured Hardware Profiles:** Ready-to-use profiles for AirTurn (Duo 500, QUAD 500, PEDpro), PageFlip (Firefly, Butterfly, Dragonfly), Coda STOMP, Donner, Joyo, Harley Benton, iRig BlueTurn, and standard keyboards.
- **1.3.** **Custom Profiles:** Create, duplicate, rename, and delete personalized pedal mappings for any device.
- **1.4.** **Short Press / Long Press Dual Actions:** Assign distinct actions for short and long presses per pedal, with adjustable sensitivity (200 ms to 1000 ms).
- **1.5.** **Full Musical Action Palette:** Next/Previous page, Next/Previous score, Metronome On/Off, Audio play/pause, 100% zoom, direct page jump, central menu, and annotation lock.

