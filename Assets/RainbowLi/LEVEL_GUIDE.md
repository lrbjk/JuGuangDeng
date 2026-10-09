# 分支关卡系统

入口仍是 `Scenes/RainbowLiGame.unity`。开始时显示路线地图，从底部选择起点，完成房间后沿连线选择下一层。默认 8 层、3 条分支，共 22 个节点，最终汇合到 Boss；鼠标滚轮或拖动地图查看上方路线。

普通战斗胜利并领取卡牌奖励后才完成节点；精英有更高生命、攻击与金币收益。休息和商店退出后前进；事件选择一次金币或治疗奖励后前进。最后战胜 Boss 通关；失败后不能继续选节点，点击新的一局重置路线与游戏状态。

## 在 Unity 中编辑

- `GameSystems/LevelSystem`：独立 LevelModule，指定 Session 与关卡 Definition。
- `Data/DefaultLevels.asset`：Floors 层数、Lanes 分支数、Seed 种子、事件金币/治疗、精英生命倍率/攻击加成/金币。Seed 为 0 跟随游戏种子，非零固定路线。
- 取消 Randomize Each Run 后编辑 Authored Nodes：Id 唯一编号；Floor 从 0 开始；Lane 横向位置；Kind 房间类型；Next 为下一层节点 Id 列表。必须逐层连接、所有节点可达，末层只有一个 Boss，之前不能有死路。配置错误会报错。
- `Prefabs/LevelNode.prefab`：原生 Button、背景和文字样式。场景节点为保存好的 Prefab 实例。
- `GameCanvas/Table/MapPanel/BranchMap`：标题、提示、滚动视口、节点和连线。非 Play 模式启用 MapPanel 后可编辑 RectTransform。相同编号、层和分支位置会保留已有节点坐标。
- `GameCanvas` 上的 RouteMapView：节点间距、内容宽度、是否自动跟随当前层。更改层数/分支数后，新节点在运行时按间距生成。
- `GameCanvas/Table/RouteEventPanel`：事件描述、选择按钮与 UnityEvent。

持久修改请在非 Play 模式完成，重新 Play 或新的一局应用配置。手工节点列表控制玩法连接，拖动节点只改变显示位置；无需重新生成整个场景。

## 模块边界

| 文件/组件 | 职责 |
|---|---|
| Domain/RouteGraph.cs | 节点、生成、合法性校验、RouteRun 可选/进行中/完成/失败状态 |
| Unity/Data/LevelDefinition.cs | ScriptableObject 配置，生成每局独立路线 |
| Unity/Systems/LevelModule.cs | 进入房间、调用战斗/服务/事件、完成节点和通关协调 |
| Unity/Systems/ProgressionModule.cs | 卡牌奖励与商店/休息操作，结束后通知关卡模块 |
| Unity/Views/RouteMapView.cs | 地图显示、滚动、节点状态与连线 |
| Unity/Views/LevelNodeView.cs | 单个节点 Button、标签与颜色 |

关卡模块不保存第二份玩家/敌人数值，地图视图不计算战斗规则。路线复制配置，完成标记不会写回资产。

旧 `ContainmentPrototype.unity` 保留线性任务逻辑供参考。新版为一局单张分支路线，尚未实现路线存档与多章节。

验证：1885 项规则断言与 550 项 Unity 场景/交互检查通过，覆盖分支限制、所有房间、奖励门槛、精英、Boss、失败与重开。无图形批处理验证，最终排版需在 Game 视图试玩确认。
