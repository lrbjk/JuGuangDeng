# 美术战斗 UI

主场景：Assets/RainbowLi/Scenes/RainbowLiGame.unity。使用 Art/GJ 战斗ui分解 的原始素材；不将示意图整张覆盖在交互控件上。1920×1080 设计坐标通过现有 CanvasScaler 适配。

## 编辑位置
- Table/ArtBattleBackdrop：完整 CRT 与边框背景。BattleArtView 根据战斗界面显示状态切换，地图、休息与开始菜单不会显示战斗背景。
- Table/Sidebar：敌人技能、敌人牌序、金币/牌组、玩家牌序。技能与 BOSS 超出可见范围时滚动显示。
- BattleHudView：artLayout 开启美术布局；skillColumns / skillScale / skillOrigin / skillSpacing 调整技能卡列表。enemyPortrait 继续由敌人/BOSS 配表的 portrait 按 ID 切换；未配置立绘不使用示意图中的怪物代替。
- BattlePanel：立绘、血条、状态、行动条和手牌。原有玩家/敌人血条 Slider 与护盾数值仍绑定真实数据。
- PokerActionLayout：minimumSlots=12；cardScale / slotGap / cardTop / horizontalPadding 调整卡槽间距和牌尺寸；slotArtwork / surfaceArtwork 选择卡槽与行动条皮肤。拖牌仍按现有行动顺序插入、排序与撤回；超过宽度可横向滚动。
- PokerCard.prefab / CardView：illustratedFace 与各行为图标设置牌面，playerFace / enemyFace 设置双方颜色。手牌、行动牌、技能牌、放大预览和飞牌共同使用新皮肤。数字与说明仍由数据动态写入，不是固定图片。
- BattleStatusPanelView：iconMode、statusIcons、fallbackIcon、iconSize、iconGap 控制状态图标；角标显示层数，点击仍可查看完整解释。
- CardPaper.mat / CardPaper.shader：将素材 CRT 纹理转换为双方纸面底色，保留 UI 遮罩、滚动裁剪和悬停透明处理。

右侧保留“开始执行 / 暂停 / 下一步”按钮，以支持现有回合操作；抽牌堆、弃牌堆、设置和牌组查看仍使用原有入口。休息界面继续隐藏场地牌序。

## 美术素材与工具
背景2作为独立背景；血槽/血量、牌序框/数字框、卡槽、设置、抽弃牌堆与状态图标使用分层 Sprite。背景2额外导入 CardNoise 子 Sprite 作为牌面纹理，不改动原 PNG。新增材质和 Shader 也保存在同一 RainbowLi 美术目录。

需要重新应用时：RainbowLi → UI → 应用美术战斗界面。该工具只作用于当前 RainbowLi 场景。完成后保存场景；手动调整后无需每次重新运行工具。

验证：18 项美术界面交互检查、69 项悬停与卡牌攻击检查、1485 项实际场景敌人/BOSS技能牌序与奖励检查通过。未改动战斗结算规则和配表数值。

## 血条与卡牌排布细调
血条使用独立 ArtTrack + ArtFill，填充为九宫格，右侧斜角保留固定像素宽度；独立 ArtFillArea 按血量调整宽度，避免旧 Slider 的 FillArea/Handle 叠加和纹理拉伸。可在场景中调整 ArtFill 内边距。
行动条 leadingEmptySlots 控制前置空槽（默认3），enemyInsertionGap 控制敌方行动前插牌位（默认1）；只调整显示位置，结算顺序不变。美术模式隐藏行动牌上方的序号标签。
手牌从左侧开始，默认 handScale=.94、cardGap=6，牌面底部说明计入牌内高度；数量过多仍可横向滚动。
