using System;
using System.Collections.Generic;
using System.Linq;

namespace RainbowLi
{
    // Pure C# rules. The Unity panel only submits choices and displays this state.
    public sealed partial class ContainmentGame
    {
        public const int MaxHealth = 70;
        public const int MaxPlayerActions = 4;
        public PlayerState Player { get; }
        public EnemyState Enemy { get; } = new EnemyState();
        public List<Card> Deck => Player.Deck;
        public List<Card> Hand => Player.Hand;
        public readonly List<BattleAction> Timeline = new List<BattleAction>();
        public List<SequenceRule> PlayerRules => Player.Rules;
        public readonly List<SequenceRule> ArenaRules = new List<SequenceRule>();
        public readonly List<int> NumberStream = new List<int>();
        public readonly List<string> History = new List<string>();
        public readonly List<Card> Rewards = new List<Card>();
        public SequenceRule EnemyRule { get => Enemy.Rule; set => Enemy.Rule=value; }
        public GamePhase Phase { get; private set; }
        // Compatibility accessors expose the one authoritative actor state to existing views.
        public int Health { get => Player.Health; set => Player.Health=value; }
        public int Shield { get => Player.Shield; set => Player.Shield=value; }
        public int Power { get => Player.Power; set => Player.Power=value; }
        public int Coins { get => Player.Coins; set => Player.Coins=value; }
        public int EnemyHealth { get => Enemy.Health; set => Enemy.Health=value; }
        public int EnemyMaxHealth { get => Enemy.MaxHealth; set => Enemy.MaxHealth=value; }
        public int EnemyShield { get => Enemy.Shield; set => Enemy.Shield=value; }
        public int EnemyPower { get => Enemy.Power; set => Enemy.Power=value; }
        public int Victories, Round, ArenaIndex, ArenaTriggerCount;
        public bool Boss { get => Enemy.Boss; set => Enemy.Boss=value; }
        public bool ServiceUsed;
        public bool Elite => Enemy.Elite;
        public int EliteAttackBonus {get=>Enemy.EliteAttackBonus;set=>Enemy.EliteAttackBonus=value;}
        public int EliteGold=40;
        public bool RestAvailable = true;
        public string EnemyName => Boss ? "失控档案 · BOSS" : Elite ? "精英档案 " + (Victories + 1) : "逸散实体 " + (Victories + 1);
        public string Message = "选择收容任务。可在此调整三组基础牌序。";
        public string LastAction = "等待行动";
        public BattleAction LastResolvedAction { get; private set; }
        public int PlayerActionCount => Timeline.Count(a => a.Card != null);
        private readonly Random random;
        private readonly GameSetup setup;
        public int HealthLimit => Player.MaxHealth;
        public int PlayerActionLimit => setup==null?MaxPlayerActions:setup.PlayerActions;
        private readonly List<Card> drawPile = new List<Card>();
        private readonly List<Card> discard = new List<Card>();
        public IReadOnlyList<Card> DrawPileCards => drawPile;
        public IReadOnlyList<Card> DiscardCards => discard;
        private readonly List<BattleAction> pending = new List<BattleAction>();
        private bool rotateArena;
        private uint battleRandomState;
        private bool endTicksQueued;
        private int processedActions;
        private readonly List<BattleAction> requiredEnemyActions=new List<BattleAction>();
        public int HandLimit=>setup==null?9:setup.HandLimit;
        public int BaseAttack=>setup==null?0:setup.PlayerAttack;
        public bool CanConfirm=>Phase==GamePhase.Planning && PlayerActionCount>0 && requiredEnemyActions.All(a=>Timeline.Count(t=>ReferenceEquals(t,a))==1);

        public ContainmentGame(int seed = 0, GameSetup setup = null)
        {
            if(setup!=null&&!setup.RandomStartingDeck&&setup.InitialDeck.Count>0&&setup.InitialDeck.Count<6)throw new ArgumentException("起始牌组至少需要 6 张卡牌",nameof(setup));
            this.setup = setup; Player=new PlayerState(setup==null?MaxHealth:setup.PlayerHealth);Coins=setup==null?0:setup.StartingCoins;
            random = seed == 0 ? new Random() : new Random(seed);
            GenerateInitialDeck();
            PlayerRules.Add(Rule("基础 / 冲击", new[] { 1, 2, 3 }, RuleEffect.Damage, 10));
            PlayerRules.Add(Rule("基础 / 屏障", new[] { 3, 4, 5 }, RuleEffect.Shield, 10));
            var power = Rule("基础 / 连锁", new[] { 2, 2, 2 }, RuleEffect.Power, 2);
            power.InsertsTwo = true;
            PlayerRules.Add(power);
            if (setup != null)
            {
                if (!setup.RandomStartingDeck && setup.InitialDeck.Count > 0) { Deck.Clear(); Deck.AddRange(setup.InitialDeck.Select(c=>{var clone=c.CopyAsNewInstance();clone.AttackBase=BaseAttack;return clone;})); }
                if (setup.PlayerRules.Count > 0) { PlayerRules.Clear(); PlayerRules.AddRange(setup.PlayerRules.Select(CloneRule)); }
            }
            foreach(var rule in PlayerRules){AddAvailableEffect(new SequenceEffect {Effect=rule.Effect,Value=rule.Value,InsertsTwo=rule.InsertsTwo,Requirement=rule.Requirement});if(rule.AdditionalEffect!=null)AddAvailableEffect(rule.AdditionalEffect);}
            if(setup!=null)foreach(var effect in setup.AvailablePlayerEffects)AddAvailableEffect(effect);
            Phase = GamePhase.Map;
        }

