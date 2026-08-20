# Changelog

All notable Maytrix Menu changes are recorded here.

## 0.3.0 Beta - 2026-08-19

- Replaced the linear template with a square six-category Maytrix menu
- Added explicit Discord and GitHub opening confirmations
- Added smoothed FPS/frame-time reporting and an optional FPS HUD
- Added configurable optimizer goals and reversible render-scale profiles
- Added conservative automatic low-FPS optimization with warm-up and cooldowns
- Added manual unused-asset cleanup with single-flight and 60-second protection
- Added Smooth, Normal, Rough, and exact-value Reset Lighting controls
- Added themes, accents, text sizing, menu sizing, pointer sizing, handedness, placement, smoothing, angle calibration, and haptics
- Added page navigation, trigger arming, action debounce, confirmations, toasts, and persistent configuration
- Added captured graphics restoration on reset, scene change, and plugin disposal

## 0.2.0 Beta - 2026-08-19

### Added

- Hold-to-open left-controller behavior
- Live FPS/frame-time and controller-input displays
- Four original Maytrix color palettes
- Menu size, pointer length/hand/tilt, follow speed, and anchor controls
- Large text, minimal mode, comfort preset, and saved preferences
- Automated clean-room compile, ZIP packaging, and prerelease publishing

### Changed

- Replaced every placeholder card with a working local interface action
- Reorganized the sidebar into Home, Interface, Controls, Access, and Community
- Updated the README with drag-and-drop installation and exact controls

### Validation

- Source was compiled by GitHub Actions before an artifact or release was published
- Real-headset and current-game-version compatibility reports were still requested

## 0.1.0 Preview - 2026-08-19

### Added

- Original Maytrix world-space VR interface scaffold
- Category navigation, two-column cards, paging, and hover descriptions
- Controller ray selection, trigger input, haptics, and an F6 desktop fallback
- XR tracking-space to world-space pose conversion
- High-contrast, reduced-motion, and tooltip preferences
- Community buttons for the Maytrix repository and Discord route
- Public project documentation and original Maytrix branding

### Known limitations

- No compiled public build was available
- In-game validation against the current game and BepInEx versions was pending
- The supplied Discord address was a server route for existing members, not a public invite
