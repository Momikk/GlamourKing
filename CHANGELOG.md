# 更新记录

## 0.1.0.9 — 2026-10-14

- 主窗口改用 KamiToolKit 创建的游戏原生窗口、勾选框、分类标签、图标及滚动列表。
- 使用紧凑行距：每行 28 像素，装备图标 24 像素；长名称省略显示，悬停查看完整信息。
- 将“隐藏套装中的物品”移至左侧，保留过滤选择与默认“可补齐”分类。
- 保留部位分组、自动刷新及原生装备选择列表的可补齐名称标红。
- 改用异步加载和卸载，关闭窗口时释放自建节点；更新安装包，包含原生 UI 依赖与 MIT 许可证。

国服 API 15 编译与离线回归检查通过。原生外观、滚动、主题/缩放及反复关闭与卸载仍待游戏内验收。

### English

- Replace the main window with game-native controls built with KamiToolKit, including tabs, checkbox, icons and a scrolling equipment list.
- Use compact 28 px rows with 24 px icons; truncate long labels and show full details on hover.
- Move the hide-gearset-items checkbox to the left; retain its saved choice and open on the Completable tab by default.
- Preserve equipment grouping, automatic refresh and completable-item name highlighting in the game's equipment selector.
- Load and unload asynchronously, release owned nodes when closing, and include the native UI dependency and its MIT license in the download.

CN API 15 compilation and offline regression checks pass. Native rendering, scrolling, themes/scaling, repeated closing and unloading still require in-game validation.


## 0.1.0.8 — 2026-10-14

- 将分类下拉框改为单击标签，每次打开默认显示“可补齐”。
- 装备列表改为图标、名称、部位、所属套装和状态五列，按游戏部位顺序排列并分组。
- 移除顶部大段说明、扫描时间、手动刷新按钮和搜索框；保留自动刷新与装备套装过滤。
- 原生“幻影化”装备选择列表的可补齐装备名称标红；关闭原生界面或禁用插件时恢复。
- 标红不受插件当前分类影响，数据待确认时不标红，同名但状态有歧义时保守跳过。

编译、排序与高亮目标筛选检查通过；原生颜色显示、滚动翻页和恢复仍待游戏内验收。

## 0.1.0.7 — 2026-10-14

- 新增“已保存相同套装”“可补齐现有套装”“尚未建立套装”三类状态。
- 按投影台内的套装记录和具体部件是否保存来判断；散件存放不算建立套装。
- 新增状态筛选、颜色提示与分类数量，鼠标悬停状态可查看各所属套装详情。
- 多套装情况下优先显示可补齐状态；数据未加载时显示“待确认”，避免误判为未建立。

三类状态不自动操作游戏，品质与收纳条件仍需游戏确认。编译和离线分类检查通过；截图中两类状态与尚未建立套装的实机结果仍待验收。

## 0.1.0.6 — 2026-10-14

- 新增“隐藏套装中的物品”勾选框，过滤已登记到装备套装（职业配装）中的装备。
- 使用游戏原生装备套装登记判断，显示过滤后数量及隐藏数量。
- 保存过滤选择；默认不隐藏，且不改变游戏窗口自身的勾选状态。
- 过滤可以与名称搜索同时使用，未确认登记状态的物品保留显示并提示。

已确认先前 36 件与游戏 30 件的差异来自游戏的装备套装过滤，并非多读了 6 件物品。新开关的实际显示和保存表现仍待游戏内验收。

## 0.1.0.5 — 2026-10-14

- 新增套装幻影化装备汇总，覆盖物品栏四页和兵装库各装备分类，无需逐页翻找。
- 显示装备名称、HQ/NQ、所在位置和所属套装，重复副本分别列出。
- 新增装备与套装名称搜索、立即刷新和每秒自动刷新。
- 打开投影台自动显示列表，关闭投影台或切换角色时清空结果。
- 数据未完整加载时提示等待，读取失败时清空结果并延迟重试。

范围仅限物品栏与兵装库；支持套装幻影化不代表已集齐整套。编译、离线筛选与安装包核对通过，实际游戏内显示及刷新仍待验收。

## 0.1.0.4 — 2026-10-13

- 将内部标识、代码命名空间、程序集、项目目录和 GitHub 仓库统一改为 SuitKing。
- 更新发布清单、下载地址及文档链接。

从命名空间为 `glamourking` 的旧版迁移时，请先卸载旧插件再安装 SuitKing；统一库链无需更换。

## 0.1.0.3 — 2026-10-13

- 显示名称改为“套装之王 SuitKing”，同步窗口文字和使用说明。
- 新增 `/suitking` 与 `/sking`，移除 `/glamking`。

## 0.1.0.2 — 2026-10-13

- 插件安装器显示名称改为“幻化之王 GlamourKing”，中英文并存。

## 0.1.0.1 — 2026-10-13

- 添加斗篷图标及图标地址。

## 0.1.0.0 — 2026-10-13

- 发布首个 Dalamud API 15 基准版本。
- 打开幻化柜时显示测试窗口，关闭时收起。
- 提供 `/glamking` 手动开关窗口。
