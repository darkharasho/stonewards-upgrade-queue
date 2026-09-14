# stonewards-upgrade-queue

A BepInEx/Harmony mod for Stonewards (a Unity game) that stops upgrade popups from interrupting play. Instead of opening right away, they go into a queue with a small on-screen counter, and players open them later with a rebindable hotkey or a button. Upgrade choices and odds stay exactly as in the normal game. The mod is packaged for Thunderstore so it can be installed with r2modman.

## Development

Requires the .NET SDK, Stonewards, and an r2modman profile with BepInExPack 5.4.2305 installed.

- `dotnet build src/UpgradeQueue/UpgradeQueue.csproj` builds the plugin and copies it into the r2modman profile's `BepInEx/plugins/darkharasho-UpgradeQueue`.
- `scripts/package.sh` builds Release and writes a Thunderstore zip to `dist/`. It fails if the versions in `thunderstore/manifest.json`, `Plugin.cs` and the `.csproj` differ, or if `CHANGELOG.md` has no entry for that version. The package uses `thunderstore/README.md` (the store page), not this file.
- `thunderstore/icon.svg` is the icon source. Regenerate the PNG with `rsvg-convert -w 256 -h 256 thunderstore/icon.svg -o thunderstore/icon.png`.

Game and profile paths default to this dev machine. Override them with `-p:GamePath=... -p:ProfilePath=...` or a gitignored `Local.props`:

```xml
<Project>
  <PropertyGroup>
    <GamePath>C:\Program Files (x86)\Steam\steamapps\common\Stonewards</GamePath>
    <ProfilePath>C:\Users\you\AppData\Roaming\r2modmanPlus-local\Stonewards\profiles\Default</ProfilePath>
  </PropertyGroup>
</Project>
```
