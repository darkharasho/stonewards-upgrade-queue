using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using HarmonyLib;
using Mirror;

namespace UpgradeQueue
{
    /// <summary>
    /// Keeps a multiplayer client's queue on disk so it survives a crash or disconnect and comes
    /// back when the player rejoins the same run. The host keeps each player's picked upgrades by
    /// Steam ID and restores them on rejoin, but queued upgrades only exist on the client.
    /// </summary>
    /// <remarks>
    /// The game's rejoin flag is true for anyone who joined the host's session before, even in a
    /// later run, so a snapshot is only restored when the lobby matches, it is recent, the wave
    /// has not gone backwards and the restored level-up upgrades match those saved with it.
    /// </remarks>
    internal static class QueueSnapshot
    {
        private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);
        private const int FormatVersion = 1;

        private static readonly AccessTools.FieldRef<StoneWardsNetworkManager, string> HubScene =
            AccessTools.FieldRefAccess<StoneWardsNetworkManager, string>("hubScene");

        private static string FilePath => Path.Combine(Paths.ConfigPath, "UpgradeQueue.rejoin.txt");

        /// <summary>Set while the queue is cleared for a run ending, which a disconnect also triggers.</summary>
        public static bool Paused { get; set; }

        public static void Init()
        {
            QueueState.Changed += _ => Save();
        }

        /// <summary>Writes the queue for the current lobby. Only a client in a level is saved.</summary>
        public static void Save()
        {
            if (Paused || !InClientLevel(out var lobbyId))
                return;

            var count = QueuedUpgradeSession.PendingIncludingOpen;
            var lines = new List<string>
            {
                FormatVersion.ToString(CultureInfo.InvariantCulture),
                lobbyId.ToString(CultureInfo.InvariantCulture),
                DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture),
                NetworkHelper.Instance.currentWaveIndex.ToString(CultureInfo.InvariantCulture),
                TomeLevels().ToString(CultureInfo.InvariantCulture),
                count.ToString(CultureInfo.InvariantCulture),
            };
            var choices = count > 0 ? QueuedUpgradeSession.FrontChoices : null;
            if (choices != null)
            {
                foreach (var choice in choices)
                {
                    if (!choice.IsValid)
                        continue;
                    lines.Add(string.Join("\t", choice.Upgrade.ID,
                        choice.Level.ToString(CultureInfo.InvariantCulture),
                        ((int)choice.Rarity).ToString(CultureInfo.InvariantCulture)));
                }
            }

