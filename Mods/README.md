# Feature modules

This directory is reserved for future Maytrix feature modules. Version 0.2.0 keeps its working local interface features in `Menu/MaytrixMenu.cs` while the extension boundary remains small.

A module should:

- have one clear purpose;
- expose its state separately from menu rendering;
- clean up everything it creates;
- document compatibility and testing;
- avoid hidden network activity; and
- be suitable for private or properly modded environments without disrupting other players.

The 0.2.0 beta contains local interface and diagnostic options only. Future modules should follow the same private/modded-environment and no-disruption rules.
