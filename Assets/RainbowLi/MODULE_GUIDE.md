# RainbowLi：模块与场景编辑指南

## 打开新版场景

在 Unity Project 中双击 `Assets/RainbowLi/Scenes/RainbowLiGame.unity`。旧的 `ContainmentPrototype.unity` 是 IMGUI 回归参考，继续保留；新版场景不使用旧控制器，不会在运行时重建整个界面。

## 现在能在 Hierarchy 编辑什么

```
GameSystems
  PlayerSystem        PlayerModule：玩家定义与独立运行时状态
  EnemySystem         EnemyModule：敌人定义、行动模式与独立运行时状态
  LevelSystem         LevelModule：路线、节点进入与通关状态
  CardSystem          CardModule：选牌、插入、移动、移除
  EngravingSystem     EngravingModule：升级和铭刻操作
  SequenceSystem      SequenceModule：修改和购买牌序
  BattleSystem        BattleModule：逐步结算、暂停、自动执行
  ProgressionSystem   ProgressionModule：任务、奖励、商店、维护
  BattleEffects       BattleEffects：粒子、震动、数字、音效、叠加
GameCanvas
  Table
    HUD               双方血条、护盾、增伤、金币和回合
    SharedRules       三张牌序卡、场地与敌方牌序
    MapPanel          BranchMap：分层路线、节点与连线
    RouteEventPanel   事件选择
    BattlePanel       HandCard、ActionCard、InsertSlot、控制按钮
    RewardPanel       三张奖励卡和选择按钮
    ServicePanel      商店、维护、DeckScroll
    EndPanel          成功/失败
  EffectsOverlay      动效层
EventSystem           InputSystemUIInputModule
Main Camera
```

场景中的手牌、行动卡、奖励卡与牌序卡是保存好的 Prefab 实例，编辑模式下就存在。

1. 使用顶部 `RainbowLi → 场景预览 → 战斗牌桌/奖励/商店与维护`，显示要编辑的面板；预览操作支持 Undo。运行时会按照游戏阶段显示正确面板。
2. 用 RectTransform 移动、缩放对象；用 Image、Text、Button 调颜色、字体和按钮样式。
3. 双击 `Prefabs/PokerCard.prefab` 编辑牌面、花色、角标、效果标签和铭刻标记。修改 Prefab 后各处实例会一起更新。
4. `SequenceCard.prefab` 控制牌序卡样式；`DeckRow.prefab` 控制商店卡牌行；`ImpactSpark.prefab`、`DamageNumber.prefab` 控制粒子与伤害数字。
5. 布局通过 CanvasScaler 的 Expand 模式适配窗口，1440×900 牌桌居中完整显示，不会按窗口比例切掉底部按钮。

不要在日常调 UI 时重新执行“创建可编辑游戏场景”：它是初始化工具，会覆盖生成的布局和默认配置，已有场景时会提示确认。直接编辑场景或 Prefab 即可。

## 配置资产

| 资产 | 可以修改 |
|---|---|
| `Data/DefaultGame.asset` | 玩家/敌人定义引用、卡池、场地牌序 |
| `Data/DefaultPlayer.asset` | 玩家生命、手牌数、行动上限、初始金币、起始牌组、玩家牌序 |
| `Data/DefaultEnemy.asset` | 敌人/Boss 生命与攻击、护盾、行动数、护盾槽位及回合周期、固定或随机行动数字、敌方牌序 |
| `Data/Cards/*.asset` | 数字、效果、基础加成、升级加成、使用的铭刻定义 |
| `Data/DefaultEngraving.asset` | 完成牌序时的额外伤害与描述 |
| `Data/Sequences/*.asset` | 三个数字、效果种类、数值、是否追加数字 2 |

右键 Project：`Create → RainbowLi` 可以创建新的定义；再拖入 DefaultGame 或系统组件的对应字段。

新版默认配置采用递减权重随机生成的 18 张起始牌与三组固定场地模板；取消 PlayerDefinition 的 Random Starting Deck 可以恢复手工起始牌。GameDefinition 每次新游戏会复制配置，生命、护盾、触发次数、升级和铭刻只修改运行时数据，不污染配置资产。

初始牌序视图有 3 个位置，手牌有 9 个位置，行动有 10 个位置（可随额外技能扩展），奖励有 3 个位置。每回合抽牌默认仍为 6，手牌上限独立为 9。

配置资产修改后停止并重新 Play（或点击“新的一局”）生效。Play 时改动场景属性一般在停止后回退；持久修改请在非 Play 模式进行。

## 模块代码边界

