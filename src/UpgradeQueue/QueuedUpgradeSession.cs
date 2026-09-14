using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace UpgradeQueue
{
    /// <summary>
    /// Opens a queued upgrade in the game's own RogueLikeUpgradeMenu, outside of any network
    /// upgrade phase. Choices are rolled on open with the game's GenerateThreeChoices, and the
    /// pick is applied by the menu's normal hide routine.
    /// </summary>
    internal static class QueuedUpgradeSession
    {
        private static readonly AccessTools.FieldRef<RogueLikeUpgradeMenu, Coroutine> AutoChooseCoroutine =
            AccessTools.FieldRefAccess<RogueLikeUpgradeMenu, Coroutine>("autoChooseCoroutine");

        private static readonly System.Reflection.MethodInfo OnUpgradeDraftGenerated =
            AccessTools.Method(typeof(RogueLikeUpgradeMenu), "OnUpgradeDraftGenerated");

        private static readonly System.Reflection.MethodInfo OnScreenClosed =
            AccessTools.Method(typeof(RogueLikeUpgradeMenu), "OnScreenClosed");

        /// <summary>True from opening a queued upgrade until the menu closes after applying it.</summary>
        public static bool IsOpen { get; private set; }

        /// <summary>Set while this class drives the menu, so the intercept patch lets it through.</summary>
        public static bool IsOpening { get; private set; }

        /// <summary>
        /// Opens the next queued upgrade from gameplay, or from the inventory by closing it first.
        /// The inventory borrows the upgrade menu's stats and inventory panels, so the menu is
        /// opened a frame after the inventory has handed them back.
        /// </summary>
        public static void RequestOpen()
        {
            if (IsOpen || QueueState.PendingCount == 0 || InputManager.Instance == null)
                return;

            if (InputManager.Instance.CurrentInputState == InputManager.InputState.Inventory && InventoryScreen.Instance != null)
            {
                InventoryScreen.Instance.CloseInventoryScreen();
                Plugin.Instance.StartCoroutine(OpenNextFrame());
                return;
            }

            TryOpen();
        }

        private static IEnumerator OpenNextFrame()
        {
            yield return null;
            TryOpen();
        }

        public static bool TryOpen()
        {
            if (IsOpen || QueueState.PendingCount == 0)
                return false;

            if (InputManager.Instance == null || InputManager.Instance.CurrentInputState != InputManager.InputState.Gameplay)
                return false;

            if (NetworkHelper.Instance == null || NetworkHelper.Instance.NetworkblockingState != NetworkHelper.BlockingState.None)
                return false;

            var manager = RogueLikeUpgradeManager.Instance;
            var menu = Object.FindAnyObjectByType<RogueLikeUpgradeMenu>();
            if (manager == null || menu == null || GameManager.Instance.LocalPlayer == null)
                return false;

            if (!QueueState.TryDequeue())
                return false;

            IsOpen = true;
            IsOpening = true;
            try
            {
                OnUpgradeDraftGenerated.Invoke(menu, new object[] { manager.GenerateThreeChoices() });
            }
            finally
            {
                IsOpening = false;
            }

            // Queued picks have no time limit.
            var autoChoose = AutoChooseCoroutine(menu);
            if (autoChoose != null)
            {
                menu.StopCoroutine(autoChoose);
                AutoChooseCoroutine(menu) = null;
            }

            // The menu always freezes time; only keep that in single player.
            if (!Plugin.PauseInSolo.Value || !IsSolo())
                Time.timeScale = 1f;

            Plugin.Log.LogInfo($"Opened queued upgrade ({QueueState.PendingCount} still pending)");
            return true;
        }

        /// <summary>
        /// Forgets an open queued pick. Used when the run ends: the scene unloads with the menu,
        /// so Close() may never run to end the session.
        /// </summary>
        public static void Reset()
        {
            IsOpen = false;
            IsOpening = false;
        }

        private static bool IsSolo()
        {
            return FirstPersonController.LocalPlayers.Count <= 1;
        }

        // There is no server phase to send TargetCloseUpgradeScreen for a queued pick, so close
        // the screen as soon as the player picks or skips. The menu's own CmdOnUpgradeChosen call is
        // ignored by the server because no phase is active.
        [HarmonyPatch(typeof(RogueLikeUpgradeMenu), "OnUpgradeClicked")]
        private static class CloseOnPickPatch
        {
            private static void Postfix(RogueLikeUpgradeMenu __instance)
            {
                if (IsOpen && __instance.IsUpgradeSelected)
                    OnScreenClosed.Invoke(__instance, null);
            }
        }

        // HideUpgradeScreen applies the pick and then calls Close(), which ends the session.
        [HarmonyPatch(typeof(BaseMenu), nameof(BaseMenu.Close))]
        private static class EndSessionOnClosePatch
        {
            private static void Postfix(BaseMenu __instance)
            {
                if (IsOpen && __instance is RogueLikeUpgradeMenu)
                    IsOpen = false;
            }
        }
    }
}
