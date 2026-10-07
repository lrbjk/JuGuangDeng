using System;
using System.Collections.Generic;
using System.Linq;

namespace RainbowLi
{
    public enum CardEffect { Strike, Guard, Mend }
    public enum GamePhase { Map, Planning, Resolving, Reward, Rest, Shop, Lost, Won }
    public enum RuleEffect { Damage, Shield, Power, Insert }

    [Serializable]
    public sealed class Card
    {
        public int Number;
        public CardEffect Effect;
        public int Upgrade;
        public bool Engraved;
        public int Value => Number + 3 + Upgrade * 2;
        public string Name => Effect == CardEffect.Strike ? "冲击" : Effect == CardEffect.Guard ? "屏障" : "修复";
        public string Description => Name + " " + Value + (Engraved ? " · 铭刻：牌序收尾时额外伤害 5" : "");
    }

    public sealed class SequenceRule
    {
        public string Name;
        public int[] Numbers;
        public RuleEffect Effect;
        public int Value;
        public bool InsertsTwo;
        public int Triggers;
        public string Pattern => string.Join(" → ", Numbers.Select(n => n.ToString()).ToArray());
        public string Description => Effect == RuleEffect.Damage ? "伤害 " + Value :
            Effect == RuleEffect.Shield ? "护盾 " + Value :
            Effect == RuleEffect.Power ? "后续卡效 +" + Value + (InsertsTwo ? "，插入数字 2" : "") : "插入数字 " + Value;
    }

    public sealed class BattleAction
    {
        public Card Card;
        public bool Enemy;
        public int Number;
        public int Value;
        public CardEffect Effect;
        public SequenceRule Rule;
        public bool IsNumberOnly;
        public string Label => Rule != null ? Rule.Name + "：" + Rule.Description :
            IsNumberOnly ? "插入数字 " + Number : Enemy ? "实体 " + Number + " · " + (Effect == CardEffect.Strike ? "攻击 " : "护盾 ") + Value :
            "卡牌 " + Card.Number + " · " + Card.Description;
    }

    public sealed class BattleForecast
    {
        public int Health, Shield, EnemyHealth, EnemyShield;
        public bool Victory, Defeat;
        public readonly List<int> Numbers = new List<int>();
        public readonly Dictionary<string, int> RuleHits = new Dictionary<string, int>();
    }

    // Pure C# rules. The Unity panel only submits choices and displays this state.
    public sealed class ContainmentGame
    {
        public const int MaxHealth = 70;
        public const int MaxPlayerActions = 4;
        public readonly List<Card> Deck = new List<Card>();
        public readonly List<Card> Hand = new List<Card>();
        public readonly List<BattleAction> Timeline = new List<BattleAction>();
        public readonly List<SequenceRule> PlayerRules = new List<SequenceRule>();
        public readonly List<SequenceRule> ArenaRules = new List<SequenceRule>();
        public readonly List<int> NumberStream = new List<int>();
        public readonly List<string> History = new List<string>();
        public readonly List<Card> Rewards = new List<Card>();
        public SequenceRule EnemyRule;
        public GamePhase Phase { get; private set; }
        public int Health = MaxHealth, Shield, EnemyHealth, EnemyMaxHealth, EnemyShield;
        public int Coins, Victories, Round, Power, ArenaIndex, ArenaTriggerCount;
        public bool Boss, ServiceUsed;
        public bool RestAvailable = true;
        public string EnemyName => Boss ? "失控档案 · BOSS" : "逸散实体 " + (Victories + 1);
        public string Message = "选择收容任务。可在此调整三组基础牌序。";
        public string LastAction = "等待行动";
        public BattleAction LastResolvedAction { get; private set; }
        public int PlayerActionCount => Timeline.Count(a => a.Card != null);
        private readonly Random random;
        private readonly List<Card> drawPile = new List<Card>();
        private readonly List<Card> discard = new List<Card>();
        private readonly List<BattleAction> pending = new List<BattleAction>();
        private bool rotateArena;

