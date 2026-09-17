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
            return AccessTools.Method(
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
        }

        [HarmonyPostfix]
        public static void Postfix(QuestObjectiveView __instance, object[] __args)
        {
            try
            {
                if (!SkipperPlugin.ModEnabled.Value) return;
                if (__args == null || __args.Length < 3) return;

                var quest = __args[0] as Quest;
                var condition = __args[1] as Condition;
                var questController = __args[2] as QuestController;

                if (quest == null || condition == null || questController == null) return;

                string condIdStr = condition.id.ToString();

                var handoverButton = __instance._handoverButton;

                if (handoverButton != null)
                {
                    //支线
                    SkipperPlugin.LastSeenObjectivesBlock =
                        __instance.transform.parent.gameObject;

                    var skipButton = Object.Instantiate(
                        handoverButton,
                        handoverButton.transform.parent);

                    skipButton.SetRawText("SKIP", 22);
                    skipButton.gameObject.name = SkipperPlugin.SkipButtonName;

                    var layout = skipButton.gameObject
                        .GetComponent<UnityEngine.UI.LayoutElement>();
                    if (layout != null)
                        layout.minWidth = 100f;

                    bool shouldShow = SkipperPlugin.AlwaysDisplay.Value
                                      && !quest.IsConditionDone(condition);
                    skipButton.gameObject.SetActive(shouldShow);

                    BindClick(skipButton, quest, condition, questController);

                    SkipperPlugin.Logger.LogInfo(
                        $"[Skipper] 支线按钮 condId={condIdStr} shouldShow={shouldShow}");
                }
                else
                {
                    //主线
                    var targetParent = __instance.transform.parent;
                    if (targetParent == null) return;

                    if (quest.Id != SkipperPlugin.CurrentQuestId)
                    {
                        foreach (var b in SkipperPlugin.TrackedButtons)
                            if (b != null) Object.Destroy(b);
                        SkipperPlugin.TrackedButtons.Clear();
                        SkipperPlugin.CurrentQuestId = quest.Id;
                    }

                    string wantName = SkipperPlugin.SkipButtonName + "_" + condIdStr;

                    DefaultUIButton skipButton2 = null;
                    for (int i = SkipperPlugin.TrackedButtons.Count - 1; i >= 0; i--)
                    {
                        var b = SkipperPlugin.TrackedButtons[i];
                        if (b == null)
                        {
                            SkipperPlugin.TrackedButtons.RemoveAt(i);
                            continue;
                        }
                        if (b.name == wantName)
                            skipButton2 = b.GetComponent<DefaultUIButton>();
                    }

                    if (skipButton2 == null)
                    {
                        DefaultUIButton template = null;
                        var arr = __instance.GetComponentsInChildren<DefaultUIButton>(true);
                        if (arr != null && arr.Length > 0) template = arr[0];

                        if (template == null && ItemUiContext.Instance != null)
                        {
                            var arr2 = ItemUiContext.Instance
                                .GetComponentsInChildren<DefaultUIButton>(true);
                            if (arr2 != null && arr2.Length > 0) template = arr2[0];
                        }

                        if (template == null) return;

                        skipButton2 = Object.Instantiate(template, targetParent);
                        skipButton2.gameObject.name = wantName;
                        skipButton2.SetRawText("SKIP", 22);

                        var viewRt = __instance.transform as RectTransform;
                        var btnRt = skipButton2.transform as RectTransform;
                        if (viewRt != null && btnRt != null)
                        {
                            btnRt.anchorMin = viewRt.anchorMin;
                            btnRt.anchorMax = viewRt.anchorMax;
                            btnRt.pivot = new Vector2(0f, 0.5f);
                            btnRt.anchoredPosition = new Vector2(
                                viewRt.anchoredPosition.x + viewRt.sizeDelta.x + 4f,
                                viewRt.anchoredPosition.y);
                            btnRt.sizeDelta = new Vector2(80f, 28f);
                        }

                        SkipperPlugin.TrackedButtons.Add(skipButton2.gameObject);
                    }

                    bool shouldShow2 = SkipperPlugin.AlwaysDisplay.Value
                                       && !quest.IsConditionDone(condition);
                    skipButton2.gameObject.SetActive(shouldShow2);

                    skipButton2.OnClick.RemoveAllListeners();

                    var cq = quest;
                    var cc = condition;
                    var cctrl = questController;
                    var cb = skipButton2;
                    var ccid = condIdStr;

                    System.Action onClickManaged = () =>
                    {
                        System.Action acceptManaged = () =>
                        {
                            SkipConditionMain(cq, cc, cctrl, ccid);
                            cb.gameObject.SetActive(false);
                        };

                        System.Action cancelManaged = () => { };

                        ItemUiContext.Instance.ShowMessageWindow(
                            "Are you sure you want to autocomplete this quest objective?",
                            (Il2CppSystem.Action)acceptManaged,
                            (Il2CppSystem.Action)cancelManaged,
                            "Confirmation");
                    };

                    skipButton2.OnClick.AddListener((UnityAction)onClickManaged);
                }
            }
            catch (Exception ex)
            {
                SkipperPlugin.Logger.LogError($"[Skipper] Postfix error: {ex}");
            }
        }

        // ========== 主线跳过逻辑 ==========
        private static void SkipConditionMain(
            Quest quest, Condition condition, QuestController ctrl, string condId)
        {
            string questInstanceId = quest.Id;
            string templateIdFromTemplate = null;
            try
            {
                if (quest.Template != null)
                    templateIdFromTemplate = quest.Template.Id;
            }
            catch { }

            SkipperPlugin.Logger.LogInfo(
                $"[Skipper] 主线 skip 开始 questId={questInstanceId} templateId={templateIdFromTemplate} condId={condId} status={quest.QuestStatus}");

            // ---- 1. 强制 ProgressChecker ----
            try
            {
                if (quest.ProgressCheckers != null
                    && quest.ProgressCheckers.ContainsKey(condition))
                {
                    var checker = quest.ProgressCheckers[condition];
                    if (checker != null)
                    {
                        var captured = condition;
                        System.Func<ConditionProgressChecker, double> getterManaged =
                            _ => (double)captured.value;
                        checker.SetCurrentValueGetter(
                            (Il2CppSystem.Func<ConditionProgressChecker, double>)
                            getterManaged);

                        try { checker.CallConditionChanged(); }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                SkipperPlugin.Logger.LogError($"主线 SetCurrentValueGetter: {ex}");
            }

            // ---- 2. CompleteConditionById 用 template.Id ----
            if (!string.IsNullOrEmpty(templateIdFromTemplate))
            {
                try
                {
                    ctrl.CompleteConditionById(templateIdFromTemplate, condId);
                    SkipperPlugin.Logger.LogInfo(
                        $"[Skipper] CompleteConditionById(template.Id) 后 isDone={quest.IsConditionDone(condition)} status={quest.QuestStatus}");
                }
                catch (Exception ex)
                {
                    SkipperPlugin.Logger.LogError($"CompleteConditionById(template.Id): {ex}");
                }
            }

            if (quest.IsConditionDone(condition)) return;

            // ---- 3. CompleteConditionById 用 quest.Id ----
            try
            {
                ctrl.CompleteConditionById(questInstanceId, condId);
                SkipperPlugin.Logger.LogInfo(
                    $"[Skipper] CompleteConditionById(quest.Id) 后 isDone={quest.IsConditionDone(condition)} status={quest.QuestStatus}");
            }
            catch (Exception ex)
            {
                SkipperPlugin.Logger.LogError($"CompleteConditionById(quest.Id): {ex}");
            }

            if (quest.IsConditionDone(condition)) return;

            // ---- 4. CompleteConditionGeneric 反射兜底 ----
            try
            {
                var m = AccessTools.Method(
                    typeof(QuestController),
                    "CompleteConditionGeneric",
                    new[] { typeof(Quest), typeof(Condition) });
                if (m != null)
                {
                    m.Invoke(ctrl, new object[] { quest, condition });
                    SkipperPlugin.Logger.LogInfo(
                        $"[Skipper] CompleteConditionGeneric 后 isDone={quest.IsConditionDone(condition)} status={quest.QuestStatus}");
                }
            }
            catch (Exception ex)
            {
                SkipperPlugin.Logger.LogError($"CompleteConditionGeneric: {ex}");
            }

            if (quest.IsConditionDone(condition)) return;

            // ---- 5. CheckForStatusChange ----
            try
            {
                quest.CheckForStatusChange(false, false, true);
            }
            catch { }

            if (quest.IsConditionDone(condition)) return;

            // ---- 6. 最终兜底：直接反射把任务状态改为 AvailableForFinish ----
            try
            {
                var setStatus = AccessTools.Method(
                    typeof(Quest),
                    "SetStatus",
                    new[] { typeof(EQuestStatus), typeof(bool), typeof(bool) });

                if (setStatus != null)
                {
                    SkipperPlugin.Logger.LogInfo(
                        "[Skipper] 全部失败，强制 SetStatus(AvailableForFinish)");
                    setStatus.Invoke(quest, new object[]
                    {
                        EQuestStatus.AvailableForFinish, true, false
                    });
                    SkipperPlugin.Logger.LogInfo(
                        $"[Skipper] 强制 SetStatus 后 status={quest.QuestStatus}");
                }
            }
            catch (Exception ex)
            {
                SkipperPlugin.Logger.LogError($"强制 SetStatus: {ex}");
            }
        }

        // ========== 支线点击绑定：一字未动 ==========
        private static void BindClick(
            DefaultUIButton skipButton,
            Quest quest,
            Condition condition,
            QuestController questController)
        {
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

                    try
                    {
                        if (quest.ProgressCheckers != null
                            && quest.ProgressCheckers.ContainsKey(condition))
                        {
                            var checker = quest.ProgressCheckers[condition];
                            System.Func<ConditionProgressChecker, double> getterManaged =
                                _ => (double)condition.value;
                            checker.SetCurrentValueGetter(
                                (Il2CppSystem.Func<ConditionProgressChecker, double>)
                                getterManaged);
                        }
                    }
                    catch (Exception ex)
                    {
                        SkipperPlugin.Logger.LogError($"SetCurrentValueGetter: {ex}");
                    }

                    try
                    {
                        questController.CompleteConditionById(
                            quest.Id, condition.id.ToString());
                    }
                    catch (Exception ex)
                    {
                        SkipperPlugin.Logger.LogError($"CompleteConditionById: {ex}");
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
    }
}