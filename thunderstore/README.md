# UpgradeQueue

Level-up upgrade popups no longer interrupt play. Each level-up goes into a queue, and you pick your upgrades whenever it suits you.

Choices, odds and results are the game's own: the three options are rolled with the normal draft when you open a queued upgrade.

## How to use

- A counter in the corner of the HUD shows how many upgrades are waiting, e.g. `Upgrades: 3  [U]`.
- Press **U** to open the next queued upgrade and pick as usual. Queued picks have no timer.
- With the inventory open, the counter becomes an **Open upgrade** button you can click.
- Queued upgrades last for the current run. They are cleared when the run ends, including if you disconnect.

## Multiplayer

Queued picks never hold up other players: your level-up counts as handled right away, and the game keeps running while you pick later. In single player the game pauses while the upgrade screen is open (configurable).

Every player who wants queueing needs the mod. Players without it get the normal popups.

## Configuration

Edit in r2modman under **Config editor** → `com.darkharasho.stonewards.upgradequeue.cfg`, or in `BepInEx/config`.

| Setting | Default | Description |
| --- | --- | --- |
| General → Enabled | `true` | Queue level-up popups. Turn off for normal popups. |
| General → PauseInSolo | `true` | Pause while picking a queued upgrade in single player. |
| Controls → OpenQueueKey | `U` | Key that opens the next queued upgrade. Modifiers work, e.g. `U + LeftShift`. |
| HUD → ShowCounter | `true` | Show the queued upgrade counter and inventory button. |
| HUD → CounterPosition | `TopRight` | `TopLeft`, `TopRight`, `BottomLeft` or `BottomRight`. |
| HUD → CounterOffsetX / CounterOffsetY | `0` | Nudge the counter away from its corner, in UI pixels, if it overlaps other HUD elements. |

## Not affected

- End-of-wave upgrade votes and upgrade pickups work as normal.

## Issues

Report bugs at https://github.com/darkharasho/stonewards-upgrade-queue/issues. Please attach `BepInEx/LogOutput.log`.
