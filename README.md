<p align="center">
  <img src="Resources/maytrix-banner.png" alt="Maytrix Mod Menu banner" width="900">
</p>

<h1 align="center">Maytrix Mod Menu</h1>

<p align="center">
  A polished, original, controller-first world-space menu for the Maytrix Mods project.
</p>

<p align="center">
  <a href="https://github.com/koolaid-dotcom/Maytrix-Mods/releases/latest"><img alt="Version 0.2.0 Beta" src="https://img.shields.io/badge/version-0.2.0%20beta-19d8ff?style=for-the-badge"></a>
  <a href="https://github.com/koolaid-dotcom/Maytrix-Mods/actions/workflows/build.yml"><img alt="Automated build" src="https://github.com/koolaid-dotcom/Maytrix-Mods/actions/workflows/build.yml/badge.svg"></a>
  <img alt="PCVR target" src="https://img.shields.io/badge/target-PCVR-8b5cf6?style=for-the-badge">
  <img alt="Local interface features" src="https://img.shields.io/badge/features-local%20UI-2563eb?style=for-the-badge">
</p>

<p align="center">
  <a href="#install">Install</a> ·
  <a href="#controls">Controls</a> ·
  <a href="#working-features">Features</a> ·
  <a href="CHANGELOG.md">Changelog</a> ·
  <a href="https://github.com/koolaid-dotcom/Maytrix-Mods/issues">Issues</a> ·
  <a href="https://discord.com/channels/1520945955375943822">Discord</a>
</p>

---

Maytrix Mod Menu uses an original navy, cyan, and violet design with a left-hand panel and right-hand laser pointer. Version 0.2.0 replaces the old demonstration placeholders with working local interface controls and an automated build-and-release pipeline.

> [!IMPORTANT]
> **This is a PCVR beta.** GitHub Actions compiles the DLL before publishing it, but real-headset compatibility can still change when Gorilla Tag, Unity, or BepInEx updates. Please report your exact headset/runtime and attach the BepInEx log when filing a bug.

## Install

1. Install a compatible **BepInEx 5** setup for the Windows PC version of Gorilla Tag.
2. Open the [latest Maytrix release](https://github.com/koolaid-dotcom/Maytrix-Mods/releases/latest).
3. Download `Maytrix Mods.dll` or extract the release ZIP.
4. Put the DLL here:

```text
Gorilla Tag/BepInEx/plugins/Maytrix Mods/Maytrix Mods.dll
```

5. Start the game. BepInEx loads Maytrix automatically.

No installer, account, telemetry service, or extra Maytrix dependency is included.

## Controls

| Action | Control |
| --- | --- |
| Show the menu | Hold the left controller primary button or menu button |
| Hide the menu | Release that left controller button |
| Aim | Point the right controller |
| Press a card | Right trigger |
| Optional left-handed aiming | Enable **Left-Hand Pointer**, then use left trigger |

## Working features

<details open>
<summary><strong>Interface and information</strong></summary>

- Live smoothed FPS and frame-time display
- Live left/right primary, trigger, and grip input monitor
- Four original palettes: Neon, Aurora, Sunset, and Ice
- High-contrast mode, large text, minimal mode, and optional tooltips
- Soft-glow accent rail and pointer styling
- Saved interface choices and last-page memory

</details>

<details>
<summary><strong>Controls and comfort</strong></summary>

- Right- or left-controller selection beam
- Short, normal, and long pointer distances
- Five pointer-tilt adjustments for different controller aim poses
- Small, medium, and large panel sizes
- Smooth, balanced, and snappy panel following
- Wrist, palm, and floating panel anchors
- Toggleable haptics and hover animation
- Reduced-motion mode and a one-press comfort preset
- Full reset to safe defaults

</details>

<details>
<summary><strong>Community</strong></summary>

- Explicit cards for the Maytrix GitHub repository and configured Discord route
- Built-in control guide, credits, and version information
- No background network requests; links open only after you press them

</details>

## Build from source

The project can compile in two modes:

- On a development PC with Gorilla Tag installed, it uses the local game and BepInEx assemblies.
- In GitHub Actions, it uses pinned compile-only BepInEx and Unity reference packages and never bundles those dependencies into the plugin.

```powershell
git clone https://github.com/koolaid-dotcom/Maytrix-Mods.git
cd Maytrix-Mods
dotnet build MaytrixMods.sln -c Release
```

For a different Steam library:

```powershell
dotnet build MaytrixMods.sln -c Release -p:GorillaTagPath="D:\SteamLibrary\steamapps\common\Gorilla Tag"
```

A successful local build copies the plugin to `BepInEx\plugins\Maytrix Mods\` when that folder exists. Otherwise the DLL is in `bin\Release\netstandard2.1\`.

See [Architecture](Documentation/ARCHITECTURE.md), [Compatibility](Documentation/COMPATIBILITY.md), and [Local References](References/README.md) for details.

## Privacy, safety, and originality

Maytrix contains no telemetry, analytics, advertising, account system, remote configuration, anti-cheat bypass, or hidden web requests. It is designed for local interface customization in private or properly modded environments. Respect other players, the game's rules, and platform policies.

Maytrix is an independent community project and is not affiliated with, sponsored by, or endorsed by Another Axiom. Gorilla Tag and related names belong to their respective owners.

The Maytrix interface, banner, branding, and source are original. Seralyth code, artwork, logos, branding, donation information, and binaries are not included.

## Contact

- [GitHub repository](https://github.com/koolaid-dotcom/Maytrix-Mods)
- [Bug reports and feature requests](https://github.com/koolaid-dotcom/Maytrix-Mods/issues)
- [Configured Discord server route](https://discord.com/channels/1520945955375943822)

> [!NOTE]
> The supplied Discord URL works for existing server members; it is not a public `discord.gg` invitation. Replace it with a public invite when one is available.

## License

Copyright © 2026 Maytrix Mods. See [LICENSE](LICENSE) for the repository's terms.
