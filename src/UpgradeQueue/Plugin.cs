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

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Queue level-up upgrade popups instead of opening them immediately.");
            OpenQueueKey = Config.Bind("Controls", "OpenQueueKey", new KeyboardShortcut(KeyCode.U),
                "Key that opens the next queued upgrade.");
            PauseInSolo = Config.Bind("General", "PauseInSolo", true,
                "Pause the game while picking a queued upgrade in single player. Never pauses in multiplayer.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void Update()
        {
            if (Hotkey.WasPressed(OpenQueueKey.Value))
                QueuedUpgradeSession.TryOpen();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
