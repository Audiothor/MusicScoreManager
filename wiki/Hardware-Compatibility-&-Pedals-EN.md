> [🇫🇷 Version Française](Hardware-Compatibility-&-Pedals) | **🇬🇧 English**

# 🦶 Hardware Compatibility, Bluetooth Pedals & MIDI Controllers

Hands-free control on stage is critical for an uninterrupted musical performance. This page lists tested devices, switch mechanism comparisons, and optimal hardware settings for live reliability.

---

## 1. Tested & Recommended Bluetooth Page Turners Matrix

| Pedal Model | Switch Type | Integrated MSM Profile | Recommended Bluetooth Mode | Stage Notes |
| :--- | :--- | :--- | :--- | :--- |
| **PageFlip Dragonfly** (4 pedals) | Semi-silent mechanical | `PageFlip Dragonfly` | Mode 1 (Left/Right arrows) | **Highly Recommended**: 4 pedals allow assigning Previous/Next Page + Previous/Next Song in setlist. |
| **PageFlip Firefly & Butterfly** | Soft mechanical | `PageFlip Firefly & Butterfly` | Mode 1 (Arrows) or Mode 2 (PageUp/Down) | Heavy-duty, months of battery life on standard AA batteries. |
| **AirTurn Duo 500 & BT500S-2** | Silent membrane switch | `AirTurn Duo 500 & PEDpro` | Mode 2 (Standard HID keyboard) | 100% silent switches ideal for chamber music and acoustic recitals. |
| **AirTurn Quad 500** (4 pedals) | Silent membrane switch | `AirTurn Quad 500` | Mode 2 (Arrows + 1/2) | Complete control: 2 page turn pedals + 2 pedals for metronome or audio. |
| **AirTurn PEDpro** | Flat touch sensor | `AirTurn Duo 500 & PEDpro` | Mode 2 | Ultra-slim form factor, fits easily in instrument gig bags. |
| **Joyo JSP-01 Wireless** | Soft click mechanical | `Joyo JSP-01` | Mode 1 (Arrows) | Excellent value, USB-C rechargeable battery. |
| **Thomann / Harley Benton PageTurn** | Solid mechanical | `Thomann / Harley Benton` | Mode 1 (Arrows) | Rugged metal enclosure for amplified live concerts. |
| **Donner Wireless Page Turner** | Mechanical switch | `Donner Wireless` | Mode 1 or Mode 3 | Very popular, clear LED battery indicator. |
| **IK Multimedia iRig BlueTurn** | Soft backlit pads | `iRig BlueTurn` | Page Up / Down mode | Backlit soft buttons, very convenient on pitch-black stages. |
| **Coda Music STOMP** | Guitar-grade stomp switch | `Coda STOMP` | Mode 1 | Indestructible die-cast aluminum enclosure for guitarist pedalboards. |

---

## 2. Silent vs Mechanical Switches: Choosing for Your Genre

- **Classical Music, Acoustic Ensembles, Choirs & Theatre**:
  - Strongly choose **silent switches** (e.g., *AirTurn Duo 500*, *AirTurn PEDpro*, or *PageFlip Dragonfly*).
  - A mechanical click on a quiet stage or near sensitive vocal microphones is noticeable by audiences.
- **Modern Music, Rock, Jazz, Amplified Worship**:
  - **Mechanical switches** with tactile feedback (e.g., *Joyo*, *Thomann*, *Donner*, *Coda STOMP*) are favored because you feel the tactile click through stage shoes in loud environments.

---

## 3. MIDI Controllers (USB-OTG & Bluetooth MIDI)

Music Score Manager features a native MIDI event parser translating incoming hardware messages into instant stage commands:

### A. Supported MIDI Messages
1. **Control Change (CC)**:
   - `CC 64` (Sustain Pedal): value > 63 = pressed, value 0 = released.
   - `CC 66` (Sostenuto).
   - `CC 67` (Soft Pedal / Una Corda).
   - Any custom CC from 0 to 127.
2. **MIDI Notes (Note On / Note Off)**:
   - Master keyboards or MIDI bass pedals: map specific keys (e.g., C1 to F1) to turn pages or start the metronome.
3. **Program Change (PC)**:
   - Synchronize song changes in a setlist with synthesizer patch changes or guitar multi-effect presets.

### B. Recommended Connections
- **Wired USB-OTG**: Plug a USB-C to USB-A adapter into your Android tablet, then connect your pedalboard (Boss, Behringer FCB1010, Morningstar, Line 6 Helix). Zero latency, zero dropouts, and no wireless batteries to worry about.
- **Bluetooth MIDI (BLE)**: Connect via adapters such as *CME Widi Master* or native BLE hardware.

---

## 4. Crucial Android Tip: Restoring On-Screen Keyboard

When a Bluetooth pedal is connected in **HID Keyboard Mode**, Android may assume a physical typing keyboard is connected and suppress the virtual keyboard:
1. In Android settings, go to *System > Languages & Input > Physical Keyboard*.
2. Enable **« Show virtual on-screen keyboard »**.
3. Now you can type in the search bar or text annotation boxes without having to turn off your page-turning pedal!

---

## 5. Tablet Recommendations for Music Stands

| Device Category | Display Size & Aspect Ratio | Sheet Music Readability | Battery Life |
| :--- | :--- | :--- | :--- |
| **Large Tablets 12.4" to 13.3" (e.g., Samsung Galaxy Tab S8+/S9+/S10+, Lenovo P12 Pro)** | 12.4" to 12.7" (16:10) | **Outstanding**: Near 1:1 match to physical A4 paper. Ideal for conductors and classical pianists. | 8 to 11 hours |
| **Mid-Size Tablets 11" (e.g., Samsung Tab S9, Xiaomi Pad 6)** | 11.0" (16:10) | **Great**: Ideal balance between gig-bag portability and notation clarity. | 9 to 12 hours |
| **Compact Tablets 8" to 10"** | 8.4" to 10.1" | **Fair for singers/lead sheets**: Good for chord charts and lyrics, but small for 3-staff piano scores. | 7 to 9 hours |

> **Stand Mounting Tip**: For 12-inch tablets on microphone stands, use metal-reinforced clamps like *K&M (König & Meyer)* or *Hercules Stands* to eliminate risk of falling caused by stage vibrations.
