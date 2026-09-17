using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Configuration;
using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace Terkoiz.Skipper
{
    [BepInPlugin("com.terkoiz.skipper", "Terkoiz.Skipper", "1.1.5")]
    public class SkipperPlugin : BasePlugin
    {
        internal const string SkipButtonName = "SkipButton";

        internal static ManualLogSource Logger { get; private set; }

        private const string MainSectionName = "Main";
        internal static ConfigEntry<bool> ModEnabled;
        internal static ConfigEntry<bool> AlwaysDisplay;
        internal static ConfigEntry<KeyboardShortcut> DisplayHotkey;

        internal static GameObject LastSeenObjectivesBlock;

        public override void Load()
        {
            Logger = Log;
            InitConfiguration();

            Logger.LogInfo("[Skipper] Load() start");

            try
            {
                var harmony = new Harmony("com.terkoiz.skipper");
                harmony.PatchAll(typeof(QuestObjectiveViewPatch));
                Logger.LogInfo("[Skipper] Patch applied");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Skipper] Patch failed: {ex}");
            }

            try
            {
                AddComponent<SkipperBehaviour>();
                Logger.LogInfo("[Skipper] AddComponent<SkipperBehaviour> called");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Skipper] AddComponent failed: {ex}");
            }

            Logger.LogInfo("[Skipper] Load() done");
        }

        private void InitConfiguration()
        {
            ModEnabled = Config.Bind(
                MainSectionName, "1. Enabled", true, "Global mod toggle.");

            AlwaysDisplay = Config.Bind(
                MainSectionName, "2. Always display Skip button", false, "Always visible.");

            DisplayHotkey = Config.Bind(
                MainSectionName, "3. Display hotkey",
                new KeyboardShortcut(KeyCode.LeftControl),
                "Holding this key will show the Skip buttons.");
        }
    }

    public class SkipperBehaviour : MonoBehaviour
    {
        private static bool _loggedFirstUpdate = false;

        public SkipperBehaviour(IntPtr ptr) : base(ptr)
        {
        }

        public void Awake()
        {
            SkipperPlugin.Logger.LogInfo("[Skipper] SkipperBehaviour.Awake");
        }

        public void OnEnable()
        {
            SkipperPlugin.Logger.LogInfo("[Skipper] SkipperBehaviour.OnEnable");
        }

        public void Update()
        {
            if (!_loggedFirstUpdate)
            {
                _loggedFirstUpdate = true;
                SkipperPlugin.Logger.LogInfo("[Skipper] Update running (first tick)");
            }

            if (!SkipperPlugin.ModEnabled.Value)
                return;

            if (SkipperPlugin.AlwaysDisplay.Value)
                return;

            if (SkipperPlugin.LastSeenObjectivesBlock == null)
                return;

            if (!SkipperPlugin.LastSeenObjectivesBlock.activeSelf)
                return;

            if (SkipperPlugin.DisplayHotkey.Value.IsDown())
            {
                SkipperPlugin.Logger.LogInfo("[Skipper] Hotkey IsDown -> show");
                ChangeButtonVisibility(true);
            }

            if (SkipperPlugin.DisplayHotkey.Value.IsUp())
            {
                SkipperPlugin.Logger.LogInfo("[Skipper] Hotkey IsUp -> hide");
                ChangeButtonVisibility(false);
            }
        }

        private static void ChangeButtonVisibility(bool visible)
        {
            var buttons = SkipperPlugin.LastSeenObjectivesBlock
                .GetComponentsInChildren<DefaultUIButton>(includeInactive: true);

            int matched = 0;
            foreach (var button in buttons)
            {
                if (button.name != SkipperPlugin.SkipButtonName)
                    continue;

                matched++;
                button.gameObject.SetActive(visible);
            }

            SkipperPlugin.Logger.LogInfo(
                $"[Skipper] ChangeButtonVisibility({visible}) matched {matched}");
        }
    }
}