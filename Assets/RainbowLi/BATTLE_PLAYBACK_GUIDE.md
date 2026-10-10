# 战斗动效与播放控制

对应《界面与工具任务清单》第 2 项。工程 F:/JuGuangDeng，所有改动在 Assets/RainbowLi。

## 游戏里怎么用

进入战斗并点击「开始执行」，行动安排区上方显示播放控制。

- 暂停 / 继续：一起暂停或恢复结算、飞牌、命中粒子、飘字、震屏和战斗音效；不会停止 UI 点击，也不改全局 Time.timeScale。
- 单步：暂停后点击，先播放完尚未完成的飞牌，再调用一次真实 ResolveOne，播放完这一个结算事件的命中反馈后停住。重复点击不会叠加请求。牌序、重复效果、状态结算分别算一个事件。
- 1× / 2× / 4× 倍速：即时改变整个表现时钟，正在飞行的牌也会变速。抽牌、出牌、命中、粒子、结算间隔统一变速。连锁自动加速与手动倍速相乘。
- 跳过本回合：清理正在播放的表现，按原玩法逐次结算到本回合结束、死亡或奖励阶段。每帧最多处理 32 次，避免长连锁占满一帧；不会跳过后面的回合，也不会自动选奖励。
- 重开：取消单步和跳过任务，清理飞牌、临时隐藏牌面、命中粒子、飘字、震屏、音效和连锁计数；保留当前手动倍速和自动播放选择。

筹划阶段即使自动播放关闭，抽牌和安排行动的飞行仍正常播放。最后一击要等飞牌、命中反馈和弃牌表现完成后显示奖励或失败面板；可用跳过直接显示结果。

## 策划在哪里调

菜单「RainbowLi → 动效 → 打开默认动效配置」。资源：Data/DefaultBattlePresentation.asset。继续使用已有中文 Inspector 调飞行、蓄力、命中、伤害强度曲线、连锁、震屏、粒子、音效和普通/牌序行动间隔，详见 PRESENTATION_TUNING_GUIDE.md。

配置时长都是 1× 的秒数，不要为了玩家倍速直接改资源。运行中修改资源会保留。策划预览有独立的临时播放许可，暂停时也能预览；完成后回到暂停，不产生真实结算。

## 程序 / 场景在哪里改

| 位置（均在 Assets/RainbowLi 下） | 负责的事情 |
|---|---|
| Scripts/Unity/Systems/BattleModule.cs | 自动播放、单步、跳过、真实 ResolveOne 调度、倍速与连锁、重开状态 |
| Scripts/Unity/Effects/BattlePlaybackClock.cs | 共享表现时钟，每帧只推进一次；只影响表现，不改全局时间 |
| Scripts/Unity/Effects/CardMotionController.cs | 抽牌/出牌/弃牌、攻击/治疗/护盾/状态飞行与命中、血条命中时更新 |
| Scripts/Unity/Effects/BattleEffects.cs | 命中飘字、粒子、震屏、闪光、连锁和音效；沿用同一时钟 |
| Scripts/Unity/Views/BattlePlaybackView.cs | 四个按钮的可点击状态、暂停/单步/跳过状态文字、倍速显示 |
| Scripts/Unity/Views/BattleHudView.cs | 设置面板的倍速按钮，共用 BattleModule.CycleSpeed |
| Editor/BattlePlaybackSceneMigration.cs | 将播放控制接到原生场景；重复运行不会重复创建 |
| Scenes/RainbowLiGame.unity | GameView / Table / BattlePanel / PlaybackControls，可在 Unity 编辑位置、尺寸、颜色和字体 |

默认场景已经绑定。其他 RainbowLi 场景可以在编辑模式运行「RainbowLi → UI → 接入战斗播放控制」，然后保存场景。

## 另一位程序怎么接

继续使用 GameSession.ResolveOne、Changed、Resolved 和现有 ResolutionEvent，事件字段和玩法文件未修改。正常游戏走 BattleModule.Confirm / Step / ToggleAutoPlay / Skip；不要在 BattleModule 之外开另一个自动 ResolveOne 循环，否则会绕过动画顺序和暂停。

GameSession.ResolveOne 保留先更新游戏状态并 Notify，再发 Resolved 的顺序。表现层先记录生命/护盾变化，在命中时更新显示。Skip 保留真实事件，其他系统的事件监听仍正常收到。

单步接口现在是异步请求：调用返回后等 IsStepping 变为 false，才表示这一事件及其表现已经结束；不能在调用后的同一行假设伤害已结算。HasPresentation 表示当前飞牌/命中表现仍在运行；IsPresentationPaused 和 IsSkipping 可给外部界面读取。

存档、主菜单、设置持久化、整局构建属于后续分工项，本项不新增这些流程。

## 验证

Verification~/BattlePlaybackChecks.cs 和 battle-playback.log：原生按钮引用、迁移幂等性、暂停、单步一次、2×/4×、飞牌和震屏冻结、跳过与普通结算结果一致、事件数量不减少、策划预览、死亡/奖励表现结束、重开取消等 50 项 Unity 运行验证通过；双效果牌序产生超过 200 次真实结算，跳过结果及事件数量相同，每帧不超过 32 次。截图 Verification~/PlaybackPreview/01-paused.png。
