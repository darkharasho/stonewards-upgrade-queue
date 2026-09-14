using HarmonyLib;

namespace UpgradeQueue.Patches
{
    /// <summary>
    /// The end-of-wave vote opens over whatever is on screen and, when it closes, returns input to
    /// the state it replaced. Over an open queued upgrade that is the upgrade menu's state with no
    /// menu left to hand it back, so the player can't move. Close the queued upgrade first.
    /// </summary>
    [HarmonyPatch(typeof(EndWaveUpgradeScreen), "OnEndOfWaveUpgradeDraftGenerated")]
    internal static class CloseForEndOfWavePatch
    {
        private static void Prefix()
        {
            QueuedUpgradeSession.CloseNow("End-of-wave upgrade opened");
        }
    }
}
