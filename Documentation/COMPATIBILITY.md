# Compatibility

Maytrix 0.3.0 is a Windows PCVR beta for Unity Mono environments using BepInEx 5. Compilation has been verified, but real-headset support remains under validation.

| Environment | Status | Notes |
|---|---|---|
| Windows PCVR | Beta | Intended runtime through BepInEx 5 |
| SteamVR headsets | Validation requested | Uses standard Unity XR input devices |
| Meta Quest through PCVR | Validation requested | Requires the Windows PC game and a working PCVR connection |
| Quest standalone | Not supported | This build targets Unity Mono on Windows |
| Desktop without XR controllers | Not supported | Opening and selection require tracked controllers |

Compatibility can change when the game, Unity, BepInEx, headset firmware, or XR runtime changes. A useful report includes the game version, headset, XR runtime, BepInEx version, controller mapping, and relevant sanitized BepInEx log lines.
