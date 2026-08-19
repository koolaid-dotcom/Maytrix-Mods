# Architecture

Maytrix is intentionally small and uses only BepInEx plus stable Unity APIs.

## Runtime flow

1. `Plugin` is loaded by BepInEx.
2. `MenuManager` owns the menu lifecycle.
3. `MaytrixMenu` builds and updates the world-space interface.
4. `MenuItem` stores each card's label, state, kind, and action.
5. GitHub Actions performs a clean compile and packages only the Maytrix DLL, README, and license.

## Main areas

| Path | Purpose |
| --- | --- |
| `Plugin.cs` | BepInEx entry point |
| `PluginInfo.cs` | Identity, version, and public links |
| `Managers/` | Runtime ownership and lifecycle |
| `Menu/` | Layout, interaction, rendering, and menu data |
| `Mods/` | Extension guidance for future feature modules |
| `References/` | Instructions for local-only game dependencies |
| `Resources/` | Project-owned branding and media |

## Design rules

- Keep the entry point small.
- Keep feature state out of rendering code when practical.
- Reuse materials and avoid per-frame allocations.
- Convert XR tracking poses into world space before positioning UI.
- Treat external links as explicit user actions.
- Do not add hidden telemetry or disruptive public-room behavior.
- Keep every visible card connected to a real local effect.
- Publish a binary only after the automated build succeeds.
