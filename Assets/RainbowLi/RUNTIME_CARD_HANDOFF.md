# 卡牌运行时基础接口（2026-10-10）

本批只修改核心规则与检查，不修改场景、Prefab、配表、配置资产和编辑器。新的铭刻/牌序表尚未同步，编号到行为的转换仍由工具方接入；本批不表示任务清单第 1 项或第 7 节已全部完成。

## 卡牌身份与状态

- `Card.InstanceId`：创建卡牌时生成的稳定字符串 ID。不依赖玩法随机数，不随抽牌、升级、铭刻或换场改变；同一模板生成的卡牌各有自己的 ID。
- `Card.CopyAsNewInstance()`：创建新的物理卡牌，生成新 ID，深复制卡效与旧配置，保留升级、铭刻及永久成长，清除本场充能。
- `Card.CopyForSnapshot()`：保留 ID 和全部状态，但深复制可变数据。预览将同一张卡在牌组、手牌、抽弃牌堆与行动队列中的引用映射为同一个快照对象。
- `Card.EngravingCode`：预留铭刻编号；旧 `Engraved` / `EngravingDamage` 行为仍有效。仅填写编号不会自动激活新铭刻。
- `Card.PermanentGrowth` / `BattleCharge`：独立的永久成长和本场充能存储。回合切换保留两者，下场战斗只清空充能。本批不自动累加或将其计入卡效；触发时机与影响范围待策划确认。

实例 ID 的存档序列化与恢复接口、铭刻固定参数/行为及成长规则尚未实现。

## 多个卡效按顺序执行

`Card.Effects` 非空时，以其中的 `CardEffectInstance` 顺序执行；为空时完全沿用旧单效果字段。每项包含：

- `Spec`：普通 C# `CardEffectSpec` 副本，指定现有 `CardEffect`、性质和持续方式。
- `BaseX`：生成时确定的整数 X；执行与预览不重新抽取。
- `Multiplier`：正整数倍率，默认 1。当前承载 X、2X；不用于尚未确认取整方式的 1.5 倍引爆。
- `UpgradeBonus`：每次升级对该项效果增加的数值，默认 0。

数值计算为基础公式结果 × 倍率 + 升级次数 × 本项升级加成。使用卡牌数字的增值伤害/减值伤害保持原公式。倍率只计算一次。成长和充能不隐式混入此公式。

复合卡牌仍算一次实际行为、一次数字填入；流血、行动后效果、往复按整张卡处理，往复会重放整组效果。两个效果之间任何一方死亡，停止剩余效果；随后沿用统一战斗结束处理。每项按自身性质决定增益/减益目标，玩家和敌方卡牌共用结算入口。

示例（配置转换方构造并复制 `Spec`，不要直接引用共享可变对象）：

```csharp
card.Effects.Add(new CardEffectInstance {
    Spec = strengthSpec.Copy(), BaseX = 2
});
card.Effects.Add(new CardEffectInstance {
    Spec = strikeSpec.Copy(), BaseX = 4, Multiplier = 2, UpgradeBonus = 1
});
```

现有 UI 的牌面类别仍读取 `Card.Effect`，转换方应将其设为第一项效果的类型。完整描述已支持显示多个效果；专门的复合效果 UI 和逐效果表现事件属于后续接入。本批没有支持新铭刻行为、牌序新行为或随机效果池。

## 验证入口

安装 .NET 9 SDK 后，在工程根目录运行：

```powershell
powershell -ExecutionPolicy Bypass -File 'Assets/RainbowLi/Verification~/RunRuntimeChecks.ps1'
```

临时编译工程位于系统临时目录，不被 Unity 导入。检查包括现有规则、角色内容、26 项卡效及新增实例/复合效果检查。旧 `RuleChecks` 中两处手工安排已修正为保留系统生成的全部敌方行动，符合现有确认约束；没有放宽核心规则。

本批验证：88 项玩法规则、716 项角色内容、472 项表格卡效、31 项实例/复合效果断言全部通过，共 1307 项。另使用 Unity 6000.3.12f1 的程序集引用及 .NET Standard 2.1 编译 RainbowLi 的运行时和编辑器脚本，0 错误；现有编辑器代码有一项过时 API 警告。本批未进行 Unity 场景运行和视觉验收。
