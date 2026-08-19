# Compatibility

Maytrix 0.2.0 is an automatically compiled PCVR beta. The release workflow verifies that the source builds, while real-headset validation is still community-reported.

| Environment | Status | Notes |
| --- | --- | --- |
| Windows PCVR | Beta | Intended runtime; install through BepInEx 5 |
| SteamVR headsets | Report requested | Uses standard Unity XR input devices |
| Meta Quest through PCVR | Report requested | Requires the Windows PC game and a working PCVR connection |
| Quest standalone | Not supported | This BepInEx project targets the PC game |
| Desktop without VR | Not supported | Maytrix requires XR controllers for hold-to-open and card interaction |

Compatibility can change when Gorilla Tag, Unity, or BepInEx updates. A useful report includes the game version, headset, XR runtime, BepInEx version, and `BepInEx/LogOutput.log`.
