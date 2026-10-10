# 玩家和敌人卡牌作用对象修复

## 修复内容

原先状态行为优先读取配表的正负面分类来决定对象。复制行后只修改“结算效果”，或创建 CardEffectSpec 时保留默认 Positive，都可能让中毒给自己、力量给对手。

现在目标由实际行为决定，结算与动效共用 `CardEffectTargeting`：

| 行为 | 玩家出牌 | 敌人出牌 |
| --- | --- | --- |
| 伤害、增值伤害、减值伤害 | 敌人 | 玩家 |
| 中毒、烧伤、流血、易伤、弱化、无序 | 敌人 | 玩家 |
| 防御、恢复、力量、再生、坚固、铁壁、招架、壁垒、限伤 | 玩家 | 敌人 |
| 随机数字、吟唱、往复、点燃、淬毒、变动、加值、减值 | 玩家 | 敌人 |

点燃、淬毒先增加出牌者的增益，再在行动后向对手施加状态。已有流血、中毒仍会伤害受影响者，伤害标签显示“流血”或“中毒”。流血致死时，卡牌行动中断，播放状态反应而非朝对手攻击。

敌人飞行卡牌也保留实际效果标识；从技能组定位来源时同时匹配数字与效果，避免同数字的不同技能串用视图。

## 在哪里改

- 核心共用目标规则：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/CardEffectSpec.cs` 的 `CardEffectTargeting`。
- 状态结算消费规则：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/ContainmentGame.Effects.cs`。
- 弹体落点与流血行动中断：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Effects/CardMotionController.cs`。
- 流血/中毒伤害来源标签：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Effects/BattleEffects.cs`。
- 卡牌绑定的实际效果标识：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Views/CardView.cs` 的 `BoundEffect`。
- 配表目标说明：`F:/JuGuangDeng/Assets/RainbowLi/Editor/CardTableEditorWindow.cs`。

新增效果时由核心程序扩展共用目标规则，再由界面方补表现。继续使用现有 Excel 列与配置资产；修改正负面分类只改变分类，不反转对象。

Unity 等待重新编译后重新开始游戏即可使用修复。场景、玩家表和敌人表的资产绑定无需调整。

## 验证材料

修复后通过 1154 项检查：634 项双方目标矩阵、472 项原卡牌效果检查，以及 11 个实际场景中的 48 项结算/动效检查。原生脚本编译通过。

- `F:/JuGuangDeng/Assets/RainbowLi/Verification~/EffectTargetChecks.cs`：双方 26 种行为、无配置及三种正负面分类的目标矩阵。
- `F:/JuGuangDeng/Assets/RainbowLi/Verification~/EffectTargetPlayChecks.cs`：真实结算和动画落点、护盾/治疗、流血致死、状态伤害标签。
- `F:/JuGuangDeng/Assets/RainbowLi/Verification~/effect-target-final.log`：最终检查输出。
