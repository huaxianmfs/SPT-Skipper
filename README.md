SPT 5.0 的任务目标跳过 Mod。支线任务的任务条件添加一个 SKIP 按钮，点击后可以直接完成该条件。

基于 Terkoiz 原版 Skipper 移植到 SPT 5.0.0_BE

功能
在任务详情窗口的每个任务条件右侧显示一个 SKIP 按钮

按住热键（默认 左 Ctrl）显示所有 SKIP 按钮，松开隐藏

点击 SKIP 弹出确认窗口，确认后跳过对应条件

安装

将 terkoiz-skipper文件夹 放在：

.\BepInEx\plugins\

首次运行时会在以下路径生成配置文件：

<SPT目录>\BepInEx\config\com.terkoiz.skipper.cfg

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

<TarkovDir>\BepInEx\plugins\Terkoiz.Skipper\terkoiz-skipper.dll
同时在项目目录下生成 release/Terkoiz.Skipper.zip，方便分发。

路径配置
如果你的 SPT 目录不是 D:\SPT-5.0.0-47242-BE\，修改 Terkoiz.Skipper.csproj 里的这一行：

xml
<TarkovDir Condition=" '$(TarkovDir)' == '' ">D:\你的SPT目录\</TarkovDir>

致谢
原 Mod 作者：Terkoiz

SPT 团队

BepInEx 团队

Il2CppInterop 团队
