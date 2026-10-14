using System.Linq;
using UnityEngine;

namespace RainbowLi
{
    public sealed partial class ContainmentPrototype : MonoBehaviour
    {
        private ContainmentGame game;
        private bool autoPlay = true;
        private float nextStep;
        private Card selectedCard;
        private BattleForecast forecast;
        private bool forecastDirty = true;
        private int displayRound, activeSlot = -1;
        private string actionFeedback = "选一张牌，再点击行动条中的 + 放入。";
        private float feedbackTime;
        private int lastPlayerDelta, lastEnemyDelta;
        private Vector2 deckScroll;
        private GUIStyle title, heading, body, muted, button, bigNumber;
        private Texture2D pixel;
        [Header("界面字体")] public Font bodyFont, titleFont;
        private Font font;
        private bool ownsFont;
        private readonly Color ink = new Color(0.035f, 0.065f, 0.055f);
        private readonly Color panel = new Color(0.065f, 0.155f, 0.12f);
        private readonly Color gold = new Color(0.92f, 0.75f, 0.4f);
        private readonly Color teal = new Color(0.35f, 0.85f, 0.78f);
        private readonly Color coral = new Color(1f, 0.48f, 0.43f);

        private void Awake() { game = new ContainmentGame(); }
        private void Update()
        {
            if (game.Round != displayRound)
            { displayRound = game.Round; selectedCard = null; activeSlot = -1; forecastDirty = true; BeginCardDeal(); }
            UpdateCardEffects();
            if (game.Phase == GamePhase.Resolving && autoPlay && Time.unscaledTime >= nextStep)
            {
                ResolveStep();
                var action = game.LastResolvedAction;
                float delay = action != null && (action.Rule != null || action.IsNumberOnly) ? 0.16f : 0.95f;
                if (action != null && action.Rule != null && action.Rule.Triggers > 5) delay = 0.035f;
                if (action != null && action.IsNumberOnly && game.NumberStream.Count > 10) delay = 0.035f;
                nextStep = Time.unscaledTime + delay;
            }
        }
        private void OnDestroy()
        { if (pixel != null) Destroy(pixel); if (font != null && ownsFont) Destroy(font); DisposeCardEffects(); }

