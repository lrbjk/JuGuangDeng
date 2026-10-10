# 界面与工具施工：UI 视图拆分

对应 Notion《界面与工具任务清单》第 1 项。工程 F:/JuGuangDeng，仅修改 Assets/RainbowLi。

## 当前批次交付

- 战斗、奖励、商店/休息、胜利/失败结算分别由独立视图处理。
- 手牌、行动、敌人技能和奖励卡牌支持悬停查看完整说明；长说明自适应面板高度，位置限制在画面内。
- 大量手牌和行动使用横向滚动，避免无限缩小卡牌；普通手牌的底部说明保持在视口内。
- 商店/休息的卡组行复用，不再每次刷新销毁重建；按钮重新绑定前清理运行时监听，避免一次点击重复消费。
- 双方状态标签、详情和地牢地图仍保留原有独立组件。

## 在哪里改

打开 Assets/RainbowLi/Scenes/RainbowLiGame.unity，在 GameView / Table 中选择以下对象：

| 场景对象 / 组件 | 脚本位置 | 负责的内容 |
|---|---|---|
| BattlePanel / BattlePanelView | Scripts/Unity/Views/BattlePanelView.cs | 手牌、行动按钮、战斗文字、UI 数量扩容 |
| RewardPanel / RewardPanelView | Scripts/Unity/Views/RewardPanelView.cs | 奖励显示、选牌按钮、说明 |
| ServicePanel / ServicePanelView | Scripts/Unity/Views/ServicePanelView.cs | 商店/休息、卡组行、购买与维护按钮 |
| EndPanel / EndPanelView | Scripts/Unity/Views/EndPanelView.cs | 胜利和失败结算文字 |
| GameView / BattleHudView | Scripts/Unity/Views/BattleHudView.cs | 手持/行动滚动布局、状态和敌人技能 |
| GameView / CardTooltip | Scripts/Unity/Views/TooltipView.cs | 说明标题、正文、字体、尺寸与边界限制 |
| 卡牌 / CardView | Scripts/Unity/Views/CardView.cs | 悬停说明、选中上浮、牌面 |
| 卡组行 / DeckRowView | Scripts/Unity/Views/DeckRowView.cs | 升级、铭刻、删牌按钮 |

GameView.cs 只统一监听游戏变化、切换面板和调用各视图。为兼容现有场景、动效和另一位程序的引用，原序列化字段暂时保留；新增视图的 View 引用指向 GameView。迁移没有修改 GameSession、GameTypes 或 ContainmentGame。

手牌滚动位置：CurrentHand / HandViewport / HandContent。行动滚动位置：ActionArrangement / ActionViewport / ActionCards。对应 ScrollRect 挂在外层面板上；鼠标滚轮或拖动查看，原加号和移动按钮继续使用原接口。

其他 RainbowLi 场景可以在编辑模式使用 RainbowLi → UI → 接入独立面板与卡牌说明。默认场景已接入。未经迁移的旧场景需先运行此菜单，确保 GameView 上的新视图引用完整。

## 后续按分工推进

第 2 项已补齐战斗暂停、单步、统一倍速、跳过本回合与重开清理，详见 BATTLE_PLAYBACK_GUIDE.md。第 3 项：卡牌导入差异预览、逐行校验、批量编辑；复合效果先与玩法程序约定运行时接口。第 4 项：剩余遭遇、奖励池、商店和事件配表及读取接口。第 5 项：主菜单、设置持久化、存档入口接线。第 6 项：完整地图/精英/Boss/死亡流程和独立玩家构建验证。

本批不代表整份清单已经完成，也不代表存档或完整独立构建已验证。

## 验证

Verification~/ui-modules.log：Unity 编译与 16 项真实运行断言通过，覆盖独立组件绑定、奖励按钮、商店多次刷新后的单次消费、手牌/行动扩容、滚动尺寸、长说明边界、结算显示与重开。

部分测试为验证界面，使用测试脚本设置奖励/结算阶段；这不替代第 6 项真实整局流程验证。截图：Verification~/UiModulesPreview。
