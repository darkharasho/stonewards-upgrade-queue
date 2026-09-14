using System.Collections;
using System.Collections.Generic;
using HarmonyLib;

namespace UpgradeQueue.Patches
{
    /// <summary>
    /// Replaces the level-up upgrade screen with a queue entry and immediately tells the server
    /// this player has chosen, so the (session-wide) upgrade phase ends and play continues.
    /// </summary>
    [HarmonyPatch(typeof(RogueLikeUpgradeMenu), "OnUpgradeDraftGenerated")]
    internal static class InterceptLevelUpPatch
    {
        private static readonly AccessTools.FieldRef<RogueLikeUpgradeMenu, RogueLikeUpgradeManager.UpgradeDraftChoice> SelectedUpgrade =
            AccessTools.FieldRefAccess<RogueLikeUpgradeMenu, RogueLikeUpgradeManager.UpgradeDraftChoice>("selectedUpgrade");

        private static readonly AccessTools.FieldRef<RogueLikeUpgradeMenu, bool> HasSkip =
            AccessTools.FieldRefAccess<RogueLikeUpgradeMenu, bool>("hasSkip");

        private static bool Prefix(RogueLikeUpgradeMenu __instance, List<RogueLikeUpgradeManager.UpgradeDraftChoice> upgradeDraftChoices)
        {
            if (QueuedUpgradeSession.IsOpening)
                return true;

            // Queueing can't be turned off mid-session: the menu is busy with a queued pick.
            if (!Plugin.Enabled.Value && !QueuedUpgradeSession.IsOpen)
                return true;

            // The menu only resets these when it opens. If a previous pick is still stored, the
            // server's close event for this phase would make OnScreenClosed apply it a second time.
            // While a queued pick is open the stored pick is live and must be left alone; the close
            // event then just re-runs the hide routine, which applies it once.
            if (!QueuedUpgradeSession.IsOpen)
            {
                SelectedUpgrade(__instance) = default;
                HasSkip(__instance) = false;
            }

            QueueState.Enqueue();
            Plugin.Log.LogInfo($"Queued level-up upgrade ({QueueState.PendingCount} pending)");

            // On the host this runs inside NetworkHelper.StartRogueUpgradePhase while it is still
            // registering participants. Reporting the choice right away could end the phase before
            // other players are added and leave them stuck on their screens, so wait one frame.
            __instance.StartCoroutine(ReportChosenNextFrame());
            return false;
        }

        private static IEnumerator ReportChosenNextFrame()
        {
            yield return null;

            var player = GameManager.Instance != null ? GameManager.Instance.LocalPlayer : null;
            if (player == null || NetworkHelper.Instance == null)
            {
                Plugin.Log.LogWarning("No local player or NetworkHelper; could not end the upgrade phase");
                yield break;
            }
            NetworkHelper.Instance.CmdOnUpgradeChosen(player);
        }
    }
}
