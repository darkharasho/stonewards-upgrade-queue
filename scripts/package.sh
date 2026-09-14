#!/usr/bin/env bash
# Builds a Release DLL and zips a Thunderstore/r2modman package into dist/.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(python3 -c "import json;print(json.load(open('$root/thunderstore/manifest.json'))['version_number'])")"
stage="$root/dist/stage"
zipfile="$root/dist/darkharasho-UpgradeQueue-$version.zip"

dotnet build "$root/src/UpgradeQueue/UpgradeQueue.csproj" -c Release

rm -rf "$stage" "$zipfile"
mkdir -p "$stage/plugins"
cp "$root/thunderstore/manifest.json" "$root/thunderstore/icon.png" "$root/README.md" "$root/CHANGELOG.md" "$stage/"
cp "$root/src/UpgradeQueue/bin/Release/UpgradeQueue.dll" "$stage/plugins/"

(cd "$stage" && zip -r "$zipfile" .)
echo "Package: $zipfile"
