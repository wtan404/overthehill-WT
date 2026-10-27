# Changelog - Over the Hill VR (Fan Edition)

All notable changes to this project will be documented in this file.

## [1.6.1-dev] - 2026-10-27
### Fixed
- **MelonLoader Packaging Hierarchy**: Restructured root directories by relocating `net35/`, `net6/`, and `Documentation/` into standard `MelonLoader/` structure to ensure the native bootstrap `version.dll` resolves runtime assemblies properly.
- **MelonLoader Runtime Dependencies**: Restored 84 missing runtime dependency assemblies from official MelonLoader v0.7.3 x64 distribution that were lost during upstream web upload truncation.
