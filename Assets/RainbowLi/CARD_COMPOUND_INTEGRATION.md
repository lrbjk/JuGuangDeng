# 第3项交接：卡牌复合配置读取接口

配置工具方已交付主表 + 追加效果。玩法方拥有 GameTypes.cs、CardEffectSpec.cs、GameSession.cs 和 ContainmentGame；本批未修改这些文件。单效果游戏继续工作，复合结算仍需玩法方接入，不能把“配置可保存”当作“效果已经执行”。

## 现在可以调用的接口

全部路径在 F:/JuGuangDeng/Assets/RainbowLi 下。

- Scripts/Unity/Data/CardTableDefinition.cs：GetEffects(string id)，按卡牌编号返回 CardEffectSpec[]。
- Scripts/Unity/Data/CardTableRow.cs：EffectSpecs()，按主效果 → 启用追加效果的列表顺序返回快照。
- Scripts/Unity/Data/CardDefinition.cs：GetEffectSpecs()，单独卡牌也可以用 cardTable + rowId 读取同一配置。
- 每次读取返回新的数组和效果对象；修改运行时 X、状态或文本不能改原配置资产。关闭的追加效果不返回，但保留在配表中。
- 主效果 Code 为原 id；追加效果为 id/2、id/3 等原序号。Name 为卡牌名，Text/Effect/Lifetime/Polarity/UsesNumber/Min/Max/Weight 各条独立。序号中可能出现被禁用项留下的空位，这是稳定标识，不是执行次数。
- 调用方仍应按 row.enabled 选择进入卡池的行；GetEffects 是查询接口，不自动应用启用、权重或敌方触发条件。

示例：Data/Examples/CompoundPlayerCards.asset 的 demo_player 返回 Strike → Guard；CompoundEnemyCards.asset 的 demo_enemy 返回 Strike → Burn。两个示例都未绑定默认角色。

## 玩法方要做什么、在哪里改

1. 在 Scripts/Domain/CardEffectSpec.cs / GameTypes.cs 约定运行时的多效果载体和各条已确定的 X 值。保留旧单效果字段的兼容性，Copy/预测/存档都要深拷贝追加效果。把约定好的字段告知配置方，再由配置方改 Unity/Data 中的 Spec/Apply/CreateCard 装配入口。
2. 在 ContainmentGame 的卡牌生成及敌方意图生成逻辑中，对每个效果各抽一次 X；同一次战斗的预览和真实结算共用这组值，不在播放时再抽。
3. 在 ContainmentGame 的卡牌结算逻辑按数组顺序执行。每条对象独立依据 CardEffectTargeting.TargetsOpponent：玩家 Strike 打敌人后 Guard 给玩家；敌人 Strike 打玩家后 Burn 也给玩家。Polarity 只作分类。
4. 约定复合卡按一次卡牌行动计数、填入一次数字、触发一次行动后机制；逐条效果执行不重复填数或把点燃/淬毒等行动后机制重复触发。致死后是否继续后续自身效果需由策划确认并写入规则；本接口不自行决定。
5. 在 GameSession 的事件出口补齐逐条效果的表示，并与界面方确认。每个反馈需能知道出牌者、实际效果、效果对象、数值/伤害/格挡、源卡与时间线位置。保持 SourceAction 指向原卡，避免追加效果找不到飞行起点；不能只报告主效果再让 UI 猜目标。
6. 把多效果接口同时接到真实结算、Forecast、敌人特殊技能、状态触发及重开流程，并验证玩家和敌人、致死、护盾、状态叠加的一致性。

## 配置方下一步接线的位置

核心接口确定后，由配置方修改 Scripts/Unity/Data/CardTableRow.cs 的 Spec、PlayerCardTable.Apply、EnemyCardTable.Apply 和 CardDefinition.CreateCard，将 GetEffects 返回的数据装成核心载体；同时接 CardMotionController/BattleEffects 的逐条事件反馈。这些文件仍按原分工由界面与工具方维护。

当前 Spec / Apply / CreateCard 在遇到启用的追加效果时明确拒绝单效果执行，不会截取第一条。Player/Enemy Apply 在修改 GameSetup 的效果池之前检查这点，不会留下已清空的半份配置。该保护应等真实多效果载体和结算、预测、事件都接好后再替换。

## Excel 与编辑器

Sheet1 仍是原 7 列 + 原 16 列扩展；复合效果另表按卡牌编号和执行序号关联。旧文件没有复合效果表时保留现有配置；有空的复合效果表时清空导入卡牌的追加效果。GUI 导入始终先预览，删除行默认保留。预览后资产发生变化必须重新预览。自动化脚本可以使用 Preview / PreviewFile 构建计划再 Apply；旧 Import(cells, replace:true) 明确执行整表替换，属于兼容脚本接口，不经过人工预览。

验证位置：Verification~/CardToolsChecks.cs、card-tools.log。82 项通过；这是配置和普通游戏接入验证，不是复合玩法完成证明。
