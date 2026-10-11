using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi
{
    public sealed partial class ContainmentGame
    {
        public static SequenceRule Rule(string name, int[] numbers, RuleEffect effect, int value) =>
            new SequenceRule { Name = name, Numbers = numbers, Effect = effect, Value = value };

        private int[] RandomPattern() => new[] { random.Next(1, 7), random.Next(1, 7), random.Next(1, 7) };

        private void ApplyRule(BattleAction action)
        {
            var rule = action.Rule;
            ApplyAdvancedRule(action);
            if (rule.Effect == RuleEffect.Damage) SettleDamage(action.Enemy,!action.Enemy,rule.Value,rule.Code??rule.Name,BattleEventSource.Sequence);
            else if (rule.Effect == RuleEffect.Shield) SettleShield(action.Enemy,action.Enemy,rule.Value,rule.Code??rule.Name,BattleEventSource.Sequence);
            else if (rule.Effect == RuleEffect.Power) SettlePower(action.Enemy,rule.Value,rule.Code??rule.Name,BattleEventSource.Sequence);
            if (Health>0&&EnemyHealth>0&&(rule.Effect == RuleEffect.Insert || rule.InsertsTwo))
                pending.Insert(0, new BattleAction { IsNumberOnly = true, Enemy = action.Enemy, Number = rule.InsertsTwo ? 2 : rule.Value });
        }

        private bool MatchSequences(bool finishingEnemy)
        {
            if (NumberStream.Count < 3) return false;
            var matches = new List<BattleAction>();
            // Snapshot all matches in priority order, then insert them ahead of subsequent actions.
            Match(ArenaRules[ArenaIndex], finishingEnemy, matches, true);
            foreach (var rule in PlayerRules) Match(rule, false, matches, false);
            foreach(var rule in Enemy.ActiveRules)Match(rule, true, matches, false);
            pending.InsertRange(0, matches);
            return matches.Count > 0;
        }

        private void Match(SequenceRule rule, bool enemy, List<BattleAction> matches, bool arena)
        {
            if (rule == null || rule.Triggers >= 99 || rule.Numbers==null || rule.Numbers.Length!=3) return;
            var participants=SequenceMatcher.Match(NumberEntries,rule.Numbers);
            if(participants==null)return;
            bool primary=rule.Accepts(rule.Requirement);var extra=rule.AdditionalEffect;bool secondary=extra!=null&&rule.Accepts(extra.Requirement);if(!primary&&!secondary)return;
            for(int i=0;i<3;i++){var participant=participants[i].Action;var c=participant?.Card;var e=c?.Engraving;if(e!=null&&(e.Effect==EngravingEffect.SequenceHeadBonus&&i==0||e.Effect==EngravingEffect.SequenceTailBonus&&i==2)&&positionBonuses.Add(c.InstanceId)){var bonus=EffectReplay(participant,e.FixedValue>0?e.FixedValue:3);bonus.BonusOnly=true;bonus.EffectBonus=e.FixedValue>0?e.FixedValue:3;matches.Add(bonus);}}
            int repetitions=(int)Math.Min(99-rule.Triggers,1L+Actor(enemy).Statuses[CardEffect.Chant]);int firstTrigger=rule.Triggers+1;rule.Triggers+=repetitions;for(int repeat=0;repeat<repetitions;repeat++){if(primary)matches.Add(new BattleAction { Rule = rule, Enemy = enemy,SequenceTrigger=firstTrigger+repeat,Participants=participants.Select(p=>p.Action).ToList(),SourceAction=participants[2].Action });
            if(secondary)matches.Add(new BattleAction {Rule=new SequenceRule {Name=rule.Name+" / 效果2",Numbers=(int[])rule.Numbers.Clone(),Code=extra.Code,BaseX=extra.BaseX,Multiplier=extra.Multiplier,FixedValue=extra.FixedValue,Effect=extra.Effect,Value=extra.Value,InsertsTwo=extra.InsertsTwo},Enemy=enemy,SequenceTrigger=firstTrigger+repeat,Participants=participants.Select(p=>p.Action).ToList(),SourceAction=participants[2].Action});}
            foreach(var participant in participants){var c=participant.Action?.Card;if(c?.Engraving?.Effect==EngravingEffect.Growth)c.PermanentGrowth=CombatResolver.Clamp((long)c.PermanentGrowth+c.Engraving.Value);}
            Log("牌序触发 [" + rule.Name + "] " + rule.Pattern + (enemy ? " · 敌人触发" : " · 玩家触发") + " · " + rule.Triggers + "/99");
            if (arena && ++ArenaTriggerCount >= 2) rotateArena = true;
        }

        private static void SetRuleEffect(SequenceRule rule,SequenceEffect effect){rule.Effect=effect.Effect;rule.Value=effect.Value;rule.InsertsTwo=effect.InsertsTwo;rule.Requirement=effect.Requirement;rule.Code=effect.Code;rule.BaseX=effect.BaseX;rule.Multiplier=effect.Multiplier;rule.FixedValue=effect.FixedValue;rule.MinX=effect.MinX;rule.MaxX=effect.MaxX;rule.RollX=effect.RollX;}
        private static SequenceEffect CopyEffect(SequenceEffect effect)=>new SequenceEffect {Effect=effect.Effect,Value=effect.Value,InsertsTwo=effect.InsertsTwo,Requirement=effect.Requirement,Code=effect.Code,BaseX=effect.BaseX,Multiplier=effect.Multiplier,FixedValue=effect.FixedValue,MinX=effect.MinX,MaxX=effect.MaxX,RollX=effect.RollX};
        private void AddAvailableEffect(SequenceEffect effect){if(!Player.AvailableEffects.Any(e=>e.Code==effect.Code&&e.Effect==effect.Effect&&e.Value==effect.Value&&e.InsertsTwo==effect.InsertsTwo&&e.Requirement==effect.Requirement))Player.AvailableEffects.Add(CopyEffect(effect));}
        public void ChangeSequenceEffect(int index,bool secondary){
            if(Phase!=GamePhase.Map||index<0||index>=PlayerRules.Count||Player.AvailableEffects.Count==0)return;var rule=PlayerRules[index];var pool=Player.AvailableEffects;
            var current=secondary?rule.AdditionalEffect:new SequenceEffect {Effect=rule.Effect,Value=rule.Value,InsertsTwo=rule.InsertsTwo,Requirement=rule.Requirement};
            int at=current==null?-1:pool.FindIndex(e=>e.Effect==current.Effect&&e.Value==current.Value&&e.InsertsTwo==current.InsertsTwo&&e.Requirement==current.Requirement);
            if(secondary&&at==pool.Count-1){rule.AdditionalEffect=null;return;}
            var next=CopyEffect(pool[(at+1)%pool.Count]);if(secondary)rule.AdditionalEffect=next;else SetRuleEffect(rule,next);
        }

        public void ChangeNumber(int rule, int digit)
        {
            if (Phase != GamePhase.Map || rule < 0 || rule >= PlayerRules.Count || digit < 0 || digit > 2) return;
            var numbers = PlayerRules[rule].Numbers; numbers[digit] = (numbers[digit]+1) % 7;
        }
    }
}
