# 核心程序接入：铭刻和牌序效果表

工程根目录：`F:/JuGuangDeng/Assets/RainbowLi/`。

## 现有工具接口

- `EffectTableDefinition.rows` 保存原 Excel 的编号、名称、具体效果、范围、所属池和备注，以及启用、权重、行为标识、倍率和固定参数。
- `Find(id)` 按编号查找并校验；`GetPool(pool)` 返回启用且属于指定池的行，空池名返回所有启用行。该查询不会进行随机抽样，也不会排除待实现行；调用方应按实现进度决定候选池。
- `EffectTableRow.TryRange(out min,out max)` 解析基础范围；`ResolveX(x)` 验证基础 X 并计算倍率。
- `SequenceDefinition` 已有 `effectTable`、`primaryEffectId`、`secondaryEffectId`、`primaryX`、`secondaryX`。
- `EngravingDefinition` 已有 `effectTable`、`effectId`、`effectX`。
- 现有定义没有配表引用时仍走旧逻辑；配表行属于未实现行为时适配器拒绝绑定运行。

## 分工与当前进度（2026-10-09）

另一位程序负责本清单中的运行时数据、规则、配置消费、随机数、存档和测试。你负责全部配表、编辑器、配置资产、场景、UI 和动效。新增目录只是建议位置，尚未实现。

- 已完成：铭刻 19 条、牌序 23 条原表录入，以及表格编辑、Excel 导入导出、按编号合并、校验和配置绑定。
- 已接现有结算：铭刻 03；牌序 01、02、03、05、16、18、19。铭刻 03 仅接现有牌序触发后的追加伤害入口，触发时机仍须核对。
- 待核心实现：铭刻其余 18 种行为；牌序其余 16 条，合并同类 X/2X 后共 11 种行为。倍率由数据决定，不需要写两套结算。
- 所有配表仍统一由你维护：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Data/`、`F:/JuGuangDeng/Assets/RainbowLi/Editor/`、`F:/JuGuangDeng/Assets/RainbowLi/Data/`。另一位程序提供运行时转换接口，再由你接适配器。

## 接入任务（按顺序执行）

### 1. 建立运行时效果和卡牌实例数据

- [ ] **要干什么：** 为卡牌建立稳定的实例 ID；保存铭刻编号、行为、基础 X、倍率、固定参数、成长值和本场充能值。牌序保存主副效果及其独立参数。区分配置数据和可变战斗状态，补齐复制、预览快照和跨战斗保留逻辑。
- **在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/GameTypes.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/CardEffectSpec.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/GameSetup.cs`。需要时新增到 `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/Engravings/`。
- **完成标准：** 两张同配置卡牌互不共享成长/充能；复制和预览不修改原卡；旧配置仍能启动。

### 2. 补齐牌序的治疗和状态效果

- [ ] **要干什么：** 接牌序 04/17 恢复、06/20 燃烧、07/21 掷毒、08/22 强化、09/23 撕裂、13 破甲、14 无敌。复用现有状态系统；增益给触发方，减益给对方，治疗不超过生命上限。X 与 2X 走同一行为，倍率只计算一次。
- **在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Sequences.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Effects.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/CombatStatuses.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/CombatantState.cs`。
- **完成标准：** 玩家和敌人均能触发；状态层数、护盾、伤害和治疗显示与实际结果一致。原表 09 的所属池为“增益池”，不要擅自改池。

### 3. 补齐铭刻的数值和引爆效果

- [ ] **要干什么：** 接铭刻 01 毒素、02 灼烧、18 毒爆、19 引燃；核对 03 追击触发时机。引爆读取目标现有层数，消耗后结算伤害，并走统一伤害/死亡检查。
- **在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Engravings.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Effects.cs`。
- **完成标准：** 零层引爆不产生额外伤害；层数消耗与倍率正确；玩家/敌人目标不写死。毒爆 1.5 倍的取整规则先与策划确认。

### 4. 接卡牌位置、行动顺序和持续成长

- [ ] **要干什么：** 接铭刻 04 成长、07 收尾、08 冲锋、15 谢幕、16 开幕、17 充能。记录构成牌序的卡牌及实际行动次序；跨战斗保留成长，本场充能在下一场重置。临时加成与永久升级分别保存，避免重放时重复累加。
- **在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Engravings.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Combat.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Sequences.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/GameTypes.cs`。
- **完成标准：** 每次合法触发只累计一次；预览不消耗或增长真实状态；先明确“最先/最后”是否包含敌方行动，以及成长影响本次还是后续卡效。

### 5. 改造数字匹配，支持特殊牌序构建

- [ ] **要干什么：** 接铭刻 05 变化、06 泛用、09 阻断、10 桥接、12 千变万化。数字流保留来源卡牌、所属方和候选数字；匹配结果返回具体参与卡牌。支持多个数字候选、排除阻断卡及相邻卡、跨位置匹配和后续数字随机化。
- **在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Sequences.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Effects.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/GameTypes.cs`。需要时新增 `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/Sequences/SequenceMatcher.cs`。
- **完成标准：** 场地牌序仍由最后一张参与牌所属方触发；双方护盾都给正确对象；同一匹配不重复结算。桥接出现多个匹配、泛用能否同时触发多组、随机化作用范围先确认。

