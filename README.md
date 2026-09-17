Terkoiz.Skipper
SPT 5.0 的任务目标跳过 Mod。为任务窗口里的每个任务条件添加一个 SKIP 按钮，点击后可以直接完成该条件，无需实际去做任务目标。

基于 Terkoiz 原版 Skipper 移植到 SPT 5.0（BepInEx 6 + IL2CPP + SPTushonka）。

功能
在任务详情窗口的每个任务条件右侧显示一个 SKIP 按钮

按住热键（默认 左 Ctrl）显示所有 SKIP 按钮，松开隐藏

点击 SKIP 弹出确认窗口，确认后跳过对应条件

新增主线功能的跳过
但建议与商人对话不要跳过，可能会坏档，不触发下一阶段的主线剧情
主线跳过仍然有些小bug，不过至少能用了

支线任务：
条件通过原版逻辑强制完成

主线任务：
条件通过多层递进兜底强制完成，包括 ProgressChecker 强制置值、CompleteConditionById（先试 Template.Id 再试 Quest.Id）、CompleteConditionGeneric 反射调用、最后兜底强制将任务状态推到 AvailableForFinish

安装
确认你使用的是 SPT 5.0（BepInEx 6 / IL2CPP 版本）

将 terkoiz-skipper.dll 复制到：

<SPT目录>\BepInEx\plugins\Terkoiz.Skipper\
启动游戏，插件会自动加载

首次运行时会在以下路径生成配置文件：
<SPT目录>\BepInEx\config\com.terkoiz.skipper.cfg


键	默认值	说明
1. Enabled	true	Mod 总开关。修改后需要重新打开任务窗口才能生效
2. Always display Skip button	false	开启后 SKIP 按钮始终可见，无需按热键
3. Display hotkey	LeftControl	显示 SKIP 按钮的热键。格式为 KeyCode + KeyCode，例如 LeftControl、LeftShift + A

已知限制
个别主线任务由于自身状态机较复杂，SKIP 后可能需要手动切换一次界面才会刷新显示为"可交付"

主线任务的 SKIP 按钮位置相比支线任务略偏，不影响使用

从源码编译
依赖
.NET SDK 6.0 或更高

已安装的 SPT 5.0（脚本会自动从 TarkovDir 读取游戏 DLL）

编译
直接运行：

bat
build.bat
或手动：

bat
dotnet build Terkoiz.Skipper.csproj -c Release
编译产物会自动复制到：

text
<TarkovDir>\BepInEx\plugins\Terkoiz.Skipper\terkoiz-skipper.dll
同时在项目目录下生成 release/Terkoiz.Skipper.zip，方便分发。

路径配置
如果你的 SPT 目录不是 D:\SPT-5.0.0-47242-BE\，修改 Terkoiz.Skipper.csproj 里的这一行：

xml
<TarkovDir Condition=" '$(TarkovDir)' == '' ">D:\你的SPT目录\</TarkovDir>
文件结构
text
Terkoiz.Skipper/
├── Terkoiz.Skipper.csproj    # 项目文件
├── SkipperPlugin.cs          # BepInEx 插件入口 + Update 循环
├── QuestObjectiveViewPatch.cs # Harmony Patch，注入 SKIP 按钮
├── build.bat                 # 一键编译脚本
└── README.md
技术说明
通过 Harmony 对 EFT.UI.QuestObjectiveView.Show 做 Postfix Patch

支线任务：复制 __instance._handoverButton 作为 SKIP 按钮模板，位置跟随原 UI 布局

主线任务：主线任务没有 _handoverButton，因此从 view 子节点里找任意 DefaultUIButton 作为模板，挂到 view 的父节点下

条件跳过核心调用：

quest.ProgressCheckers[condition].SetCurrentValueGetter(_ => (double)condition.value)

questController.CompleteConditionById(templateId, conditionId)

失败时逐级 fallback 到 CompleteConditionGeneric 反射调用、CheckForStatusChange、SetStatus(AvailableForFinish)

致谢
原 Mod 作者：Terkoiz

SPT 团队

BepInEx 团队

Il2CppInterop 团队
