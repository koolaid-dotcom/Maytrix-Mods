# Maytrix Menu v0.3.0 Beta

This beta introduces the complete square category menu with Discord, Performance, Lighting, Appearance, Controls, and Help & About sections.

## Download choice

- Most testers should use `Maytrix-Menu-v0.3.0-Beta.zip` and copy its `Maytrix Menu` folder into `BepInEx/plugins/`.
- Developers should use `Maytrix-Menu-v0.3.0-Source.zip`.
- The direct `Maytrix-Menu.dll` is provided for manual installation.

## Upgrading from v0.2.0

Close the game and remove `BepInEx/plugins/Maytrix Mods/Maytrix Mods.dll` before copying the new `Maytrix Menu` folder. The plugin identity is preserved, but the DLL and folder names changed; do not leave both versions installed.

## Important behavior

- Discord and GitHub open only after an in-menu two-press confirmation.
- FPS Goal controls Maytrix's local optimizer target; it does not change internet speed or force the headset refresh rate.
- Auto Optimize uses headset-aware frame timing and reversible render-scale adjustments.
- Unused-memory cleanup is manual, confirmed, single-flight, and limited to once per 60 seconds.
- Lighting and Performance capture, own, and restore their settings separately.

## Beta limitation

The source passes compile, formatting, dependency, and safety checks, including compilation against the currently installed game files. Real-headset layout, controller, focus-return, graphics, and comfort testing remains required before a stable release.
