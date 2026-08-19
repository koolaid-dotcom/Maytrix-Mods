# Local references

Maytrix compiles against assemblies from a local Gorilla Tag and BepInEx installation.

Do not commit those DLLs. They may be copyrighted, version-specific, or unsafe if obtained from an unofficial source.

Set the local path during build:

```powershell
dotnet build MaytrixMods.sln -c Release -p:GorillaTagPath="D:\SteamLibrary\steamapps\common\Gorilla Tag"
```

The project file resolves the required assemblies beneath that directory.
