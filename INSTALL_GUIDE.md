# Over the Hill VR — Installation & Setup Guide

A complete, step-by-step setup and deployment guide for the **Over the Hill VR (Fan Edition)** mod, featuring MelonLoader 0.7.3, full 6DOF PCVR cockpit integration, dynamic dashboard & GPS telemetry, Logitech G29 support, and Fanatec CSL DD dual-topology Direct Drive input.

---

## Table of Contents
1. [Prerequisites & System Requirements](#1-prerequisites--system-requirements)
2. [Safety First: Archiving & Backing Up Existing Files](#2-safety-first-archiving--backing-up-existing-files)
3. [Mod Installation Steps](#3-mod-installation-steps)
4. [Steam Launch Options Setup](#4-steam-launch-options-setup)
5. [Controller & Steering Wheel Configuration](#5-controller--steering-wheel-configuration)
6. [First Launch & In-Game Calibration](#6-first-launch--in-game-calibration)
7. [Rollback & Uninstallation Protocol](#7-rollback--uninstallation-protocol)
8. [Troubleshooting & Diagnostics](#8-troubleshooting--diagnostics)

---

## 1. Prerequisites & System Requirements

Before installing the mod, verify the following prerequisites are installed and ready:

* **Base Game**: A legal Windows Steam copy of **Over the Hill** installed on your PC.
* **VR Headset**: PCVR headset (Meta Quest 2 / 3 / Pro via Link, AirLink, or Virtual Desktop; Valve Index; HTC Vive; Bigscreen Beyond; etc.).
* **SteamVR**: Installed and configured as your active OpenXR / SteamVR runtime.
* **.NET 6 Desktop Runtime (Windows x64)**: Required by MelonLoader 0.7.3.  
  Download: [.NET 6.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* **Visual C++ 2015–2022 Redistributable (x64)**:  
  Download: [Microsoft VC++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)
* **Hardware Drivers (if using wheel/pedals)**:
  * **Fanatec**: Fanatec Control Panel and/or FanaLab.
  * **Logitech**: Logitech G HUB (for G29 / G920 / G923).

---

## 2. Safety First: Archiving & Backing Up Existing Files

> [!IMPORTANT]
> **Never overwrite your existing files directly without archiving.**
> If you have previously installed mods, earlier builds of MelonLoader, or custom wrappers, follow this archival procedure to preserve original files and enable an instant rollback.

### Step 2.1: Locate the Game Directory
1. Open your **Steam Client**.
2. Go to **Library** > Right-click **Over the Hill** > **Manage** > **Browse local files**.
3. A Windows Explorer window will open directly to your game root folder (e.g., `SteamLibrary\steamapps\common\Over the Hill\`).

### Step 2.2: Create an Archival Directory
Inside the game root folder, create a new backup folder:
* Name it `_ORIGINAL_BACKUP` or `_MOD_BACKUP_YYYYMMDD`.

### Step 2.3: Check for and Move Conflicting/Original Files
Check if any of the following files or folders already exist in the game directory:

| Item in Game Folder | Action | Recommended Safe Archival Destination |
| :--- | :--- | :--- |
| `version.dll` | **Rename / Move** | Move to `_ORIGINAL_BACKUP\version.dll` (or rename to `version.dll.orig`) |
| `MelonLoader\` | **Rename / Move** | Move folder to `_ORIGINAL_BACKUP\MelonLoader\` |
| `Mods\` | **Rename / Move** | Move folder to `_ORIGINAL_BACKUP\Mods\` |
| `UserData\` | **Rename / Move** | Move folder to `_ORIGINAL_BACKUP\UserData\` |
| `openvr_api.dll` (if in root) | **Rename / Move** | Move to `_ORIGINAL_BACKUP\openvr_api.dll` |

> [!NOTE]
> A clean vanilla installation of *Over the Hill* does **not** ship with a root `version.dll`, `MelonLoader\`, or `Mods\` folder. If these do not exist in your game folder, you are running a clean vanilla install and may proceed directly to Step 3.

---

## 3. Mod Installation Steps

Deploying the mod consists of placing the mod distribution files beside `over the hill.exe`:

1. Download or unpack the **Over the Hill VR** mod package (or your local `overthehillvr-fan` build).
2. Copy the following items from the mod folder:
   * `version.dll` *(Native MelonLoader bootstrap hook)*
   * `MelonLoader\` *(Core runtime & dependencies)*
   * `Mods\` *(Contains `OverTheHillVR.dll` and `OverTheHillVR.CockpitUpdate.dll`)*
   * `UserData\` *(Contains preconfigured default preferences)*
3. Paste all copied files and folders directly into your game directory (the folder where `over the hill.exe` is located).
4. Verify your folder structure resembles the following:

```text
Over the Hill/
├── _ORIGINAL_BACKUP/          <-- (Your archived files from Step 2)
├── over the hill.exe          <-- Base game executable
├── over the hill_Data/        <-- Base game data folder
├── version.dll                <-- Mod bootstrap
├── MelonLoader/               <-- Mod runtime engine
├── Mods/                      <-- VR mod assemblies
│   ├── OverTheHillVR.dll
│   └── OverTheHillVR.CockpitUpdate.dll
└── UserData/                  <-- Configuration preferences
    └── MelonPreferences.cfg
```

---

## 4. Steam Launch Options Setup

To force the Unity engine to initialize Direct3D 11 (essential for the OpenVR render hook to display stereo VR viewports):

1. In Steam, right-click **Over the Hill** and select **Properties...**.
2. Under the **General** tab, locate **Launch Options**.
3. Type or paste the following argument:
   ```text
   -force-d3d11
   ```
4. Close the Properties window.

---

## 5. Controller & Steering Wheel Configuration

### A. Wheel & Pedals: Critical Steam Input Setting
> [!WARNING]
> Steam Input will intercept and translate raw DirectInput wheel calls into generic controller axes, which breaks force feedback and custom axis recognition.

1. In Steam, right-click **Over the Hill** > **Properties...** > **Controller**.
2. Under **Override for Over the Hill**, select **Disable Steam Input** from the dropdown.

### B. Fanatec Direct Drive (CSL DD / GT DD Pro / ClubSport DD / Podium)
* **Rotation**: Open Fanatec Control Panel / FanaLab and set your wheel angle to **900°**.
* **Topology Support**:
  * **Topology A (Unified Base Hub)**: If your pedals, shifter, or handbrake are connected directly into the back of your CSL DD base via RJ12, the mod reads all axes and buttons through the unified base DirectInput profile.
  * **Topology B (Split USB)**: If your pedals are connected via standalone USB directly to the PC, the mod automatically searches and binds the auxiliary pedal controller.
* **Load Cell Brakes**: Both standard potentiometer axes (`lRz`) and load cell brake axes (`lZ`) are automatically handled with Direct Drive safety damping applied.

### C. Logitech Wheels (G29 / G920 / G923)
* Keep **Logitech G HUB** running in the background.
* Set Operating Range to **900°** in G HUB.
* Keep keyboard and mouse assignments disabled in the active G HUB game profile.

### D. Meta Quest Touch Controllers
* Disconnect or unplug physical gamepads or wheels if you wish to steer using VR hand gestures (motion controllers).
* Power on and wake both Quest controllers **before** launching the game.
* Grip either or both controllers like a virtual steering wheel and rotate to steer; release grip to self-center.

### E. Standard Gamepads (Xbox / PlayStation)
* Plug and play; default bindings are active in [`UserData/MelonPreferences.cfg`](file:///D:/Projects/overthehillvr-fan/UserData/MelonPreferences.cfg).

---

## 6. First Launch & In-Game Calibration

1. Put your VR headset on or connect via Quest Link / Virtual Desktop.
2. Ensure **SteamVR** is running and status indicates the headset is tracking.
3. Launch **Over the Hill** through Steam.
4. **First Launch Notice**: The first boot will take **1 to 3 minutes**. MelonLoader must assemble dynamic interop assemblies and caching layers (`MelonLoader/Dependencies/SupportModules/`). A diagnostic console window will open displaying startup progress; do not close it.
5. In-Game View & Seat Adjustment:
   * **Enter Driver's Seat**: Zoom all the way into the vehicle to trigger the 6DOF cockpit camera, functional mirrors, and live GPS map.
   * **Recenter View**: Press `F6` (or click both thumbsticks together on gamepad/Quest).
   * **Toggle VR Mode**: Press `F8` on your keyboard (or `View + Y` on gamepad).
   * **Direct Cockpit Toggle**: Press `F11`.
   * **Seat Adjustments**:
     * `Page Up` / `Page Down`: Raise / lower driver seat height.
     * `Home` / `End`: Move driver seat forward / backward.
     * `Insert` / `Delete`: Move driver seat left / right.
   * **Toggle HUD**: Press `F12`.

---

## 7. Rollback & Uninstallation Protocol

If you ever wish to disable the mod or revert completely to the pure vanilla game:

1. Close the game completely.
2. In the game root directory, delete:
   * `version.dll`
   * `MelonLoader\`
   * `Mods\`
   * `UserData\`
3. If you archived an original `version.dll` or other files in Step 2, restore them from your `_ORIGINAL_BACKUP` folder back to the game root.
4. (Optional) In Steam, right-click **Over the Hill** > **Properties** > **Installed Files** > click **Verify integrity of game files** to ensure all vanilla assets are 100% verified.

---

## 8. Troubleshooting & Diagnostics

* **Game starts on desktop monitor only (Flat Mode)**:
  * Check that `-force-d3d11` is entered in Steam Launch Options.
  * Confirm SteamVR is active and tracking before launching the executable.
  * Press `F8` on the keyboard inside the game to force the VR camera hook.
* **Game launches without MelonLoader console**:
  * Verify that `.NET 6 Desktop Runtime (x64)` is installed on Windows.
  * Verify `version.dll` is located in the exact same folder as `over the hill.exe`.
* **Wheel / Pedals do not respond or invert**:
  * Verify that Steam Input is disabled in Steam game properties.
  * Check [`UserData/MelonPreferences.cfg`](file:///D:/Projects/overthehillvr-fan/UserData/MelonPreferences.cfg) to fine-tune deadzones, inversion, and sensitivity flags.
* **Inspecting Diagnostic Logs**:
  * Mod logs and loader diagnostic traces are written directly to [`MelonLoader/Latest.log`](file:///D:/Projects/overthehillvr-fan/MelonLoader).
