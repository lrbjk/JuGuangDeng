# 玩家与敌人内容配置

按《玩家（角色）内容项》《敌人内容项》的规则调整，新入口仍为 `Scenes/RainbowLiGame.unity`。角色、技能、卡效名称和未指定的数值为示例；可在 Inspector 编辑。

## 玩家

选择 `GameSystems/PlayerSystem` 的 Definition 引用，打开 `Data/DefaultPlayer.asset`。

| 字段 | 含义 |
|---|---|
| Attack / Max Health | 基础攻击与生命上限 |
| Hand Size | 每回合抽牌数量，默认 6，独立于手牌上限 |
| Hand Limit | 手牌容量，默认 9；抽到的溢出卡进入弃牌堆 |
| Max Actions | 单回合出牌上限，默认 4 |
| Random Starting Deck | 默认启用：生成 3 组数字 1–6，共 18 张起始牌 |
| Repeat Weight Multiplier | 每组内抽中某卡效后，该卡效权重乘以此值，默认 0.5；每组重新计算 |
| Starting Deck | 关闭随机起始牌时使用的手工牌组，至少 6 张 |
| Sequences | 三组基础牌序，引用对应 SequenceDefinition 资产 |
| Available Sequence Effects | 额外已拥有的牌序效果；基础牌序的效果自动加入可选池 |

随机起始牌现在从 CardSystem 的 Catalog 获取 26 项表格效果、X 范围和权重。每组保证数字 1–6 各一张，效果使用递减权重选择。卡牌取值已按最新表格调整，完整公式见 [CARD_EFFECT_GUIDE.md](CARD_EFFECT_GUIDE.md)。基础攻击字段保留用于未配置表格效果的兼容卡牌和敌人技能。

回合结束弃置全部已使用/未使用手牌；抽牌堆空时洗回弃牌堆，继续完成当次抽取数量。删牌界面在剩余 6 张时禁用删除，规则层也会拒绝操作并提示。文档没有提供保留手牌的具体卡效，本版未添加保留效果。

## 牌序

在选关地图上方点击三个数字槽，循环 1–6 与空槽；任一槽为空时该牌序无效。牌序下方按钮在已拥有的效果池内切换效果 1、添加/切换/移除效果 2。战斗中这些操作禁用。

打开 `Data/Sequences/*.asset`：

- Numbers：三个需求数字，0 表示空槽。
- Effect / Value / Insert Two：第一种效果及其数值。
- Requirement：Any 无限制、Straight 连续递增或递减、Triple 三个相同数字。
- Second Effect Enabled：是否携带第二种效果；Second Effect 设置其种类、数值、插入数字和独立触发要求。

两个效果分别检查各自要求；满足时依次结算。条件不满足的效果不会执行。场地牌序仍由最后一个数字所属方获得效果，玩家与敌人的牌序各自生效。购买的牌序效果会加入玩家可选池。

## 敌人

选择 `GameSystems/EnemySystem` 的 Definition 引用，打开 `Data/DefaultEnemy.asset`。

基础属性 Max Health、Attack、Action Count 决定生命、基础攻击和每回合抽取技能数量；Boss 使用独立的生命、攻击和技能次数。

展开 **Skills**，添加至少一个基础技能：

| 字段 | 含义 |
|---|---|
| Name / Number | 技能名称与牌序数字（1–6） |
| Effect | Strike 伤害对方，Guard 给自身护盾，Mend 治疗自身 |
| Modifier | 在基础攻击上加减的数值；实际结果最低为 0 |
| Weight | 基础技能抽取权重；同一回合重复抽中该技能后权重减半，下回合恢复 |
| Special | 是否为条件解锁的特殊技能 |
| Special Weight Ratio | 解锁后权重相对基础技能平均初始权重的倍率，限制在 0.3–0.7 |
| Condition / Threshold | Round At Least：达到指定回合；Health Percent At Most：生命百分比不高于指定值 |

基础技能可重复抽取。特殊技能首次达成条件时强制额外加入当前回合，不占 Action Count；从之后回合进入随机池，每回合至多随机抽到一张特殊技能。结算途中达成条件的强制技能追加到当前队列尾部。一次性解锁状态在本场战斗内保存，下一场重置。

**Conditional Sequences** 指定同样的 Condition/Threshold 和一个牌序资产。未激活时不触发；满足后整场战斗持续生效，即使生命后来恢复，也不会重新锁定。

当前示例：普通敌人每回合抽 2 个技能，Boss 抽 3 个；基础技能为冲击和屏障；第 3 回合额外加入重击；生命不高于 50% 时激活 4→5→6 的觉醒屏障。敌方所有初始技能都已放在安排栏，可移动但不可移除，缺少任一敌方技能时不能开始执行。

Skills 为空时保留旧的护盾周期/行动数字逻辑，供旧配置迁移使用；填写技能池后由 Skills 控制技能内容。精英的生命倍率、攻击额外加成和金币仍在 `DefaultLevels.asset` 中配置。

## 场景与模块

- 玩家与敌人继续使用独立 State / Module / Definition。
- `GameCanvas/Table/BattlePanel` 保存 9 个手牌槽、10 个行动槽、11 个插入按钮。卡牌均连接 PokerCard Prefab；极多特殊技能造成的溢出行动会自动扩展视图。
- `GameCanvas/Table/SharedRules/PlayerRule_*` 保存两种效果的按钮，事件引用 SequenceModule；数字按钮在 SequenceView 初始化时绑定。
- GameSession 的预测与真实结算使用同一规则，包括条件解锁、第二效果和特殊技能，预测不会解锁真实敌人的技能。
- 运行时选择 EnemySystem 可以查看所有已激活的敌方牌序。

在非 Play 模式修改资产后重新 Play 或点击新的一局。不要为了修改配置或牌面重新生成整个场景。

验证覆盖原有规则、文档新增规则、原生按钮、额外技能显示与分支关卡完整流程。批处理使用无图形模式，最终排版和动画节奏仍需在 Game 视图试玩确认。

验证记录：1885 项原有规则、716 项内容规则、18 项原生内容/Prefab 检查与 678 项路线回归通过。
