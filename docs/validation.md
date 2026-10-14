# 0.1.0.0 基准版本验证记录

日期：2026-10-13。

使用 .NET SDK 10.0.203、Dalamud.NET.Sdk 15.0.0 和本机 XIVLauncherCN 的 Dalamud 15.0.3.6 开发程序集，执行 `build.ps1` 的 Release 构建成功，0 警告、0 错误。

生成的 manifest：InternalName 为 GlamourKing，AssemblyVersion 为 0.1.0.0，DalamudApiLevel 为 15。输出使用 .NET 10，插件无额外第三方运行依赖。发布包由官方 SDK 的打包器生成。

源码检查确认：只在幻化柜从不可见/不存在变为就绪且可见时自动打开窗口；每帧重新取得公开包装器，不保留游戏指针；手动关闭不会在同次打开期间反复弹窗；Dispose 取消 Framework、UiBuilder 订阅并移除命令和窗口。这些是代码检查结论，不是游戏运行证据。

本次未进行游戏内运行验证：开发时游戏进程未启动。首次安装需按 README 核对加载、柜子开关、手动关闭、柜子已开时热加载和重复启停。

## 0.1.0.4 完整改名

项目、代码命名空间、程序集和 InternalName 已统一为 SuitKing，GitHub 仓库改为 Momikk/SuitKing。Release 构建通过，0 警告、0 错误。ZIP 内 SuitKing.json 与发布清单的名称、版本和内部标识一致，仅注册 /suitking 与 /sking。

旧版 GlamourKing 的记录保留为历史证据；当前安装包使用 SuitKing.dll。更改内部标识后需卸载旧插件再安装新版，游戏内最终显示仍需验收。

## 0.1.0.5 全分页套装装备汇总

日期：2026-10-14。API 15 / Dalamud.NET.Sdk 15.0.0 / .NET 10。

实现物品栏四页及全部有效兵装库装备分类的只读扫描，通过本机 MirageStoreSetItemLookup 筛选套装幻影化装备。保留重复副本、HQ/NQ 和所在格位，提供套装名称、搜索及每秒刷新。投影台关闭、角色切换和读取失败时清空快照；容器未加载时明确提示结果不完整。

Release 构建 0 警告、0 错误。8 项汇总回归检查通过，另有 4 项国服数据检查通过：实际套装表可加载，帕戈尔赞治愈胸衣（33531）映射为帕戈尔赞治愈套装，改良型斯卡艾瓦咏咒长衣（23525）没有套装映射，以及真实目录与汇总筛选的联合检查。截图另一件名称未能精确匹配，不计作已验证样例。

数据结构以本机当前 FFXIVClientStructs.dll 为准，不复制上游偏移、不引入 Hook。当前安装 ZIP 的 SuitKing.dll / SuitKing.json 与 API 15、版本 0.1.0.5 的发布清单一致。真实游戏内投影台触发、容器读取、列表显示和更新仍待用户验收，不能以离线检查替代。
