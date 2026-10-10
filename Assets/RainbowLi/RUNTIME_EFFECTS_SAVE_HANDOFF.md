# 配表、牌序、铭刻与存档接入记录

2026-10-10。本文所有路径相对于项目根目录，交接文件中的其他盘符不影响本项目。

## 已实现的核心接口

- 铭刻 19 种行为、牌序 23 行（15 种行为）进入运行时转换。保留编号、基础 X、倍率、固定参数；X/2X 使用同一行为，倍率只计算一次。
- `RuntimeEngraving`、`SequenceEffect`、`SequenceRule` 是普通 C# 运行时类型。主副牌序效果独立保存参数。
- `SequenceEffectTable.CreateEffect(id,x)` 和 `EngravingEffectTable.CreateEffect(id,x)` 返回完整效果。`ResolveDamage` 仅兼容追击伤害，其他铭刻应使用 `CreateEffect`。
- `CardDefinition.orderedEffects` 支持有序复合卡效；非空时优先于旧单效果配置。复制时深拷贝，物理复制生成新 ID，预览和存档保持 ID。
- `GameDefinition.sequenceEffects`、`engravingEffects` 接入效果池；默认 `DefaultGame.asset` 已绑定项目现有两张表，原表行、所属池和权重未修改。
- `DrawSequenceEffect(pool)` / `DrawEngraving(pool)` 过滤启用、有限正权重、匹配所属池的已支持行，再抽行及 X。空命名池抛出明确异常。奖励获得牌序效果、商店购买牌序效果、稀有铭刻和商店铭刻分别使用配置的池名。
- `randomPlayerSequenceEffects` / `randomEnemySequenceEffects` 可选择生成角色/敌人牌序的主效果；默认保留原配置行为。配置指定 X 时保留该值，X 为 0 时在生成阶段从范围抽取，副效果独立抽取。生成后不会每次触发重新抽 X。
- 奖励数量、卡牌/升级/铭刻/牌序价格、休息恢复量在 `GameDefinition` 配置。敌人与关卡继续消费已有定义；事件奖励继续使用 `LevelDefinition` 的金币/治疗配置。

## 已确认的玩法约定

用户已确认：毒爆 1.5 倍向下取整；最先/最后包括敌方行动；成长和充能从下一次卡效生效；桥接和泛用可触发不同牌序，每组每个数字位置只结算一次；千变万化影响本回合后续数字；传承只到下一张己方卡；重放仅执行卡效，不再填数或触发铭刻；弃牌不足三张取全部；混乱从启用卡效池抽取；背水基于最大生命、向下取整、已低于目标不减血、减血绕过护盾和招架。

实现细节：

- 数字流同时保存显示数字、候选数字、所属方、来源行动和阻断标记。匹配返回具体参与卡牌。桥接可跳过之后的位置；每组牌序在一个新数字位置只选择一个匹配，多个规则分别触发。
- 场地牌序归最后一个参与数字所属方。玩家/敌人牌序保持原来的所属方、场地→玩家→敌人优先级和每回合每组 99 次上限。
- 收尾/冲锋在实际匹配成立后，为相应参与卡补结算固定 +3；同一张卡每回合只补一次，不重放整个卡效、不填数、不重复施加强化数值。它属于匹配后的补结算事件。
- 数值铭刻在卡牌行为后的填数阶段触发；追击兼容旧规则，只在这张牌完成牌序时触发。成长按合法匹配累计，吟唱的重复效果不重复成长。充能只计真实打出，不计往复或效果重放。
- 确认回合时，将未上场手牌登记为本回合弃牌；独木成林从这些牌中无放回抽最多三张，重放时各获得本次 +2。卡牌仍在正常回合结束时进入弃牌堆。
- 传承保留原卡效参数和当时加成，在下一张己方真实卡行动前执行一次。回合结束清空传承。重放不触发流血行动税、往复、插数、铭刻及牌序。
- 原有往复仍按原规则重复行为并填数；共享 `BattleActionQueue` 限制所有队列插入，本回合最多 4096 个行动，避免其他插入入口绕过上限。

## 保存与继续游戏

- `ContainmentGame.CaptureSave()` / `RestoreSave(data,setup)` 使用版本化 `RunSave`，保存地图节点、进度、牌组/手牌/抽弃牌堆/奖励、实例 ID、效果参数、成长、充能、数字来源、参与卡牌、行动队列、状态寿命、敌人解锁状态和随机状态。
- 完整快照复用同一捕获/恢复路径，恢复卡牌与行动引用关系。多次预览不消耗实际随机数。
- `GameSession.SaveGame()` / `ContinueGame()` 提供 Unity 接口；`LevelModule.Save()` / `Continue()` 可供按钮绑定。
- 存档位置为 `Application.persistentDataPath/RainbowLi.save.json`。写入临时文件后替换正式文件，已有存档保留 `.bak`。读取失败保留当前游戏。
- 版本 0/1 按本 DTO 的兼容字段迁移（补缺失实例 ID、默认效果倍率）；未来版本拒绝加载。项目此前没有可迁移的已发布存档格式，不声称能迁移任意外部 JSON。
- 当前版本显式保存可空配置标记，防止 Unity JSON 将空配置对象还原成默认对象改变行为。

## 程序集与验证

纯规则程序集位于 `Scripts/Domain/RainbowLi.Domain.asmdef`；`ContainmentGame.cs` 连同原 `.meta` 从 `Scripts` 根移至 `Scripts/Domain`。旧 GUI 原型仍留在根目录。Unity 运行时和编辑器分别为 `RainbowLi.Runtime` / `RainbowLi.Editor`；EditMode 测试为 `RainbowLi.Tests`。

- `Verification~/RunRuntimeChecks.ps1`：原有规则断言和新增效果、随机数、存档延续检查。
- `Verification~/RunSessionSettlementChecks.ps1`：托管会话事件转发验证。
- `Verification~/RunNativeCoreChecks.ps1 -UnityEditor <Unity.exe>`：创建隔离临时工程，使用真实 Unity 加载默认配置，检查 42 行适配器、JSON 和保存后的执行一致性，不修改正在打开的场景。
- `Tests/EditMode/RuntimeEditModeTests.cs`：Unity Test Runner 的新增规则与 JSON 测试入口。

原生核心验证不包含原场景的按钮和动效播放。完整隔离场景工程启动遇到包管理器 `path argument ... undefined` 错误，改用无包的核心隔离工程；界面与编辑器脚本另外使用现有 Unity 引用编译检查。

本次验证结果：1623 项运行时断言（其中 248 项新增效果/存档检查）和 12 项托管会话断言通过；原生 Unity 核心验证通过；现有 Unity 引用的脚本编译 0 错误、2 条既有编辑器 API 过时警告。新增牌序的显示目标也已接入与结算共用的目标规则。

## 合作者剩余联调

场景和 Prefabs 继续由界面方维护。需要绑定保存/继续按钮，并按 `Settled` 的效果编号、目标、实际数值、行动次序和牌序触发次数补齐新效果表现。旧 `Resolved` 接口保持兼容。清单第 7 节第 10 项的原场景视觉联调仍待完成，不能仅凭适配器支持标记勾选。
