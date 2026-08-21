<p align="center">
  <img src="Resources/maytrix-banner.png" alt="Maytrix Menu banner" width="900">
</p>

<h1 align="center">Maytrix Menu</h1>

<p align="center">
  A polished, controller-first square menu for local PCVR settings, accessibility, performance monitoring, and reversible visual presets.
</p>

<p align="center">
  <a href="https://github.com/koolaid-dotcom/Maytrix-Mods/releases/tag/v0.3.0"><img alt="Download v0.3.0 Beta" src="https://img.shields.io/badge/download-v0.3.0%20beta-19d8ff?style=for-the-badge&amp;logo=github&amp;logoColor=white"></a>
  <a href="https://discord.com/channels/1540210584149295134"><img alt="Open Discord (existing members)" src="https://img.shields.io/badge/Discord-open%20community-5865F2?style=for-the-badge&amp;logo=discord&amp;logoColor=white"></a>
  <a href="https://github.com/koolaid-dotcom/Maytrix-Mods/actions/workflows/build.yml"><img alt="Build status" src="https://github.com/koolaid-dotcom/Maytrix-Mods/actions/workflows/build.yml/badge.svg?branch=main"></a>
  <img alt="PCVR" src="https://img.shields.io/badge/target-PCVR-8b5cf6?style=for-the-badge">
  <img alt="Local only" src="https://img.shields.io/badge/privacy-local%20only-2563eb?style=for-the-badge">
</p>

<p align="center">
  <a href="https://github.com/koolaid-dotcom/Maytrix-Mods/releases/tag/v0.3.0"><strong>Download v0.3.0</strong></a> ·
  <a href="#install-for-testing">Install</a> ·
  <a href="#categories">Features</a> ·
  <a href="#controls">Controls</a> ·
  <a href="https://discord.com/channels/1540210584149295134"><strong>Discord</strong></a> ·
  <a href="CHANGELOG.md">Changelog</a> ·
  <a href="https://github.com/koolaid-dotcom/Maytrix-Mods/issues">Issues</a> ·
  <a href="#build">Build</a>
</p>

---

> [!IMPORTANT]
> **Version 0.3.0 is a PCVR beta.** Both build modes pass with zero warnings, including validation against the currently installed game and BepInEx files. Real-headset testing is still required before a stable release.

Maytrix contains no gameplay cheats, player targeting, anti-cheat bypasses, room disruption, telemetry, or background network requests.

## At a glance

| Item | Details |
|---|---|
| Menu | Square world-space panel with six categories |
| Open | Hold the menu-hand primary button |
| Select | Aim with the opposite hand and press its trigger |
| Platform | Unity Mono PCVR with BepInEx 5 |
| Version | 0.3.0 Beta |
| Privacy | Local settings only; no telemetry or background requests |

## Categories

### Discord

- Opens the existing-members Maytrix Discord page only after a deliberate two-press confirmation
- Uses the fixed address `https://discord.com/channels/1540210584149295134`
- Opens the PC's default browser; Maytrix never reads Discord login details or tokens
- Includes an explicit GitHub button with the same confirmation behavior

### Performance

- Smoothed FPS and frame-time display
- Optional floating FPS counter
- Custom optimizer goals: 72, 80, 90, or 120 FPS
- Original, Balanced, and Performance render-scale profiles
- Optional Auto Optimize with warm-up, sustained-low-FPS detection, cooldowns, and gradual recovery
- Manual unused-memory cleanup with single-flight protection and a 60-second cooldown
- Full performance reset

Internet speed does not determine rendering FPS. Maytrix performs no speed test and sends no network request. The FPS Goal guides local quality adjustment; it does not force a headset refresh rate. The XR runtime remains in control of the headset's actual refresh rate.

### Lighting

- Smooth Lighting: softer, higher-quality local lighting
- Normal Lighting: balanced local lighting
- Rough Lighting: simpler, lower-cost local lighting
- Reset Lighting: confirmed restoration of the exact captured game values

Maytrix captures Performance and Lighting separately, applies only local Unity quality settings, and restores values it still owns when reset, unloaded, or moved to a new scene.

### Appearance

- Midnight, Neon, and High Contrast themes
- Cyan, Purple, Emerald, and Amber accents
- Three text sizes
- Three menu sizes
- Thin, Normal, and Bold pointers

### Controls

- Left- or right-hand menu placement; the opposite controller becomes the pointer
- Close, Normal, and Far placement
- Smoothed menu following
- Pointer-angle calibration from -30 to +30 degrees
- Optional selection haptics

