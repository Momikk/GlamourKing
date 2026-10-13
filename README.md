# 幻化之王 / GlamourKing

<img src="images/icon.png" alt="幻化之王图标" width="128" />

FFXIV Dalamud 插件，基准版本 `0.1.0.2`。使用 API 15、Dalamud.NET.Sdk 15.0.0 和 .NET 10。

玩家打开幻化柜时，自动显示测试窗口；关闭幻化柜时收起窗口。在柜子仍打开时手动关闭测试窗口，不会每帧重新弹出，下次打开柜子时才再次显示。插件在幻化柜已打开时加载，也会显示测试窗口。聊天命令 `/glamking` 可手动开关窗口，插件管理器的主界面按钮也可打开。

该版本只检测游戏界面，不读取收藏库存，也不修改物品或幻化模板。检测对象为 `MiragePrismPrismBox`，通过 Dalamud 的 IGameGui 公共包装器检查有效性、就绪和可见状态。

## 构建

安装 .NET 10 SDK，并通过 XIVLauncher 启动过一次带 Dalamud 的游戏。在仓库目录执行：

```powershell
.\build.ps1
```

脚本优先查找国服 XIVLauncherCN 的开发程序集，再查找 XIVLauncher；只支持 API 15。如需使用另一套程序集，先指定 `DALAMUD_HOME`：

```powershell
$env:DALAMUD_HOME = '你的 Dalamud 开发程序集目录'
.\build.ps1 -Configuration Release
```

输出 DLL 和生成的 manifest 位于 `GlamourKing/bin/Release/`；SDK 打包结果位于 `GlamourKing/bin/Release/GlamourKing/latest.zip`。不要把 Dalamud 自带程序集复制进插件分发包。

## 在游戏中加载

### 从第三方插件仓库安装

在 `/xlsettings` → 插件 → 第三方插件中添加以下链接，启用该仓库并保存设置：

```text
https://raw.githubusercontent.com/Momikk/DalamudPlugins/main/pluginmaster.json
```

随后在 `/xlplugins` 中刷新插件列表，搜索“幻化之王”或 `GlamourKing` 并安装。此清单发布的是 API 15 基准版本，安装包位于仓库的 `dist/`。同一版本请只采用仓库安装或开发插件加载其中一种方式。

### 本地开发插件加载

1. 输入 `/xlsettings`，进入 Experimental / 实验性设置，将输出目录中 `GlamourKing.dll` 的完整路径添加到 Dev Plugin Locations / 开发插件位置。
2. 输入 `/xlplugins`，在开发插件页找到“幻化之王”并启用。
3. 打开旅馆内的幻化柜，确认测试窗口显示“已打开”；关闭柜子，确认窗口收起。
4. 用 `/glamking` 手动打开测试窗口；用 `/xllog` 查看加载、柜子打开/关闭及卸载日志。

应使用与当前游戏分发渠道匹配的 Dalamud 程序集。框架 API 相同不等于所有原生功能跨区服兼容；此版本仅使用公开的界面包装器。

## 游戏内验收

确认正常启用、首次打开柜子、关闭后重开、手动关闭测试窗口、柜子已打开时重新加载插件，以及禁用后再次启用。重载后不应出现重复命令、重复窗口或重复打开日志。游戏 UI 隐藏时窗口由 Dalamud UiBuilder 的默认行为管理，不把隐藏 UI 当成库存加载状态。

编译成功只能确认 API 和打包兼容，实际游戏加载与打开柜子的行为需在游戏中验收。

本次基准版的构建与验证记录见 [docs/validation.md](docs/validation.md)。

## Momikk 统一插件仓库

统一清单由独立仓库 [Momikk/DalamudPlugins](https://github.com/Momikk/DalamudPlugins) 维护。此项目维护幻化之王的源码、图标、安装包和专用 `repo.json`，不再保存统一 `pluginmaster.json`。

请使用独立仓库的统一库链，并移除旧的 `GlamourKing/pluginmaster.json` 或 `GlamourKing/repo.json` 来源，避免重复配置。新插件发布后加入独立仓库清单，统一链接保持不变。
