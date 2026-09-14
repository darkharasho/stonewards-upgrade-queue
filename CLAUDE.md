## Project Context

A BepInEx/Harmony mod for Stonewards (a Unity game) that stops upgrade popups from interrupting play. Instead of opening right away, they go into a queue with a small on-screen counter, and players open them later with a rebindable hotkey or a button. Upgrade choices and odds stay exactly as in the normal game. The mod is packaged for Thunderstore so it can be installed with r2modman.

## Goals

- Catch the game's upgrade popup before it shows and add it to a queue instead
- Show a small on-screen counter of how many upgrades are waiting
- Let players open queued upgrades whenever they want with a rebindable hotkey or a button
- Keep upgrade choices, odds and results the same as the normal game
- Package and publish on Thunderstore so it installs with r2modman

## Out of scope

- Changing upgrade options, odds or balance
- Redesigning the game's upgrade UI beyond what queueing needs

## Suggested stack

- **C#** — Standard language for Unity/BepInEx mods
- **BepInEx (5 or 6, TBD)** — The game's community mod loader, available on Thunderstore; the version depends on whether the game uses Mono or IL2CPP
- **HarmonyX** — Patches the game's popup method so popups can be sent to the queue
- **Thunderstore / r2modman packaging** — Where the Stonewards modding community shares and installs mods
