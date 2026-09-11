# 📋 Setlists Menu

The **Setlists** menu is specially designed for planning, organizing, and executing your concert programs, auditions, church services, and rehearsals without downtime.

---

## 📋 Presentation & List Layout

The Setlists tab displays all your created programs:
- **Top Toolbar**: Real-time search, multi-criteria sort (Name A-Z/Z-A, Creation Date, Status), and fast creation button (**➕**).
- **Horizontal Status Filters**:
  - *All*
  - *Active* (green badge): Programs currently being toured or practiced.
  - *Upcoming* (orange badge): Planned future concerts.
  - *Completed* (gray badge): Archived past performances.
- **Setlist Cards**:
  - Program title, creation date, status pill, and lock padlock `🔒`.
  - Swipe gestures for immediate access to *Rename* and *Delete*.

---

## ▶️ Instant Stage Start (One-Tap on Card)

In Music Score Manager, going on stage is instantaneous:
- **A single tap on a setlist card** launches **the first score directly** in the fullscreen stage viewer.
- If the setlist is empty, a helpful message guides you to add scores to it.
- During performance, the setlist activates **continuous reading mode**: when you turn the last page of a piece, the app automatically transitions to the first page of the next piece!

---

## 📋 Live Setlist Progress Drawer (v2.0.1.1)

While playing scores from a setlist inside the viewer:
- **Discreet Top Edge Opening**: A single tap at the top-center of the screen (invisible touch bar) smoothly slides down the progress drawer pinned to the top of the display.
- **Instant Automatic Centering**: Upon opening, the drawer automatically scrolls to center the currently played song in the list.
- **Dedicated Visual Status Palette**:
  - **Currently playing score**: High-contrast midnight blue background, cyan glowing outline, vibrant green `▶` badge, bright white title, and *« Now Playing »* cyan label.
  - **Passed scores**: Sage green checkmark `✓` (`#52B788`), muted bluish-gray text (`#8A95A5`), and *« Passed »* label.
  - **Upcoming scores**: Pastel blue number (`#74C0FC`), soft off-white text (`#EAF2FF`), composer in azure (`#A5C8E4`), and *« Upcoming »* label (`#4DABF7`).
- **Direct Navigation & Easy Dismissal**:
  - Tap any piece in the drawer to jump directly and immediately to it.
  - Close the drawer via the `✕` cross button, by tapping the semi-transparent backdrop, or by clicking the current song.

---

## ⋮ Context Menu (3 dots)

Each setlist provides its own action menu via the **⋮** button:

1. **▶️ Start setlist**:
   - Launches stage mode starting on the first song in full screen.

2. **✏️ Edit setlist**:
   - Opens the editor ([`SetlistEditPage`](file:///c:/Users/comme/Documents/GitHub/MusicScoreManager/SetlistEditPage.xaml)) to adjust the title, status, add songs from your library, rearrange pieces using **▲ / ▼** arrows (or drag-and-drop), and remove pieces.

3. **📡 Send via Wi-Fi Direct**:
   - Transmits the entire setlist and all its score files to other musicians' tablets via direct local streaming without internet.
   - Options modal:
     * ☑️ *Include handwritten annotations*.
     * ☑️ *Include attached audio backing tracks*.

4. **📦 Export (.msmsetlist)**:
   - Generates a standalone `.msmsetlist` package containing the entire program, page order, PDFs, and optional annotations/audios.

5. **📑 Duplicate setlist**:
   - Instantly clones the setlist with all pieces preserved in their exact order (ideal for variations of concert programs).

6. **🏷️ Rename**:
   - Fast dialog to rename the setlist.

7. **🔒 Lock / Unlock**:
   - Locks the setlist into Stage Mode to prevent accidental deletions or modifications during a concert.

8. **🗑️ Delete**:
   - Deletes the setlist (your original scores remain completely safe in your library).
