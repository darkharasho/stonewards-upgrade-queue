using HarmonyLib;

namespace UpgradeQueue.Patches
{
    /// <summary>
    /// Queued upgrades belong to one run. MetaProgressManager brackets a run on every machine:
    /// StartRun from LevelManager.Init when a level loads, and EndRun when returning to the hub or
    /// disconnecting mid-run. Clearing on both covers paths that skip EndRun, such as quitting to
    /// the main menu from the pause menu.
    /// </summary>
    internal static class ClearQueueOnRunEndPatch
    {
        [HarmonyPatch(typeof(MetaProgressManager), nameof(MetaProgressManager.StartRun))]
        private static class StartRunPatch
        {
            private static void Prefix() => Clear("Run started");
        }

        [HarmonyPatch(typeof(MetaProgressManager), nameof(MetaProgressManager.EndRun))]
        private static class EndRunPatch
        {
            private static void Postfix() => Clear("Run ended");
        }

        private static void Clear(string reason)
        {
            var dropped = QueueState.PendingCount;
            QueueState.Clear();
            QueuedUpgradeSession.Reset();
            if (dropped > 0)
                Plugin.Log.LogInfo($"{reason}; dropped {dropped} queued upgrade(s)");
        }
    }
}
