using System;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace Terkoiz.Skipper
{
    using Object = UnityEngine.Object;

    [HarmonyPatch]
    public static class QuestObjectiveViewPatch
    {
        [HarmonyTargetMethod]
        public static System.Reflection.MethodBase TargetMethod()
        {
            var m = AccessTools.Method(
                typeof(QuestObjectiveView),
                "Show",
                new[]
                {
                    typeof(Quest),
                    typeof(Condition),
                    typeof(QuestController),
                    typeof(ItemController),
                    typeof(Sprite),
                    typeof(bool),
                    typeof(string)
                });
            SkipperPlugin.Logger.LogInfo($"[Skipper] TargetMethod: {m?.ToString() ?? "NULL"}");
            return m;
        }

        [HarmonyPostfix]
        public static void Postfix(QuestObjectiveView __instance, object[] __args)
        {
            int argLen = (__args != null) ? __args.Length : -1;
            SkipperPlugin.Logger.LogInfo($"[Skipper] Postfix fired, args len={argLen}");

            try
            {
                if (!SkipperPlugin.ModEnabled.Value)
                {
                    SkipperPlugin.Logger.LogInfo("[Skipper] ModEnabled=false");
                    return;
                }

                if (__args == null || __args.Length < 3)
                {
                    SkipperPlugin.Logger.LogInfo("[Skipper] __args too short");
                    return;
                }

                var quest = __args[0] as Quest;
                var condition = __args[1] as Condition;
                var questController = __args[2] as QuestController;

                SkipperPlugin.Logger.LogInfo(
                    $"[Skipper] q={quest != null} c={condition != null} ctrl={questController != null}");

                if (quest == null || condition == null || questController == null)
                    return;

                var handoverButton = __instance._handoverButton;

                SkipperPlugin.Logger.LogInfo(
                    $"[Skipper] handoverButton={(object)handoverButton != null}");

                if (handoverButton == null)
                    return;

                SkipperPlugin.LastSeenObjectivesBlock = __instance.transform.parent.gameObject;

                var skipButton = Object.Instantiate(
                    handoverButton,
                    handoverButton.transform.parent.transform);

                skipButton.SetRawText("SKIP", 22);
                skipButton.gameObject.name = SkipperPlugin.SkipButtonName;

                var layout = skipButton.gameObject
                    .GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout != null)
                    layout.minWidth = 100f;

                bool shouldShow = SkipperPlugin.AlwaysDisplay.Value
                                  && !quest.IsConditionDone(condition);

                skipButton.gameObject.SetActive(shouldShow);

                SkipperPlugin.Logger.LogInfo(
                    $"[Skipper] Skip button created, alwaysDisplay={SkipperPlugin.AlwaysDisplay.Value}, shouldShow={shouldShow}");

                skipButton.OnClick.RemoveAllListeners();

                System.Action onClickManaged = () =>
                {
                    System.Action acceptManaged = () =>
                    {
                        if (quest.IsConditionDone(condition))
                        {
                            skipButton.gameObject.SetActive(false);
                            return;
                        }

                        SkipperPlugin.Logger.LogInfo(
                            $"[Skipper] Accept: skipping {condition.id}");

                        try
                        {
                            if (quest.ProgressCheckers != null
                                && quest.ProgressCheckers.ContainsKey(condition))
                            {
                                var checker = quest.ProgressCheckers[condition];
                                System.Func<ConditionProgressChecker, double> getterManaged =
                                    _ => (double)condition.value;
                                checker.SetCurrentValueGetter(
                                    (Il2CppSystem.Func<ConditionProgressChecker, double>)getterManaged);
                            }
                        }
                        catch (Exception ex)
                        {
                            SkipperPlugin.Logger.LogError($"SetCurrentValueGetter failed: {ex}");
                        }

                        try
                        {
                            questController.CompleteConditionById(
                                quest.Id, condition.id.ToString());
                        }
                        catch (Exception ex)
                        {
                            SkipperPlugin.Logger.LogError($"CompleteConditionById failed: {ex}");
                        }

                        skipButton.gameObject.SetActive(false);
                    };

                    System.Action cancelManaged = () => { };

                    ItemUiContext.Instance.ShowMessageWindow(
                        "Are you sure you want to autocomplete this quest objective?",
                        (Il2CppSystem.Action)acceptManaged,
                        (Il2CppSystem.Action)cancelManaged,
                        "Confirmation");
                };

                skipButton.OnClick.AddListener((UnityAction)onClickManaged);
            }
            catch (Exception ex)
            {
                SkipperPlugin.Logger.LogError($"[Skipper] Postfix error: {ex}");
            }
        }
    }
}