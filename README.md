# Maytrix Mod Menu

An original, polished world-space VR menu scaffold for the **Maytrix Mods** Gorilla Tag project.

The design takes general inspiration from modern category-based VR menus while using original Maytrix branding, structure, colors, and code. It does not copy Seralyth artwork or source.

## Current layout

- Dark navy and black panel with cyan-violet Maytrix accents
- Left-side category navigation
- Two-column cards with enabled/disabled states
- Page navigation and hover tooltips
- Right-controller ray and trigger selection
- Haptic feedback
- Maytrix Mods Discord and GitHub buttons
- F6 desktop-testing fallback
- Harmless UI/demo options ready to connect to legitimate mods

## Controls

- **Left controller X/menu button:** open or close the menu
- **Right controller:** point at a card
- **Right trigger:** select a card
- **F6:** desktop testing fallback

## Build

1. Install BepInEx for Gorilla Tag.
2. Make sure Gorilla Tag is installed at the default Steam location, or pass a custom path:

   ```powershell
   dotnet build -c Release -p:GorillaTagPath="D:\SteamLibrary\steamapps\common\Gorilla Tag"
   ```

3. When the BepInEx plugins folder exists, the project copies `Maytrix Mods.dll` into `BepInEx/plugins/Maytrix Mods` after a successful build.

The project deliberately does not commit game DLLs or BepInEx binaries.

## Community links

- [Maytrix Mods Discord](https://discord.com/channels/1520945955375943822)
- [GitHub repository](https://github.com/koolaid-dotcom/Maytrix-Mods)

The current Discord address is a Discord server route. Replace `PluginInfo.DiscordUrl` with a public `discord.gg` invitation so new users can join directly.

## Responsible use

Keep gameplay mods in private or properly modded environments, respect the game's rules, and avoid features intended to disrupt other players.
