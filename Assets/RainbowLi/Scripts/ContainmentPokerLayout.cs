using System;
using System.Linq;
using UnityEngine;

namespace RainbowLi
{
    public sealed partial class ContainmentPrototype
    {
        private GUIStyle pokerLabel, pokerSmall, pokerHeading;
        private readonly Color hudBlue = new Color(0.13f, 0.43f, 0.68f);
        private readonly Color hudRed = new Color(0.8f, 0.22f, 0.2f);
        private readonly Color hudGrey = new Color(0.24f, 0.28f, 0.28f);

        private void DrawPokerLayout()
        {
            if (pokerLabel == null)
            {
                pokerLabel = new GUIStyle(body) { fontSize = 20, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0), clipping = TextClipping.Clip };
                pokerSmall = new GUIStyle(pokerLabel) { fontSize = 16 };
                pokerHeading = new GUIStyle(pokerLabel) { fontSize = 28, fontStyle = FontStyle.Bold };
            }
            Matrix4x4 saved = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1440 * scale) / 2, (Screen.height - 900 * scale) / 2), Quaternion.identity, new Vector3(scale, scale, 1));
            cardPointer = GUI.matrix.inverse.MultiplyPoint3x4(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
            {
                Vector2 shake = AttackShake();
                GUI.matrix = GUI.matrix * Matrix4x4.Translate(new Vector3(shake.x, shake.y, 0));
            }
            PreparePoker();
            Fill(new Rect(0, 0, 1440, 900), new Color(0.055f, 0.24f, 0.16f));
            for (int y = 0; y < 900; y += 4) Fill(new Rect(300, y, 1140, 1), new Color(0.1f, 0.34f, 0.22f, 0.18f));
            for (int x = 316; x < 1440; x += 48) Fill(new Rect(x, 0, 1, 900), new Color(0, 0, 0, 0.025f));
            DrawPokerHud();
            if (game.Phase == GamePhase.Planning || game.Phase == GamePhase.Resolving) DrawPokerBattle();
            else if (game.Phase == GamePhase.Map) DrawPokerMap();
            else if (game.Phase == GamePhase.Reward) DrawPokerReward();
            else if (game.Phase == GamePhase.Rest || game.Phase == GamePhase.Shop) DrawPokerService();
            else DrawPokerEnd();
            DrawAttackEffects();
            DrawFlyingCards();
            GUI.matrix = saved;
        }
        private void DrawPokerHud()
        {
            Fill(new Rect(0, 0, 302, 900), new Color(0.14f, 0.18f, 0.17f));
            Fill(new Rect(294, 0, 8, 900), new Color(0.045f, 0.065f, 0.05f));
            ChunkyPanel(new Rect(17, 18, 264, 64), hudGrey);
            ShadowLabel(new Rect(20, 26, 256, 44), "魔法封存局", pokerHeading);
            bool battle = game.Phase == GamePhase.Planning || game.Phase == GamePhase.Resolving;
            ChunkyPanel(new Rect(17, 98, 264, 166), battle && game.Boss ? new Color(0.43f, 0.2f, 0.38f) : hudGrey);
            ShadowLabel(new Rect(25, 108, 248, 32), battle ? game.EnemyName : "收容任务", pokerLabel);
            ShadowLabel(new Rect(25, 146, 248, 25), battle ? "敌人生命 / 护盾 " + game.EnemyShield : "完成普通收容后挑战核心", pokerSmall);
            PixelDigits(new Rect(36, 177, 230, 42), battle ? game.EnemyHealth.ToString() : "2", 5, coral);
            if (battle) PokerHealthBar(new Rect(29, 230, 240, 24), game.EnemyHealth, game.EnemyMaxHealth, hudRed);
            ChunkyPanel(new Rect(17, 282, 264, 57), new Color(0.12f, 0.16f, 0.15f));
            ShadowLabel(new Rect(23, 292, 252, 35), "回合 " + game.Round + " / " + PhaseName(), pokerSmall);
            ChunkyPanel(new Rect(17, 355, 264, 120), hudGrey);
            ShadowLabel(new Rect(27, 363, 244, 26), "玩家生命 / 护盾", pokerSmall);
            ChunkyPanel(new Rect(29, 395, 112, 43), hudBlue);
            ChunkyPanel(new Rect(158, 395, 111, 43), new Color(0.3f, 0.4f, 0.5f));
            PixelDigits(new Rect(32, 401, 105, 32), game.Health.ToString(), 4, Color.white);
            PixelDigits(new Rect(162, 401, 104, 32), game.Shield.ToString(), 4, Color.white);
            PokerHealthBar(new Rect(29, 443, 240, 24), game.Health, ContainmentGame.MaxHealth, teal);
            ChunkyPanel(new Rect(17, 493, 264, 123), new Color(0.12f, 0.16f, 0.15f));
            ShadowLabel(new Rect(24, 501, 250, 25), game.PlayerActionCount + " / 4 行动", pokerLabel);
            ShadowLabel(new Rect(24, 535, 250, 27), "收容 " + game.Victories + "    牌组 " + game.Deck.Count, pokerSmall);
            PixelDigits(new Rect(23, 576, 250, 27), "$" + game.Coins, 3, gold);
            if (battle && game.Phase == GamePhase.Planning)
            {
                if (forecastDirty || forecast == null) { forecast = game.Forecast(); forecastDirty = false; }
                ChunkyPanel(new Rect(17, 635, 264, 92), hudGrey);
                ShadowLabel(new Rect(23, 641, 250, 25), "本回合预计结果", pokerSmall);
                ShadowLabel(new Rect(23, 674, 250, 32), "你 " + forecast.Health + "    敌人 " + forecast.EnemyHealth, pokerLabel);
            }
            else
            {
                ChunkyPanel(new Rect(17, 635, 264, 92), hudGrey);
                ShadowLabel(new Rect(23, 646, 250, 61), battle ? game.LastAction : "调整牌序\n选择你的下一个任务", pokerSmall);
            }
            if (PokerButton(new Rect(21, 752, 258, 48), "新的一局", hudRed))
            {
                game = new ContainmentGame(); selectedCard = null; forecast = null;
                forecastDirty = true; activeSlot = -1; deckScroll = Vector2.zero; ResetCardEffects(); attacks.Clear();
            }
            ShadowLabel(new Rect(20, 823, 262, 45), "RAINBOWLI\n数字牌序 · 收容战斗", pokerSmall);
        }
        private void PokerHealthBar(Rect rect, int current, int maximum, Color color)
        {
            Fill(rect, new Color(0.035f, 0.055f, 0.045f));
            var inner = new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6);
            Fill(inner, new Color(0.16f, 0.19f, 0.18f));
            float ratio = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0;
            float flash = AttackFlash(rect.y < 300);
            color = Color.Lerp(color, Color.white, flash);
            Fill(new Rect(inner.x, inner.y, inner.width * ratio, inner.height), color);
            if (ratio > 0) Fill(new Rect(inner.x, inner.y, inner.width * ratio, 2), Color.Lerp(color, Color.white, 0.3f));
            if (flash > 0) Border(rect, new Color(1, 1, 1, flash), 2);
            ShadowLabel(rect, Mathf.Max(0, current) + " / " + maximum, pokerSmall);
        }
        private void DrawPokerRules(bool editable)
        {
            for (int i = 0; i < game.PlayerRules.Count; i++)
            {
                var rule = game.PlayerRules[i]; var rect = new Rect(353 + i * 155, 40, 140, 170);
                Rounded(new Rect(rect.x + 6, rect.y + 8, rect.width, rect.height), new Color(0, 0, 0, 0.35f));
                Rounded(rect, new Color(0.8f, 0.84f, 0.68f));
                Border(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, 105), new Color(0.18f, 0.38f, 0.27f), 3);
                for (int j = 0; j < 3; j++)
                {
                    var digit = new Rect(rect.x + 13 + j * 38, rect.y + 43, 34, 44);
                    if (editable && PokerButton(digit, rule.Numbers[j].ToString(), hudBlue)) game.ChangeNumber(i, j);
                    else PixelDigits(digit, rule.Numbers[j].ToString(), 4, new Color(0.11f, 0.23f, 0.17f));
                }
                cardText.normal.textColor = new Color(0.16f, 0.23f, 0.17f); cardText.fontSize = 14;
                FittedLabel(new Rect(rect.x + 8, rect.y + 115, 124, 47), rule.Description, cardText);
                int hits = !editable && forecast != null && forecast.RuleHits.ContainsKey(rule.Name) ? forecast.RuleHits[rule.Name] : 0;
                if (game.Phase == GamePhase.Resolving) hits = rule.Triggers;
                if (hits > 0) Border(rect, gold, 3);
                ShadowLabel(new Rect(rect.x - 5, 217, 150, 27), "牌序 " + (i + 1) + (hits > 0 ? " · " + hits + " 次" : ""), pokerSmall);
            }
            if (editable)
                ShadowLabel(new Rect(871, 66, 459, 139), "点击牌序卡上的数字进行修改\n每三个连续数字匹配一次牌序\n场地 → 玩家 → 敌方", pokerLabel);
            else
            {
                var arena = game.ArenaRules[game.ArenaIndex];
                ChunkyPanel(new Rect(854, 52, 184, 143), new Color(0.15f, 0.3f, 0.36f));
                ShadowLabel(new Rect(859, 65, 174, 24), "场地牌序", pokerSmall);
                ShadowLabel(new Rect(859, 101, 174, 35), arena.Pattern, pokerLabel);
                ShadowLabel(new Rect(859, 142, 174, 43), arena.Description + "\n最后一张的所属方触发", pokerSmall);
                ChunkyPanel(new Rect(1073, 52, 304, 143), hudGrey);
                ShadowLabel(new Rect(1080, 63, 290, 27), "敌方意图", pokerLabel);
                ShadowLabel(new Rect(1080, 101, 290, 43), string.Join(" / ", game.Timeline.Where(a => a.Enemy).Select(a => EffectText(a.Effect, a.Value)).ToArray()), pokerSmall);
                ShadowLabel(new Rect(1080, 151, 290, 27), "敌方牌序 " + game.EnemyRule.Pattern, pokerSmall);
            }
        }
        private static Rect PokerActionRect(int index) => new Rect(376 + index * 135, 335, 94, 145);
        private void DrawPokerBattle()
        {
            bool planning = game.Phase == GamePhase.Planning;
            DrawPokerRules(false);
            ShadowLabel(new Rect(352, 261, 1040, 33), planning ? "安排行动  /  从左向右执行" : ActionDirection(), pokerHeading);
            for (int i = 0; i < game.Timeline.Count; i++)
            {
                var action = game.Timeline[i]; var rect = PokerActionRect(i);
                ShadowLabel(new Rect(rect.x - 4, 300, 103, 26), (i + 1) + " / " + (action.Enemy ? "敌人" : "你"), pokerSmall);
                DrawActionPoker(rect, action, i, !planning && activeSlot == i);
                ShadowLabel(new Rect(rect.x - 15, 487, 124, 28), EffectText(action.Effect, action.Card == null ? action.Value : action.Card.Value), pokerSmall);
                if (planning)
                {
                    if (PokerButton(new Rect(rect.x, 520, 28, 27), "<", hudGrey, i > 0)) { game.MoveAction(i, -1); forecastDirty = true; }
                    if (PokerButton(new Rect(rect.x + 33, 520, 28, 27), ">", hudGrey, i < game.Timeline.Count - 1)) { game.MoveAction(i, 1); forecastDirty = true; }
                    if (PokerButton(new Rect(rect.x + 66, 520, 28, 27), "×", hudRed, !action.Enemy)) { game.RemoveAction(i); forecastDirty = true; break; }
                }
            }
            for (int i = 0; i <= game.Timeline.Count; i++)
                if (PokerButton(new Rect(344 + i * 135, 390, 26, 42), "+", gold, planning && selectedCard != null && game.PlayerActionCount < 4))
                { var card = selectedCard; if (game.InsertCard(card, i)) FlyCardToSlot(card, i); selectedCard = null; forecastDirty = true; break; }
            for (int i = 0; i < game.Hand.Count; i++)
            {
                Card card = game.Hand[i]; bool placed = game.Timeline.Any(a => a.Card == card);
                var rect = new Rect(409 + i * 133, 591 + Mathf.Abs(i - 2.5f) * 3, 115, 163);
                if (DrawHandPoker(rect, card, i, placed, selectedCard == card, planning && !placed && game.PlayerActionCount < 4))
                { selectedCard = selectedCard == card ? null : card; KickCard(card); }
                if (!placed) ShadowLabel(new Rect(rect.x - 4, 769, 123, 25), EffectText(card.Effect, card.Value), pokerSmall);
            }
            DrawDeckBack(new Rect(1301, 634, 96, 134));
            ShadowLabel(new Rect(1290, 787, 113, 28), game.Deck.Count + " 张", pokerSmall);
            if (planning)
            {
                if (PokerButton(new Rect(409, 820, 241, 54), "开始执行", hudBlue, game.PlayerActionCount > 0))
                { game.Confirm(); selectedCard = null; activeSlot = -1; nextStep = Time.unscaledTime + 0.6f; }
                ShadowLabel(new Rect(670, 821, 579, 54), selectedCard == null ? "选择手牌  →  点 + 放置" : "选中 " + selectedCard.Number + " · " + EffectText(selectedCard.Effect, selectedCard.Value), pokerLabel);
            }
            else
            {
                if (PokerButton(new Rect(409, 820, 188, 54), autoPlay ? "暂停" : "继续", hudBlue)) autoPlay = !autoPlay;
                if (PokerButton(new Rect(613, 820, 188, 54), "下一步", hudRed, !autoPlay)) ResolveStep();
                ShadowLabel(new Rect(818, 809, 472, 74), actionFeedback, pokerSmall);
            }
        }
        private void DrawPokerMap()
        {
            DrawPokerRules(true);
            ShadowLabel(new Rect(385, 276, 925, 46), "选择下一个收容任务", pokerHeading);
            if (PokerButton(new Rect(410, 370, 390, 106), "普通收容\n25 金币 + 新卡牌", hudBlue)) game.StartBattle();
            if (PokerButton(new Rect(842, 370, 390, 106), "休息\n恢复 / 激发 / 删牌 / 重构", new Color(0.31f, 0.49f, 0.28f), game.RestAvailable)) game.OpenService(false);
            if (PokerButton(new Rect(410, 522, 390, 106), "商店\n升级 / 铭刻 / 卡牌", new Color(0.65f, 0.4f, 0.17f))) game.OpenService(true);
            if (PokerButton(new Rect(842, 522, 390, 106), game.Victories >= 2 ? "核心收容\nBOSS 已解锁" : "核心收容\n需完成两次普通战斗", hudRed, game.Victories >= 2)) game.StartBattle(true);
            ShadowLabel(new Rect(410, 705, 824, 92), "先构建牌序，再选择任务\n把你的卡牌插入敌方行动之间，完成数字连锁", pokerLabel);
        }
        private void DrawPokerReward()
        {
            ChunkyPanel(new Rect(340, 110, 1060, 680), hudGrey);
            ShadowLabel(new Rect(370, 140, 1000, 48), "收容成功 / 选择一张卡牌", pokerHeading);
            ShadowLabel(new Rect(370, 205, 1000, 32), "金币已结算。选择一张新卡牌加入当前牌组。", pokerLabel);
            for (int i = 0; i < game.Rewards.Count; i++)
            {
                var card = game.Rewards[i]; float x = 376 + i * 330;
                DrawRewardPoker(new Rect(x + 69, 292, 160, 226), card, i);
                ShadowLabel(new Rect(x, 547, 298, 45), card.Description, pokerLabel);
                if (PokerButton(new Rect(x, 613, 298, 54), "加入牌组", hudBlue))
                { game.TakeReward(i); return; }
            }
        }
        private void DrawPokerService()
        {
            bool shop = game.Phase == GamePhase.Shop;
            ChunkyPanel(new Rect(340, 32, 1060, 835), hudGrey);
            ShadowLabel(new Rect(365, 54, 720, 44), shop ? "档案交易 / 构筑牌组" : "维护装置 / 选择一次维护", pokerHeading);
            if (PokerButton(new Rect(1138, 54, 234, 44), "返回任务选择", hudBlue))
            { game.LeaveService(); return; }
            if (shop)
            {
                ShadowLabel(new Rect(366, 123, 380, 42), "定向数字生成 / 15 金币", pokerLabel);
                for (int i = 1; i <= 6; i++)
                    if (PokerButton(new Rect(777 + (i - 1) * 96, 123, 82, 42), i.ToString(), hudBlue, game.Coins >= 15)) game.BuyCard(i);
                for (int i = 0; i < 3; i++)
                    if (PokerButton(new Rect(366 + i * 336, 193, 322, 54), "替换牌序 " + (i + 1) + " 效果 / 18 金币", new Color(0.55f, 0.4f, 0.16f), game.Coins >= 18)) game.BuyRule(i);
            }
            else
            {
                if (PokerButton(new Rect(366, 123, 490, 46), "恢复 24 生命", hudBlue, !game.ServiceUsed)) game.RestHeal();
                if (PokerButton(new Rect(878, 123, 490, 46), "激发 / 生成随机卡牌", hudBlue, !game.ServiceUsed)) game.RestGenerate();
                for (int i = 0; i < 3; i++)
                    if (PokerButton(new Rect(366 + i * 336, 193, 322, 54), "重构牌序 " + (i + 1), hudBlue, !game.ServiceUsed)) game.RestRebuild(i);
            }
            ShadowLabel(new Rect(366, 264, 1002, 30), shop ? "选择要升级或铭刻的卡牌" : game.ServiceUsed ? "本次维护已完成，可返回任务选择" : "也可以删除一张牌（牌组至少保留 6 张）", pokerSmall);
            // Scroll content uses local coordinates; no rotated cards or nested matrix scaling here.
            deckScroll = GUI.BeginScrollView(new Rect(366, 310, 1002, 524), deckScroll, new Rect(0, 0, 980, game.Deck.Count * 62));
            var tablePointer = cardPointer;
            cardPointer = Event.current.mousePosition;
            for (int i = 0; i < game.Deck.Count; i++)
            {
                var card = game.Deck[i]; float y = i * 62;
                Fill(new Rect(0, y, 980, 56), new Color(0.12f, 0.18f, 0.15f));
                ShadowLabel(new Rect(8, y + 6, 500, 44), (i + 1).ToString("00") + " / 数字 " + card.Number + "   " + card.Description, pokerSmall);
                if (shop)
                {
                    if (PokerButton(new Rect(525, y + 7, 200, 42), "升级 +2 / 12 金币", hudBlue, game.Coins >= 12)) game.BuyUpgrade(card, false);
                    if (PokerButton(new Rect(740, y + 7, 225, 42), card.Engraved ? "已铭刻" : "铭刻 / 20 金币", hudBlue, !card.Engraved && game.Coins >= 20)) game.BuyUpgrade(card, true);
                }
                else if (PokerButton(new Rect(740, y + 7, 225, 42), "删除这张牌", hudRed, !game.ServiceUsed && game.Deck.Count > 6))
                { game.RestRemove(card); break; }
            }
            cardPointer = tablePointer;
            GUI.EndScrollView();
        }
        private void DrawPokerEnd()
        {
            ChunkyPanel(new Rect(340, 160, 1060, 520), hudGrey);
            ShadowLabel(new Rect(380, 225, 980, 65), game.Phase == GamePhase.Won ? "核心实体 / 收容完成" : "装置离线 / 收容失败", pokerHeading);
            ShadowLabel(new Rect(380, 355, 980, 140), "完成收容 " + game.Victories + " 次 · 最终牌组 " + game.Deck.Count + " 张\n点击左侧“新的一局”，尝试另一种数字排列与卡牌构筑。", pokerLabel);
        }
        private void ChunkyPanel(Rect rect, Color color)
        {
            Fill(new Rect(rect.x + 4, rect.y + 6, rect.width, rect.height), new Color(0, 0, 0, 0.34f));
            Fill(rect, new Color(0.075f, 0.1f, 0.09f));
            Fill(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8), color);
            Fill(new Rect(rect.x + 6, rect.y + 4, rect.width - 12, 3), Color.Lerp(color, Color.white, 0.16f));
        }
        private bool PokerButton(Rect rect, string text, Color color, bool enabled = true)
        {
            bool hovered = enabled && rect.Contains(cardPointer);
            ChunkyPanel(rect, enabled ? Color.Lerp(color, Color.white, hovered ? 0.12f : 0) : new Color(0.2f, 0.24f, 0.22f));
            bool old = GUI.enabled; GUI.enabled = enabled;
            bool pressed = GUI.Button(rect, GUIContent.none, GUIStyle.none); GUI.enabled = old;
            ShadowLabel(new Rect(rect.x + 3, rect.y + 2, rect.width - 6, rect.height - 4), text, rect.width < 50 ? pokerSmall : pokerLabel);
            return pressed;
        }
        private void ShadowLabel(Rect rect, string text, GUIStyle style)
        {
            var fitted = FitTextStyle(new Rect(rect.x, rect.y, rect.width - 1, rect.height - 1), text, style);
            Color old = GUI.color; GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width - 1, rect.height - 1), text, fitted);
            GUI.color = old;
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 1, rect.height - 1), text, fitted);
        }
        private void FittedLabel(Rect rect, string text, GUIStyle style)
        { GUI.Label(rect, text, FitTextStyle(rect, text, style)); }
        private GUIStyle FitTextStyle(Rect rect, string text, GUIStyle source)
        {
            var result = new GUIStyle(source) { padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0), fixedWidth = 0, fixedHeight = 0, clipping = TextClipping.Clip };
            var content = new GUIContent(text ?? "");
            bool explicitLines = (text ?? "").Contains("\n");
            result.wordWrap = explicitLines || rect.height >= source.fontSize * 2.6f;
            int originalSize = source.fontSize > 0 ? source.fontSize : 16;
            for (int size = originalSize; size >= 10; size--)
            {
                result.fontSize = size;
                if (result.CalcHeight(content, Mathf.Max(1, rect.width)) <= rect.height &&
                    (result.wordWrap || result.CalcSize(content).x <= rect.width)) return result;
            }
            result.fontSize = 10;
            return result;
        }
        private void DrawDeckBack(Rect rect)
        {
            for (int i = 3; i >= 0; i--) Rounded(new Rect(rect.x + i * 3, rect.y + i * 3, rect.width, rect.height), new Color(0.95f, 0.91f, 0.79f));
            Fill(new Rect(rect.x + 6, rect.y + 6, rect.width - 12, rect.height - 12), hudRed);
            for (int y = 12; y < rect.height - 12; y += 8)
                for (int x = 12; x < rect.width - 12; x += 8)
                    Fill(new Rect(rect.x + x, rect.y + y, 3, 3), new Color(0.98f, 0.8f, 0.68f));
            Border(new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20), new Color(0.98f, 0.85f, 0.73f), 2);
        }
        private void PixelDigits(Rect rect, string value, int unit, Color color)
        {
            string[] glyphs = { "11111100011000110001100011000111111", "00100011000010000100001000010001110", "11111000010000111111100001000011111", "11111000010000101111000010000111111", "10001100011000111111000010000100001", "11111100001000011111000010000111111", "11111100001000011111100011000111111", "11111000010001000100010000100001000", "11111100011000111111100011000111111", "11111100011000111111000010000111111" };
            float start = rect.center.x - value.Length * unit * 6 / 2f;
            for (int c = 0; c < value.Length; c++)
            {
                if (value[c] < '0' || value[c] > '9') { ShadowLabel(new Rect(start + c * unit * 6, rect.y, unit * 5, unit * 7), value[c].ToString(), pokerSmall); continue; }
                string glyph = glyphs[value[c] - '0'];
                for (int j = 0; j < 35; j++) if (glyph[j] == '1')
                {
                    var dot = new Rect(start + c * unit * 6 + j % 5 * unit, rect.y + j / 5 * unit, unit, unit);
                    Fill(new Rect(dot.x + 2, dot.y + 3, unit, unit), new Color(0, 0, 0, 0.5f)); Fill(dot, color);
                }
            }
        }
    }
}
