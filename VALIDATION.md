# Validation record

Last checked: 2026-08-19

## Passed

- Clean `netstandard2.1` release build using compile-only BepInEx 5.4.21 and Unity 2021.3 reference packages
- Clean release build against the currently installed Gorilla Tag, BepInEx, and Unity managed assemblies
- Zero compiler warnings with warnings treated as errors
- Formatting verification with no required source changes
- Framework and game DLLs are not copied into the output
- Assembly-reference allowlist contains only BepInEx, .NET Standard, and the required Unity Core, Physics, Text Rendering, XR, VR, and Subsystems modules
- Source/output scan contains no Harmony, Photon, game assembly, filesystem scanning, networking client, Malachi, or known cheat-feature references
- Discord and GitHub are fixed HTTPS constants and open only after an in-menu two-press confirmation
- Performance and Lighting have separate ownership plus captured-value reset and unload restoration paths
- Moving menu buttons use collider-free local hit tests and cannot participate in gameplay physics
- Background/unfocused frames are ignored by Auto Optimize and restart its warm-up window
- Unused-asset cleanup is manual, single-flight, and limited to once per 60 seconds
- Packaged source can be extracted and rebuilt independently

## Still requires a real-headset test

- Square layout, text direction, text fit, and pointer visibility
- Exact primary-button and trigger mappings for each supported controller/runtime
- Left/right menu-hand switching after close and reopen
- Menu placement and pointer-angle calibration
- FPS HUD placement and comfort in motion
- Auto Optimize behavior at 72, 80, 90, and 120 FPS goals
- Smooth, Normal, Rough, and exact-value Reset Lighting appearance
- Tracking loss, controller reconnect, application focus return, and scene transitions
- Return to the game after opening Discord or GitHub
- Haptic behavior on controllers with and without impulse support

A successful compile is not runtime certification. Test the exact packaged DLL against the exact game, BepInEx, headset, and XR runtime versions you plan to support before creating a public stable release.
