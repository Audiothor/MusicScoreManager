# 🚪 Quit Menu

The **Quit** menu provides a clean and safe exit from the application.

---

## 🔒 Safe Exit Workflow

When you tap the **Quit (🚪)** tab in the bottom navigation bar:

1. **Confirmation Prompt (Optional)**:
   - If enabled in settings, a confirmation prompt asks you to verify before exiting the application.
2. **Session State Persistence**:
   - The application automatically commits any pending changes, active filters, last viewed scores, and SQLite database transactions to disk.
3. **Resource & Memory Cleanup**:
   - PDF rendering buffers, background audio threads, and hardware resources are gracefully freed to optimize device battery life.
4. **Graceful App Termination**:
   - A brief status animation confirms disk writes are complete before closing the application cleanly with zero risk of database corruption.

---

## ⚙️ Settings Customization

Under **Settings > App Settings (General)**, the **Application Behavior** section includes two customization switches:
- **Show « Quit » tab in bottom navigation**: Toggle to hide this tab if you navigate via Android system gestures or your device's physical back button.
- **Confirm Before Quitting**: Protects against accidental exits with a confirmation alert.
