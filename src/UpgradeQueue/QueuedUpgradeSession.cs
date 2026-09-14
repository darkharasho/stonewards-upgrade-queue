using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

namespace UpgradeQueue
{
    /// <summary>
    /// Opens a queued upgrade in the game's own RogueLikeUpgradeMenu, outside of any network
    /// upgrade phase. Choices are rolled on open with the game's GenerateThreeChoices, and the
    /// pick is applied by the menu's normal hide routine. An upgrade put back without picking
    /// keeps its choices, so closing and reopening is not a free reroll.
    /// </summary>
    internal static class QueuedUpgradeSession
    {
        private static readonly AccessTools.FieldRef<RogueLikeUpgradeMenu, Coroutine> AutoChooseCoroutine =
            AccessTools.FieldRefAccess<RogueLikeUpgradeMenu, Coroutine>("autoChooseCoroutine");

        private static readonly System.Reflection.MethodInfo OnUpgradeDraftGenerated =
            AccessTools.Method(typeof(RogueLikeUpgradeMenu), "OnUpgradeDraftGenerated");

        private static readonly AccessTools.FieldRef<RogueLikeUpgradeMenu, Coroutine> HideCoroutine =
            AccessTools.FieldRefAccess<RogueLikeUpgradeMenu, Coroutine>("hideCoroutine");

        private static readonly AccessTools.FieldRef<RogueLikeUpgradeMenu, Coroutine> RerollCoroutine =
            AccessTools.FieldRefAccess<RogueLikeUpgradeMenu, Coroutine>("_rerollCoroutine");

        private static readonly System.Reflection.MethodInfo OnScreenClosed =
            AccessTools.Method(typeof(RogueLikeUpgradeMenu), "OnScreenClosed");

        private static readonly System.Reflection.MethodInfo HideUpgradeScreen =
            AccessTools.Method(typeof(RogueLikeUpgradeMenu), "HideUpgradeScreen");

        private static readonly System.Reflection.MethodInfo EnableOptions =
            AccessTools.Method(typeof(RogueLikeUpgradeMenu), "EnableOptions");

        private static readonly System.Reflection.MethodInfo ShowControllers =
            AccessTools.Method(typeof(RogueLikeUpgradeMenu), "ShowControllers");

        private static readonly System.Reflection.MethodInfo HandleInput =
            AccessTools.Method(typeof(BaseMenu), "HandleInput");

        private static readonly AccessTools.FieldRef<RogueLikeUpgradeMenu, RogueLikeUpgradeManager.UpgradeDraftChoice> SelectedUpgrade =
            AccessTools.FieldRefAccess<RogueLikeUpgradeMenu, RogueLikeUpgradeManager.UpgradeDraftChoice>("selectedUpgrade");

        /// <summary>True from opening a queued upgrade until the menu closes after applying it.</summary>
        public static bool IsOpen { get; private set; }

        /// <summary>Set while this class drives the menu, so the intercept patch lets it through.</summary>
        public static bool IsOpening { get; private set; }

        private static RogueLikeUpgradeMenu _menu;

        // The open screen is closing without a pick, so the upgrade went back in the queue and
        // the chain stops.
        private static bool _puttingBack;

        // Choices currently on the open screen, updated by the menu's own rerolls.
        private static List<RogueLikeUpgradeManager.UpgradeDraftChoice> _shownChoices;

        // Choices of the put-back upgrade at the front of the queue, shown again when it reopens.
        private static List<RogueLikeUpgradeManager.UpgradeDraftChoice> _savedChoices;

        /// <summary>
        /// Hotkey: opens the next queued upgrade, or closes the open one if nothing is picked yet.
        /// </summary>
        public static void Toggle()
        {
            if (IsOpen)
                PutBack();
            else
                RequestOpen();
        }

        /// <summary>
        /// Closes the open queued upgrade without picking and returns it to the queue. Ignored once
        /// a choice is made or the screen is already closing.
        /// </summary>
        public static void PutBack()
        {
            if (!IsOpen || _puttingBack || _menu == null || _menu.IsUpgradeSelected)
                return;

            _puttingBack = true;
            EnableOptions.Invoke(_menu, new object[] { false });
            var reroll = RerollCoroutine(_menu);
            if (reroll != null)
            {
                _menu.StopCoroutine(reroll);
                RerollCoroutine(_menu) = null;
            }

            // The menu's own hide routine; with nothing selected it applies no upgrade.
            var hide = HideCoroutine(_menu);
            if (hide != null)
                _menu.StopCoroutine(hide);
            HideCoroutine(_menu) = _menu.StartCoroutine((IEnumerator)HideUpgradeScreen.Invoke(_menu, null));

            _savedChoices = _shownChoices;
            QueueState.Enqueue();
            Plugin.Log.LogInfo($"Closed queued upgrade without picking ({QueueState.PendingCount} pending)");
        }

