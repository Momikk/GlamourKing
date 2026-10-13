# 0.1.0.0 基准版本验证记录

日期：2026-10-13。

使用 .NET SDK 10.0.203、Dalamud.NET.Sdk 15.0.0 和本机 XIVLauncherCN 的 Dalamud 15.0.3.6 开发程序集，执行 `build.ps1` 的 Release 构建成功，0 警告、0 错误。

生成的 manifest：InternalName 为 GlamourKing，AssemblyVersion 为 0.1.0.0，DalamudApiLevel 为 15。输出使用 .NET 10，插件无额外第三方运行依赖。发布包由官方 SDK 的打包器生成。

源码检查确认：只在幻化柜从不可见/不存在变为就绪且可见时自动打开窗口；每帧重新取得公开包装器，不保留游戏指针；手动关闭不会在同次打开期间反复弹窗；Dispose 取消 Framework、UiBuilder 订阅并移除命令和窗口。这些是代码检查结论，不是游戏运行证据。

本次未进行游戏内运行验证：开发时游戏进程未启动。首次安装需按 README 核对加载、柜子开关、手动关闭、柜子已开时热加载和重复启停。

## 0.1.0.4 完整改名

项目、代码命名空间、程序集和 InternalName 已统一为 SuitKing，GitHub 仓库改为 Momikk/SuitKing。Release 构建通过，0 警告、0 错误。ZIP 内 SuitKing.json 与发布清单的名称、版本和内部标识一致，仅注册 /suitking 与 /sking。

旧版 GlamourKing 的记录保留为历史证据；当前安装包使用 SuitKing.dll。更改内部标识后需卸载旧插件再安装新版，游戏内最终显示仍需验收。