### Help & About

- In-menu controls guide
- Version, privacy, and safety information
- Confirmed Reset Everything action

## Interaction safeguards

- Opening while the trigger is held cannot activate a button until release and a fresh press
- Page changes and confirmed actions re-arm the trigger
- External and reset actions require two presses within five seconds
- Actions have a 250 ms debounce
- Tracking loss hides the menu and clears confirmations
- The pointer uses collider-free local button hit tests and never queries game-world colliders
- Text refreshes four times per second instead of rebuilding the menu every frame

## Install for testing

1. Use a legitimate BepInEx 5 setup for a compatible Unity Mono PCVR game.
2. Download [Maytrix Menu v0.3.0 Beta](https://github.com/koolaid-dotcom/Maytrix-Mods/releases/tag/v0.3.0). If upgrading from v0.2.0, read the cleanup note below before copying files.
3. Extract the `Maytrix Menu` folder from `Maytrix-Menu-v0.3.0-Beta.zip` into `BepInEx/plugins/`.
4. Confirm that `BepInEx/plugins/Maytrix Menu/Maytrix-Menu.dll` exists.
5. Start the game and check the BepInEx log for `Maytrix Menu 0.3.0 loaded`.
6. Hold the left controller primary button, aim with the right controller, and press trigger.

### Upgrading from v0.2.0

Close the game and remove the old `BepInEx/plugins/Maytrix Mods/Maytrix Mods.dll` before copying the new `Maytrix Menu` folder. Version 0.3.0 keeps the existing BepInEx plugin identity for upgrade compatibility, but the DLL and install-folder names changed; leaving both copies installed can cause duplicate-plugin errors.

Do not copy Unity, BepInEx, or game DLLs from the build folder. Controller names and behavior vary by headset/runtime, so complete the real-headset checklist in `VALIDATION.md` before publishing.

Settings are saved in the BepInEx configuration for `com.maytrixmods.menu`.

## Build

Requires the .NET 8 SDK or newer.

Compile without local game files:

```powershell
dotnet restore MaytrixMenu.sln -p:UseLocalGameAssemblies=false
dotnet build MaytrixMenu.sln -c Release --no-restore -p:UseLocalGameAssemblies=false -p:TreatWarningsAsErrors=true
```

Validate against a legitimate local installation without installing anything:

```powershell
dotnet build MaytrixMenu.sln -c Release -p:GorillaTagPath="D:\SteamLibrary\steamapps\common\Gorilla Tag" -p:InstallAfterBuild=false -p:TreatWarningsAsErrors=true
```

After reviewing and headset-testing the source, installation can be explicitly enabled:

```powershell
dotnet build MaytrixMenu.sln -c Release -p:GorillaTagPath="D:\SteamLibrary\steamapps\common\Gorilla Tag" -p:InstallAfterBuild=true
```

The DLL is written to `bin/Release/netstandard2.1/Maytrix-Menu.dll`. Automatic installation is off by default.

## Project map

| File | Purpose |
|---|---|
| `Plugin.cs` | Starts and safely disposes the menu and graphics services |
| `Menu/MenuController.cs` | Square category UI, navigation, pointer interaction, confirmations, and links |
| `Menu/FpsHud.cs` | Optional local FPS and frame-time overlay |
| `Menu/MenuSettings.cs` | Persistent settings with safe value limits |
| `Menu/XrPoseResolver.cs` | Converts headset/controller tracking poses into world space |
| `Services/LocalGraphicsController.cs` | Headset-aware FPS sampling, render profiles, lighting snapshots, restoration, and cleanup cooldown |
| `VALIDATION.md` | Completed automated checks and required headset tests |
| `.github/workflows/build.yml` | Compile-only GitHub Actions validation |
| `.github/workflows/release.yml` | Tag-driven beta packaging and prerelease publishing |

## Safety boundary

Appropriate additions include local interface appearance, accessibility, controller comfort, diagnostics, and reversible graphics settings.

Do not add movement advantages, fly, noclip, platforms, invisibility, teleporting, forced interactions, player/file scanning, anti-cheat bypasses, hidden web requests, or copied code/assets without permission.

Use modifications only where the game and platform permit them. Maytrix is independent and is not affiliated with or endorsed by Another Axiom or Discord.

## License

Maytrix is source-available under the repository's custom all-rights-reserved `LICENSE`, including its limited contribution exception. It is not open source. Read the license before copying, modifying, or redistributing the project. Third-party game, Unity, BepInEx, Discord, and GitHub names or assets are not included or claimed.