### 6. 接随机卡效、传承和回环

- [ ] **要干什么：** 接铭刻 11 传承、13 轮盘赌、14 独木成林；牌序 12 混乱、15 回环。保留本回合弃牌记录和匹配参与卡，重复执行复用统一卡效入口。轮盘赌使用可恢复随机数，按 50% 不触发/50% 翻倍结算。
- **在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Engravings.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Sequences.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Cards.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Combat.cs`。
- **完成标准：** 重复触发、传承和回环有明确连锁上限。先确认重放是否再次触发铭刻/牌序、弃牌不足三张怎么处理、传承持续多久、混乱从哪个池抽取。

### 7. 接随机数字与背水

- [ ] **要干什么：** 接牌序 10 重构、11 背水。重构按 X 次插入合法随机数字，保留数字所属方；背水按确认后的血量基准算目标生命，再以实际减少血量的两倍结算伤害。
- **在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Sequences.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Effects.cs`。
- **完成标准：** 插数连锁不会无限运行。先确认背水百分比基于当前还是最大生命、取整方式、当前生命已低于目标时的处理，以及减血是否受护盾/招架影响。

### 8. 接效果池、权重和 X 抽取

- [ ] **要干什么：** 按所属池筛选启用且已实现的行，再按正权重抽取；按基础范围生成 X，应用倍率一次。分别明确角色/敌人牌序、奖励、商店和铭刻的抽取入口。区分生成时抽取和触发时随机，使用统一可恢复随机状态。
- **在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/GameSetup.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Cards.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Progression.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/ContainmentGame.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Systems/GameSession.cs`。
- **完成标准：** 空池有明确处理；禁用/待实现行不进入运行时候选池；同一随机状态重放结果一致。`GetPool` 仅查询，尚未实现权重抽取；配置绑定目前使用固定 X。

### 9. 补存档、预览和表现事件

- [ ] **要干什么：** 存档保存实例 ID、效果编号、参数、成长和随机状态；战斗快照额外复制充能、弃牌记录、数字来源和连锁计数。结算输出触发方、目标、效果编号、实际伤害/治疗/层数以及连锁次数，供 UI 和动效消费。
- **在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/ContainmentGame.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Systems/GameSession.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/GameTypes.cs`。需要时新增到 `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/Save/` 和 `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/Battle/BattleEvent.cs`。
- **完成标准：** 多次预览不改变实际随机状态；继续游戏保留成长；UI 能按实际伤害和连锁强度播放动效。UI 和动效脚本由你接。

### 10. 每批效果完成后联调并开放配表绑定

- [ ] **要干什么：** 补规则检查，列出本批已支持的行为及转换接口，交给工具方更新适配器和可用状态。逐条验证编号、X、2X、固定值、归属方、连锁和预览一致性，再在 Unity 编辑器绑定配置实测。
- **核心在哪里改：** `F:/JuGuangDeng/Assets/RainbowLi/Verification~/RuleChecks.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Verification~/TableEffectChecks.cs`。需要时新增测试到 `F:/JuGuangDeng/Assets/RainbowLi/Tests/EditMode/`。
- **工具方接入位置：** `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Data/EngravingEffectTable.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Data/SequenceEffectTable.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Data/EngravingDefinition.cs`、`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Data/SequenceDefinition.cs`。根据运行时接口更新 `IsRuntimeSupported` 和转换函数；必要时同步 `CardDefinition.cs` 与 `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Systems/EngravingModule.cs`。
- **完成标准：** 可用行能真正运行；待实现行仍拒绝绑定。仅修改支持标识或映射为普通伤害，不算完成。

## 双方先约定的转换接口

核心方先提供普通 C# 运行时效果类型、参数约束和创建方式，工具方再实现 ScriptableObject 到运行时对象的转换。目前 `EngravingEffectTable.ResolveDamage` 只返回整数伤害；`SequenceEffectTable.CreateEffect` 只能转换四种旧行为，不能承载全部新效果。新增接口名称由双方确定，本清单不把计划接口写成现有 API。

联调入口：Unity 菜单 `RainbowLi → 配表 → 铭刻效果编辑器` / `牌序效果编辑器`。源资产为 `F:/JuGuangDeng/Assets/RainbowLi/Data/DefaultEngravingEffects.asset` 和 `F:/JuGuangDeng/Assets/RainbowLi/Data/DefaultSequenceEffects.asset`；用法见 `F:/JuGuangDeng/Assets/RainbowLi/EFFECT_TABLE_EDITOR_GUIDE.md`。

## 完整原表与程序标识

以下具体效果原文来自用户 Excel。数字条件没有被额外改写。

### 铭刻效果

