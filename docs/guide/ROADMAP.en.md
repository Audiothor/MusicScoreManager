**Music Score Manager ROADMAP** :

---

**Urgent blockers or bugs:**
None at the moment

---

**Quick wins (high impact, low complexity):**

- **Cloud Backup/Restore via Android (SAF):** Export/import of a `.zip` file via the native Android file picker (compatible with Google Drive, pCloud, USB) while maintaining the "100% local / zero data collection" status.
  -> APPROVED

- **Page jump links (*Da Capo*, *Coda*):** Addition of clickable badges between two pages using your existing SQLite database.
  -> APPROVED

- **Automatic white margin cropping:** Detection of the usable area of the PDF to enlarge the staves on tablets.
  -> TO BE CONFIRMED based on user feedback regarding usefulness

- **MIDI Control (*Program Change*):** Sending a program change to a keyboard or multi-effects unit when opening a score via the native `Android.Media.Midi` API.
  -> APPROVED: to assess interest and how to implement simply

---

**More complex (heavy lifting or blocking tasks):**

- **Half-page turns:** Overhaul of the reader to simultaneously display two different half-pages and recalibrate annotation placement.
  -> UNDER CONSIDERATION: if there is strong user demand

- **Windows Port:** Necessary rewrite of all modules tied to Android hardware (Wi-Fi Direct P2P, camera scanner, file management).
  -> UNDER CONSIDERATION: currently, part of the application already runs completely on Windows.

- **Real-time Cloud Sync:** Complex handling of offline SQLite/PDF merge conflicts and requirement to update the Google Play privacy policy.
  -> NOT APPROVED FOR NOW: users prefer a local solution