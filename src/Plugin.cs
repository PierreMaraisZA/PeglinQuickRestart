using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PeglinQuickRestart
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        // BepInEx names the config file after the GUID, so this gives BepInEx/config/PeglinQuickRestart.cfg.
        public const string Guid = "PeglinQuickRestart";
        public const string Name = "PeglinQuickRestart";
        public const string Version = "1.1.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyboardShortcut> RestartKey;
        internal static ConfigEntry<KeyboardShortcut> ControllerRestartButton;
        internal static ConfigEntry<float> HoldSeconds;
        internal static ConfigEntry<bool> ShowHoldProgress;

        private void Awake()
        {
            Log = Logger;

            RestartKey = Config.Bind("General", "RestartKey", new KeyboardShortcut(KeyCode.X),
                "Hold this key to abandon the current run and start a new one with the same class, Cruciball level and loadout. " +
                "A run started with a custom seed restarts on that seed; any other run gets a new random seed.");
            ControllerRestartButton = Config.Bind("General", "ControllerRestartButton", new KeyboardShortcut(KeyCode.JoystickButton8),
                "Controller button to hold for a restart. Default JoystickButton8 is clicking the left stick (L3) on Xbox-layout " +
                "controllers, including PlayStation and other pads when Steam Input is on. Set to None to disable.");
            HoldSeconds = Config.Bind("General", "HoldSeconds", 1.5f,
                "How long to hold the restart key, in real seconds (not affected by game speed or pause).");
            ShowHoldProgress = Config.Bind("General", "ShowHoldProgress", true,
                "Show a countdown at the top of the screen while the restart key is held.");

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);

            // Our own object rather than BepInEx's manager object, which some setups hide or destroy on scene loads.
            var host = new GameObject(Name) { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(host);
            host.AddComponent<RestartKeyWatcher>();

            Log.LogInfo($"{Name} {Version} loaded. Hold {RestartKey.Value} or {ControllerRestartButton.Value} for {HoldSeconds.Value}s to restart the run.");
        }
    }
}
