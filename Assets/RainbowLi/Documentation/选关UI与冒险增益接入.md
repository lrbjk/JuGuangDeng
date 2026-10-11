# 选关 UI 与关卡敌人增益

打开 `Assets/RainbowLi/Scenes/RainbowLiGame.unity`。

## 当前增益规则

每完成 **10 个关卡**，敌人获得一次全局增益：**敌人卡牌效果 +1**，累计生效。完成 0～9 关为 +0，10～19 关为 +1，20～29 关为 +2。战斗、精英、休息、商店、事件全部计入；进入、未完成或失败不计入。完成记录去重，重复刷新不多计。

增益作用于后续所有普通、精英和首领敌人的每张卡牌的每个效果数值：攻击伤害、防御护盾、恢复生命、状态层数。它是全局数值修正，不添加“力量”状态，不修改卡牌资产，不增加卡牌数字、牌序效果或已施加状态的每次结算伤害。同一张牌重放时仍使用该全局加成，不再额外累加一遍。

## 策划调整位置

- 搜索挂载 `GameSession` 的对象，编辑其 `EnemyGrowthModule`：`Enable Growth` 开关、`Levels Per Stack` 每几关增长一次（默认 10）、`Amount Per Stack` 每次效果增加多少（默认 1）。配置开始新局时读取，现阶段只有卡牌效果加成一种类型。
- `GameCanvas` 的 `RouteMapView`：卡片宽、高、后排露出高度与数量。
- `GameCanvas/Table/MapPanel/BannerRoute/AdventureSidebar`：左侧信息；`BossRoom`：首领入口；`BattleProgress`：战斗胜场进度；`RouteScroll/Viewport/RouteContent`：关卡卡片。均为原生 UGUI，可编辑颜色、文字与布局。
- `AdventureSidebar/EnemyGrowthGroup` 显示全局增益次数、当前加成及距下次增益还差多少关。上方五格显示本层 BOSS 的战斗解锁进度，与全局增益的关卡计数独立。
- `Assets/RainbowLi/Prefabs/RouteSelectionCard.prefab`：卡片外观；关卡类型、名称、图标和长度在现有 `LevelDefinition` 与关卡编辑器配置。当前示例基础路线有 8 行；BOSS 尚未解锁时会补充战斗行，实际可完成关卡数可能超过 10 并触发全局增益。

## 程序接入位置

- `Scripts/Unity/Systems/EnemyGrowthModule.cs`：Inspector 配置写入 `GameSetup`；次数为 `Route.Completed.Count / EnemyGrowthLevelInterval`，加成为次数乘 `EnemyGrowthAmount`。
- `Scripts/Domain/ContainmentGame.Combat.cs`：`StartBattle` 按完成关卡数快照本场 `EnemyCardEffectBonus`，删除战斗次数到力量状态的旧逻辑。
- `Scripts/Domain/ContainmentGame.cs`、`ContainmentGame.Effects.cs`：`CardEffectGrowthFor` 排除玩家行动、数字插入、牌序触发、状态结算和仅加值重放；每个敌人卡牌效果通过同一入口加入全局加成。复合牌每项效果加一次。
- `Scripts/Domain/RunSave.cs`：保存本场加成与完成记录。读档、战斗预览使用相同数值，不重复叠加。
- `Scripts/Unity/Views/BattlePanelView.cs`、`BattleHudView.cs`：行动卡效果值和技能详情展示全局加成。
- `Scripts/Unity/Views/AdventureMapHudView.cs`：关卡完成数、全局增益、胜场进度、首领入口和玩家牌序；首领按完成 5 + 当前冒险大层数场战斗解锁，解锁后可从选关顶部随时挑战。
- `Scripts/Unity/Systems/GameSession.cs`、`RunSave.cs`：新局重置计时，存档恢复已用时间。
- `Editor/RouteDiagramSceneMigration.cs`：菜单 `RainbowLi/UI/接入三列卡片选关界面`；已接入场景不重复创建控件。


## BOSS 关卡规则

当前层数为冒险大层数，从第 1 层起，与卡片行号无关。在 `LevelDefinition` Inspector 或菜单 `RainbowLi/关卡/关卡编辑器` 的“当前冒险大层数”配置；新局读取。第 1 层完成 6 场普通或精英战斗解锁，第 2 层需要 7 场。计数来自本层路线的已完成战斗节点，事件、休息、商店不计入；胜利后领取奖励完成节点才计入。

解锁后无须走到路线末行，可继续探索其他卡片，也可随时点击顶部 `BOSS 房间`。挑战仍要求当前处于选关界面、没有未完成的当前关卡，失败或已通关后不能再次进入。

`RouteRun.CompletedBattleCount`、`BossRequiredBattles`、`BossUnlocked` 是程序查询接口。`LevelModule.SelectBoss()` 是统一挑战入口；`ProgressionModule.StartBoss()` 和顶部 UI 均调用它。`RouteRun.ConfigureBossUnlock(layer)` 在新路线初始化时设置层数，读档保留已保存层数。`RunSave.RouteAdventureLayer` 与 `RouteBossUnlockByBattles` 保存规则，支持提前进入 BOSS 后读档；`ContainmentGame.StartBattle` 也检查解锁条件，不能通过直接调用绕过。

配置路线时需留足可完成的战斗节点，确保玩家有途径达到所配置的 BOSS 解锁数量。现有首领节点继续作为首领内容入口，不要求它紧邻玩家当前卡片位置。
卡片地牢的普通路线用尽且 BOSS 尚未解锁时，RouteRun 会补充一行普通战斗卡片，并将首领内容节点后移。补充节点和完成记录一起存档，保证短路线也有途径达到解锁门槛。
