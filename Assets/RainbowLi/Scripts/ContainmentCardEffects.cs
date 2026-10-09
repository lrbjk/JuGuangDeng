using System.Collections.Generic;
using UnityEngine;

namespace RainbowLi
{
    public sealed partial class ContainmentPrototype
    {
        private sealed class CardMotion
        {
            public float Lift, Velocity, Target;
            public Rect Home;
        }
        private sealed class FlyingCard
        {
            public Card Card;
            public Rect From, To;
            public float Started;
        }
        private sealed class FloatingText
        {
            public string Text;
            public Vector2 Position;
            public Color Color;
            public float Started;
        }
        private sealed class AttackImpact
        {
            public BattleAction Action;
            public Vector2 From, Target;
            public int Slot, Damage, Blocked, Combo;
            public float Gain;
            public bool ToEnemy;
            public float Started, Strength, Duration;
        }
        private readonly List<AttackImpact> attacks = new List<AttackImpact>();
        private AudioClip[] attackSounds;
        private int effectCombo;
        private bool comboEnemy;
        private float comboStarted = -10;
        private Vector2 comboOrigin;
        private readonly Dictionary<Card, CardMotion> motions = new Dictionary<Card, CardMotion>();
        private readonly List<FlyingCard> flights = new List<FlyingCard>();
        private readonly List<FloatingText> floatingTexts = new List<FloatingText>();
        private Texture2D roundedCard;
        private Texture2D[] suitTextures;
        private GUIStyle cardText, cardCorner, cardPip, cardCaption;
        private Vector2 cardPointer;
        private float dealStarted, impactStarted = -10;
        private int impactSlot = -1;
        private bool impactIsRule;
        private AudioSource cardAudio;
        private AudioClip pickSound, placeSound, scoreSound;