- `Scripts/Domain/GameTypes.cs`：卡牌、牌序、行动和预测的数据类型。
- `Scripts/ContainmentGame.cs`：共享游戏状态、初始化、预测副本。
- `Scripts/Domain/ContainmentGame.Cards.cs`：生成、抽牌、洗牌、行动中的卡牌操作。
- `Scripts/Domain/ContainmentGame.Engravings.cs`：升级、铭刻购买与触发。
- `Scripts/Domain/ContainmentGame.Sequences.cs`：匹配优先级、归属、触发上限、效果与数字插入。
- `Scripts/Domain/ContainmentGame.Combat.cs`：战斗开始、回合、结算与护盾抵伤。
- `Scripts/Domain/ContainmentGame.Progression.cs`：奖励、任务循环、商店、休息。
- `Scripts/Unity/Data`：ScriptableObject 配置。
- `Scripts/Unity/Systems`：可挂在场景中的独立模块组件，显式引用 GameSession。
- `Scripts/Unity/Views`：原生 uGUI 视图，接收数据并把点击交给系统组件。
- `Scripts/Unity/Effects`：监听结算事件的表现模块；不修改战斗数值。
- `Editor`：场景生成、编辑模式预览与 GameSession 的运行时 Inspector。

纯 C# 模型使用 partial 分文件，共享一个状态与同一套预测算法，避免各系统各自保存一份生命/牌组导致不同步。Unity 模块是独立的 MonoBehaviour，场景中可以单独选中、配置；系统之间通过显式 Session 引用与 Changed/Resolved 事件通信，没有全局单例。

## 调速度与动效

选择 `GameSystems/BattleSystem`：修改行动间隔、牌序间隔、是否自动执行。

选择 `GameSystems/BattleEffects`：修改最大震幅、粒子数量、同时爆发数、连锁时间窗口、最大叠加强度；可赋予轻/重命中 AudioClip。空音效字段使用生成的冲击声。

卡牌悬停和选中抬升在 CardView 上配置；静态卡牌坐标从场景 RectTransform 读取，旋转用 Unity RectTransform，不再混用 IMGUI 缩放坐标。

## 调试与验证

运行时选择 GameSystems 的 GameSession，Inspector 可以观察双方生命/护盾/增伤、牌组与回合。

- 79 项纯规则断言通过，包含双方场地归属、护盾抵伤、预测一致性，以及配置和实例数据隔离。
- Unity 6 隔离工程生成场景、导入字体/花色、保存 Prefab 成功。
- 456 项原生场景回归检查通过，覆盖引用、Prefab 实例、按钮选牌与插入、确认结算、胜利奖励、升级、铭刻、休息和下一回合。
- 无图形批处理验证不等同于视觉验收；仍需在 Game 视图调整最终排版和动效节奏。

没有修改 SampleScene、Packages、ProjectSettings 或项目构建列表；如果要出包，请把新版场景加入你的 Build Profile。


## 玩家与敌人已独立

场景的 `GameSystems/PlayerSystem` 与 `GameSystems/EnemySystem` 分别挂 PlayerModule、EnemyModule。在 Inspector 点击各自的 Definition 引用即可编辑配置；Play 时各自 Inspector 展示各自生命、护盾与增伤。玩家还显示牌组、手牌和金币；敌人显示 Boss 状态与敌方牌序。

运行时：`GameSession.Game.Player` 为 PlayerState，拥有玩家生命、护盾、增伤、牌组、手牌、牌序和金币；`GameSession.Game.Enemy` 为 EnemyState，拥有敌方生命、护盾、增伤、Boss 和牌序，并负责生成敌方行动。双方共用基础抵伤算法，各自状态独立。

GameSession 只协调双方、共享数字流、行动队列和结算事件。旧 Health/Shield 等访问器转发到对应状态，兼容现有 UI，不保存第二份数值。场景组件上的 Definition 优先于 DefaultGame 的对应引用；停止后重新 Play 或新的一局应用配置。旧 DefaultGame 中的扁平数值已隐藏，只用于迁移旧资产。

## 分支关卡

路线配置、节点 Prefab 与场景编辑步骤见 [LEVEL_GUIDE.md](LEVEL_GUIDE.md)。

## 玩家与敌人内容规则

完整字段、特殊技能触发、条件牌序和双效果编辑方法见 [ACTOR_CONTENT_GUIDE.md](ACTOR_CONTENT_GUIDE.md)。

## 表格卡牌效果

CardSystem 的 Catalog 指向 DefaultCardEffects。26 项效果定义、状态时机与公式见 [CARD_EFFECT_GUIDE.md](CARD_EFFECT_GUIDE.md)。
