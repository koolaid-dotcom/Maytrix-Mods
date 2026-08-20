# Local references

Do not commit or distribute game, Unity, or BepInEx DLLs. Maytrix references the Unity facade plus only the Core, Physics, Text Rendering, XR, VR, and Subsystems modules it needs; all remain external at runtime.

Validate against a legitimate local installation without installing:

```powershell
dotnet build MaytrixMenu.sln -c Release -p:GorillaTagPath="D:\SteamLibrary\steamapps\common\Gorilla Tag" -p:InstallAfterBuild=false -p:TreatWarningsAsErrors=true
```

Compile without local game files:

```powershell
dotnet build MaytrixMenu.sln -c Release -p:UseLocalGameAssemblies=false -p:TreatWarningsAsErrors=true
```

Add `-p:InstallAfterBuild=true` only after source review and real-headset testing, when you intentionally want the output copied to `BepInEx/plugins/Maytrix Menu/`.
