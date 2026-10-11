# 双方状态 UI

打开 F:/JuGuangDeng 的 Assets/RainbowLi/Scenes/RainbowLiGame.unity。

玩家状态横排显示在玩家血条正上方，状态多时可横向滚动；敌人状态仍显示在战斗右侧，纵向滚动。绿色增益、红色负面状态、紫色特殊效果；文字分类同时显示，不只靠颜色区分。标签显示当前层数，点击查看持有者、影响对象、触发时机、持续时间和层数消耗规则。无状态显示“暂无状态”。状态消失后标签和已打开的说明自动关闭。

点燃增益属于持有者：持有者每次行动后对另一方施加烧伤。烧伤属于受影响者：回合开始时结算伤害。说明使用玩家/敌人的实际名称区分对象。

场景编辑位置：GameView / Table / BattlePanel 下的 PlayerStatuses、EnemyStatuses；对应的 PlayerStatusDetails、EnemyStatusDetails 为说明面板。

- Heading：标题和操作提示。
- Viewport / StatusContent / StatusChipTemplate：滚动区域、标签模板。模板默认隐藏，运行时克隆。
- BattleStatusPanelView：增益/负面/特殊颜色、内容区域、说明面板引用；Single Row 用于横排标签，玩家开启、敌人关闭。
- BattleHudView：Player Statuses / Enemy Statuses 引用。
- Scripts/Unity/Views/BattleStatusPanelView.cs：标签刷新、分类、作用说明。
- Scripts/Domain/CombatStatuses.cs：只读 ActiveEffects / LifetimeOf，读取实际状态和配置的持续类型。
- Editor/BuffUiSceneMigration.cs：菜单 RainbowLi → UI → 接入双方状态标签，默认场景已经接入。

验证：Verification~/buff-ui.log，11 项真实场景检查通过，覆盖双方归属、点燃与烧伤说明、临时状态、超过六个状态、消失刷新和关闭按钮。截图：Verification~/BuffPreview。

玩家血条上方布局验证：Verification~/PlayerBuffPositionChecks.cs、player-buff-position.log。检查牌桌/状态栏/血条/手牌的间距、玩家横排与敌人原布局、18 个状态的横向滚动，以及点击和关闭说明。没有修改状态归属或结算规则。