- **铭刻效果_01 毒素X** · `Poison` · 待实现：施加X层中毒
- **铭刻效果_02 灼烧X** · `Burn` · 待实现：施加X层烧伤
- **铭刻效果_03 追击X** · `FollowUpDamage` · 已接现有牌序触发后追加伤害入口，触发时机待核对：造成X点伤害。
- **铭刻效果_04 成长X** · `Growth` · 待实现：每当此卡牌触发牌序效果时，卡牌效果+X。（跨战斗继承）
- **铭刻效果_05 变化X** · `ExtraNumber` · 待实现：此卡被同时视作数字X。
- **铭刻效果_06 泛用** · `Wildcard` · 待实现：此卡被视作任意数字。
- **铭刻效果_07 收尾** · `SequenceTailBonus` · 待实现：此卡位于卡序“后”时，卡牌效果+3。
- **铭刻效果_08 冲锋** · `SequenceHeadBonus` · 待实现：此卡位于卡序“前”时，卡牌效果+3。
- **铭刻效果_09 阻断** · `BlockSequence` · 待实现：此卡与其前后卡牌无法参与任何卡序的构建。
- **铭刻效果_10 桥接** · `Bridge` · 待实现：此卡可跨越卡牌顺序，与此之后的任意卡牌实现卡序。
- **铭刻效果_11 传承** · `Inherit` · 待实现：此卡的卡牌效果触发后，会将其效果传承至下一张卡牌。
- **铭刻效果_12 千变万化** · `RandomizeFollowing` · 待实现：此卡后的所有数字为随机数字。
- **铭刻效果_13 轮盘赌** · `Roulette` · 待实现：此卡触发卡效时，50%概率不触发，50%概率效果翻倍。
- **铭刻效果_14 独木成林** · `OnlyPlayerCard` · 待实现：场上除开敌人卡牌外，只存在此卡一张卡牌时，触发效果。随机触发本回合弃置的任意三张卡牌的效果，并使其卡牌效果获得+2。
- **铭刻效果_15 谢幕** · `LastActionBonus` · 待实现：此卡最后行动时触发，此卡的卡牌效果+2。
- **铭刻效果_16 开幕** · `FirstActionBonus` · 待实现：此卡最先行动时触发，此卡的卡牌效果+2。
- **铭刻效果_17 充能** · `Charge` · 待实现：本场战斗中，每打出一次此卡时，此卡卡牌效果+1。
- **铭刻效果_18 毒爆** · `PoisonExplosion` · 待实现：消耗所有中毒，造成1.5倍消耗层数的伤害。
- **铭刻效果_19 引燃** · `BurnExplosion` · 待实现：消耗所有烧伤，造成2倍消耗层数的伤害。

### 牌序效果

- **牌序效果_01 伤害X** · `Damage` · 已接现有结算：造成X点伤害。
- **牌序效果_02 填充X** · `InsertNumber` · 已接现有结算：在牌序最后填充数字X。
- **牌序效果_03 护盾X** · `Shield` · 已接现有结算：获得X点护盾。
- **牌序效果_04 恢复X** · `Heal` · 待实现：恢复X点血量。
- **牌序效果_05 增幅X** · `Boost` · 已接现有结算：后续的卡牌效果获得+X。
- **牌序效果_06 燃烧X** · `Burn` · 待实现：施加X层烧伤。
- **牌序效果_07 掷毒X** · `Poison` · 待实现：施加X层中毒。
- **牌序效果_08 强化X** · `Strength` · 待实现：获得X层强壮。
- **牌序效果_09 撕裂X** · `Bleed` · 待实现：施加X层流血。
- **牌序效果_10 重构X** · `RandomNumbers` · 待实现：添加X个随机数字。
- **牌序效果_11 背水X** · `LastStand` · 待实现：减少血量至X%，造成2倍减少血量的伤害。
- **牌序效果_12 混乱X** · `RandomCardEffects` · 待实现：触发X次随机卡牌效果。
- **牌序效果_13 破甲X** · `Vulnerable` · 待实现：施加X层易伤。
- **牌序效果_14 无敌X** · `Parry` · 待实现：获得X层招架。
- **牌序效果_15 回环X** · `RepeatSequenceCards` · 待实现：重复触发X次构成此牌序的卡牌效果。
- **牌序效果_16 伤害2X** · `Damage` · 已接现有结算：造成2X点伤害。
- **牌序效果_17 恢复2X** · `Heal` · 待实现：恢复2X点血量。
- **牌序效果_18 护盾2X** · `Shield` · 已接现有结算：获得2X点护盾。
- **牌序效果_19 增幅2X** · `Boost` · 已接现有结算：后续的卡牌效果获得+2X。
- **牌序效果_20 燃烧2X** · `Burn` · 待实现：施加2X层烧伤。
- **牌序效果_21 掷毒2X** · `Poison` · 待实现：施加2X层中毒。
- **牌序效果_22 强化2X** · `Strength` · 待实现：获得2X层强壮。
- **牌序效果_23 撕裂2X** · `Bleed` · 待实现：施加2X层流血。

