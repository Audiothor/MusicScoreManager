> [🇫🇷 Version Française](Stage-Cookbook-&-Scenarios) | **🇬🇧 English**

# 🎭 Stage Cookbook & Real-World Scenarios

This practical guide provides field-tested workflows for rehearsals and live performances tailored to your musical ensemble.

---

## 🎺 Scenario 1: Choir / Big Band / Orchestra Broadcast (Zero-Router Setup)

### The Challenge
Before a performance or during rehearsal, the musical director needs to distribute an ordered 15-piece concert setlist along with bowing markings, dynamics, and fingerings to **20 musicians**. There is no venue Wi-Fi and mobile data is unavailable in the basement hall.

### The Music Score Manager Protocol (Total Time: 45 Seconds)
1. **On the Leader's Tablet (Sender)**:
   - Open the **Setlists** tab and select the concert program (e.g., *« Symphony Concert June »*).
   - Tap the 3-dots button `⋮` › **« 📡 Send via Wi-Fi Direct »**.
   - Check **« Include handwritten annotations »** (to share bowing and phrasing notes added during rehearsal).
   - Tap the pink button **« 📲 Group Broadcast Mode (QR Code) »**.
   - The tablet starts an embedded local server and displays a large **QR Code** on screen with a live connected musicians counter (`0 connected`).
2. **On the Musicians' Tablets (Receivers)**:
   - All musicians open the app › **Tools** tab › **« 📡 Wi-Fi Direct Transfer (P2P) »**.
   - They tap **« Scan QR Code »** and point their camera at the director's screen.
3. **The Result**:
   - The director's screen counts up: `1... 5... 12... 20 connected musicians`.
   - All PDF sheet music, metadata, song order, and annotation layers download in parallel in seconds.
   - Musicians tap the newly received setlist and are immediately ready to play!

---

## 🎹 Scenario 2: Solo Piano or Organ Recital (2-Page Mode & Turn Synchronization)

### The Challenge
On complex piano scores (e.g., Chopin Etudes or Beethoven Sonatas), turning the page during a two-handed rapid run is impossible. The pianist wants **2 pages displayed side by side in landscape mode** and needs every page turn to land cleanly during a rest or one-handed measure.

### The Intelligent Assembly Workflow
1. In the **Tools** tab, open **« 📑 PDF Assembler Studio »**.
2. Tap **« 📖 Modify a score »** and load the piece.
3. Observe where consecutive page pairs fall (Page 1-2, Page 3-4, etc.):
   - If the turn between Page 2 and Page 3 lands in the middle of a virtuosic run, but the end of Page 1 contains a full-measure rest.
4. Tap **« 📄 Blank Page »** and move this blank sheet to **Position 1**:
   - Now the screen will display:
     * *Screen 1*: [Blank Sheet] + [Music Page 1]
     * *Screen 2*: [Music Page 2] + [Music Page 3]
   - The first turn occurs at the end of Page 1 (during the rest!), and the following two pages are visible simultaneously.
5. Tap **« Overwrite existing score »** to save.
6. In **Settings > Pedals**, set long-press sensitivity to **450 ms**: this prevents double turns caused by lifting your foot slightly too slowly under stage stress.

---

## 🎸 Scenario 3: Pop/Rock Concert with Backing Tracks & Metronome Sync

### The Challenge
The band plays with orchestration tracks (strings and synthesizers), and the drummer needs a tempo click. The lead singer and guitarist need synced chord charts and lyrics.

### The Workflow
1. **Score Editing**:
   - In **Scores**, tap `⋮` › **« ✏️ Edit score »**.
   - Set the exact **Tempo (BPM)** (e.g., `128`).
   - Expand the **Metronome** accordion: set the time signature (`4/4`) and pre-count (`2 measures` = 8 countdown clicks).
   - Expand the **Audio Files** accordion: import the backing MP3 or WAV track.
2. **On Stage**:
   - Start the piece. The visual LED pulse and audio click emit the 2-measure pre-count.
   - At the end of the countdown, the backing audio track kicks in precisely on beat 1.
   - If an unexpected stage cue occurs (extended solo), tap the assigned Bluetooth pedal to pause the audio track without interrupting sheet music viewing.

---

## 📑 Scenario 4: Sheet Music Digitization & File Size Best Practices

A common issue with digital sheet music binders is slowdown caused by 150 MB PDF scans from poorly configured office copiers.

### Guidelines for Fast-Loading Scores:
- **Optimal Resolution**: **150 to 200 DPI**. This provides razor-sharp clarity on tablets without overloading the GPU memory.
- **Color Mode**:
  - Use **Grayscale** or **Black & White (Threshold)**.
  - Avoid 24-bit RGB color scans for black-and-white music (color increases file size by 3x to 4x with zero benefit).
- **Target Size**: Standardized PDF files. A 6-page piano piece should ideally weigh between **500 KB and 2 MB**.
- **Direct Smartphone Scan**:
  - Take well-lit photos without glare.
  - In Music Score Manager, tap `+` › select the photos: the app automatically stitches them into a standardized, lightweight PDF.
