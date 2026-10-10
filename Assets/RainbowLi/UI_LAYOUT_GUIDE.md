# 战斗 UI 编辑位置

打开 `F:/JuGuangDeng/Assets/RainbowLi/Scenes/RainbowLiGame.unity`。
在 Unity 顶部菜单选择 `RainbowLi > 场景预览 > 战斗牌桌`，即可在非运行状态查看布局。预览只切换面板，不会自动保存场景。

## 场景对象

以下对象都在 `GameCanvas/Table` 中，可直接改 RectTransform、颜色和文字样式。

- `Sidebar/EnemySkillGroup`：敌人技能组，预留两行共 8 张技能牌；运行时显示已启用的敌人卡牌配置。
- `Sidebar/EnemySequenceGroup`：敌人牌序三行。
- `Sidebar/Resources`：金币和牌组数量。
- `Sidebar/PlayerSequenceGroup`：玩家牌序三行；地图阶段可调整数字和效果。
- `ArenaSequence`：场地牌序与触发说明。
- `BattlePanel/EnemyPortraitArea/Portrait`：敌人立绘的 Image。在 Source Image 拖入 Sprite；运行时有图时自动显示，无图时显示占位文字。编辑模式预览立绘需启用该 Image。
- `BattlePanel/EnemyHealth`、`EnemyShieldBadge`：敌人生命与护盾。
- `BattlePanel/PlayerHealth`、`PlayerShieldBadge`：玩家生命与护盾。
- `BattlePanel/ActionArrangement/ActionCards`：行动牌、插入槽和移动/移除按钮。
- `BattlePanel/CurrentHand`：当前手牌。
- `BattlePanel/DrawPile`、`DiscardPile`：抽牌堆和弃牌堆按钮；点击查看真实牌堆内容，列表按数字排序。
- `Settings`、`SettingsPanel`：设置入口、战斗音量和播放速度；目前为本次运行即时设置，未接永久设置存档。
- `PilePanel`：牌堆查看面板及滚动列表。

## 脚本与 Prefab

- `Scripts/Unity/Views/BattleHudView.cs`：独立战斗 HUD，负责护盾、资源、敌人技能/牌序、牌堆弹窗、设置及卡牌排列。
- 在 `GameCanvas` 的 BattleHudView Inspector 调整 Hand Scale、Action Scale、Card Gap，以及各区域和立绘引用。
- `Scripts/Unity/Views/GameView.cs`：保留原流程和按钮绑定，刷新时调用 BattleHudView。
- `Prefabs/PokerCard.prefab`：修改玩家、敌人和技能缩略牌的牌面、数字、花色与效果文字样式。
- `Editor/BattleLayoutMigration.cs`：把已有场景迁移到参考图布局，保留原游戏配置及卡牌 Prefab 关联。菜单 `RainbowLi > UI > 应用牌面与UI布局到当前场景`；已经应用时不会覆盖再次手调的布局。

## 本次范围与验证

布局采用 1920×1080 设计分辨率，CanvasScaler Expand 保持整体比例。地图、奖励、商店、休息、事件和结算保留原交互。

游戏规则保持原样；`Scripts/ContainmentGame.cs` 仅新增 DrawPileCards、DiscardCards 两个只读牌堆列表入口，供界面查看。

隔离 Unity 项目已通过 857 项场景/布局/交互检查和 853 项完整路线回归检查，并渲染检查九张手牌的战斗画面。

立绘目前为可编辑占位，没有附加角色美术。设置未接存档，后续继续按任务清单实现。
