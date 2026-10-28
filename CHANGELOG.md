# Changelog - Over the Hill VR (Fan Edition)

All notable changes to this project will be documented in this file.

## [1.6.2-dev] - 2026-10-28
### Added
- **Step-by-Step Installation & Setup Guide (`INSTALL_GUIDE.md`)**:
  - Comprehensive guide covering end-to-end setup for Over the Hill VR with MelonLoader 0.7.3.
  - Non-destructive archival and backup protocol preventing accidental overwrite of existing or original game files (`version.dll`, `MelonLoader/`, `Mods/`, `UserData/`).
  - Clear rollback and uninstallation procedures for instant reversion to pure vanilla state.
  - Dedicated hardware configuration sections for Fanatec CSL DD (Topology A daisy-chain and Topology B split USB), Logitech G29, Meta Quest Touch controllers, and standard gamepads.
  - Steam launch parameters (`-force-d3d11`) and in-game calibration/hotkey reference (`F6`, `F8`, `F11`, `F12`, seat adjustments).
- **Documentation Linkage**: Linked `INSTALL_GUIDE.md` in `README.md`.

## [1.6.1-dev] - 2026-10-27
### Added
- **Fanatec CSL DD & Direct Drive Wheel Base Support**:
  - Implemented device identification for Fanatec hardware (CSL DD, GT DD Pro, ClubSport DD, Podium) with dedicated DirectInput scoring (+20 priority).
  - Implemented **Dual-Topology Input Architecture**:
    - **Topology A (Unified Hub)**: Supports single-connection setups with pedals, shifters, and handbrakes daisy-chained via RJ12 directly into the back of the CSL DD base.
    - **Topology B (Split USB)**: Auto-detects and processes modular setups where pedals or shifters are plugged into separate USB ports on the PC.
  - **Load Cell Brake Support**: Dynamically reads load cell brake axis signals (`lZ`) alongside standard potentiometer axes (`lRz`) and throttle (`lY`/`lZ`).
  - **Direct Drive Torque Safety**: Softened the startup calibration torque kick (reduced from 30% to 8%) to prevent sudden 8 Nm steering snaps on high-torque direct drive motors.
  - **Input Conflict Prevention**: Registered Fanatec device signatures (`fanatec`, `csl`, `clubsport`, `podium`) in `DisableRawWheelDevices` to prevent Unity's default InputSystem from interfering with DirectInput polling.
  - **C# Source Scaffold**: Extracted structured, editable C# source trees for both `OverTheHillVR` and `OverTheHillVR.CockpitUpdate` under `src/`.

### Fixed
- **MelonLoader Packaging Hierarchy**: Restructured root directories by relocating `net35/`, `net6/`, and `Documentation/` into standard `MelonLoader/` structure to ensure the native bootstrap `version.dll` resolves runtime assemblies properly.
- **MelonLoader Runtime Dependencies**: Restored 84 missing runtime dependency assemblies from official MelonLoader v0.7.3 x64 distribution that were lost during upstream web upload truncation.
