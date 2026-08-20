# Architecture

Maytrix is a local BepInEx 5 plugin built from small services and Unity's standard XR APIs. It does not depend on Harmony, Photon, game assemblies, player APIs, or networking clients.

## Runtime flow

1. `Plugin` creates settings, graphics, and menu services.
2. `LocalGraphicsController` samples local frame timing and owns reversible visual overrides.
3. `XrPoseResolver` converts controller and headset tracking into world-space poses.
4. `MenuController` handles pages, visibility, confirmations, and collider-free pointer hit tests.
5. `FpsHud` optionally presents a head-locked, low-frequency FPS display.
6. Disposal restores every graphics value Maytrix still owns and destroys only Maytrix-created objects.

## Main areas

| Path | Purpose |
|---|---|
| `Plugin.cs` | BepInEx lifecycle entry point |
| `Menu/` | Square UI, controller interaction, themes, settings, and FPS HUD |
| `Services/` | Frame sampling and reversible performance/lighting ownership |
| `References/` | Local-only build reference guidance |
| `Documentation/` | Architecture and compatibility notes |

## Design rules

- Keep every action local, explicit, and reversible.
- Never scan player files or use player/network APIs.
- Never let menu hit testing interact with game colliders.
- Require fresh trigger presses and confirmations for external or reset actions.
- Ignore unfocused/background frame samples.
- Publish from an exact version tag only after CI passes.
