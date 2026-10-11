# 扑克式行动牌桌与拖放

打开 Scenes/RainbowLiGame.unity 进入战斗，拖动可用手牌到行动牌桌。金色竖线表示插入位置，松开后提交放置。桌上的玩家牌可以拖动排序，拖回下方手牌区撤回；拖到其他区域松开会取消。敌方牌不能拖动，结算阶段锁定编辑。行动次数已满时仍能重排和撤回，不能从手牌继续放新牌。

卡下左右移动、移除按钮及“+”按钮均已隐藏。原数组和接口保留兼容，不再作为玩家交互入口。手牌悬停说明和原有放牌飞行动画继续使用。

牌桌参考德州扑克公共牌区：圆角绿毡、细金边、居中固定大小的横排牌位、空牌位编号。红色标识敌方，青绿色标识玩家，从左到右执行。最少显示五个牌位，真实牌位数按本回合容量扩展，长行动列可以横向滚动。

## 场景调整

选择 BattlePanel → ActionArrangement → ActionViewport → ActionCards 上的 **PokerActionLayout**：

| 字段 | 调整内容 |
|---|---|
| Minimum Slots | 最少牌位数，不限制行动上限 |
| Card Scale | 桌面牌的缩放 |
| Slot Gap | 牌位间距 |
| Horizontal Padding | 长牌列两侧留白 |
| Card Top | 卡牌距顶部的位置 |
| Felt Color / Rail Color | 绿毡和金边颜色 |
| Empty Slot Color / Slot Border Color | 空牌位底色与边框 |

PokerTableSurface 和 PokerSlot 均为原生 Unity UI，可在 Hierarchy 编辑。PokerTableGraphic 使用 UI 网格绘制圆角，不依赖外部图片。编辑器中修改 PokerActionLayout 参数会更新预览。

选中 BattlePanel 上的 BattlePanelView：
- Drop Area：自定义拖放接收区域，默认使用行动条的可见 viewport。
- Insertion Color：插入线颜色。
- Dragged Hand Opacity：拖动时原手牌的透明度。

不用重新生成场景或手工绑定每张手牌。BattlePanelView 在刷新时自动为手牌挂载 HandCardDrag；运行时增加的手牌也会接入。

默认场景已迁移。Unity 若仍开着修改前的场景，重新打开 RainbowLiGame 即可。菜单 **RainbowLi → UI → 应用扑克式行动牌桌** 可以接入布局，重复执行不会重复生成桌面。

## 修改位置

- Scripts/Unity/Views/PokerActionLayout.cs：集中横排、牌位数量、空位、编辑器预览。
- Scripts/Unity/Views/PokerTableGraphic.cs：圆角桌面和牌位绘制。
- Scripts/Unity/Views/HandCardDrag.cs：手牌放置、行动牌重排/撤回、指针预览与取消。
- Scripts/Unity/Views/BattlePanelView.cs：放置区域、插入位置与标记、手牌接线和提示。
- Scripts/Unity/Views/BattleHudView.cs：接入独立牌桌布局。
- Scripts/Unity/Systems/CardModule.cs：Place(Card,int)、Reposition(Card,int)、Withdraw(Card)，成功提交后通知刷新。
- Editor/PokerActionSceneMigration.cs：场景迁移。

Reposition 参数为插入边界，范围 0～Timeline.Count；向右移动时移除原位置后自动减一。拖到自身前后边界视为不变。接口拒绝编辑敌方行动和结算阶段的行动。

没有修改 ContainmentGame、作用对象或结算规则。

## 验证

Verification~/DragPlacementChecks.cs 在隔离 Unity 实际场景中通过 40 项检查：前后插入、真实 UI 射线、点击不放牌、预览不挡射线、透明度跨帧、无效松开、重复提交、行动限制、移除重用、动态手牌、阶段切换、重开与残留清理。验证通过调用 Unity 指针事件处理器和射线检测进行；未进行人工鼠标操作验收。

Verification~/PokerTableChecks.cs：61 项通过，覆盖场景引用/幂等迁移、圆角网格、集中横排、空位、双向拖动、原位不变、撤回、行动限制、敌方锁定、阶段切换、重开与长列滚动。预览图已检查。

原有战斗播放回归通过 50 项检查，覆盖暂停、单步、速度、跳过一致性、长链效果与重开。日志为 Verification~/drag-placement.log 和 drag-playback-regression.log。

本次回归日志为 Verification~/poker-hand-regression.log 和 poker-playback-regression.log。
