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
        public const string PluginVersion = "0.1.6";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<KeyboardShortcut> OpenQueueKey;
        internal static ConfigEntry<bool> PauseInSolo;
        internal static ConfigEntry<bool> PickAllInARow;
        internal static ConfigEntry<bool> ShowCounter;
        internal static ConfigEntry<bool> PingOnLevelUp;
        internal static ConfigEntry<bool> IdlePing;
        internal static ConfigEntry<float> PingIntensity;
        internal static ConfigEntry<CounterCorner> CounterPosition;
        internal static ConfigEntry<int> CounterOffsetX;
        internal static ConfigEntry<int> CounterOffsetY;
        internal static Plugin Instance;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Instance = this;

            // Display names and order are for the in-game ModSettings menu; they don't change the .cfg.
            Enabled = Config.Bind("General", "Enabled", true, new ConfigDescription(
                "Queue level-up upgrade popups instead of opening them immediately.", null,
                new ConfigurationManagerAttributes { DispName = "Queue upgrades", Order = 20 }));
            PickAllInARow = Config.Bind("General", "PickAllInARow", true, new ConfigDescription(
                "After picking a queued upgrade, open the next one right away until the queue is empty. Press the open key again to stop and keep the rest queued.", null,
                new ConfigurationManagerAttributes { DispName = "Pick all queued upgrades in a row", Order = 15 }));
            PauseInSolo = Config.Bind("General", "PauseInSolo", true, new ConfigDescription(
                "Pause the game while picking a queued upgrade in single player. Never pauses in multiplayer.", null,
                new ConfigurationManagerAttributes { DispName = "Pause while picking (single player)", Order = 10 }));
            OpenQueueKey = Config.Bind("Controls", "OpenQueueKey", new KeyboardShortcut(KeyCode.U), new ConfigDescription(
                "Key that opens the next queued upgrade. Press it again before picking to close the screen and keep the upgrade queued.", null,
                new ConfigurationManagerAttributes { DispName = "Open / close queued upgrade" }));
            ShowCounter = Config.Bind("HUD", "ShowCounter", true, new ConfigDescription(
                "Show the queued upgrade counter. In the inventory it becomes a button that opens the next upgrade.", null,
                new ConfigurationManagerAttributes { DispName = "Show counter", Order = 40 }));
            PingOnLevelUp = Config.Bind("HUD", "PingOnLevelUp", true, new ConfigDescription(
                "Play a sonar ping animation around the counter when a level-up is queued.", null,
                new ConfigurationManagerAttributes { DispName = "Ping on new level-up", Order = 35 }));
            IdlePing = Config.Bind("HUD", "IdlePing", true, new ConfigDescription(
                "Keep a faint ping repeating around the counter every few seconds while upgrades are queued.", null,
                new ConfigurationManagerAttributes { DispName = "Ping while upgrades wait", Order = 34 }));
            PingIntensity = Config.Bind("HUD", "PingIntensity", 1f, new ConfigDescription(
                "Strength of the ping. Scales how far the rings spread, how bright they are and how thick they are. 1 is the default.",
                new AcceptableValueRange<float>(0.25f, 2f),
                new ConfigurationManagerAttributes { DispName = "Ping intensity", Order = 33 }));
            CounterPosition = Config.Bind("HUD", "CounterPosition", CounterCorner.TopRight, new ConfigDescription(
                "Screen corner for the queued upgrade counter.", null,
                new ConfigurationManagerAttributes { DispName = "Counter corner", Order = 30 }));
            CounterOffsetX = Config.Bind("HUD", "CounterOffsetX", 0, new ConfigDescription(
                "Extra horizontal distance, in UI pixels, from the counter's corner. Negative moves it toward the edge.",
                new AcceptableValueRange<int>(-500, 500),
                new ConfigurationManagerAttributes { DispName = "Counter offset X", Order = 20 }));
            CounterOffsetY = Config.Bind("HUD", "CounterOffsetY", 0, new ConfigDescription(
                "Extra vertical distance, in UI pixels, from the counter's corner. Increase to move it clear of other HUD elements.",
                new AcceptableValueRange<int>(-500, 500),
                new ConfigurationManagerAttributes { DispName = "Counter offset Y", Order = 10 }));

            QueueSnapshot.Init();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void Update()
        {
            if (Hotkey.WasPressed(OpenQueueKey.Value))
                QueuedUpgradeSession.Toggle();

            HudCounter.Update();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
