# 统一结算与事件接口（2026-10-10）

本批完成任务清单第 2 项的核心结算、状态生命周期和事件接入。沿用已有的 26 项卡效与旧牌序/铭刻，不新增尚待交接的新效果行为。场景、Prefab、配表和动效脚本未修改。

## 结算入口

- `Domain/Battle/CombatResolver.cs`：唯一的伤害、直接减血、治疗和护盾数值算法。`CombatantState` 原公开方法保持兼容，内部委托给该算法。
- `Domain/ContainmentGame.Settlement.cs`：玩法层的统一入口，负责数值变更、事件记录、状态增减/清除、回合重置和死亡检查。后续新增效果应使用这里的 `SettleDamage`、`SettleHealthLoss`、`SettleHealing`、`SettleShield`、`SettlePower`、`ApplyStatus` 和 `ChangeStatus`，不要绕过它直接改生命或状态。
- 卡牌、复合效果、牌序、铭刻、流血、中毒、烧伤、再生、壁垒、点燃/淬毒、数字状态消耗、回合清除、休息和事件治疗已接入。
- 伤害顺序保持为力量 → 弱化 → 易伤 → 坚固 → 限伤 → 招架 → 护盾 → 生命。中毒/烧伤不使用持有者的输出力量和弱化，但仍经过目标的防御规则。流血直接减少生命。
- 零伤害不会因力量/易伤变成攻击，也不会消耗招架；治疗不复活已死亡角色，过量治疗只报告实际恢复量。较大数值使用宽整数计算并限制在合法整数范围内，避免叠加溢出。
- 任一方死亡立即结束战斗，后续复合效果、数字填入和连锁取消；胜利只结算一次。导致死亡的状态仍完成自身的层数消耗记账。

## 不可变逐项事件

`BattleEvent` 不持有可变卡牌、行动或状态引用。主要字段：

- `Kind`：伤害、直接减血、治疗、护盾/强化变化、状态施加/变化/移除、招架或死亡。
- `SourceSide` / `TargetSide`：触发方和目标。状态回合结算的触发方是状态持有者；目前状态系统不追溯最初施加者。
- `Source` / `Timing`：来自卡牌、技能、牌序、铭刻、状态、回合、遭遇或服务，以及行动前后/回合开始结束等时点。
- `EffectCode`：卡效使用配表编号，铭刻使用铭刻编号；旧牌序使用名称，旧铭刻使用 `LegacyEngraving`。它不会伪造尚不存在的牌序表编号。
- `RequestedAmount`：提交给本次结算的数值（卡牌的旧 Power 加成已包含在内）。`ResolvedAmount`：伤害经过攻防修正后的数值，或该事件的实际层数/数值变化。`Amount`：实际损失/恢复的生命或变更数量，非负。
- `Before` / `After` / `Delta`：该事件对应数值的前后状态及有符号变化。状态移除后的 `Lifetime` 为 null，施加事件记录生效的持续方式。
- `Blocked` / `Parried`：实际护盾抵挡量、是否招架。招架另产生明确的 `Parry` 事件和层数消耗事件。
- `HealthBefore/After`、`ShieldBefore/After`：事件结算时的角色数值。
- `CardInstanceId`、`Round`、`Slot`、`ActionOrder`、`EffectIndex`、`IsRepeat`、`RuleTriggerCount`：卡牌身份、回合、布局位置、本回合普通结算顺序、复合卡效索引、重放标记及本次牌序触发序号。无来源卡牌时 ID 为 null；无行动时 Slot/EffectIndex 为 -1。回合开始/结束管理事件的 ActionOrder 为 0。牌序主副效果共享同一触发序号，吟唱排队的每次触发保留自己的序号。

每次成功进入 `StartBattle`、`Step` 或服务治疗操作，`ContainmentGame.LastSettlementEvents` 保存本次操作的事件；它不是整局历史。需要保留时使用 `ToArray()`。数值初始化、预测复制及兼容属性的直接赋值不产生游戏结算事件。

## Unity 接入

订阅 `GameSession.Settled` 获取每条事件：

```csharp
session.Settled += OnSettlement;
// 禁用或销毁监听组件时取消订阅。
session.Settled -= OnSettlement;
```

现有系统操作后继续调用 `session.Notify()`；它先刷新视图，再发布尚未发送的事件。同一局按事件 ID 去重，重复刷新不重发；新游戏重新建立去重范围。操作应执行后及时 Notify，避免下一次结算覆盖未发送的批次。视图刷新触发重开时，旧局事件不会继续发布。

原 `GameSession.Resolved` 与 `ResolutionEvent` 保留供当前动效消费。伤害、护盾抵挡和招架改为汇总实际事件，避免“受伤后治疗”或“先获得护盾”掩盖真实结算。新增 `PlayerHealing` / `EnemyHealing` 与 `Events`。回合切换即使 `Step()` 返回 false，也通过 `Settled` 发布事件，不伪造一次普通行动。

新治疗/状态动效可订阅 `Settled`；不要同时把旧汇总与逐项事件作为同一伤害的两个表现来源，以免重复播放。旧动效脚本本批未改。

## 预览与验证

预览使用独立游戏状态、事件批次和事件编号。牌组/手牌/行动引用和卡牌 ID 在快照中保留；真实游戏的事件、生命、状态和随机数不被预览修改。预览不调用真实 Session 的 Notify 或事件订阅。

工程根目录运行（.NET 9 SDK）：

```powershell
powershell -ExecutionPolicy Bypass -File 'Assets/RainbowLi/Verification~/RunRuntimeChecks.ps1'
powershell -ExecutionPolicy Bypass -File 'Assets/RainbowLi/Verification~/RunSessionSettlementChecks.ps1'
```

第二条从 Unity 生成的 `Assembly-CSharp.csproj` 读取实际 CoreModule 引用，只验证 Session 的托管通知与汇总逻辑，不创建原生 GameObject。临时编译工程均位于系统临时目录。

本批规则检查共 1375 项通过，其中新增 68 项统一结算/事件检查；另有 12 项托管 Session 检查通过，共 1387 项。运行时与编辑器脚本使用 Unity 6000.3.12f1 程序集引用、.NET Standard 2.1 编译通过。未进行 Unity 场景试玩或视觉验收。
