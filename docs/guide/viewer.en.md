# 📖 Fullscreen Sheet Music Viewer (Stage Mode)

The **Viewer** ([`ViewerPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/ViewerPage.xaml)) is where music comes to life. Tailored to eliminate all visual distractions, it offers maximum readability, zero-flash synchronous rendering, and rapid gesture and hardware controls.

---

## 👆 Stage Gestures & Navigation

- **Turn to Next Page**:
  - Tap the right half of the screen or swipe right-to-left (fully configurable in Settings: horizontal swipe, tap, or vertical swipe).
- **Turn to Previous Page**:
  - Tap the left half or swipe left-to-right.
- **Continuous Setlist Transition**:
  - When reaching the final page of a score in a setlist, the next page turn immediately opens page 1 of the next piece.
- **Pinch-to-Zoom & Smooth Pan**:
  - Spread two fingers to enlarge a tight measure or complex notation. Freely pan around the score even while zoomed.
- **2-Page Landscape View**:
  - On horizontal tablet screens, enable dual-page mode in Settings to display two consecutive pages side by side.
  - Annotations are projected with millimeter geometric accuracy on left (`leftPage`) and right (`rightPage`) pages, respecting letterbox and pillarbox margins.
- **Clickable Page Indicator**:
  - Bottom-right page count badge (`e.g., 2/8`). Tapping it opens a direct jump dialog to any page number.

---

## 🦶 Hands-Free Foot Control: Bluetooth Pedals & MIDI Events

Music Score Manager features universal hardware controller support:
- **Bluetooth HID Pedals & Wireless Keyboards**: AirTurn, PageFlip Dragonfly/Firefly, Joyo, Donner, Thomann/Harley Benton, IK Multimedia iRig BlueTurn, Coda STOMP, standard keyboard arrows, etc.
- **MIDI Controllers (USB-OTG & Bluetooth MIDI)**: Sustain pedals (CC 64), sostenuto (CC 66), soft pedals (CC 67), MIDI note events, and Program Changes.
- **Assignable Stage Actions** (independently assignable to short press or long press with 200–1000 ms sensitivity):
  - Next / previous page
  - Next / previous song in setlist
  - Song start (page 1) / song end
  - Scroll up / down
  - Start / stop metronome & mute sound
  - Backing audio Play / Pause / Restart
  - Reset zoom to 100%
  - Open direct page jump
  - Lock / unlock annotations
  - Undo / Redo annotations
  - Open central menu / Close viewer

---

## 🎛️ Central Quick Menu (Double-Tap)

**Double-tap at the center of the screen** to open the quick action menu without leaving your score:

1. **↻ Rotation (+90°)**:
   - Rotates the score clockwise by 90 degrees.
   - **« Apply to this page only » toggle**: Rotate only an inverted landscape page.
   - **« Remember rotations » toggle**: Saves orientation permanently in SQLite.
2. **🔍 Reset zoom to 100%**:
   - Instantly restores default full-page scaling and centers the document.
3. **📄 Page**: Jump directly to a page number.
4. **⏱️ Show / Hide Metronome**: Toggles the metronome overlay.
5. **🎧 Show / Hide Audio Player**: Toggles the audio transport overlay.
6. **📑 Modify PDF Assembly**: Direct shortcut to PDF Assembler Studio.
7. **✏️ Edit Score**: Instant access to musical metadata editing.
8. **Return to Home / Close**: Clean exit from the viewer.

---

## ✍️ Complete Annotation Suite

The annotation toolbar can be dragged vertically across the screen (*Pan*), with secondary option boxes magnetically docked directly above:

- **🖌️ Chiseled Highlighter (Stabilo)**:
  - Natural translucent stroke with flat, chiseled ends (`PenLineCap.Flat`).
  - 4 vibrant colors (yellow, green, blue, pink) and 3 widths (5 mm, 10 mm, 18 mm).
- **T Typographic Text**:
  - Place custom typed text directly on the sheet music, 6 colors, 5 font sizes.
- **✎ Freehand Pencil**:
  - 100% opaque foreground drawing, 6 solid colors, 5 calibrated widths from 1 mm to 5 mm.
- **❏ Musical Stickers & Favorites**:
  - Over 100 stickers organized into categories: Favorites, Fingerings, Dynamics, Articulations, Repeats, Notes (𝅝, 𝅗𝅥, ♩, ♪, ♫...), Rests (𝄻, 𝄼, 𝄽, 𝄾...), Accidentals (♯, ♭, ♮, 𝄪, 𝄫), and Symbols.
  - Large tactile slider to effortlessly adjust sticker size with your fingers.
- **↩️ Undo & ↪️ Redo**:
  - Dynamic step-by-step history to quickly reverse or reapply any drawing or sticker placement.
- **🔒/🔓 Strict Safety Lock**:
  - Automatically locks by default on score open to safeguard the score from unwanted touches during live play.
  - Automatically unlocks when selecting an editing tool.
  - Automatically relocks when closing the toolbar (cross button ✕).
- **✧ Clear Page**: Reset all annotations on the current page with one tap.
- **🗑️ Targeted Delete**: Touch any existing sticker or text to modify or delete it.

---

## ⏱️ High-Precision Built-in Metronome

- **Thread-Safe Regulated Engine**: Zero CPU drift via double timing loop (`Stopwatch` + `SpinWait`).
- **Zero Audio Latency**: Direct hardware sample triggers via Android `SoundPool`.
- **Visual Pulse & LED**: Flashing LED indicator synchronized with downbeats and upbeats.
- **Audio Click Mute**: Toggle sound on/off on the fly while retaining visual pulsing.
- **Synchronized Pre-Count**: Precise measure countdown before starting audio playback.

---

## 🎧 Synchronized Audio Backing Player

- Supports attached MP3, WAV, AAC, and FLAC accompaniment tracks.
- Compact transport bar with Play/Pause button.
- Scrubbing progress slider with elapsed time and total duration display.
