# 敌人 / BOSS 立绘

普通敌人：菜单 RainbowLi → 配表 → 普通敌人内容编辑器，选择敌人，设置「敌人立绘（Sprite）」。
BOSS：菜单 RainbowLi → 配表 → BOSS内容配表编辑器，选择 BOSS，设置「BOSS立绘（Sprite）」。也可直接在 DefaultNormalEnemies / DefaultBosses 资产中设置对应条目的 portrait。

将图片 Import Settings 的 Texture Type 改为 Sprite (2D and UI)，Apply 后将 Sprite 拖入立绘字段并保存。图片和资产均放在 Assets/RainbowLi 内。

接口：EnemyDefinition.GetPortrait(string enemyId, bool isBoss) 按实际战斗 Encounter.Definition.Id 查找图片。BattleHudView.RefreshEnemyPortrait(game) 在刷新时自动绑定 enemyPortrait.sprite，保持宽高比。普通敌人与 BOSS 使用各自的表，允许相同 ID；未配置或删除图片时显示占位，不沿用上一位敌人的图片。读取存档后仍按保存的敌人 ID 查找，不在运行时存档中写入 Sprite。

立绘在 Unity 配表中维护，不嵌入 Excel。Excel 导入时按原工作表名保留已有立绘，即使数值或描述变化也不会清掉图片。导出数值 JSON 不包含有效 Sprite 引用，防止跨 Unity 会话错误引用图片。
