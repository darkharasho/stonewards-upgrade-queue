using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace UpgradeQueue
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.darkharasho.stonewards.upgradequeue";
        public const string PluginName = "UpgradeQueue";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<KeyboardShortcut> OpenQueueKey;
        internal static ConfigEntry<bool> PauseInSolo;
        internal static ConfigEntry<bool> ShowCounter;
        internal static ConfigEntry<CounterCorner> CounterPosition;
        internal static Plugin Instance;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Instance = this;

            Enabled = Config.Bind("General", "Enabled", true,
                "Queue level-up upgrade popups instead of opening them immediately.");
            OpenQueueKey = Config.Bind("Controls", "OpenQueueKey", new KeyboardShortcut(KeyCode.U),
                "Key that opens the next queued upgrade.");
            PauseInSolo = Config.Bind("General", "PauseInSolo", true,
                "Pause the game while picking a queued upgrade in single player. Never pauses in multiplayer.");
            ShowCounter = Config.Bind("HUD", "ShowCounter", true,
                "Show the queued upgrade counter. In the inventory it becomes a button that opens the next upgrade.");
            CounterPosition = Config.Bind("HUD", "CounterPosition", CounterCorner.TopRight,
                "Screen corner for the queued upgrade counter.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void Update()
        {
            if (Hotkey.WasPressed(OpenQueueKey.Value))
                QueuedUpgradeSession.RequestOpen();

            HudCounter.Update();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
