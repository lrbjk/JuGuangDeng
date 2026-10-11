using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi
{
    public enum CardEffect { Strike, Guard, Mend, IncreasedStrike, InverseStrike, Strength, Vulnerable, Weaken, Regeneration, Fortify, Poison, Bleed, IronWall, Parry, Barrier, DamageCap, RandomNumbers, Chant, Reciprocity, Burn, Ignite, Envenom, Disorder, Mutation, AddNumber, SubtractNumber }
    public enum GamePhase { Map, Planning, Resolving, Reward, Rest, Shop, Lost, Won, Event }
    public enum RuleEffect { Damage, Shield, Power, Insert, Heal, Burn, Poison, Strength, Bleed, RandomNumbers, LastStand, RandomCardEffects, Vulnerable, Parry, RepeatSequenceCards }
    public enum EngravingEffect { Poison, Burn, FollowUpDamage, Growth, ExtraNumber, Wildcard, SequenceTailBonus, SequenceHeadBonus, BlockSequence, Bridge, Inherit, RandomizeFollowing, Roulette, OnlyPlayerCard, LastActionBonus, FirstActionBonus, Charge, PoisonExplosion, BurnExplosion }

    [Serializable]
    public sealed class Card
    {
        // Identity belongs to the physical card, not its configuration or deck slot.
        public string InstanceId { get; private set; } = Guid.NewGuid().ToString("N");
        public string EngravingCode;
        public RuntimeEngraving Engraving;
        public int PermanentGrowth, BattleCharge;
        // Nonempty ordered effects take precedence over the legacy single effect.
        public List<CardEffectInstance> Effects = new List<CardEffectInstance>();
        public int Number;
        public CardEffect Effect;
        public int Upgrade;
        public bool Engraved;
        public int AttackBase;
        public CardEffectSpec Spec; public int EffectAmount;
        public bool CanUpgrade=>HasOrderedEffects?Effects.Any(e=>e.UpgradeBonus>0):UpgradeBonus>0;
        public bool HasOrderedEffects=>Effects!=null&&Effects.Count>0;
        public string RulesText=>HasOrderedEffects?string.Join(" / ",Effects.Select(e=>e.Spec.Effect==CardEffect.Ignite?IgniteRules(ResolvedEffectValue(e),e.Spec.Lifetime):EffectDescription(e)))+(Engraved?" · 铭刻："+(Engraving==null?"牌序收尾时追加伤害":Engraving.Description):""):Effect==CardEffect.Ignite?IgniteRules(Value,Spec==null?EffectLifetime.Temporary:Spec.Lifetime):Spec==null?Description:(Spec.Text??"").Replace("X",(Spec.UsesNumber?Number:Value).ToString())+(Upgrade>0?" · 升级后数值 "+Value:"");
        private int ResolvedEffectValue(CardEffectInstance e)=>CombatResolver.Clamp((long)e.ResolveValue(Number,Upgrade)+PermanentGrowth+BattleCharge);
        private string EffectDescription(CardEffectInstance e)=>CombatStatuses.Name(e.Spec.Effect)+" "+ResolvedEffectValue(e)+(e.Spec.Effect==CardEffect.Ignite?" · 自身增益":"");
        private static string IgniteRules(int value,EffectLifetime lifetime)=>"自身获得 "+value+" 层点燃增益；自身每次行动后，对对方施加 "+value+" 层烧伤。点燃增益不会使自身受到烧伤。"+(lifetime==EffectLifetime.Temporary?"增益在本回合结束时消失。":"增益在本场战斗内保留，按效果规则消耗。");
        public int BaseBonus = 3, UpgradeBonus = 2, EngravingDamage = 5;
        public int Value => CombatResolver.Clamp((long)UnmodifiedValue+PermanentGrowth+BattleCharge);
        internal int UnmodifiedValue => HasOrderedEffects?Effects[0].ResolveValue(Number,Upgrade):Spec==null?Math.Max(0,AttackBase + Number + BaseBonus + Upgrade * UpgradeBonus):Math.Max(Effect==CardEffect.InverseStrike?1:0,(Spec.UsesNumber?(Effect==CardEffect.IncreasedStrike?1+Number:Effect==CardEffect.InverseStrike?7-Number:Number):EffectAmount)+Upgrade*UpgradeBonus);
        public string Name => HasOrderedEffects?Effects[0].Spec.Name:Spec!=null?Spec.Name.Replace("X",""):Effect == CardEffect.Ignite ? "点燃增益" : Effect == CardEffect.Strike ? "冲击" : Effect == CardEffect.Guard ? "屏障" : "修复";
        public string Description => (HasOrderedEffects?string.Join(" / ",Effects.Select(EffectDescription)):Name + " " + Value + (Effect==CardEffect.Ignite?" · 自身增益":"") + (Spec==null||Spec.Lifetime==EffectLifetime.Direct?"":Spec.Lifetime==EffectLifetime.Temporary?" · 本回合":" · 持续")) + (Engraved ? " · 铭刻："+(Engraving==null?"牌序收尾时追加伤害":Engraving.Description) : "");

        internal void RestoreIdentity(string id){InstanceId=string.IsNullOrWhiteSpace(id)?Guid.NewGuid().ToString("N"):id;}
        public void ResetBattleState() { BattleCharge = 0; }
        // Snapshots retain identity and all state; a new physical copy has a new ID
        // and starts without encounter-local charge. Neither shares effect objects.
        public Card CopyForSnapshot() => Copy(true);
        public Card CopyAsNewInstance() => Copy(false);
        private Card Copy(bool snapshot)
        {
            var copy = (Card)MemberwiseClone();
            copy.InstanceId = snapshot ? InstanceId : Guid.NewGuid().ToString("N");
            copy.Spec = Spec?.Copy();
            copy.Engraving = Engraving?.Copy();
            copy.Effects = Effects==null?new List<CardEffectInstance>():Effects.Select(e=>e.Copy()).ToList();
            if(!snapshot)copy.ResetBattleState();
            return copy;
        }
    }

    public enum PatternRequirement { Any, Straight, Triple }

    [Serializable]
    public sealed class SequenceEffect { public RuleEffect Effect; public int Value; public bool InsertsTwo; public PatternRequirement Requirement; public string Code; public int BaseX, Multiplier=1, FixedValue, MinX,MaxX; public bool RollX; }

    [Serializable] public sealed class SequenceRule
    {
        public string Name;
        public string Code; public int BaseX, Multiplier=1, FixedValue, MinX,MaxX; public bool RollX;
        public int[] Numbers;
        public RuleEffect Effect;
        public int Value;
        public bool InsertsTwo;
        public int Triggers;
        public PatternRequirement Requirement;
        public SequenceEffect AdditionalEffect;
        public bool Accepts(PatternRequirement requirement) => Numbers!=null && Numbers.Length==3 && Numbers.All(n=>n>=1&&n<=6) && (requirement==PatternRequirement.Any || requirement==PatternRequirement.Triple && Numbers[0]==Numbers[1] && Numbers[1]==Numbers[2] || requirement==PatternRequirement.Straight && Math.Abs(Numbers[1]-Numbers[0])==1 && Numbers[2]-Numbers[1]==Numbers[1]-Numbers[0]);
        public string Pattern => string.Join(" → ", Numbers.Select(n => n.ToString()).ToArray());
        public string Description => Describe(Effect,Value,InsertsTwo)+(AdditionalEffect==null?"":" / "+Describe(AdditionalEffect.Effect,AdditionalEffect.Value,AdditionalEffect.InsertsTwo))+(Requirement==PatternRequirement.Any?"":" · "+RequirementName(Requirement))+(AdditionalEffect==null||AdditionalEffect.Requirement==PatternRequirement.Any?"":" · 效果2 "+RequirementName(AdditionalEffect.Requirement));
        private static string RequirementName(PatternRequirement requirement)=>requirement==PatternRequirement.Straight?"顺子":"三同号";
        private static string Describe(RuleEffect effect,int value,bool insertsTwo) {
            string[] names={"伤害","护盾","后续卡效 +","插入数字","恢复","烧伤","中毒","力量","流血","随机数字","生命降至百分比","随机卡效次数","易伤","招架","参与卡牌重放次数"};
            return names[(int)effect]+" "+value+(insertsTwo?"，插入数字 2":"");
        }
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
        public string SkillName; public bool Special;
        public CardEffectSpec EffectSpec;
        public bool IsRepeat,IsStatusTick,EndTick;
        public int SequenceTrigger;
        public bool EffectOnly, BonusOnly;
        public int EffectBonus, EffectMultiplier=1;
        public List<BattleAction> Participants=new List<BattleAction>();
        public BattleAction SourceAction;
        public string Label => IsStatusTick?(Enemy?"敌方":"玩家")+" / 回合结束中毒结算":Rule != null ? Rule.Name + (Enemy ? "（敌人触发）" : "（玩家触发）") + "：" + Rule.Description :
            IsNumberOnly ? "插入数字 " + Number : Enemy ? (Special?"特殊 / ":"")+(string.IsNullOrEmpty(SkillName)?"实体":SkillName)+" " + Number + " · " + CombatStatuses.Name(Effect)+" " + Value :
            "卡牌 " + Card.Number + " · " + Card.Description;
    }

    public sealed class BattleForecast
    {
        public int Health, Shield, EnemyHealth, EnemyShield;
        public bool Victory, Defeat;
        public readonly List<int> Numbers = new List<int>();
        public readonly Dictionary<string, int> RuleHits = new Dictionary<string, int>();
    }

}
