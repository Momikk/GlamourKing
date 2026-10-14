# 开发与验证

使用 .NET 10 SDK 与 Dalamud API 15 开发程序集，在仓库根目录运行：

```powershell
.\build.ps1
```

构建脚本优先查找 XIVLauncherCN，再查找 XIVLauncher。可通过 `DALAMUD_HOME` 指定开发程序集目录。生成的 DLL 与 manifest 在 `SuitKing/bin/Release/`，SDK 安装包在 `SuitKing/bin/Release/SuitKing/latest.zip`。不要分发 Dalamud 自带程序集。

## 回归检查

运行跨分页汇总、重复副本、品质、武器分类、缺失与移除场景：

```powershell
dotnet run --project tests/SuitKing.Tests.csproj -c Release
```

传入本机国服 `game/sqpack` 完整路径，可额外验证真实物品与套装数据：

```powershell
dotnet run --project tests/SuitKing.Tests.csproj -c Release -- '你的国服目录/game/sqpack'
```

测试项目默认从 XIVLauncherCN 的 dev 目录引用 Lumina；可用 `-p:DalamudHome=你的开发程序集目录` 覆盖。测试不接触运行中的游戏进程。

## 读取与筛选

窗口状态使用 IGameGui 检测 `MiragePrismPrismBox` 与 `MiragePrismPrismBoxCrystallize`。仅在已登录并打开投影台时，在 Framework.Update 中读取 InventoryManager 的物品栏四页和 11 个兵装库分类；不保留原生指针，不写游戏内存，不调用收纳、还原或换页方法。

通过本机游戏 `MirageStoreSetItemLookup` 中的有效套装映射筛选，每个副本保留品质和所在位置。该判断表示物品属于支持套装幻影化的套装，不表示整套已集齐、装备耐久等收纳条件已满足。未加载容器明确标为部分结果；关闭投影台、退出或切换角色时清空快照。自动读取间隔为一秒，读取异常时清空结果并延迟重试。

## 游戏内验收

在插件安装器更新 SuitKing；本地开发时，也可把 `SuitKing.dll` 的完整路径加入开发插件位置。请只选择一种加载方式。

打开投影台，核对标记装备跨物品栏与兵装库分类均出现在列表中，未标记的示例不出现。同物品 HQ/NQ 或多份副本应分别列出；移动、取出和收纳后应更新，切换游戏分页不应改变汇总范围。检查搜索、立即刷新、手动关闭、重新打开投影台、退出角色及插件禁用后命令注销。数据读取失败时查看 `/xllog`。

构建与离线检查不等于游戏交互已经通过。记录见 [validation.md](validation.md)。

## 发布更新记录

每次发版同步维护 CHANGELOG.md、README 中的版本，以及专用 repo.json 和统一 pluginmaster.json 中的 Changelog 字段。更新记录面向使用者，说明新增、修复、使用变化和已知限制，区分离线验证与实机验收。

装备套装过滤使用游戏原生 IsItemRegisteredToGearset，按具体 InventoryItem 判断而非只比对物品 ID。勾选框只影响显示，原始快照保留所有候选装备；配置通过 Dalamud 保存。验收时以用户当前 36 件候选、过滤后 30 件为对照，并确认切换、搜索、重载及角色切换。

套装保存状态只在 MirageManager.PrismBoxLoaded 为真时读取。PrismBoxItemIds 中映射到 MirageStoreSetItem 的条目才作为套装，保留列顺序的 11 个部位用 IsSetSlotUnlocked 判断是否已保存，不把单独存放的散件算作套装。状态分类是对存储记录的结构判断，不保证物品当前满足品质、耐久或其他提交条件。游戏内验收以用户提供的“已保存相同套装”和“发现了缺少此装备的现有套装”示例为对照，并补充未建立的实际样例。

0.1.0.8 的界面默认打开可补齐标签，装备图标/部位由本机 Item 表提供，不再显示品质和位置列，后者保留在名称悬停提示。原生标红监听 MiragePrismPrismBoxCrystallize 的 PreDraw 与 PreFinalize，遍历实时树列表渲染器查找装备名称；先保存原始文本和颜色，再临时修改显示。只从实时节点恢复自身改动，不解引用缓存指针。实机验收应确认补齐、翻页、滚动、过滤、选中行、关闭和禁用后的文字与颜色恢复。

## 0.1.0.9 原生 UI

主窗口为独立 NativeAddon，不向游戏投影台插入整张列表。通过 KamiToolKit 的原生 TabBarNode、CheckboxNode、ListNode 和 IconImageNode 构建；只创建可见行的节点并随滚动复用。窗口行高 28，图标 24，列宽固定布局，长名称使用原生 Ellipsis，完整内容放在提示中。使用 IAsyncDalamudPlugin，初始化等待工具库就绪，卸载先注销事件，再等待原生窗口关闭，最后释放工具库。窗口关闭动画完成前的重开请求延后处理。

工具库源码固定在 vendor/KamiToolKit，出处与本地适配见 THIRD-PARTY-NOTICES.md。安装包须同时包含 SuitKing.KamiToolKit.dll、Assets 及 licenses/KamiToolKit-MIT.txt，不能只替换主 DLL。现有 ImGui 主窗口已移除。

验收原生窗口的左侧勾选框、默认可补齐、四种分类和待确认、部位分组、24 px 图标、滚动和悬停；使用浅色/深色皮肤及不同缩放，重复开关投影台，点击关闭后用 /sking 重开，禁用/启用插件，确认无残留窗口。每秒自动刷新不应跳回列表顶部；切换分类应回到顶部。保留既有原生名称高亮检查。

## 0.1.0.11 自动补齐

OutfitAutoCompleter 只在用户点击后启动，Framework.Update 驱动 Next / Confirm / Convert / Result 状态。调用 AgentMiragePrismPrismSetConvert.Open 指定来源容器与格子；使用所属确认窗口的实际注册按钮事件，通用 SelectYesno 还须匹配国服的已有套装合并提示；最终点击原生 StoreAsGlamourButton。不开新套装，不直接调用 MirageManager.StoreExistingOutfit。

整个批次使用物品 ID、容器、格子及 HQ 标志授权每份装备，提交前实时检查装备套装登记与耐久，校验 PrismBoxIndex、选中套装及缺失部位。原生 Filled 标记对应的所有提交装备必须属于批次。每阶段超时 15 秒，最终提交前先进入 Result，避免逐帧重复；等待部件解锁和源物品移出，并等待原生窗口关闭后才处理下一件。取消停止后续点击，不能撤回已发送的游戏操作。按钮运行时禁用过滤勾选框；关闭插件窗口和禁用插件均停止流程。

验收需先用一件可补齐物品核对源格子、确认提示、棱晶数量、最终保存与成功计数，再检查多件、跨分页、装备套装过滤、品质/耐久限制、无棱晶、停止及关闭/重载。不要同时启用其他插件的自动收纳/自动确认功能参与验收。
