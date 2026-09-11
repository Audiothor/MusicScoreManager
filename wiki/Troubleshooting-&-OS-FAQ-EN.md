> [🇫🇷 Version Française](Troubleshooting-&-OS-FAQ) | **🇬🇧 English**

# 🚑 Troubleshooting & OS FAQ

This guide compiles tested solutions to common questions encountered on Android and Windows hardware in live settings.

---

## 1. Android: Bluetooth pedal disconnects after 10 minutes of playing

### Cause:
Device manufacturers (Samsung OneUI, Xiaomi HyperOS, Huawei EMUI) incorporate aggressive background battery optimizers (*Doze Mode*) that put Bluetooth receivers to sleep if no screen touch occurs for several minutes.

### Solution:
1. On your Android tablet, open **Android Settings**.
2. Go to **Apps** › **Music Score Manager**.
3. Tap **Battery** (or *Battery Optimization*).
4. Select **« Unrestricted »** (or disable optimization).
5. Ensure your screen timeout is set to a duration longer than your performance.

---

## 2. Cannot see PDF score files on PC/Mac via USB cable

### Cause:
Starting with Android 11, Google enforces *Scoped Storage*: files kept within an application's private sandbox (`/data/user/0/...`) are hidden from Windows Explorer and Mac Finder over USB.

### Solution:
Inside Music Score Manager:
1. Open the **Settings** tab › **App Settings**.
2. In the *Directory Paths* section, click **« Change »** next to *Scores Path*.
3. Choose a public folder on your device (such as `Documents/MusicScores` or `Download/Scores`).
4. Reconnect your tablet to your PC via USB in *File Transfer (MTP)* mode: all your PDF files and folders are now visible and synchronizable on your PC!

---

## 3. The tablet screen turns off while playing a piece

### Cause:
Standard Android screen timeout defaults to 1 to 2 minutes of touch inactivity.

### Solution:
- In the **Fullscreen Stage Viewer**, Music Score Manager automatically invokes native `DeviceDisplay.Current.KeepScreenOn = true`. The display remains active indefinitely while viewing music.
- If you notice timeouts on specific customized ROMs, check your tablet settings (*Display > Screen Timeout*) and set it to 10 minutes or « Never ».

---

## 4. Slight metronome audio latency on entry-level tablets

### Cause:
Some low-power processors experience latency when decoding compressed audio files on the fly.

### Solution:
- Music Score Manager utilizes Android's native **`SoundPool`** subsystem, pre-decoding clicks into uncompressed 16-bit PCM memory upon app launch.
- For 100% timing certainty, rely on the visual LED pulse, driven by a hardware nanosecond clock unaffected by sound card latency.

---

## 5. How to migrate my entire music library to a new tablet?

You just purchased a new tablet and want all your scores, setlists, and annotations transferred:

1. **On the Old Tablet**:
   - Go to **Tools** › **💾 Database Backups**.
   - Tap **« Backup Database »**.
   - Connect the old tablet to a computer via USB (or plug in a USB thumb drive) and copy:
     * Your score PDFs and audio files folder.
     * The latest `.db3` backup file from the backups directory.
2. **On the New Tablet**:
   - Install **Music Score Manager**.
   - Copy your score PDFs and audio files to your preferred public folder.
   - Copy the backup file into the app's backups folder.
   - Open **Tools** › **Database Backups**, locate the backup, and tap **« Restore »**.
   - Your complete library (scores, annotations, tags, and setlists) is instantly restored!

---

## 6. My pedal connects, but sends the wrong command

### Solution:
1. Open **Settings** › **Pedals & MIDI Settings**.
2. Look at the **Live Signal Monitor** with the green indicator 🟢.
3. Press the pedal: the screen displays the exact captured command (e.g., `Key: ArrowDown`, `Keycode: 20`).
4. If your pedal has a physical hardware mode switch on its side, switch it to **Mode 1** or **Mode 2**.
5. If necessary, use **« ➕ New Profile »** and **« Learn Mode »** to directly map the incoming signal to your desired action.
