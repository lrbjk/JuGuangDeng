using System.Collections.Generic;
namespace RainbowLi
{
    // Runtime copies only: authoring assets never hold health, shields or trigger counters.
    public sealed class GameSetup
    {
        public int PlayerHealth = 70, HandSize = 6, EnemyHealth = 36, EnemyHealthGrowth = 12;
        public int PlayerAttack, HandLimit=9;
        public bool RandomStartingDeck;
        public float RepeatWeightMultiplier=.5f;
        public readonly List<EnemySkill> EnemySkills=new List<EnemySkill>();
        public readonly List<ConditionalSequence> EnemyConditionalRules=new List<ConditionalSequence>();
        public int StartingCoins, PlayerActions=4, EnemyActions=2, BossActions=3, GuardSlot=1, GuardInterval=2;
        public int[] EnemyNumbers;
        public int EnemyAttack = 5, EnemyGuard = 6, BossHealth = 110, BossAttack = 9;
        public readonly List<CardEffectSpec> CardEffects=new List<CardEffectSpec>();
        public readonly List<Card> InitialDeck = new List<Card>(), CardPool = new List<Card>();
        public readonly List<SequenceRule> PlayerRules = new List<SequenceRule>(), ArenaRules = new List<SequenceRule>();
        public readonly List<SequenceEffect> AvailablePlayerEffects=new List<SequenceEffect>();
        public SequenceRule EnemyRule;
    }
}