        /// <summary>
        /// Closes the open queued upgrade at once, before another menu takes over input. A pick
        /// already made is applied; otherwise the upgrade goes back in the queue with its choices.
        /// </summary>
        public static void CloseNow(string reason)
        {
            if (!IsOpen || _menu == null)
                return;

            var menu = _menu;
            StopCoroutine(menu, HideCoroutine);
            StopCoroutine(menu, RerollCoroutine);
            StopCoroutine(menu, AutoChooseCoroutine);

            // A put-back already in progress has re-queued the upgrade.
            var picked = menu.IsUpgradeSelected;
            var alreadyPutBack = _puttingBack;
            // Also stops chaining into the next queued upgrade while the other menu is up.
            _puttingBack = true;
            if (picked)
            {
                RogueLikeUpgradeManager.Instance.ApplyUpgrade(SelectedUpgrade(menu));
            }
            else if (!alreadyPutBack)
            {
                EnableOptions.Invoke(menu, new object[] { false });
                _savedChoices = _shownChoices;
                QueueState.Enqueue();
            }

            // HideUpgradeScreen without the animation. Input goes back to gameplay while this menu
            // still owns it, so the next menu returns there when it closes.
            ShowControllers.Invoke(menu, new object[] { false, 0f, 0f });
            menu.rogueLikeUpgradeScreen.style.display = DisplayStyle.None;
            Time.timeScale = 1f;
            HandleInput.Invoke(menu, new object[] { false });
            menu.Close();

            Plugin.Log.LogInfo(picked
                ? $"{reason}; applied the queued pick early"
                : $"{reason}; put the queued upgrade back ({QueueState.PendingCount} pending)");
        }

        private static void StopCoroutine(RogueLikeUpgradeMenu menu, AccessTools.FieldRef<RogueLikeUpgradeMenu, Coroutine> field)
        {
            var coroutine = field(menu);
            if (coroutine == null)
                return;
            menu.StopCoroutine(coroutine);
            field(menu) = null;
        }

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
            _menu = menu;
            _puttingBack = false;
            try
            {
                var choices = _savedChoices ?? manager.GenerateThreeChoices();
                _savedChoices = null;
                OnUpgradeDraftGenerated.Invoke(menu, new object[] { choices });
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
            _puttingBack = false;
            _menu = null;
            _shownChoices = null;
            _savedChoices = null;
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
            // The menu re-enables its options on a timer after opening, so a closing screen can
            // still be clicked. A pick there would apply an upgrade that is already back in the queue.
            private static bool Prefix() => !_puttingBack;

            private static void Postfix(RogueLikeUpgradeMenu __instance)
            {
                if (IsOpen && __instance.IsUpgradeSelected)
                    OnScreenClosed.Invoke(__instance, null);
            }
        }

        // Opening and the menu's reroll both fill the cards through Setup, so this tracks what the
        // player is looking at.
        [HarmonyPatch(typeof(UpgradePopupController), nameof(UpgradePopupController.Setup),
            new[] { typeof(List<RogueLikeUpgradeManager.UpgradeDraftChoice>) })]
        private static class TrackShownChoicesPatch
        {
            private static void Postfix(List<RogueLikeUpgradeManager.UpgradeDraftChoice> upgradeDraftChoices)
            {
                if (IsOpen && !_puttingBack)
                    _shownChoices = new List<RogueLikeUpgradeManager.UpgradeDraftChoice>(upgradeDraftChoices);
            }
        }

        // HideUpgradeScreen applies the pick and then calls Close(), which ends the session. With
        // PickAllInARow the next queued upgrade opens a frame later, once the menu has restored input.
        [HarmonyPatch(typeof(BaseMenu), nameof(BaseMenu.Close))]
        private static class EndSessionOnClosePatch
        {
            private static void Postfix(BaseMenu __instance)
            {
                if (!IsOpen || !(__instance is RogueLikeUpgradeMenu))
                    return;

                var chain = !_puttingBack && Plugin.PickAllInARow.Value && QueueState.PendingCount > 0;
                IsOpen = false;
                _puttingBack = false;
                _menu = null;
                _shownChoices = null;
                if (chain)
                    Plugin.Instance.StartCoroutine(OpenNextFrame());
            }
        }
    }
}