        private void InitializeStyles()
        {
            if (pixel != null) return;
            pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            font = bodyFont != null ? bodyFont : Resources.Load<Font>("RainbowLiChinese");
            ownsFont = font == null;
            if (ownsFont) font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial" }, 20);
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 17, wordWrap = true };
            body.normal.textColor = new Color(0.88f, 0.9f, 0.93f);
            muted = new GUIStyle(body) { fontSize = 14 }; muted.normal.textColor = new Color(0.59f, 0.66f, 0.73f);
            title = new GUIStyle(body) { font = titleFont != null ? titleFont : font, fontSize = 32, fontStyle = FontStyle.Normal }; title.normal.textColor = gold;
            heading = new GUIStyle(body) { font = titleFont != null ? titleFont : font, fontSize = 20, fontStyle = FontStyle.Normal };
            bigNumber = new GUIStyle(title) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16, wordWrap = true };
            button.normal.background = pixel; button.hover.background = pixel; button.active.background = pixel;
            button.normal.textColor = new Color(0.9f, 0.93f, 0.95f);
            button.hover.textColor = Color.white;
            button.padding = new RectOffset(8, 8, 6, 6);
        }

        private void OnGUI()
        {
            InitializeStyles();
            DrawPokerLayout();
        }

        private void DrawStats()
        {
            Fill(new Rect(24, 112, 1392, 66), panel);
            Text(new Rect(40, 125, 330, 36), "魔法师   " + game.Health + " / " + ContainmentGame.MaxHealth + "     护盾 " + game.Shield, heading);
            Text(new Rect(428, 126, 430, 34), "金币 " + game.Coins + "     牌组 " + game.Deck.Count + "     收容 " + game.Victories, body);
            Text(new Rect(1020, 125, 360, 36), "状态 / " + PhaseName(), heading);
        }
        private string PhaseName()
        {
            switch (game.Phase)
            {
                case GamePhase.Planning: return "安排 · 回合 " + game.Round;
                case GamePhase.Resolving: return "执行 · 回合 " + game.Round;
                case GamePhase.Reward: return "战利品";
                case GamePhase.Rest: return "休息";
                case GamePhase.Shop: return "商店";
                case GamePhase.Won: return "收容完成";
                case GamePhase.Lost: return "收容失败";
                default: return "任务选择";
            }
        }

        private void DrawMap()
        {
            Fill(new Rect(24, 194, 866, 624), panel);
            Text(new Rect(44, 212, 800, 38), "01 / 选择下一个节点", heading);
            Text(new Rect(44, 258, 800, 50), "阅读实体意图，安排敌我行动，用三位数字触发牌序。完成两次普通收容后解锁核心实体。", body);
            if (Btn(new Rect(44, 330, 385, 116), "战斗 / 逸散实体\n金币 +25 · 生成卡牌 · 稀有铭刻", coral)) game.StartBattle();
            if (Btn(new Rect(449, 330, 415, 116), "休息 / 维护装置\n恢复 · 激发 · 删牌 · 重构", teal, game.RestAvailable)) game.OpenService(false);
            if (Btn(new Rect(44, 465, 385, 116), "商店 / 档案交易\n铭刻 · 升级 · 数字定向生成 · 牌序", gold)) game.OpenService(true);
            if (Btn(new Rect(449, 465, 415, 116), "BOSS / 失控档案\n" + (game.Victories >= 2 ? "已解锁 · 进入最终收容" : "需完成两次普通战斗"), coral, game.Victories >= 2)) game.StartBattle(true);
            Text(new Rect(44, 612, 800, 36), "操作提示", heading);
            Text(new Rect(44, 658, 800, 130), "① 右侧点击数字，调整自己的三组牌序。\n② 战斗中选一张手牌，再点行动之间的 + 放入。\n③ 看右侧预计生命与亮起的牌序，再调整顺序。\n④ 开始执行，观察高亮行动；可以暂停或单步。", body);
            DrawRules(new Rect(910, 194, 506, 624), true);
        }

        private void DrawRules(Rect rect, bool editable)
        {
            Fill(rect, panel);
            float x = rect.x + 18, y = rect.y + 18;
            Text(new Rect(x, y, rect.width - 36, 32), editable ? "02 / 基础牌序构建" : "牌序 / 连续三个实际数字", heading); y += 48;
            for (int i = 0; i < game.PlayerRules.Count; i++)
            {
                var rule = game.PlayerRules[i];
                Text(new Rect(x, y, rect.width - 36, 25), rule.Name + (editable ? "" : " · " + rule.Triggers + "/99"), muted); y += 30;
                for (int d = 0; d < 3; d++)
                {
                    if (editable)
                    { if (Btn(new Rect(x + d * 72, y, 62, 38), rule.Numbers[d].ToString(), teal)) game.ChangeNumber(i, d); }
                    else Text(new Rect(x + d * 60, y, 50, 30), rule.Numbers[d].ToString(), heading);
                }
                y += 45; Text(new Rect(x, y, rect.width - 36, 42), rule.Description, body); y += 65;
            }
            if (editable)
                Text(new Rect(x, y, rect.width - 36, 120), "点击数字循环 1–6。\n场地 > 玩家 > 实体；插入数字继续匹配。\n每个牌序每回合最多触发 99 次。", muted);
        }

        private void DrawBattle()
        {
            bool planning = game.Phase == GamePhase.Planning;
            if (planning && (forecastDirty || forecast == null)) { forecast = game.Forecast(); forecastDirty = false; }
            Fill(new Rect(24, 194, 990, 154), panel);
            Text(new Rect(44, 210, 300, 30), "你 / 魔法师", heading);
            HealthBar(new Rect(44, 251, 278, 14), game.Health, ContainmentGame.MaxHealth, teal);
            Text(new Rect(44, 273, 290, 28), "生命 " + game.Health + " / 70   护盾 " + game.Shield, body);
            Text(new Rect(692, 210, 300, 30), game.EnemyName, heading);
            HealthBar(new Rect(692, 251, 278, 14), game.EnemyHealth, game.EnemyMaxHealth, coral);
            Text(new Rect(692, 273, 300, 28), "生命 " + game.EnemyHealth + " / " + game.EnemyMaxHealth + "   护盾 " + game.EnemyShield, body);
            Text(new Rect(361, 210, 290, 31), "第 " + game.Round + " 回合 / " + (planning ? "安排中" : "执行中"), heading);
            Text(new Rect(361, 246, 310, 24), planning ? "基础排列 / 最近三位数字" : ActionDirection(), muted);
            var digits = (planning ? game.Timeline.Select(a => a.Number) : game.NumberStream).ToList();
            for (int i = 0; i < System.Math.Min(3, digits.Count); i++)
                NumberBadge(new Rect(380 + i * 82, 278, 58, 42), digits[System.Math.Max(0, digits.Count - 3) + i], gold);
            Text(new Rect(44, 315, 600, 24), "敌方意图：" + string.Join("   /   ", game.Timeline.Where(a => a.Enemy).Select(a => EffectText(a.Effect, a.Value)).ToArray()), muted);
            if (!planning && Time.unscaledTime - feedbackTime < 0.9f)
            {
                if (lastPlayerDelta != 0) Text(new Rect(268, 210, 70, 34), Signed(lastPlayerDelta), heading);
                if (lastEnemyDelta != 0) Text(new Rect(938, 210, 70, 34), Signed(lastEnemyDelta), heading);
            }

            Fill(new Rect(24, 362, 990, 190), panel);
            Text(new Rect(42, 373, 750, 30), selectedCard == null ? "1 / 安排行动顺序    从左向右执行 →" : "将数字 " + selectedCard.Number + " 放在这里：点击 +", heading);
            Text(new Rect(817, 377, 185, 24), "已用 " + game.PlayerActionCount + " / 4 张", muted);
            for (int i = 0; i < game.Timeline.Count; i++)
            {
                var action = game.Timeline[i]; float x = 60 + i * 134;
                Color color = action.Enemy ? coral : EffectColor(action.Effect);
                var tile = new Rect(x, 414, 108, 91);
                DrawActionPoker(tile, action, i, !planning && i == activeSlot);
                if (Btn(new Rect(x, 511, 30, 28), "←", teal, planning && i > 0)) { game.MoveAction(i, -1); forecastDirty = true; }
                if (Btn(new Rect(x + 34, 511, 30, 28), "→", teal, planning && i < game.Timeline.Count - 1)) { game.MoveAction(i, 1); forecastDirty = true; }
                if (Btn(new Rect(x + 68, 511, 40, 28), "×", coral, planning && !action.Enemy)) { game.RemoveAction(i); forecastDirty = true; break; }
            }
            for (int i = 0; i <= game.Timeline.Count; i++)
            {
                if (Btn(new Rect(36 + i * 134, 445, 21, 44), "+", gold, planning && selectedCard != null && game.PlayerActionCount < 4))
                { var played = selectedCard; if (game.InsertCard(played, i)) FlyCardToSlot(played, i); selectedCard = null; forecastDirty = true; break; }
            }
            Fill(new Rect(24, 556, 990, 178), panel);
            Text(new Rect(42, 561, 900, 29), "2 / 选择手牌    牌面点数参与牌序 · 底部标注卡效", heading);
            for (int i = 0; i < game.Hand.Count; i++)
            {
                Card card = game.Hand[i]; float x = 42 + i * 157;
                bool placed = game.Timeline.Any(a => a.Card == card), selected = selectedCard == card;
                var color = EffectColor(card.Effect);
                var rect = new Rect(x + 18, 597, 108, 134);
                if (DrawHandPoker(rect, card, i, placed, selected, planning && !placed && game.PlayerActionCount < 4))
                { selectedCard = selected ? null : card; KickCard(card); }
            }
            Fill(new Rect(24, 742, 990, 76), panel);
            Text(new Rect(42, 752, 594, 52), planning ? "3 / 看右侧预览，确认后执行\n护盾要排在敌人攻击之前。" : actionFeedback, body);
            if (planning)
            {
                if (Btn(new Rect(681, 757, 308, 44), "开始执行 →", gold, game.PlayerActionCount > 0))
                { game.Confirm(); selectedCard = null; activeSlot = -1; nextStep = Time.unscaledTime + 0.7f; }
            }
            else
            {
                if (Btn(new Rect(665, 759, 156, 40), autoPlay ? "暂停" : "继续播放", teal)) autoPlay = !autoPlay;
                if (Btn(new Rect(835, 759, 154, 40), "执行下一步", gold, !autoPlay)) ResolveStep();
            }
            DrawBattleSidebar();
        }

        private void DrawBattleSidebar()
        {
            Fill(new Rect(1030, 194, 386, 624), panel);
            float x = 1048; bool planning = game.Phase == GamePhase.Planning;
            Text(new Rect(x, 210, 350, 28), planning ? "这一回合会发生什么？" : "实时结算 / 牌序触发", heading);
            if (planning && forecast != null)
            {
                Text(new Rect(x, 254, 350, 34), "你 " + game.Health + " → " + forecast.Health + "     敌人 " + game.EnemyHealth + " → " + forecast.EnemyHealth, heading);
                Text(new Rect(x, 295, 350, 28), "回合末护盾：你 " + forecast.Shield + " / 敌人 " + forecast.EnemyShield, muted);
                Text(new Rect(x, 329, 350, 32), forecast.Defeat ? "危险：此安排会导致你倒下" : forecast.Victory ? "此安排可以完成收容！" : "调整顺序，比较预计结果。", body);
            }
            else
            {
                Text(new Rect(x, 250, 350, 68), game.LastAction, body);
                Text(new Rect(x, 327, 350, 28), "后续卡效强化：+" + game.Power, muted);
            }
            var arena = game.ArenaRules[game.ArenaIndex];
            var rules = new[] { arena }.Concat(game.PlayerRules).Concat(new[] { game.EnemyRule }).ToArray();
            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i]; float y = 375 + i * 66;
                int hits = planning && forecast != null && forecast.RuleHits.ContainsKey(rule.Name) ? forecast.RuleHits[rule.Name] : rule.Triggers;
                bool active = !planning && game.LastResolvedAction != null && game.LastResolvedAction.Rule == rule;
                Fill(new Rect(x, y, 350, 60), hits > 0 ? new Color(0.15f, 0.25f, 0.24f) : new Color(0.12f, 0.14f, 0.18f));
                if (active) DrawRuleBurst(new Rect(x, y, 350, 60));
                Text(new Rect(x + 8, y + 5, 330, 24), rule.Pattern + "  " + (i == 0 ? "场地" : i == 4 ? "敌人" : "你的牌序") + (hits > 0 ? " · " + (planning ? "预计 " : "已触发 ") + hits + " 次" : ""), body);
                Text(new Rect(x + 8, y + 33, 332, 22), rule.Description + (i == 4 ? "（对你生效）" : ""), muted);
            }
            Text(new Rect(x, 716, 350, 26), "场地触发 " + game.ArenaTriggerCount + "/2，达成后下回合轮换", muted);
            var numbers = planning && forecast != null ? forecast.Numbers : game.NumberStream;
            Text(new Rect(x, 750, 350, 56), (planning ? "预计数字流：" : "实际数字流：") +
                string.Join(" → ", numbers.Take(12).Select(n => n.ToString()).ToArray()) + (numbers.Count > 12 ? " …（共 " + numbers.Count + " 位）" : ""), muted);
        }

        private void ResolveStep()
        {
            int health = game.Health, enemyHealth = game.EnemyHealth, shield = game.Shield, enemyShield = game.EnemyShield;
            game.Step();
            TriggerCardImpact(game.LastResolvedAction, game.Health - health, game.EnemyHealth - enemyHealth, game.Shield - shield, game.EnemyShield - enemyShield);
            lastPlayerDelta = game.Health - health; lastEnemyDelta = game.EnemyHealth - enemyHealth;
            int index = game.LastResolvedAction == null ? -1 : game.Timeline.IndexOf(game.LastResolvedAction);
            if (index >= 0) activeSlot = index;
            string changes = "";
            if (game.Health != health) changes += "你生命 " + Signed(game.Health - health) + "   ";
            if (game.EnemyHealth != enemyHealth) changes += "敌人生命 " + Signed(game.EnemyHealth - enemyHealth) + "   ";
            if (game.Shield != shield) changes += "你护盾 " + Signed(game.Shield - shield) + "   ";
            if (game.EnemyShield != enemyShield) changes += "敌人护盾 " + Signed(game.EnemyShield - enemyShield);
            actionFeedback = game.LastAction + "\n" + (changes.Length > 0 ? changes : "牌序数字继续累积；效果影响之后的行动。");
            feedbackTime = Time.unscaledTime;
            forecastDirty = true;
        }
        private static string Signed(int number) => number > 0 ? "+" + number : number.ToString();
        private string ActionDirection()
        {
            var action = game.LastResolvedAction;
            if (action == null) return "即将从左侧开始执行";
            if (action.Rule != null) return "牌序触发 → 立即执行效果";
            if (action.IsNumberOnly) return "新数字 → 再次检查牌序";
            if (action.Effect == CardEffect.Strike) return action.Enemy ? "敌人 → 攻击 → 你" : "你 → 攻击 → 敌人";
            return action.Effect == CardEffect.Guard ? (action.Enemy ? "敌人" : "你") + " → 获得护盾" : "你 → 恢复生命";
        }
        private string EffectText(CardEffect effect, int value) => effect == CardEffect.Strike ? "伤害 " + value : effect == CardEffect.Guard ? "护盾 +" + value : "治疗 +" + value;
        private Color EffectColor(CardEffect effect) => effect == CardEffect.Strike ? coral : effect == CardEffect.Guard ? teal : gold;
        private void Border(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
        private void HealthBar(Rect rect, int health, int maxHealth, Color color)
        {
            float age = Time.unscaledTime - impactStarted;
            bool damaged = rect.x < 500 ? lastPlayerDelta < 0 : lastEnemyDelta < 0;
            if (damaged && age < 0.4f) { rect.x += Mathf.Sin(age * 70) * Mathf.Exp(-age * 10) * 5; color = Color.Lerp(Color.white, color, Mathf.Clamp01(age * 5)); }
            Fill(rect, new Color(0.17f, 0.18f, 0.21f)); Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01((float)health / maxHealth), rect.height), color);
        }
        private void NumberBadge(Rect rect, int number, Color color)
        {
            var matrix = GUI.matrix;
            float age = Time.unscaledTime - impactStarted;
            float bounce = age < 0.5f ? Mathf.Sin(age * 25) * Mathf.Exp(-age * 8) * 0.18f : 0;
            GUIUtility.ScaleAroundPivot(Vector2.one * (1 + bounce), rect.center);
            Fill(rect, new Color(0.16f, 0.18f, 0.22f)); Border(rect, color, 1); Text(rect, number.ToString(), bigNumber);
            GUI.matrix = matrix;
        }

        private void DrawReward()
        {
            Fill(new Rect(24, 194, 1392, 624), panel);
            Text(new Rect(60, 236, 1200, 52), "收容成功 / 选择生成卡牌", title);
            Text(new Rect(60, 308, 1200, 38), "金币已结算。以下卡牌实时生成，选择一张加入当前牌组。", body);
            for (int i = 0; i < game.Rewards.Count; i++)
            {
                var card = game.Rewards[i]; float x = 60 + i * 445;
                DrawRewardPoker(new Rect(x + 115, 352, 180, 232), card, i);
                Text(new Rect(x + 28, 590, 350, 28), card.Description, body);
                if (Btn(new Rect(x + 28, 623, 354, 48), "加入牌组", teal)) { game.TakeReward(i); break; }
            }
        }

        private void DrawService()
        {
            bool shop = game.Phase == GamePhase.Shop;
            Fill(new Rect(24, 194, 1392, 624), panel);
            Text(new Rect(44, 210, 940, 35), shop ? "档案交易 / 选择牌组中的卡牌" : "维护装置 / 每场战斗之间可执行一次维护", heading);
            if (Btn(new Rect(1200, 208, 190, 40), "返回任务选择", gold)) { game.LeaveService(); return; }
            if (shop)
            {
                Text(new Rect(44, 267, 300, 35), "定向数字生成 / 15 金币", body);
                for (int i = 1; i <= 6; i++)
                    if (Btn(new Rect(350 + (i - 1) * 65, 262, 55, 38), i.ToString(), teal, game.Coins >= 15)) game.BuyCard(i);
                for (int i = 0; i < 3; i++)
                    if (Btn(new Rect(44 + i * 445, 316, 420, 48), "替换牌序 " + (i + 1) + " 效果 / 18 金币", gold, game.Coins >= 18)) game.BuyRule(i);
            }
            else
            {
                if (Btn(new Rect(44, 267, 325, 46), "恢复 24 生命", teal, !game.ServiceUsed)) game.RestHeal();
                if (Btn(new Rect(390, 267, 325, 46), "激发 / 生成随机卡牌", gold, !game.ServiceUsed)) game.RestGenerate();
                for (int i = 0; i < 3; i++)
                    if (Btn(new Rect(44 + i * 445, 330, 420, 40), "重构牌序 " + (i + 1), teal, !game.ServiceUsed)) game.RestRebuild(i);
            }
            deckScroll = GUI.BeginScrollView(new Rect(44, 398, 1346, 394), deckScroll, new Rect(0, 0, 1320, game.Deck.Count * 56));
            for (int i = 0; i < game.Deck.Count; i++)
            {
                var card = game.Deck[i]; float y = i * 56;
                Text(new Rect(8, y + 8, 690, 40), (i + 1).ToString("00") + " / 数字 " + card.Number + "   " + card.Description, body);
                if (shop)
                {
                    if (Btn(new Rect(760, y + 4, 230, 40), "升级 +2 / 12 金币", gold, game.Coins >= 12)) game.BuyUpgrade(card, false);
                    if (Btn(new Rect(1008, y + 4, 278, 40), card.Engraved ? "已铭刻" : "铭刻 / 20 金币", teal, !card.Engraved && game.Coins >= 20)) game.BuyUpgrade(card, true);
                }
                else if (Btn(new Rect(1008, y + 4, 278, 40), "删除这张牌", coral, !game.ServiceUsed && game.Deck.Count > 6)) { game.RestRemove(card); break; }
            }
            GUI.EndScrollView();
        }
        private void DrawEnd()
        {
            Fill(new Rect(24, 194, 1392, 624), panel);
            Text(new Rect(75, 315, 1270, 65), game.Phase == GamePhase.Won ? "核心实体 / 收容完成" : "装置离线 / 收容失败", title);
            Text(new Rect(75, 421, 1230, 100), "完成收容 " + game.Victories + " 次 · 最终牌组 " + game.Deck.Count + " 张\n点击右上角重新开始，尝试另一种数字排列与卡牌构筑。", heading);
        }
        private void Fill(Rect rect, Color color)
        { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, pixel); GUI.color = old; }
        private void Text(Rect rect, string value, GUIStyle style) { GUI.Label(rect, value, style); }
        private bool Btn(Rect rect, string label, Color color, bool enabled = true)
        {
            bool oldEnabled = GUI.enabled; Color old = GUI.backgroundColor;
            GUI.enabled = enabled; GUI.backgroundColor = color * 0.45f;
            bool pressed = GUI.Button(rect, label, button);
            GUI.enabled = oldEnabled; GUI.backgroundColor = old; return pressed;
        }
    }
}
