# Contributing to Maytrix

Thanks for helping improve Maytrix Mod Menu. The project is currently an early source preview, so small, focused changes are easiest to review.

## Good contributions

- Interface polish and accessibility
- Performance and reliability fixes
- Documentation and setup improvements
- Harmless local or private-environment features
- Tests and compatibility reports

Changes intended to harass players, bypass protections, steal data, disrupt public rooms, or misrepresent another project will not be accepted.

## Development setup

1. Fork the repository only for the purpose of preparing a contribution back to Maytrix, then create a descriptive branch.
2. Install the .NET SDK and a compatible BepInEx setup.
3. Set `GorillaTagPath` to your local Gorilla Tag installation.
4. Build with `dotnet build MaytrixMods.sln -c Release`.
5. Test controller alignment, readability, and every changed control in VR.

Do not commit game DLLs, BepInEx binaries, account data, tokens, or private logs.

## Pull request checklist

- Explain what changed and why.
- Keep unrelated changes out of the pull request.
- Include the exact build or test performed.
- Mark anything that could not be tested.
- Use original code and assets, or clearly document permission.

The narrow permission in `LICENSE` allows a fork and modification only to prepare a pull request back to this repository. The maintainers may ask for additional licensing confirmation before accepting outside contributions.
