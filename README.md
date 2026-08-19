<p align="center">
  <img src="Resources/maytrix-banner.png" alt="Maytrix Mod Menu banner" width="900">
</p>

<h1 align="center">Maytrix Mod Menu</h1>

<p align="center">
  An original, controller-first world-space menu framework for the Maytrix Mods project.
</p>

<p align="center">
  <img alt="Version 0.1.0 Preview" src="https://img.shields.io/badge/version-0.1.0%20preview-19d8ff?style=for-the-badge">
  <img alt="Build not yet verified" src="https://img.shields.io/badge/build-not%20yet%20verified-f2a93b?style=for-the-badge">
  <img alt="PCVR target" src="https://img.shields.io/badge/target-PCVR-8b5cf6?style=for-the-badge">
  <img alt="Public source" src="https://img.shields.io/badge/source-public-2563eb?style=for-the-badge">
</p>

<p align="center">
  <a href="#installation">Installation</a> ·
  <a href="#controls">Controls</a> ·
  <a href="#compatibility">Compatibility</a> ·
  <a href="CHANGELOG.md">Changelog</a> ·
  <a href="CONTRIBUTING.md">Contributing</a> ·
  <a href="https://github.com/koolaid-dotcom/Maytrix-Mods/issues">Issues</a> ·
  <a href="https://discord.com/channels/1520945955375943822">Discord</a>
</p>

---

Maytrix Mod Menu is an early C# interface scaffold with an original navy, cyan, and violet identity. The current source provides a clean VR menu foundation and harmless demonstration settings that future modules can build upon.

> [!IMPORTANT]
> **Version 0.1.0 is a source preview.** It has not yet been compiled or tested against a live Gorilla Tag and BepInEx installation. There is currently no verified downloadable DLL release.

## Highlights

- World-space VR panel with original Maytrix styling
- Category sidebar and two-column option cards
- Toggle, action, and external-link card types
- Controller ray selection and trigger activation
- Hover descriptions, paging, enabled states, and haptic feedback
- High-contrast, reduced-motion, and tooltip preferences
- XR tracking-to-world conversion for moving player rigs
- Desktop `F6` menu-toggle fallback
- No bundled game assemblies, BepInEx binaries, telemetry, or hidden web requests

## Controls

| Action | Control |
| --- | --- |
| Open or close menu | Left controller primary/menu button |
| Aim at an option | Point the right controller |
| Activate an option | Right trigger |
| Desktop open/close test | `F6` |

The `F6` fallback toggles the interface. Full pointing and selection currently require XR controller input.

## Installation

<details>
<summary><strong>Build from source</strong></summary>

### Requirements

- Gorilla Tag installed through Steam
- A compatible BepInEx installation
- The .NET SDK capable of targeting .NET Standard 2.1
- Git, or a downloaded copy of this repository

Clone and build the project:

```powershell
git clone https://github.com/koolaid-dotcom/Maytrix-Mods.git
cd Maytrix-Mods
dotnet build MaytrixMods.sln -c Release
```

The project initially looks for Gorilla Tag here:

```text
C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag
```

For another Steam library, provide the game directory:

```powershell
dotnet build MaytrixMods.sln -c Release -p:GorillaTagPath="D:\SteamLibrary\steamapps\common\Gorilla Tag"
```

When a valid BepInEx plugins directory is detected, a successful build copies the DLL to:

```text
<GorillaTag>\BepInEx\plugins\Maytrix Mods\
```

Otherwise, normal output appears under `bin\Release\netstandard2.1\`.

Game DLLs and BepInEx binaries are referenced locally and intentionally not committed. See [References](References/README.md).

</details>

## Compatibility

<details>
<summary><strong>Current targets and limitations</strong></summary>

| Component | Current target |
| --- | --- |
| Game platform | Windows PC / Steam |
| Mod framework | BepInEx |
| Runtime target | .NET Standard 2.1 |
| Input | Standard Unity XR controller input |
| Headsets | PCVR devices exposed through a compatible XR runtime |
| Standalone Quest / Android | Not supported by this project target |
| Verified game versions | None yet |

Compatibility information will be updated after the first successful in-game build and test. See the full [compatibility notes](Documentation/COMPATIBILITY.md).

</details>

## Project structure

```text
Maytrix-Mods/
├── .github/                 # Issue and pull-request templates
├── Documentation/          # Architecture and compatibility notes
├── Managers/               # Runtime ownership and lifecycle
├── Menu/                   # Rendering, input, cards, and interaction
├── Mods/                   # Guidance for future feature modules
├── References/             # Local-only dependency instructions
├── Resources/              # Original Maytrix branding
├── Plugin.cs               # BepInEx entry point
├── PluginInfo.cs           # Identity, version, and public links
├── MaytrixMods.csproj      # Build and reference configuration
└── MaytrixMods.sln         # Visual Studio solution
```

Read [Architecture](Documentation/ARCHITECTURE.md) for the runtime flow and design rules.

## Contact

<details>
<summary><strong>Project links</strong></summary>

- [GitHub repository](https://github.com/koolaid-dotcom/Maytrix-Mods)
- [Report a problem or request an improvement](https://github.com/koolaid-dotcom/Maytrix-Mods/issues)
- [Maytrix Mods Discord route](https://discord.com/channels/1520945955375943822)

> [!NOTE]
> The supplied Discord URL is a server route for people who are already members. It is **not a public join invitation**. A `discord.gg/...` invite can replace it when one is available.

</details>

## Privacy and security

The current preview contains no telemetry, analytics, account system, remote configuration, advertising, or user-data collection. Community cards only ask Unity to open the configured Discord or GitHub page after the user deliberately selects one.

Read [Security Policy](SECURITY.md) before reporting anything sensitive.

## Responsible use

Use Maytrix only in private or properly modded environments where modifications are allowed. Respect other players, the game's rules, and platform policies.

Maytrix Mod Menu is an independent community project. It is not affiliated with, sponsored by, or endorsed by Another Axiom. Gorilla Tag and related names belong to their respective owners.

The interface, banner, branding, and source in this repository are original Maytrix work. Seralyth source code, artwork, logos, branding, donation information, and release files are not included.

## License

Copyright © 2026 Maytrix Mods. All rights reserved. The repository is public for transparency and review; public visibility alone does not grant permission to redistribute or repackage it. See [LICENSE](LICENSE).
