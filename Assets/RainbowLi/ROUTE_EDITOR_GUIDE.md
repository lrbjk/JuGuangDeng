# 玩家移动的卡片地牢

项目：F:/JuGuangDeng。场景：Assets/RainbowLi/Scenes/RainbowLiGame.unity。
菜单：RainbowLi → 关卡 → 关卡编辑器。默认配置：Assets/RainbowLi/Data/DefaultLevels.asset。

## 当前规则

玩家从入口中间列开始，每步可进入下一行的左前、正前、右前卡片。完成战斗及奖励，或退出休息、商店、事件后，位置移动到所选卡片，再按新位置计算可选范围。中途可以左右换方向。

其他列的后续卡片仍在地牢中。下一行距离玩家超过一列的卡片变暗；更远行被前方卡片遮挡，不能提前进入。走过的行退出前方视野。最后一行的唯一首领在玩家抵达前一行后可进入，不需要清空所有列。新的一局恢复入口位置。

当前原型逐行向前移动，回头、循环地图和未知卡片揭示尚未实现。

## 编辑配置

1. 打开关卡编辑器，选择 DefaultLevels，开启“玩家移动的卡片地牢”。该模式优先于旧堆叠模式。
2. 取消“每局随机生成”使用固定配置。默认示例为三列、七行普通房间，加最后一行首领。
3. “行”从 0 起表示前进顺序；“列”从 0 起表示左右位置。拖动卡片或编辑字段修改，无需连线。
4. “起点列”设为 -1 时从中间开始，也可指定从 0 起的列索引。
5. 配置战斗、精英、休息、商店、事件、首领类型，填写名称、说明和 Sprite 图标；留空使用默认显示。
6. 点击“校验并保存”，游戏中点击“新的一局”重新读取。Ctrl+Z 撤销编辑。

校验：ID 非负且唯一，同一行、列只能有一张卡，最高行只有一个首领，入口有可达卡片，每个普通房间的下一行必须有同列或相邻列房间，或最终首领。行号必须连续才能逐步前进。

模板生成会替换旧节点及展示配置，可撤销。随机模式使用生成器，不使用固定节点的展示说明；运行种子 0 随游戏种子，其他数值固定。新配置只保存到 Assets/RainbowLi，赋给场景 LevelSystem → LevelModule → Definition。

事件奖励、精英倍率在 LevelDefinition Inspector 配置。敌人卡牌、技能及基础生命由 EnemySystem 的 EnemyDefinition 配置。

## 在场景中修改表现

- MapPanel / BannerRoute：原生 UGUI 地牢界面。
- RouteScroll / Viewport / RouteContent：卡片容器，位置由行、列及玩家当前位置计算。
- RouteContent / PlayerPosition：玩家位置标记，修改 Image 和子节点 Label。
- Prefabs/DungeonCard.prefab：矩形地牢卡片的边框、图标、名称、状态和字体。
- RoomTooltip：悬停说明。MapHealth / Resources / FloorProgress：血条、资源和位置文字。
- GameView → RouteMapView → Foreground Spacing：列间距；Player Marker：标记引用。
- Horizon_* / Stone_*：背景占位图形。

默认场景已接入。其他 RainbowLi 场景可用菜单“RainbowLi → 关卡 → 接入玩家移动的卡片地牢”，然后保存场景。

## 程序修改位置

- Scripts/Domain/RouteGraph.cs：CardDungeon / StartLane；RouteRun.PlayerFloor / PlayerLane；CanEnter 判定相邻下一行，Complete 更新位置。
- Scripts/Unity/Data/LevelDefinition.cs：cardDungeon / startingLane；CreateGraph 生成空间配置。
- Scripts/Unity/Views/RouteMapView.cs：可选范围、遮挡深度、玩家标记和位置文字。
- Scripts/Unity/Views/LevelNodeView.cs：可进入、无法到达、后方卡片显示和交互。
- Editor/LevelRouteEditorWindow.cs：网格配表、起点、校验和保存。
- Editor/CardDungeonSceneMigration.cs：场景与 DungeonCard 预制体接入。

参考：[《兰斯03》官方系统介绍](https://www.alicesoft.com/rance03/system/index.html)。本项目按已确认的玩家位置与前方相邻卡片规则实现。

验证：Verification~/card-dungeon.log，16364 项断言通过，覆盖随机网格、边界可达性、中途换方向、服务完成后移动、首领和重新开局。真实场景截图：Verification~/DungeonPreview。
