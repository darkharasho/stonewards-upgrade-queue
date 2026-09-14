# Changelog

## Unreleased

- Queued upgrades open one after another until the queue is empty (`PickAllInARow`, on by default).
- Pressing the open key before picking closes the screen and keeps the upgrade queued.
- The counter gives a sonar ping when a new level-up is queued (`PingOnLevelUp`).

## 0.1.0

- Level-up upgrade popups are queued instead of opening, without holding up the upgrade phase for other players.
- Open queued upgrades with a rebindable hotkey (default `U`), or a button while the inventory is open.
- HUD counter of queued upgrades, with configurable corner.
- Optional pause while picking in single player.
- The queue is cleared when a run starts or ends.