        public ContainmentGame(int seed = 0)
        {
            random = seed == 0 ? new Random() : new Random(seed);
            for (int group = 0; group < 3; group++)
                for (int number = 1; number <= 6; number++) Deck.Add(GenerateCard(number));
            PlayerRules.Add(Rule("基础 / 冲击", new[] { 1, 2, 3 }, RuleEffect.Damage, 10));
            PlayerRules.Add(Rule("基础 / 屏障", new[] { 3, 4, 5 }, RuleEffect.Shield, 10));
            var power = Rule("基础 / 连锁", new[] { 2, 2, 2 }, RuleEffect.Power, 2);
            power.InsertsTwo = true;
            PlayerRules.Add(power);
            Phase = GamePhase.Map;
        }

        public Card GenerateCard(int number = 0) => new Card
        { Number = number == 0 ? random.Next(1, 7) : number, Effect = (CardEffect)random.Next(0, 3) };

        public static SequenceRule Rule(string name, int[] numbers, RuleEffect effect, int value) =>
            new SequenceRule { Name = name, Numbers = numbers, Effect = effect, Value = value };

        private int[] RandomPattern() => new[] { random.Next(1, 7), random.Next(1, 7), random.Next(1, 7) };

        public void StartBattle(bool boss = false)
        {
            if (Phase != GamePhase.Map || boss && Victories < 2) return;
            Boss = boss;
            EnemyMaxHealth = boss ? 110 : 36 + Victories * 12;
            EnemyHealth = EnemyMaxHealth;
            Round = 0; ArenaIndex = 0; ArenaTriggerCount = 0; rotateArena = false;
            ArenaRules.Clear();
            for (int i = 0; i < 3; i++)
                ArenaRules.Add(Rule("场地 / " + (i + 1), RandomPattern(), (RuleEffect)random.Next(0, 3), 4 + i * 2));
            EnemyRule = Rule("实体 / 共振", boss ? new[] { 6, 6, 6 } : new[] { 6, 5, 4 }, RuleEffect.Damage, boss ? 12 : 6);
            drawPile.Clear(); discard.Clear(); Hand.Clear(); pending.Clear();
            drawPile.AddRange(Deck); Shuffle(drawPile);
            History.Clear(); Log("收容开始：" + EnemyName);
            BeginRound();
        }

