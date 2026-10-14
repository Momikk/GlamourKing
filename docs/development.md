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