        private void PreparePoker()
        {
            if (roundedCard != null) return;
            roundedCard = new Texture2D(64, 80, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 80; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = Mathf.Max(6 - x, x - 57, 0), dy = Mathf.Max(6 - y, y - 73, 0);
                    float alpha = Mathf.Clamp01(6.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    roundedCard.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            roundedCard.Apply();
            suitTextures = new Texture2D[4];
            for (int suit = 0; suit < 4; suit++)
            {
                var texture = new Texture2D(24, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                for (int y = 0; y < 32; y++) for (int x = 0; x < 24; x++)
                {
                    float px = (x - 11.5f) / 11.5f, py = (y - 15.5f) / 15.5f;
                    bool inside;
                    if (suit == 0) inside = Mathf.Abs(px) + Mathf.Abs(py) * 0.75f < 0.74f;
                    else if (suit == 1 || suit == 2)
                    {
                        float sy = suit == 1 ? -py : py;
                        float a = px * px + sy * sy - 0.75f;
                        inside = a * a * a - px * px * sy * sy * sy < 0;
                        if (suit == 1) inside |= py < -0.35f && Mathf.Abs(px) < 0.12f;
                    }
                    else inside = px * px + (py - 0.42f) * (py - 0.42f) < 0.16f ||
                        (px - 0.4f) * (px - 0.4f) + py * py < 0.16f ||
                        (px + 0.4f) * (px + 0.4f) + py * py < 0.16f || py < -0.1f && Mathf.Abs(px) < 0.12f;
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
                texture.Apply(); suitTextures[suit] = texture;
            }
            cardText = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold };
            cardCorner = new GUIStyle(cardText) { alignment = TextAnchor.UpperLeft, fontSize = 18 };
            cardPip = new GUIStyle(cardText) { fontSize = 26 };
            cardCaption = new GUIStyle(cardText) { fontSize = 12, fontStyle = FontStyle.Normal };
        }
        private void DisposeCardEffects()
        {
            if (roundedCard != null) Destroy(roundedCard);
            if (suitTextures != null) foreach (var texture in suitTextures) if (texture != null) Destroy(texture);
            if (pickSound != null) Destroy(pickSound);
            if (placeSound != null) Destroy(placeSound);
            if (scoreSound != null) Destroy(scoreSound);
            if (attackSounds != null) foreach (var clip in attackSounds) if (clip != null) Destroy(clip);
        }
        private void ResetCardEffects()
        { motions.Clear(); flights.Clear(); floatingTexts.Clear(); effectCombo = 0; comboStarted = -10; impactSlot = -1; impactStarted = -10; }
        private void BeginCardDeal()
        { ResetCardEffects(); dealStarted = Time.unscaledTime; }
        private void UpdateCardEffects()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            foreach (var motion in motions.Values)
            {
                if (game.Phase != GamePhase.Planning) motion.Target = 0;
                // Damped spring: stable at varying frame rates, with a small overshoot.
                motion.Velocity += ((motion.Target - motion.Lift) * 180 - motion.Velocity * 19) * dt;
                motion.Lift += motion.Velocity * dt;
            }
            attacks.RemoveAll(a => Time.unscaledTime - a.Started > a.Duration);
            flights.RemoveAll(f => Time.unscaledTime - f.Started > 0.42f);
            floatingTexts.RemoveAll(f => Time.unscaledTime - f.Started > 1.1f);
        }
        private void DrawCardTable()
        {
            // Subtle felt lines and warm table edge, drawn without external assets.
            Fill(new Rect(0, 98, 1440, 798), new Color(0.045f, 0.115f, 0.095f));
            for (int y = 100; y < 900; y += 5)
                Fill(new Rect(0, y, 1440, 1), new Color(0.2f, 0.38f, 0.28f, 0.045f));
            Border(new Rect(12, 102, 1416, 790), new Color(0.44f, 0.3f, 0.17f), 3);
        }
        private CardMotion Motion(Card card)
        {
            if (!motions.TryGetValue(card, out var motion)) { motion = new CardMotion(); motions.Add(card, motion); }
            return motion;
        }
        private void KickCard(Card card) { Motion(card).Velocity = 130; PlayCardSound(0); }
        private bool DrawHandPoker(Rect rect, Card card, int index, bool placed, bool selected, bool enabled)
        {
            PreparePoker();
            var motion = Motion(card); motion.Home = rect;
            bool hover = enabled && rect.Contains(cardPointer);
            motion.Target = placed ? -5 : selected ? 20 : hover ? 14 : 0;
            bool oldEnabled = GUI.enabled; GUI.enabled = enabled;
            // Keep the hit box steady while the visual spring moves the card.
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none); GUI.enabled = oldEnabled;
            float deal = Mathf.Clamp01((Time.unscaledTime - dealStarted - index * 0.07f) / 0.42f);
            float eased = 1 - Mathf.Pow(1 - deal, 3);
            var visual = new Rect(rect.x + (1 - eased) * 200, rect.y + (1 - eased) * 145 - motion.Lift,
                rect.width, rect.height);
            float angle = (index - 2.5f) * 1.35f + (hover ? (cardPointer.x - rect.center.x) * 0.065f : 0);
            DrawPoker(visual, card.Number, card.Effect, card.Value, false,
                placed ? "已安排" : selected ? "选中" : card.Engraved ? "铭刻" : "选择", angle, selected, placed ? 0.5f : 1);
            return clicked;
        }
        private void DrawActionPoker(Rect rect, BattleAction action, int index, bool active)
        {
            PreparePoker();
            float age = Time.unscaledTime - impactStarted;
            float pulse = index == impactSlot && age < 0.5f ? Mathf.Sin(age * 38) * Mathf.Exp(-age * 9) : 0;
            Rect visual = new Rect(rect.x + pulse * 5, rect.y - Mathf.Abs(pulse) * 8, rect.width, rect.height);
            foreach (var attack in attacks)
            {
                float elapsed = Time.unscaledTime - attack.Started;
                if (attack.Action != action || elapsed > 0.42f) continue;
                float kick = Mathf.Sin(Mathf.Clamp01(elapsed / 0.42f) * Mathf.PI);
                Vector2 direction = (attack.Target - rect.center).normalized;
                visual.position += direction * kick * Mathf.Lerp(12, 48, attack.Strength);
                pulse += kick * attack.Strength;
            }
            DrawPoker(visual, action.Number, action.Effect, action.Card != null ? action.Card.Value : action.Value,
                action.Enemy, (index + 1) + " / " + (action.Enemy ? "敌人" : "你"), pulse * 4, active, 1);
        }
        private void DrawRewardPoker(Rect rect, Card card, int index)
        {
            PreparePoker();
            float angle = Mathf.Sin(Time.unscaledTime * 1.2f + index * 0.8f) * 1.5f;
            DrawPoker(rect, card.Number, card.Effect, card.Value, false, "新卡牌", angle, false, 1);
        }
        private void DrawPoker(Rect rect, int number, CardEffect effect, int value, bool enemy,
            string caption, float angle, bool selected, float opacity)
        {
            var savedMatrix = GUI.matrix; var savedColor = GUI.color;
            GUI.matrix = savedMatrix * PokerRotation(rect.center, angle);
            GUI.color = new Color(1, 1, 1, opacity);
            float compact = rect.height < 110 ? 0.75f : 1;
            if (selected) Rounded(new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8), gold);
            Rounded(new Rect(rect.x + 5, rect.y + 7, rect.width, rect.height), new Color(0, 0, 0, 0.38f));
            Rounded(rect, enemy ? new Color(0.96f, 0.84f, 0.78f) : new Color(0.98f, 0.95f, 0.85f));
            Color suitColor = enemy || effect == CardEffect.Strike || effect == CardEffect.Mend ? new Color(0.68f, 0.12f, 0.14f) : new Color(0.1f, 0.22f, 0.28f);
            string suit = enemy ? "♣" : effect == CardEffect.Strike ? "♦" : effect == CardEffect.Guard ? "♠" : "♥";
            cardCorner.normal.textColor = suitColor; cardPip.normal.textColor = suitColor;
            cardCorner.fontSize = compact < 1 ? 13 : 18;
            cardText.normal.textColor = new Color(0.16f, 0.16f, 0.16f); cardCaption.normal.textColor = new Color(0.28f, 0.25f, 0.23f);
            DrawPokerCorner(rect, number, suit, compact);
            var cornerMatrix = GUI.matrix;
            GUI.matrix = cornerMatrix * PokerRotation(rect.center, 180);
            DrawPokerCorner(rect, number, suit, compact);
            GUI.matrix = cornerMatrix;
            Rect pips = new Rect(rect.x + rect.width * 0.3f, rect.y + 33, rect.width * 0.4f, rect.height - 65);
            DrawPips(pips, number, suit, compact);
            GUI.matrix = savedMatrix; GUI.color = savedColor;
        }
        // Compose in table coordinates before the screen scale and letterbox offset.
        private static Matrix4x4 PokerRotation(Vector2 pivot, float angle)
        {
            Vector3 center = new Vector3(pivot.x, pivot.y, 0);
            return Matrix4x4.Translate(center) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle))
                * Matrix4x4.Translate(-center);
        }
        private void DrawPips(Rect area, int number, string suit, float compact)
        {
            cardPip.fontSize = compact < 1 ? 17 : 22;
            if (number % 2 == 1) DrawSuit(new Rect(area.center.x - 9, area.center.y - 12, 18, 24), suit);
            int pairs = number / 2;
            for (int row = 0; row < pairs; row++)
            {
                float y = pairs == 1 ? area.center.y : Mathf.Lerp(area.y + 9, area.yMax - 9, (float)row / (pairs - 1));
                DrawSuit(new Rect(area.x - 6, y - 12, 18, 24), suit);
                DrawSuit(new Rect(area.xMax - 12, y - 12, 18, 24), suit);
            }
        }
        private void DrawPokerCorner(Rect rect, int number, string suit, float compact)
        {
            Text(new Rect(rect.x + 7, rect.y + 4, 32, 24), number.ToString(), cardCorner);
            DrawSuit(new Rect(rect.x + 8, rect.y + 25 * compact, 12 * compact, 16 * compact), suit);
        }
        private void DrawSuit(Rect rect, string suit)
        {
            int index = suit == "♦" ? 0 : suit == "♠" ? 1 : suit == "♥" ? 2 : 3;
            Color old = GUI.color; GUI.color *= cardCorner.normal.textColor;
            GUI.DrawTexture(rect, suitTextures[index]); GUI.color = old;
        }
        private void Rounded(Rect rect, Color color)
        { var previous = GUI.color; GUI.color *= color; GUI.DrawTexture(rect, roundedCard); GUI.color = previous; }
        private void FlyCardToSlot(Card card, int slot)
        {
            PlayCardSound(1);
            var motion = Motion(card);
            flights.Add(new FlyingCard { Card = card, From = motion.Home,
                To = PokerActionRect(slot), Started = Time.unscaledTime });
        }
        private void DrawFlyingCards()
        {
            if (roundedCard == null) return;
            foreach (var flight in flights)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - flight.Started) / 0.42f), ease = 1 - Mathf.Pow(1 - t, 3);
                Rect rect = new Rect(Mathf.Lerp(flight.From.x, flight.To.x, ease),
                    Mathf.Lerp(flight.From.y, flight.To.y, ease) - Mathf.Sin(t * Mathf.PI) * 60,
                    Mathf.Lerp(flight.From.width, flight.To.width, ease), Mathf.Lerp(flight.From.height, flight.To.height, ease));
                DrawPoker(rect, flight.Card.Number, flight.Card.Effect, flight.Card.Value, false, "出牌", Mathf.Sin(t * Mathf.PI) * -10, true, 1 - t * 0.25f);
            }
            foreach (var floating in floatingTexts)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - floating.Started) / 1.1f);
                Color old = GUI.color; GUI.color = new Color(floating.Color.r, floating.Color.g, floating.Color.b, 1 - t);
                var rect = new Rect(floating.Position.x, floating.Position.y - t * 48, 180, 42);
                Text(rect, floating.Text, heading); GUI.color = old;
            }
        }
        private void TriggerCardImpact(BattleAction action, int playerDelta, int enemyDelta, int playerShieldDelta, int enemyShieldDelta)
        {
            if (action == null) return;
            impactStarted = Time.unscaledTime; impactIsRule = action.Rule != null;
            if (!action.IsNumberOnly)
            {
                effectCombo = effectCombo > 0 && comboEnemy == action.Enemy && Time.unscaledTime - comboStarted <= 1.25f
                    ? Mathf.Min(effectCombo + 1, 99) : 1;
                comboEnemy = action.Enemy; comboStarted = Time.unscaledTime;
                int source = game.Timeline.IndexOf(action);
                comboOrigin = source >= 0 ? PokerActionRect(source).center : new Vector2(810, 195);
            }
            PlayCardSound(2, impactIsRule ? 1.25f : 0.95f);
            int slot = game.Timeline.IndexOf(action); if (slot >= 0) impactSlot = slot;
            if (playerDelta > 0) FloatNumber(Signed(playerDelta), new Vector2(110, 408), teal);
            if (enemyDelta > 0) FloatNumber(Signed(enemyDelta), new Vector2(120, 181), coral);
            if (impactIsRule) FloatNumber("牌序触发！", new Vector2(760, 286), gold);
            int playerDamage = Mathf.Max(0, -playerDelta) + Mathf.Max(0, -playerShieldDelta);
            int enemyDamage = Mathf.Max(0, -enemyDelta) + Mathf.Max(0, -enemyShieldDelta);
            if (playerDamage > 0) StartAttackImpact(action, false, playerDamage, Mathf.Max(0, -playerShieldDelta));
            if (enemyDamage > 0) StartAttackImpact(action, true, enemyDamage, Mathf.Max(0, -enemyShieldDelta));
        }
        // Smooth monotonic response: heavy hits stay bounded during long rule chains.
        private static float AttackStrength(int damage)
        { return (float)(1 - System.Math.Exp(-System.Math.Max(0, damage) / 18.0)); }
        private static float ComboGain(int count)
        { return (float)(1 + 1.8 * (1 - System.Math.Exp(-System.Math.Max(0, count - 1) / 3.5))); }
        private void StartAttackImpact(BattleAction action, bool toEnemy, int damage, int blocked)
        {
            int slot = game.Timeline.IndexOf(action);
            int nominal = action.Rule != null && action.Rule.Effect == RuleEffect.Damage ? action.Rule.Value :
                !action.IsNumberOnly && action.Rule == null && action.Effect == CardEffect.Strike ?
                (action.Card != null ? action.Card.Value + game.Power : action.Value) : damage;
            float strength = AttackStrength(Mathf.Max(damage, nominal));
            var attack = new AttackImpact { Action = action, Slot = slot, Damage = damage, Blocked = blocked, ToEnemy = toEnemy,
                From = slot >= 0 ? PokerActionRect(slot).center : new Vector2(810, 195),
                Target = new Vector2(149, toEnemy ? 220 : 426), Started = Time.unscaledTime,
                Combo = effectCombo, Gain = ComboGain(effectCombo),
                Strength = strength, Duration = Mathf.Lerp(0.65f, 1.05f, strength) + Mathf.Min(effectCombo - 1, 6) * 0.045f };
            if (attacks.Count >= 24) attacks.RemoveAt(0);
            attacks.Add(attack);
            PlayAttackSound(Mathf.Clamp01(strength + (attack.Gain - 1) * 0.22f));
        }
        private Vector2 AttackShake()
        {
            Vector2 shake = Vector2.zero;
            foreach (var attack in attacks)
            {
                float age = Time.unscaledTime - attack.Started - 0.14f;
                if (age < 0 || age > 0.45f) continue;
                float amplitude = Mathf.Lerp(1.5f, 11, attack.Strength) * attack.Gain * Mathf.Exp(-age * 9);
                shake += new Vector2(Mathf.Sin(age * 93), Mathf.Sin(age * 71 + 1.8f)) * amplitude;
            }
            return Vector2.ClampMagnitude(shake, 28);
        }
        private float AttackFlash(bool enemy)
        {
            float flash = 0;
            foreach (var attack in attacks)
            {
                if (attack.ToEnemy != enemy) continue;
                float age = Time.unscaledTime - attack.Started - 0.14f;
                if (age >= 0 && age < 0.3f) flash = Mathf.Max(flash, (1 - age / 0.3f) * (0.4f + attack.Strength * 0.6f));
            }
            return flash;
        }
        private void DrawAttackEffects()
        {
            if (Event.current.type != EventType.Repaint) return;
            DrawComboEffects();
            foreach (var attack in attacks)
            {
                float age = Time.unscaledTime - attack.Started;
                Color tint = attack.Damage == attack.Blocked ? teal : coral;
                if (age < 0.18f)
                {
                    float travel = Mathf.Clamp01(age / 0.14f);
                    Vector2 head = Vector2.Lerp(attack.From, attack.Target, travel);
                    Vector2 tail = Vector2.Lerp(attack.From, attack.Target, Mathf.Max(0, travel - 0.28f));
                    DrawAttackLine(tail, head, Mathf.Lerp(3, 12, attack.Strength) * attack.Gain, tint);
                    DrawAttackLine(tail, head, 2, Color.white);
                }
                float impactAge = age - 0.14f;
                if (impactAge < 0) continue;
                float progress = Mathf.Clamp01(impactAge / (attack.Duration - 0.14f));
                float fade = (1 - progress) * (1 - progress);
                float radius = Mathf.Lerp(24, 100, attack.Strength) * attack.Gain * (0.25f + progress);
                Color ring = new Color(tint.r, tint.g, tint.b, fade);
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI * 2 / 16;
                    Vector2 point = attack.Target + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    Fill(new Rect(point.x - 2, point.y - 2, 4 + attack.Strength * 4, 4 + attack.Strength * 4), ring);
                }
                int particles = Mathf.Min(90, Mathf.RoundToInt((8 + attack.Strength * 36) * attack.Gain));
                for (int i = 0; i < particles; i++)
                {
                    float angle = i * 2.39996f;
                    float speed = Mathf.Lerp(45, 210, attack.Strength) * Mathf.Sqrt(attack.Gain) * (0.55f + (i % 7) / 10f);
                    Vector2 point = attack.Target + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed * impactAge;
                    point.y += 85 * impactAge * impactAge;
                    float size = (2 + attack.Strength * 6) * Mathf.Sqrt(attack.Gain) * (1 - progress * 0.6f);
                    Fill(new Rect(point.x, point.y, size, size), i % 3 == 0 ? new Color(1, 0.85f, 0.48f, fade) : ring);
                }
                float slash = Mathf.Lerp(25, 75, attack.Strength) * attack.Gain * (1 - progress * 0.4f);
                DrawAttackLine(attack.Target + new Vector2(-slash, slash * 0.65f), attack.Target + new Vector2(slash, -slash * 0.65f), 2 + attack.Strength * 6, ring);
                if (attack.Combo >= 3)
                    DrawAttackLine(attack.Target + new Vector2(-slash, -slash * 0.65f), attack.Target + new Vector2(slash, slash * 0.65f), 3 + attack.Strength * 7, ring);
                if (attack.Combo >= 5)
                    Border(new Rect(17 - progress * 10, attack.ToEnemy ? 98 : 355, 264 + progress * 20, attack.ToEnemy ? 166 : 120), new Color(gold.r, gold.g, gold.b, fade), 3 + attack.Strength * 3);
                var numberStyle = new GUIStyle(pokerHeading) { fontSize = Mathf.RoundToInt(Mathf.Lerp(24, 48, attack.Strength) * Mathf.Sqrt(attack.Gain)), fontStyle = FontStyle.Bold };
                Color old = GUI.color; GUI.color = new Color(tint.r, tint.g, tint.b, 1 - progress);
                ShadowLabel(new Rect(4, attack.Target.y - 80 - progress * 60, 290, 80),
                    attack.Damage == attack.Blocked ? "格挡 " + attack.Blocked : "伤害 " + (attack.Damage - attack.Blocked), numberStyle);
                GUI.color = old;
            }
        }
        private void DrawComboEffects()
        {
            float age = Time.unscaledTime - comboStarted;
            if (effectCombo < 2 || age > 1.1f) return;
            float gain = ComboGain(effectCombo), fade = Mathf.Clamp01((1.1f - age) / 0.5f);
            float pulse = Mathf.Sin(Mathf.Clamp01(age / 0.28f) * Mathf.PI);
            Color old = GUI.color;
            GUI.color = new Color(1, 1, 1, fade);
            var style = new GUIStyle(pokerHeading) { fontSize = Mathf.RoundToInt(30 + (gain - 1) * 18 + pulse * 12) };
            ShadowLabel(new Rect(695, 245 - pulse * 10, 640, 80),
                (comboEnemy ? "敌方" : "你的") + "效果连锁 ×" + effectCombo + (effectCombo >= 5 ? "  爆发！" : ""), style);
            GUI.color = old;
            float radius = (25 + age * 115) * gain;
            // Non-damage effects also contribute: shield, power and inserted rule effects build the burst.
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI / 12 + age * 1.5f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Color tint = new Color(gold.r, gold.g, gold.b, fade * Mathf.Exp(-age * 2.8f));
                DrawAttackLine(comboOrigin + direction * radius * 0.7f, comboOrigin + direction * radius, 2 + gain * 2, tint);
            }
        }
        private void DrawAttackLine(Vector2 from, Vector2 to, float width, Color color)
        {
            var saved = GUI.matrix;
            GUI.matrix = saved * PokerRotation(from, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
            Fill(new Rect(from.x, from.y - width / 2, Vector2.Distance(from, to), width), color);
            GUI.matrix = saved;
        }
        private void PlayAttackSound(float strength)
        {
            if (attackSounds == null)
            {
                attackSounds = new AudioClip[3];
                for (int tier = 0; tier < 3; tier++)
                {
                    const int rate = 22050;
                    float duration = 0.12f + tier * 0.06f;
                    var samples = new float[Mathf.CeilToInt(rate * duration)];
                    uint noise = 314159;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        noise = noise * 1664525u + 1013904223u;
                        float t = (float)i / rate, p = (float)i / samples.Length;
                        float hiss = ((noise >> 8) / 16777215f * 2 - 1);
                        float bass = Mathf.Sin(2 * Mathf.PI * (160 - tier * 35) * t * (1 - p * 0.45f));
                        samples[i] = (hiss * 0.35f + bass * 0.65f) * Mathf.Pow(1 - p, 3) * Mathf.Min(1, t * 1500) * 0.7f;
                    }
                    attackSounds[tier] = AudioClip.Create("Attack impact " + tier, samples.Length, 1, rate, false);
                    attackSounds[tier].SetData(samples, 0);
                }
            }
            cardAudio.pitch = Mathf.Lerp(1.15f, 0.75f, strength);
            cardAudio.PlayOneShot(attackSounds[strength < 0.3f ? 0 : strength < 0.65f ? 1 : 2], Mathf.Lerp(0.5f, 1, strength));
        }
        private void FloatNumber(string text, Vector2 position, Color color)
        { floatingTexts.Add(new FloatingText { Text = text, Position = position, Color = color, Started = Time.unscaledTime }); }
        private void DrawRuleBurst(Rect rect)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - impactStarted) / 0.7f);
            float spread = impactIsRule ? t * 12 : 0;
            Border(new Rect(rect.x - spread, rect.y - spread, rect.width + spread * 2, rect.height + spread * 2),
                new Color(gold.r, gold.g, gold.b, 1 - t), 2);
            Border(rect, gold, 2);
        }
        private void PlayCardSound(int kind, float pitch = 1)
        {
            if (cardAudio == null)
            {
                cardAudio = gameObject.AddComponent<AudioSource>();
                cardAudio.playOnAwake = false; cardAudio.spatialBlend = 0; cardAudio.volume = 0.28f;
                pickSound = CreateTone("Card pick", 520, 0.055f);
                placeSound = CreateTone("Card place", 310, 0.085f);
                scoreSound = CreateTone("Card score", 730, 0.12f);
            }
            cardAudio.pitch = pitch;
            cardAudio.PlayOneShot(kind == 0 ? pickSound : kind == 1 ? placeSound : scoreSound);
        }
        private static AudioClip CreateTone(string name, float frequency, float duration)
        {
            const int rate = 22050;
            int count = Mathf.CeilToInt(rate * duration); var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate, progress = (float)i / count;
                float envelope = Mathf.Min(1, t * 900) * Mathf.Pow(1 - progress, 3);
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * (t - t * t * 1.8f)) * envelope * 0.3f;
            }
            var clip = AudioClip.Create(name, count, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
    }
}
