# 策划动效调参

项目 F:/JuGuangDeng。只在 Assets/RainbowLi 内维护动效资源。

## 打开与调整

1. Unity 菜单 **RainbowLi → 动效 → 打开默认动效配置**。
2. 选中的资源是 **Assets/RainbowLi/Data/DefaultBattlePresentation.asset**，在中文 Inspector 调整参数。
3. 进入播放模式并进入战斗，在配置 Inspector 底部点击预览抽牌、出牌或攻击。攻击可指定预览伤害、连锁次数及命中玩家/敌人。预览自动暂停播放；它不扣血、不出牌、不增加真实行动次数。
4. 点“清理预览 / 恢复牌面”结束预览，继续真实战斗时使用游戏内“继续”。修改配置后按 Ctrl+S 保存资源。

运行中修改这个 ScriptableObject 资源会保留，结束播放不会自动恢复配置；不满意可以撤销。下一次动效读取新参数，已经飞出的牌保持启动时长，避免半途跳变。震屏开关、闪光、粒子扩散等逐帧读取。

## 常见调整

| 想调整什么 | Inspector 参数 | 改大后的结果 |
|---|---|---|
| 发牌、出牌、弃牌快慢 | 抽牌 / 出牌 / 弃牌飞行时长 | 飞得更慢 |
| 一张张发牌的节奏 | 逐张发牌间隔 | 间隔更长 |
| 飞牌弯曲、摆动 | 飞行弧线高度 / 飞牌摇摆角度 | 弧线更高、摆动更明显 |
| 攻击前停顿和突进 | 蓄力时长 / 卡牌突进比例 | 停顿更久、突进更远 |
| 攻击飞行、命中停留 | 攻击飞行时长 / 命中光圈时长 | 过程更慢、光圈保留更久 |
| 大伤害的表现 | 伤害 → 视觉强度曲线 | 横轴是真实伤害，纵轴为视觉强度 0～1 |
| 连锁更夸张 | 攻击 / 命中连锁最大倍率 | 光圈、投射物和命中效果更强 |
| 连锁多久变猛烈 | 攻击 / 命中连锁增长缓急 | 越大增长越缓慢；越小越快 |
| 效果多久算连续 | 连续效果判定时间 | 连锁更容易接续 |
| 震屏与闪光 | 启用震屏 / 血条闪光、各自强度、持续时间 | 更强、更久；关闭开关即可停用 |
| 粒子与飘字 | 数量上限、扩散速度、大小增长、飘字字号和上升速度 | 更密、更大或上升更快 |
| 颜色 | 攻击 / 护盾 / 治疗 / 状态 / 粒子颜色 | 替换对应颜色 |
| 音效 | 轻击 / 重击音效、音调、音量倍率 | 自己拖入 AudioClip；留空沿用组件或默认生成音效 |
| 行动播放间隔 | 普通 / 牌序行动间隔（1×秒） | 每步等待更长；游戏速度按钮仍然生效 |
| 长连锁加速 | 保持原速次数 / 每次增速 / 最大播放倍率 | 前 N 次原速，从第 N+1 次开始提速 |

命中粒子时长包含无飞牌模式的飞行阶段，工具会保证命中后留出可见时间。粒子数量设为 0 可以关闭粒子；这不关闭投射物、光圈和飘字。

例：希望伤害 10 普通、伤害 40 明显猛烈，在曲线上设置 (10, 0.3)、(40, 0.9)。希望连锁很快爆发，降低“连锁增长缓急”并提高最大倍率。这里调整的是表现，不修改真实伤害、目标或效果判定。

## 多套配置与场景接线

在 Project 中 Ctrl+D 复制 DefaultBattlePresentation，保存在 Assets/RainbowLi/Data，命名为自己的方案。选中复制资源，在非播放模式点击 **“将此配置绑定到当前 RainbowLi 场景”**，然后保存场景；该按钮统一绑定飞牌、命中和播放控制三个组件。

默认场景已经绑定。场景 GameView → CardMotionController、系统 BattleEffects 和 BattleModule 的 Profile 引用应指向同一资源。绑定配置后，组件上的旧调参字段显示为只读，点击“打开策划动效配置”即可跳转。未绑定配置的旧场景仍使用原组件参数。

牌面和特效素材入口仍在场景的 CardMotionController → Card Prefab、BattleEffects → Particle Prefab / Number Prefab；它们影响所有共用该预制体的表现。此次提供的是参数与预览工具，不包含 Animator 时间轴或任意新特效编排器。

## 程序维护位置

- Scripts/Unity/Data/BattlePresentationProfile.cs：统一资源字段、伤害曲线和最低时长保护。
- Editor/BattlePresentationProfileEditor.cs：中文 Inspector、预览按钮。
- Editor/PresentationBoundComponentEditors.cs：组件只读提示与跳转。
- Editor/PresentationProfileSceneMigration.cs：创建默认资源、场景统一绑定。
- Scripts/Unity/Effects/CardMotionController.cs：飞牌、攻击和运行时预览。
- Scripts/Unity/Effects/BattleEffects.cs：命中、粒子、震屏、飘字和音效。
- Scripts/Unity/Systems/BattleModule.cs：行动间隔和长连锁速度，保留游戏速度按钮。

Unity 编译及 181 项验证通过：默认曲线单调、配置一致性、运行中修改、当前飞行保留启动时长、用户播放倍率、预览双方目标、预览不改变玩法、重新开局清理。日志 Verification~/presentation-profile.log。
