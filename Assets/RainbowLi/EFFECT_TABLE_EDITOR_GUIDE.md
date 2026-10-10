# 铭刻与牌序配表使用说明

## 打开编辑器

在 Unity 顶部菜单打开：

- `RainbowLi > 配表 > 铭刻效果编辑器`
- `RainbowLi > 配表 > 牌序效果编辑器`

也可双击 `Data/DefaultEngravingEffects.asset` 或 `Data/DefaultSequenceEffects.asset`。
所有路径以 `F:/JuGuangDeng/Assets/RainbowLi/` 为根目录。

## 配表流程

1. 在窗口选择配表资产。默认表已经导入原 Excel 的 19 条铭刻、23 条牌序效果。
2. 直接修改表格，也可使用“导入 XLSX（替换）”导入整表，或“按编号合并”更新部分行。合并保持现有行顺序，新编号加在末尾。
3. 从 Excel 复制包含编号的完整行，使用“从 Excel 粘贴”；支持带换行和制表符的描述。
4. 使用“保存配表”保存资产，再重新开始游戏。导入和手动编辑支持 Unity Undo。
5. 使用“导出 XLSX”得到包含程序配置列的完整模板。不要修改原表前几列的顺序；新增编号必须填写程序效果标识。

铭刻表前五列、牌序表前六列与原表完全对应。导入会忽略尾部空白行。公式需先转成值。

## 新增配置列

- **启用**：是否进入可选池；禁用行仍保留在表内。
- **权重**：供核心程序后续抽取使用，必须为有限正数；本次没有新增随机抽取规则。
- **程序效果标识**：稳定的行为标识。改名称和描述不会改变结算逻辑；未知标识会被校验拒绝。
- **X倍率**：基础 X 的倍率。例如伤害2X，范围仍是 5–10，倍率是 2，绑定 X=7 后得到 14。
- **固定参数**：无 X 效果的固定数值参数，供核心程序接入，例如收尾/冲锋的 +3、谢幕/开幕的 +2、充能的 +1。复杂效果的其他条件仍见原始描述，需要核心程序对应实现。
- **运行状态**：由现有适配器判断，只读；修改配表描述不会把未实现效果变成可用效果。

牌序“卡效所属池”保留原表内容，可同时属于多个池。用窗口的“所属池筛选”输入单个池名，例如默认池、伤害池或数字效果池。池名称表示分类，不会自动改变牌序的顺子/三同号要求。

## 绑定到游戏配置

选中一行，在窗口底部拖入已有配置资产，填 X，再点击“绑定到配置”。也可以点击“新建效果配置”。X=0 使用表中范围下限；本次绑定取固定 X，不会自动随机抽取。

- 铭刻对应 `EngravingDefinition`，如 `Data/DefaultEngraving.asset`。新铭刻配置需拖到场景 `GameSystems/EngravingSystem` 的 Definition，或对应 `CardDefinition` 的 Engraving 字段。
- 牌序对应 `SequenceDefinition`，如 `Data/Sequences/PlayerStrike.asset`。窗口支持绑定为主效果或第二效果。新牌序配置需放入玩家配置的 Sequences、游戏配置的 Arena Rules、或敌人配置的 Sequence。
- 数字组合继续在牌序配置的 Numbers 中编辑；本窗口配的是效果。
- 现有默认配置没有被自动替换，原战斗规则和当前 UI 布局保持不变。

## 已可接入的效果

- 铭刻：铭刻效果_03 追击X，接入现有“牌序收尾时追加伤害”的触发方式。其他铭刻时机和行为交由核心程序实现。
- 牌序：牌序效果_01 伤害X、_02 填充X、_03 护盾X、_05 增幅X、_16 伤害2X、_18 护盾2X、_19 增幅2X，复用现有伤害、护盾、增幅和插数结算。

其余行可以完整配表、保存和导出，窗口显示“待核心程序”，绑定按钮禁用。即便手动在 Inspector 填入未实现的编号，也会明确报错，不会默默转换为伤害效果。

## 你负责的修改位置

- `Scripts/Unity/Data/EffectTableRow.cs`：共同字段、X范围和倍率。
- `Scripts/Unity/Data/EffectTableDefinition.cs`：编号、字段和范围校验，按池查询。
- `Scripts/Unity/Data/EngravingEffectTable.cs`、`SequenceEffectTable.cs`：两类配表资产及现有结算适配。
- `Editor/EffectTableWorkbook.cs`：原始列与程序字段的导入导出。
- `Editor/EffectTableEditorWindow.cs`：共同表格界面、筛选、复制、合并、Undo、绑定。
- `Editor/EngravingEditorWindow.cs`、`SequenceEffectEditorWindow.cs`：两个菜单入口。
- `Data/DefaultEngravingEffects.asset`、`DefaultSequenceEffects.asset`：默认配表数据。

另一个人的接入清单见 `EFFECT_TABLE_CORE_HANDOFF.md`。

验证：115 项新配表检查、158 项原卡牌配表和原生游戏启动回归；两个导出文件经独立 Excel 读取确认原始列逐格一致。
