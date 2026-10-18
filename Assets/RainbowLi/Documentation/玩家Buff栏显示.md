# 玩家Buff栏显示

玩家图标栏保持在战斗界面右侧安全区域，宽410、高58；46像素图标加6像素间距，一屏完整显示8枚。超过8枚时下方出现横向滚动条，可拖动滑块或在图标上滚动滚轮查看后面的状态。每枚图标保留层数与点击详情。

背景和裁切框不拦截鼠标，只有状态图标和滚动条可交互。显示区域避开玩家血条与播放控制按钮。移除状态后内容自动收缩，状态不足一屏时隐藏滚动条并复位。

场景选择 `PlayerStatuses`：ScrollRect 的 Viewport/Horizontal Scrollbar 是实际显示和滚动入口；`BattleStatusPanelView.iconSize/iconGap` 是图标尺寸和间距。

程序入口：`Scripts/Unity/Views/BattleStatusPanelView.cs`。菜单 `RainbowLi > UI > 修复玩家Buff栏裁切` 重新套用当前布局；重新应用美术战斗界面也会保留该修复。
