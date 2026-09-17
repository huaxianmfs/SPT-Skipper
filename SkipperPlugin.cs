using System;
using System.Collections.Generic;
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

        // 支线：原版用
        internal static GameObject LastSeenObjectivesBlock;

        // 主线：单独追踪
        internal static readonly List<GameObject> TrackedButtons = new List<GameObject>();
        internal static string CurrentQuestId = "";

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

            if (SkipperPlugin.DisplayHotkey.Value.IsDown())
                ChangeButtonVisibility(true);

            if (SkipperPlugin.DisplayHotkey.Value.IsUp())
                ChangeButtonVisibility(false);
        }

        private static void ChangeButtonVisibility(bool visible)
        {
            int matched = 0;

            // 支线按钮：从 LastSeenObjectivesBlock 子树找
            if (SkipperPlugin.LastSeenObjectivesBlock != null)
            {
                var buttons = SkipperPlugin.LastSeenObjectivesBlock
                    .GetComponentsInChildren<DefaultUIButton>(true);

                foreach (var b in buttons)
                {
                    if (b == null) continue;
                    if (b.name != SkipperPlugin.SkipButtonName) continue;

                    b.gameObject.SetActive(visible);
                    matched++;
                }
            }

            // 主线按钮：从 TrackedButtons 找
            SkipperPlugin.TrackedButtons.RemoveAll(b => b == null);
            foreach (var b in SkipperPlugin.TrackedButtons)
            {
                if (b == null) continue;
                b.SetActive(visible);
                matched++;
            }

            SkipperPlugin.Logger.LogInfo(
                $"[Skipper] ChangeButtonVisibility({visible}) matched {matched}");
        }
    }
}