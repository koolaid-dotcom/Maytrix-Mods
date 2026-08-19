# Local references

For normal development, Maytrix compiles against assemblies from a local Gorilla Tag and BepInEx installation.

Do not commit those DLLs. They may be copyrighted, version-specific, or unsafe if obtained from an unofficial source.

Set the local path during build:

```powershell
dotnet build MaytrixMods.sln -c Release -p:GorillaTagPath="D:\SteamLibrary\steamapps\common\Gorilla Tag"
```

The project file resolves the required assemblies beneath that directory.

When those files are unavailable, such as in GitHub Actions, the project uses compile-only `BepInEx.Core` and `UnityEngine.Modules` package references. Those packages are not copied into `Maytrix Mods.dll` or the release ZIP. A local game build remains the best compatibility check because it uses the assemblies from the exact installed game version.
