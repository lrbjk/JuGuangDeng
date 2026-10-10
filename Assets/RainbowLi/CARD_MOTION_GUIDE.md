# 卡牌飞行与战斗效果动效

工程位置：`F:/JuGuangDeng/Assets/RainbowLi/`。

## 现在能看到什么

- 进入战斗：卡牌从抽牌堆依次飞到手牌区，途中翻面；敌方行动牌从技能组飞入行动安排区。
- 放置手牌：卡牌沿弧线飞到对应行动位置，已放置的手牌隐藏。
- 调整/撤回：行动牌平滑移位；撤回的牌飞回原手牌位置。
- 回合结束：本回合手牌从手牌区或行动区飞入弃牌堆，再发下一回合手牌；需要洗牌时先表现弃牌堆回到抽牌堆。
- 行动结算：卡牌亮起、蓄力、向目标前冲，释放带拖尾的能量弹；命中时播放冲击环、原有爆点、伤害数字、震屏和声音。
- 护盾/治疗/状态牌：蓝色护盾、绿色治疗、紫色状态效果分别飞向自身或对方；插数效果指向场地区域。
- 高伤害和连续效果会增强拖尾、弹体、冲击环和原有爆点。血条、护盾和增伤数值在命中时更新。
- 最后一击播放完成并收牌后，再显示奖励或胜败界面。

## 在 Unity 哪里调整

默认场景已经统一绑定 `Data/DefaultBattlePresentation.asset`。策划请使用菜单 **RainbowLi → 动效 → 打开默认动效配置**，在中文 Inspector 调整并使用运行时预览。完整操作见 [策划动效调参](PRESENTATION_TUNING_GUIDE.md)。以下组件旧调参字段仅适用于未绑定 Profile 的兼容场景；绑定后它们显示为只读，场景引用仍然可以调整。

打开 `F:/JuGuangDeng/Assets/RainbowLi/Scenes/RainbowLiGame.unity`，在 Hierarchy 搜索组件 `CardMotionController`，选择挂有该组件的 UI 根对象。

| Inspector 字段 | 调整内容 |
| --- | --- |
| Draw Duration / Deal Stagger | 发牌飞行时长 / 逐张间隔 |
| Place Duration | 放置、移位、撤回时长 |
| Discard Duration | 弃牌飞行时长 |
| Flight Arc | 飞行弧线高度 |
| Charge Duration / Projectile Duration / Impact Duration | 蓄力 / 弹体飞行 / 命中停留 |
| Attack Lunge | 卡牌朝目标前冲的距离比例 |
| Max Chain Gain | 连续效果的最大表现倍率 |
| Attack / Shield / Heal / Status Color | 攻击、护盾、治疗、状态效果颜色 |
| Player Target / Enemy Target / Rule Origin | 玩家、敌人和场地的效果落点，可拖入其他 RectTransform |
| Draw Pile / Discard Pile | 发牌和收牌的起终点 |
| Enable Motion | 开关本次新增动效；关闭后使用即时展示 |

战斗设置里的播放速度会同时缩短飞行动画和结算等待。暂停会冻结当前飞牌、命中反馈和结算；点击“单步”播放完一个结算事件再停住。“跳过本回合”保留真实规则与事件，只省略表现。完整控制说明见 BATTLE_PLAYBACK_GUIDE.md。

长连锁现在自动加速：前6次追加效果维持原速，第7次开始每次增加0.25倍，最多6倍；与设置中的手动倍速相乘。牌序追加效果、往复和追加数字都计入，双方交替触发也能加速。新普通卡牌行动、回合结束或新一局恢复正常。蓄力、弹体、命中等待和结算间隔同步缩短，实际数值及触发次数不变。

在统一动效配置的“播放节奏与长连锁加速”中调整加速开关、保持原速次数、每次增速和最高倍率。未绑定 Profile 的旧场景才使用 BattleModule 中的兼容参数。

旧场景需要接入时：打开 RainbowLi 内的场景，使用菜单 **RainbowLi → 动效 → 接入卡牌飞行到当前场景**，检查目标引用并保存场景。已接入的场景重复执行不会增加重复组件。

## 修改脚本的位置与分工

- 动画轨迹、刷新差分、目标选择、命中时数值展示：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Effects/CardMotionController.cs`。
- 原生 UI 冲击环网格：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Effects/DuelRingGraphic.cs`。
- 原有伤害爆点、震屏、声音、连锁：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Effects/BattleEffects.cs`。现在由飞行控制器在命中时调用 `PlayResolution`；动效关闭时走原事件入口。
- 界面刷新入口：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Views/GameView.cs`。刷新前调用 `PrepareRefresh`，布局完成后调用 `Observe`。
- 卡牌副本禁用点击与悬停：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Views/CardView.cs` 的 `SetPresentationOnly`。
- 自动播放等待：`F:/JuGuangDeng/Assets/RainbowLi/Scripts/Unity/Systems/BattleModule.cs` 的 `presentation` 引用和 `HasPresentation` 判断（包含飞牌和命中反馈）。另一位程序调整结算播放入口时保留这项等待。
- 场景绑定菜单：`F:/JuGuangDeng/Assets/RainbowLi/Editor/CardMotionSceneMigration.cs`。

飞行使用独立卡牌副本，不改变牌组、手牌、弃牌堆或牌序规则。新效果接入时，核心方在 `F:/JuGuangDeng/Assets/RainbowLi/Scripts/Domain/CardEffectSpec.cs` 的 `CardEffectTargeting` 中定义对象，界面方在 `EffectColor`、`Caption` 中补齐颜色和名称。结算与动效共用目标规则；正负面分类不能反转行为对象。当前通过已有 `Changed` / `Resolved` 事件接入。中毒、流血在受影响者身上结算，并显示伤害来源；流血致死会中断卡牌攻击表现。

参考方向：[《游戏王：大师决斗》官方展示](https://www.konami.com/yugioh/masterduel/us/en/)；本次实现卡牌飞行、翻面、蓄力、目标弹体与命中节奏，场景中的立绘和特效资源可继续替换。

## 验证

原飞牌版本的隔离 Unity 场景通过 874 项检查，日志保留作为历史记录；当前播放控制改为异步单步接口，以 Verification~/BattlePlaybackChecks.cs / battle-playback.log 的 50 项检查为准。旧测试假定 Step 同步返回伤害，需按 IsStepping 等待接口迁移后才能重跑。动效检查脚本：`F:/JuGuangDeng/Assets/RainbowLi/Verification~/CardMotionChecks.cs`。覆盖场景引用、目标选择、真实发牌/放置/撤回、播放等待、命中数值、回合切换、新局清理、开关回退和致命攻击转场。日志在 `F:/JuGuangDeng/Assets/RainbowLi/Verification~/card-motion-final.log`；截图在同目录的 `CardMotionPreview/`。