        private void Log(string text)
        { History.Add(text); if (History.Count > 150) History.RemoveAt(0); }

        // Forecast executes exactly the same rules on a private snapshot. It never
        // consumes live RNG, mutates cards, or advances the live turn.
        public BattleForecast Forecast()
        {
            if (Phase != GamePhase.Planning) return null;
            var copy = CreateBattleSnapshot();
            // Stop before BeginRound so the forecast describes this round only.
            while (copy.Phase == GamePhase.Resolving && copy.pending.Count > 0) copy.Step();
            if(copy.Phase==GamePhase.Resolving){copy.QueueEndTicks();while(copy.Phase==GamePhase.Resolving&&copy.pending.Count>0)copy.Step();}
            var result = new BattleForecast { Health = copy.Health, Shield = copy.Shield,
                EnemyHealth = copy.EnemyHealth, EnemyShield = copy.EnemyShield,
                Victory = copy.EnemyHealth <= 0, Defeat = copy.Health <= 0 };
            result.Numbers.AddRange(copy.NumberStream);
            foreach (var rule in copy.ArenaRules.Concat(copy.PlayerRules).Concat(copy.Enemy.ActiveRules))
                if (rule != null) result.RuleHits[rule.Name] = rule.Triggers;
            return result;
        }

        internal ContainmentGame CreateBattleSnapshot()
        {
            if(Phase!=GamePhase.Planning)throw new InvalidOperationException("只有安排阶段可以创建战斗预览快照");
            var copy = new ContainmentGame(42, setup)
            {
                Phase = GamePhase.Resolving, Health = Health, Shield = Shield,
                EnemyHealth = EnemyHealth, EnemyMaxHealth = EnemyMaxHealth, EnemyShield = EnemyShield,
                Coins = Coins, Victories = Victories, Round = Round, Power = Power, EnemyPower = EnemyPower,
                ArenaIndex = ArenaIndex, ArenaTriggerCount = ArenaTriggerCount, Boss = Boss, EliteGold=EliteGold
            };
            copy.Player.Statuses.CopyFrom(Player.Statuses);copy.Enemy.Statuses.CopyFrom(Enemy.Statuses);copy.battleRandomState=battleRandomState;
            copy.Enemy.Elite=Enemy.Elite;copy.EliteAttackBonus=EliteAttackBonus;copy.Enemy.CopyActivationFrom(Enemy);
            // Preserve aliases: a card in the deck, hand and action queue must be
            // the same snapshot object, while remaining isolated from live state.
            var cards = new Dictionary<Card, Card>();
            Card SnapshotCard(Card card) {
                if(card==null)return null;
                if(!cards.TryGetValue(card,out var snapshot)){snapshot=card.CopyForSnapshot();cards.Add(card,snapshot);}
                return snapshot;
            }
            copy.Deck.Clear(); copy.Deck.AddRange(Deck.Select(SnapshotCard));
            copy.Hand.Clear(); copy.Hand.AddRange(Hand.Select(SnapshotCard));
            copy.drawPile.Clear(); copy.drawPile.AddRange(drawPile.Select(SnapshotCard));
            copy.discard.Clear(); copy.discard.AddRange(discard.Select(SnapshotCard));
            copy.PlayerRules.Clear(); copy.PlayerRules.AddRange(PlayerRules.Select(CloneRule));
            copy.ArenaRules.Clear(); copy.ArenaRules.AddRange(ArenaRules.Select(CloneRule));
            copy.EnemyRule = CloneRule(EnemyRule);copy.Enemy.ConditionalRules.AddRange(Enemy.ConditionalRules.Select(c=>new ConditionalSequence {Condition=c.Condition,Threshold=c.Threshold,Rule=CloneRule(c.Rule)}));
            foreach (var action in Timeline) {
                var snapshot=new BattleAction { Card = SnapshotCard(action.Card),
                    Enemy = action.Enemy, Number = action.Number, Value = action.Value, Effect = action.Effect, EffectSpec=action.EffectSpec?.Copy(), SkillName=action.SkillName, Special=action.Special };
                copy.Timeline.Add(snapshot);copy.pending.Add(snapshot);
            }
            return copy;
        }
        private static SequenceRule CloneRule(SequenceRule rule) => rule == null ? null : new SequenceRule
        { Name = rule.Name, Numbers = (int[])rule.Numbers.Clone(), Effect = rule.Effect,
            Value = rule.Value, InsertsTwo = rule.InsertsTwo, Triggers = rule.Triggers, Requirement=rule.Requirement, AdditionalEffect=rule.AdditionalEffect==null?null:new SequenceEffect {Effect=rule.AdditionalEffect.Effect,Value=rule.AdditionalEffect.Value,InsertsTwo=rule.AdditionalEffect.InsertsTwo,Requirement=rule.AdditionalEffect.Requirement} };
    }
}