        private void Shuffle(List<Card> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); Card t = cards[i]; cards[i] = cards[j]; cards[j] = t; }
        }

        private void BeginRound()
        {
            Round++; Shield = 0; EnemyShield = 0; Power = 0;
            if (rotateArena) { ArenaIndex = (ArenaIndex + 1) % 3; rotateArena = false; ArenaTriggerCount = 0; }
            foreach (var rule in PlayerRules.Concat(ArenaRules)) rule.Triggers = 0;
            EnemyRule.Triggers = 0;
            NumberStream.Clear(); Timeline.Clear(); pending.Clear();
            LastResolvedAction = null; LastAction = "等待行动";
            while (Hand.Count < 6)
            {
                if (drawPile.Count == 0)
                { drawPile.AddRange(discard); discard.Clear(); Shuffle(drawPile); }
                if (drawPile.Count == 0) break;
                var card = drawPile[drawPile.Count - 1]; drawPile.RemoveAt(drawPile.Count - 1); Hand.Add(card);
            }
            int count = Boss ? 3 : 2;
            for (int i = 0; i < count; i++)
            {
                bool guard = i == 1 && Round % 2 == 0;
                Timeline.Add(new BattleAction { Enemy = true, Number = random.Next(1, 7),
                    Effect = guard ? CardEffect.Guard : CardEffect.Strike,
                    Value = guard ? 6 : (Boss ? 9 : 5) + Round / 3 });
            }
            Phase = GamePhase.Planning;
            Message = "点击手牌追加行动，再用箭头调整敌我顺序。玩家最多 4 张；实体行动必须全部执行。";
            Log("第 " + Round + " 回合：抽取 " + Hand.Count + " 张，护盾与强化重置。");
        }

        public bool AddCard(Card card)
        {
            if (Phase != GamePhase.Planning || PlayerActionCount >= MaxPlayerActions || !Hand.Contains(card) || Timeline.Any(a => a.Card == card)) return false;
            Timeline.Add(new BattleAction { Card = card, Number = card.Number, Effect = card.Effect });
            return true;
        }
        public bool InsertCard(Card card, int index)
        {
            if (index < 0 || index > Timeline.Count || !AddCard(card)) return false;
            var action = Timeline[Timeline.Count - 1];
            Timeline.RemoveAt(Timeline.Count - 1); Timeline.Insert(index, action);
            return true;
        }
        public void MoveAction(int index, int offset)
        {
            int target = index + offset;
            if (Phase != GamePhase.Planning || index < 0 || index >= Timeline.Count || target < 0 || target >= Timeline.Count) return;
            var action = Timeline[index]; Timeline.RemoveAt(index); Timeline.Insert(target, action);
        }
        public void RemoveAction(int index)
        {
            if (Phase == GamePhase.Planning && index >= 0 && index < Timeline.Count && Timeline[index].Card != null) Timeline.RemoveAt(index);
        }
        public bool Confirm()
        {
            if (Phase != GamePhase.Planning || PlayerActionCount == 0) return false;
            pending.AddRange(Timeline); Phase = GamePhase.Resolving; Message = "行动依次执行，牌序效果立即插入。";
            return true;
        }

        public bool Step()
        {
            if (Phase != GamePhase.Resolving) return false;
            if (pending.Count == 0)
            { discard.AddRange(Hand); Hand.Clear(); BeginRound(); return false; }
            var action = pending[0]; pending.RemoveAt(0);
            LastResolvedAction = action;
            LastAction = action.Label; Log(LastAction);
            if (action.Rule != null) ApplyRule(action);
            else
            {
                if (!action.IsNumberOnly)
                {
                    int value = action.Card != null ? action.Card.Value + Power : action.Value;
                    if (action.Effect == CardEffect.Strike) Damage(!action.Enemy, value);
                    else if (action.Effect == CardEffect.Guard) { if (action.Enemy) EnemyShield += value; else Shield += value; }
                    else { if (!action.Enemy) Health = Math.Min(MaxHealth, Health + value); }
                }
                if (Health > 0 && EnemyHealth > 0)
                {
                    NumberStream.Add(action.Number);
                    bool matched = MatchSequences();
                    if (matched && action.Card != null && action.Card.Engraved)
                    { Damage(true, 5); Log("铭刻触发：额外伤害 5"); }
                }
            }
            if (Health <= 0) { Phase = GamePhase.Lost; Message = "收容失败。重新开始可生成一副新卡组。"; pending.Clear(); }
            else if (EnemyHealth <= 0) FinishBattle();
            return true;
        }

        private void ApplyRule(BattleAction action)
        {
            var rule = action.Rule;
            if (rule.Effect == RuleEffect.Damage) Damage(!action.Enemy, rule.Value);
            else if (rule.Effect == RuleEffect.Shield) { if (action.Enemy) EnemyShield += rule.Value; else Shield += rule.Value; }
            else if (rule.Effect == RuleEffect.Power) Power += rule.Value;
            if (rule.Effect == RuleEffect.Insert || rule.InsertsTwo)
                pending.Insert(0, new BattleAction { IsNumberOnly = true, Number = rule.InsertsTwo ? 2 : rule.Value });
        }

        private bool MatchSequences()
        {
            if (NumberStream.Count < 3) return false;
            var matches = new List<BattleAction>();
            // Snapshot all matches in priority order, then insert them ahead of subsequent actions.
            Match(ArenaRules[ArenaIndex], false, matches, true);
            foreach (var rule in PlayerRules) Match(rule, false, matches, false);
            Match(EnemyRule, true, matches, false);
            pending.InsertRange(0, matches);
            return matches.Count > 0;
        }
        private void Match(SequenceRule rule, bool enemy, List<BattleAction> matches, bool arena)
        {
            if (rule == null || rule.Triggers >= 99) return;
            int start = NumberStream.Count - 3;
            for (int i = 0; i < 3; i++) if (NumberStream[start + i] != rule.Numbers[i]) return;
            rule.Triggers++; matches.Add(new BattleAction { Rule = rule, Enemy = enemy });
            Log("牌序触发 [" + rule.Name + "] " + rule.Pattern + " · " + rule.Triggers + "/99");
            if (arena && ++ArenaTriggerCount >= 2) rotateArena = true;
        }

        private void Damage(bool toEnemy, int value)
        {
            if (toEnemy) { int blocked = Math.Min(EnemyShield, value); EnemyShield -= blocked; EnemyHealth = Math.Max(0, EnemyHealth - value + blocked); }
            else { int blocked = Math.Min(Shield, value); Shield -= blocked; Health = Math.Max(0, Health - value + blocked); }
        }

        private void FinishBattle()
        {
            pending.Clear(); Coins += Boss ? 50 : 25; Victories++; RestAvailable = true;
            if (Boss) { Phase = GamePhase.Won; Message = "核心实体已收容，原型通关！"; return; }
            Rewards.Clear(); for (int i = 0; i < 3; i++) Rewards.Add(GenerateCard());
            if (random.NextDouble() < 0.2)
            {
                var candidates = Deck.Where(c => !c.Engraved).ToList();
                if (candidates.Count > 0) { candidates[random.Next(candidates.Count)].Engraved = true; Log("稀有战利品：随机一张牌获得铭刻。"); }
            }
            Phase = GamePhase.Reward; Message = "收容成功，获得 25 金币。选择一张生成卡牌加入牌组。";
        }
        public void TakeReward(int index)
        {
            if (Phase != GamePhase.Reward || index < 0 || index >= Rewards.Count) return;
            Deck.Add(Rewards[index]); Rewards.Clear(); Phase = GamePhase.Map; Message = "奖励已加入牌组，继续选择任务。";
        }
        public void OpenService(bool shop)
        {
            if (Phase != GamePhase.Map || !shop && !RestAvailable) return;
            Phase = shop ? GamePhase.Shop : GamePhase.Rest;
            ServiceUsed = false;
        }
        public void LeaveService()
        { if (Phase == GamePhase.Shop || Phase == GamePhase.Rest) Phase = GamePhase.Map; }
        public void RestHeal()
        {
            if (Phase != GamePhase.Rest || ServiceUsed) return;
            Health = Math.Min(MaxHealth, Health + 24); ServiceUsed = true; RestAvailable = false; Message = "恢复 24 生命。";
        }
        public void RestGenerate()
        {
            if (Phase != GamePhase.Rest || ServiceUsed) return;
            Deck.Add(GenerateCard()); ServiceUsed = true; RestAvailable = false; Message = "随机生成一张新卡牌。";
        }
        public void RestRemove(Card card)
        {
            if (Phase != GamePhase.Rest || ServiceUsed || Deck.Count <= 6 || !Deck.Contains(card)) return;
            Deck.Remove(card); ServiceUsed = true; RestAvailable = false; Message = "指定卡牌已删除。";
        }
        public void RestRebuild(int index)
        {
            if (Phase != GamePhase.Rest || ServiceUsed || index < 0 || index >= PlayerRules.Count) return;
            PlayerRules[index].Numbers = RandomPattern(); ServiceUsed = true; RestAvailable = false; Message = "已重构牌序数字。";
        }
        public bool BuyCard(int number)
        {
            if (Phase != GamePhase.Shop || Coins < 15 || number < 1 || number > 6) return false;
            Coins -= 15; Deck.Add(GenerateCard(number)); Message = "已生成指定数字卡牌。"; return true;
        }
        public bool BuyUpgrade(Card card, bool engrave)
        {
            int cost = engrave ? 20 : 12;
            if (Phase != GamePhase.Shop || Coins < cost || !Deck.Contains(card) || engrave && card.Engraved) return false;
            Coins -= cost; if (engrave) card.Engraved = true; else card.Upgrade++;
            Message = engrave ? "铭刻完成，每张牌最多一次。" : "卡牌效果 +2。"; return true;
        }
        public bool BuyRule(int index)
        {
            if (Phase != GamePhase.Shop || Coins < 18 || index < 0 || index >= PlayerRules.Count) return false;
            Coins -= 18; var rule = PlayerRules[index];
            rule.Effect = (RuleEffect)(((int)rule.Effect + 1) % 3);
            rule.Value = rule.Effect == RuleEffect.Power ? 2 : 10;
            rule.InsertsTwo = rule.Effect == RuleEffect.Power;
            Message = "牌序效果已替换。"; return true;
        }
        public void ChangeNumber(int rule, int digit)
        {
            if (Phase != GamePhase.Map || rule < 0 || rule >= PlayerRules.Count || digit < 0 || digit > 2) return;
            var numbers = PlayerRules[rule].Numbers; numbers[digit] = numbers[digit] % 6 + 1;
        }
        private void Log(string text)
        { History.Add(text); if (History.Count > 150) History.RemoveAt(0); }

        // Forecast executes exactly the same rules on a private snapshot. It never
        // consumes live RNG, mutates cards, or advances the live turn.
        public BattleForecast Forecast()
        {
            if (Phase != GamePhase.Planning) return null;
            var copy = new ContainmentGame(42)
            {
                Phase = GamePhase.Resolving, Health = Health, Shield = Shield,
                EnemyHealth = EnemyHealth, EnemyMaxHealth = EnemyMaxHealth, EnemyShield = EnemyShield,
                Coins = Coins, Victories = Victories, Round = Round, Power = Power,
                ArenaIndex = ArenaIndex, ArenaTriggerCount = ArenaTriggerCount, Boss = Boss
            };
            copy.Deck.Clear(); copy.Deck.AddRange(Deck.Select(CloneCard));
            copy.PlayerRules.Clear(); copy.PlayerRules.AddRange(PlayerRules.Select(CloneRule));
            copy.ArenaRules.Clear(); copy.ArenaRules.AddRange(ArenaRules.Select(CloneRule));
            copy.EnemyRule = CloneRule(EnemyRule);
            foreach (var action in Timeline)
                copy.pending.Add(new BattleAction { Card = action.Card == null ? null : CloneCard(action.Card),
                    Enemy = action.Enemy, Number = action.Number, Value = action.Value, Effect = action.Effect });
            // Stop before BeginRound so the forecast describes this round only.
            while (copy.Phase == GamePhase.Resolving && copy.pending.Count > 0) copy.Step();
            var result = new BattleForecast { Health = copy.Health, Shield = copy.Shield,
                EnemyHealth = copy.EnemyHealth, EnemyShield = copy.EnemyShield,
                Victory = copy.EnemyHealth <= 0, Defeat = copy.Health <= 0 };
            result.Numbers.AddRange(copy.NumberStream);
            foreach (var rule in copy.ArenaRules.Concat(copy.PlayerRules).Concat(new[] { copy.EnemyRule }))
                if (rule != null) result.RuleHits[rule.Name] = rule.Triggers;
            return result;
        }
        private static Card CloneCard(Card card) => new Card
        { Number = card.Number, Effect = card.Effect, Upgrade = card.Upgrade, Engraved = card.Engraved };
        private static SequenceRule CloneRule(SequenceRule rule) => rule == null ? null : new SequenceRule
        { Name = rule.Name, Numbers = (int[])rule.Numbers.Clone(), Effect = rule.Effect,
            Value = rule.Value, InsertsTwo = rule.InsertsTwo, Triggers = rule.Triggers };
    }
}
