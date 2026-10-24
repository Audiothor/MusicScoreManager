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

## 🛠️ Quick Tools Drawer (Top-Right)

Tap the top-right corner of the screen (or the `[ 🛠️ ]` icon if enabled in settings) to open the **Quick Tools Drawer**. It lets you toggle stage components instantaneously in real time without interrupting playback:

1. **✍️ Annotations**: Shows or hides the bottom annotation toolbar.
2. **⏱️ Metronome**: Expands or collapses the synchronized visual metronome.
3. **🎧 Audio Player**: Shows or hides the floating backing track transport bar.
4. **📄 Page Number**: Toggles the bottom-right page indicator.
5. **🎛️ Tuner**: Launches the floating multi-instrument precision tuner.
6. **🏷️ Score Title**: Toggles the top title bar showing the active piece name.

All toggles update live dynamically on screen immediately upon tapping.

---

## 🎛️ High-Precision Multi-Instrument Tuner

Music Score Manager includes a professional stage-grade chromatic tuner accessible directly during rehearsals or live performances without leaving your score:

- **Movable & Draggable Floating Panel**:
  - Equipped with a dedicated tactile drag handle `⠿ Move` and smooth pan gesture: reposition the tuner anywhere on screen so it never covers critical bars or notes.
  - Fast-close `✕` button in the top right.
- **State-of-the-Art DSP Engine (YIN Algorithm)**:
  - **YIN Pitch Detection ($\theta = 0.15$)**: Computes cumulative mean normalized difference $d'(\tau)$ for pinpoint pitch accuracy, eradicating octave jumping and false harmonics.
  - **2-Pole Low-Pass Filter (1200 Hz)**: Eliminates breath noise, reed sizzle on woodwinds, and sharp pick attack transients.
  - **Smart Sub-Harmonic & Octave Correction**: Resolves fundamental masking common to acoustic instruments whose fundamental ($f_0$) is overshadowed by the 2nd or 3rd harmonic (saxophones, French horn, bassoon, oboe, cello, double bass).
  - **Sliding Median Filter & Exponential Needle Smoothing (EMA)**: Rock-solid cents needle response with zero visual jitter ($\alpha = 0.45$).
- **Diapason Reference Calibration (A4 Frequency)**:
  - Fine-tune reference pitch from Baroque (415 Hz), Natural pitch (432 Hz), International standard (440 Hz), Symphonic orchestra (442 Hz), up to 444 Hz.
- **Extensive Instrument Library & Tailored Presets**:
  - **Universal Chromatic Tuner**: Detects all 12 semitones across all octaves with exact frequency readout in Hz.
  - **Guitars & Basses**: Standard Guitar (E A D G B E), 4-string Bass (E A D G), 5-string Bass (B E A D G).
  - **Bowed Strings & Folk**: Violin (G D A E), Viola (C G D A), Cello (C G D A), Double Bass (E A D G), Ukulele (G C E A), 5-string Banjo (Open G).
  - **Saxophone Family (Full Quartet)**: Alto Saxophone (in E♭), Tenor Saxophone (in B♭), Baritone Saxophone (in E♭), Soprano Saxophone (in B♭).
  - **Brass & Harmony Winds**: Trumpet & Clarinet (in B♭), French Horn (in F), Trombone / Euphonium / Tuba (in C - Bass Clef).
  - **Woodwinds**: Concert Flute (in C), Oboe & Bassoon (in C, including the **Oboe A 440 Hz benchmark** for orchestral tuning).
  - **Vocals & Singing**: Dedicated singing ranges and warmup benchmark notes (Bass, Baritone, Tenor, Alto, Mezzo, Soprano).
- **Intuitive Visual Gauge**:
  - Calibrated cents arc (-50 to +50 cents) with dynamic color feedback (Green = in tune, Amber/Red = flat or sharp).
  - Dual note name display (Standard *A, B, C...* and classical Solfège *Do, Ré, Mi...*), octave index, and precise live frequency in Hz.

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

The annotation toolbar expands from the bottom of the screen and adapts effortlessly to your movements:

- **Dedicated Vertical Pan Handle (`⠿`)**:
  - Located at the left of the toolbar, the `⠿` handle allows smooth vertical sliding across the screen without conflicting with horizontal tool scrolling.
- **Smartphone & Mobile Responsiveness**:
  - On phones, the close button `✕` is fixed to the right edge so it remains instantly accessible regardless of screen width.
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
- **✧ Clear Page**: Reset all annotations on the current page with safety confirmation.
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
- Floating and freely draggable anywhere on the screen.
