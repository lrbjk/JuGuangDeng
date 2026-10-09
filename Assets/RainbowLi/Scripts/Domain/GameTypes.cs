using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi
{
    public enum CardEffect { Strike, Guard, Mend, IncreasedStrike, InverseStrike, Strength, Vulnerable, Weaken, Regeneration, Fortify, Poison, Bleed, IronWall, Parry, Barrier, DamageCap, RandomNumbers, Chant, Reciprocity, Burn, Ignite, Envenom, Disorder, Mutation, AddNumber, SubtractNumber }
    public enum GamePhase { Map, Planning, Resolving, Reward, Rest, Shop, Lost, Won, Event }
    public enum RuleEffect { Damage, Shield, Power, Insert }

    [Serializable]
    public sealed class Card
    {
        public int Number;
        public CardEffect Effect;
        public int Upgrade;
        public bool Engraved;
        public int AttackBase;
        public CardEffectSpec Spec; public int EffectAmount;
        public bool CanUpgrade=>UpgradeBonus>0;
        public string RulesText=>Spec==null?Description:Spec.Text.Replace("X",(Spec.UsesNumber?Number:Value).ToString())+(Upgrade>0?" · 升级后数值 "+Value:"");
        public int BaseBonus = 3, UpgradeBonus = 2, EngravingDamage = 5;
        public int Value => Spec==null?Math.Max(0,AttackBase + Number + BaseBonus + Upgrade * UpgradeBonus):Math.Max(Effect==CardEffect.InverseStrike?1:0,(Spec.UsesNumber?(Effect==CardEffect.IncreasedStrike?1+Number:Effect==CardEffect.InverseStrike?7-Number:Number):EffectAmount)+Upgrade*UpgradeBonus);
        public string Name => Spec!=null?Spec.Name.Replace("X",""):Effect == CardEffect.Strike ? "冲击" : Effect == CardEffect.Guard ? "屏障" : "修复";
        public string Description => Name + " " + Value + (Spec==null||Spec.Lifetime==EffectLifetime.Direct?"":Spec.Lifetime==EffectLifetime.Temporary?" · 本回合":" · 持续") + (Engraved ? " · 铭刻：牌序收尾时追加伤害" : "");
    }

    public enum PatternRequirement { Any, Straight, Triple }

    [Serializable]
    public sealed class SequenceEffect { public RuleEffect Effect; public int Value; public bool InsertsTwo; public PatternRequirement Requirement; }

    public sealed class SequenceRule
    {
        public string Name;
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
        private static string Describe(RuleEffect Effect,int Value,bool InsertsTwo) => Effect == RuleEffect.Damage ? "伤害 " + Value :
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
        public string SkillName; public bool Special;
        public CardEffectSpec EffectSpec;
        public bool IsRepeat,IsStatusTick,EndTick;
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
