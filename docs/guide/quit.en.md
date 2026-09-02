# 🚪 Quit Menu

The **Quit** menu provides a clean and safe exit from the application.

---

## 🔒 Safe Exit Workflow

When you tap the **Quit (🚪)** tab in the bottom navigation bar:

1. **Session State Persistence**:
   - The application automatically commits any pending changes, active filters, last viewed scores, and SQLite database transactions to disk.
2. **Resource & Memory Cleanup**:
   - PDF rendering buffers, background audio threads, and hardware resources are gracefully freed to optimize device battery life.
3. **Graceful App Termination**:
   - A brief status animation confirms disk writes are complete before closing the application cleanly with zero risk of database corruption.