            try
            {
                var tmp = FilePath + ".tmp";
                File.WriteAllLines(tmp, lines);
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not save the queue for rejoining: {e.Message}");
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not delete the rejoin snapshot: {e.Message}");
            }
        }

        private static void TryRestore(bool restoredFromSession)
        {
            var snapshot = Read();
            Delete();
            if (snapshot == null || !restoredFromSession || !InClientLevel(out var lobbyId))
                return;

            string reject = null;
            if (snapshot.LobbyId != lobbyId)
                reject = "different lobby";
            else if (DateTime.UtcNow - new DateTime(snapshot.Ticks, DateTimeKind.Utc) > MaxAge)
                reject = "too old";
            else if (NetworkHelper.Instance.currentWaveIndex < snapshot.Wave)
                reject = "earlier wave";
            else if (TomeLevels() != snapshot.TomeLevels)
                reject = "upgrades don't match";
            else if (QueueState.PendingCount > 0 || QueuedUpgradeSession.IsOpen)
                reject = "queue already in use";

            if (reject != null)
            {
                if (snapshot.Count > 0)
                    Plugin.Log.LogInfo($"Not restoring {snapshot.Count} queued upgrade(s) from before rejoining: {reject}");
                Save();
                return;
            }

            if (snapshot.Count > 0)
            {
                QueuedUpgradeSession.RestoreFrontChoices(snapshot.Choices);
                QueueState.Restore(snapshot.Count);
                HudCounter.Ping();
                Plugin.Log.LogInfo($"Rejoined the run; restored {snapshot.Count} queued upgrade(s)");
            }
            Save();
        }

        private static bool InClientLevel(out ulong lobbyId)
        {
            lobbyId = SteamLobby.Instance != null ? SteamLobby.Instance.currentLobbyID : 0uL;
            return lobbyId != 0uL && NetworkClient.active && !NetworkServer.active
                && GameManager.Instance != null && GameManager.Instance.SceneType == GameSceneType.Level
                && NetworkHelper.Instance != null && RogueLikeUpgradeManager.Instance != null;
        }

        // Level-up upgrades only change through picks, which are saved, and the host restores
        // exactly these on rejoin. A new run starts from none.
        private static int TomeLevels()
        {
            var total = 0;
            foreach (var tome in RogueLikeUpgradeManager.Instance.GetTomes())
                total += tome.Level;
            return total;
        }

        private sealed class Data
        {
            public ulong LobbyId;
            public long Ticks;
            public int Wave;
            public int TomeLevels;
            public int Count;
            public List<RogueLikeUpgradeManager.UpgradeDraftChoice> Choices;
        }

        private static Data Read()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return null;
                var lines = File.ReadAllLines(FilePath);
                if (lines.Length < 6 || lines[0] != FormatVersion.ToString(CultureInfo.InvariantCulture))
                    return null;

                var inv = CultureInfo.InvariantCulture;
                var data = new Data
                {
                    LobbyId = ulong.Parse(lines[1], inv),
                    Ticks = long.Parse(lines[2], inv),
                    Wave = int.Parse(lines[3], inv),
                    TomeLevels = int.Parse(lines[4], inv),
                    Count = int.Parse(lines[5], inv),
                };

                var choices = new List<RogueLikeUpgradeManager.UpgradeDraftChoice>();
                for (var i = 6; i < lines.Length; i++)
                {
                    var parts = lines[i].Split('\t');
                    var upgrade = parts.Length == 3 ? RogueLikeUpgradeManager.Instance?.GetUpgradeSOByID(parts[0]) : null;
                    if (upgrade == null)
                    {
                        // Roll fresh choices rather than show a partial set.
                        choices = null;
                        break;
                    }
                    choices.Add(new RogueLikeUpgradeManager.UpgradeDraftChoice
                    {
                        Upgrade = upgrade,
                        Level = int.Parse(parts[1], inv),
                        Rarity = (Rarity)int.Parse(parts[2], inv),
                    });
                }
                data.Choices = choices != null && choices.Count > 0 ? choices : null;
                return data;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not read the rejoin snapshot: {e.Message}");
                return null;
            }
        }

        // Sent to a client once its player is set up in a level, after the host has restored its
        // upgrades. restoredFromSession is false for a first-time join.
        [HarmonyPatch(typeof(NetworkHelper), "UserCode_TargetNotifyJoinedClass__NetworkConnectionToClient__PlayerClass__Boolean")]
        private static class RestoreOnJoinPatch
        {
            private static void Postfix(bool restoredFromSession) => TryRestore(restoredFromSession);
        }

        // Picks outside the queue, such as with queueing turned off, change the upgrade check.
        [HarmonyPatch(typeof(RogueLikeUpgradeManager), nameof(RogueLikeUpgradeManager.ApplyUpgrade))]
        private static class SaveOnUpgradeAppliedPatch
        {
            private static void Postfix() => Save();
        }

        // Going back to the hub ends the run for good. A disconnect doesn't pass through here.
        [HarmonyPatch(typeof(StoneWardsNetworkManager), nameof(StoneWardsNetworkManager.OnClientChangeScene))]
        private static class DeleteOnHubPatch
        {
            private static void Prefix(StoneWardsNetworkManager __instance, string newSceneName)
            {
                if (GameManager.Instance != null && GameManager.Instance.SceneType == GameSceneType.Level
                    && newSceneName == HubScene(__instance))
                    Delete();
            }
        }
    }
}
