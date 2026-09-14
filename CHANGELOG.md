# Changelog

## 0.1.6

- New `IdlePing` setting, on by default: while upgrades are queued, a faint ping repeats around the counter every few seconds as a reminder. Turn it off in the HUD settings.

## 0.1.5

- Queued upgrades now survive a crash or disconnect. When you rejoin the same run, they come back with the choices you had rolled. The queue is saved to `BepInEx/config/UpgradeQueue.rejoin.txt` and is only restored if it matches the run you rejoined.

## 0.1.4

- Fixed losing control of your character when the end-of-wave upgrade vote opened while a queued upgrade was on screen. The queued upgrade now closes first: a pick you already made is applied, otherwise it goes back in the queue with the same choices.

## 0.1.3

- Closing a queued upgrade without picking keeps its choices, so reopening it is no longer a free reroll.

## 0.1.2

- `PingIntensity` setting (0.25–2) scales how far, bright and thick the level-up ping is.

## 0.1.1

- Queued upgrades open one after another until the queue is empty (`PickAllInARow`, on by default).
- Pressing the open key before picking closes the screen and keeps the upgrade queued.
- The counter gives a sonar ping when a new level-up is queued (`PingOnLevelUp`).

## 0.1.0

- Level-up upgrade popups are queued instead of opening, without holding up the upgrade phase for other players.
- Open queued upgrades with a rebindable hotkey (default `U`), or a button while the inventory is open.
- HUD counter of queued upgrades, with configurable corner.
- Optional pause while picking in single player.
- The queue is cleared when a run starts or ends.
