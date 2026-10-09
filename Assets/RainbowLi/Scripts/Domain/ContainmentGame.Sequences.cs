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
            if (rule.Effect == RuleEffect.Damage) Damage(!action.Enemy, rule.Value);
            else if (rule.Effect == RuleEffect.Shield) { Actor(action.Enemy).GainShield(rule.Value); }
            else if (rule.Effect == RuleEffect.Power) { if (action.Enemy) EnemyPower += rule.Value; else Power += rule.Value; }
            if (rule.Effect == RuleEffect.Insert || rule.InsertsTwo)
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
            int start = NumberStream.Count - 3;
            for (int i = 0; i < 3; i++) if (NumberStream[start + i] != rule.Numbers[i]) return;
            bool primary=rule.Accepts(rule.Requirement);var extra=rule.AdditionalEffect;bool secondary=extra!=null&&rule.Accepts(extra.Requirement);if(!primary&&!secondary)return;
            int repetitions=Math.Min(99-rule.Triggers,1+Actor(enemy).Statuses[CardEffect.Chant]);rule.Triggers+=repetitions;for(int repeat=0;repeat<repetitions;repeat++){if(primary)matches.Add(new BattleAction { Rule = rule, Enemy = enemy });
            if(secondary)matches.Add(new BattleAction {Rule=new SequenceRule {Name=rule.Name+" / 效果2",Numbers=(int[])rule.Numbers.Clone(),Effect=extra.Effect,Value=extra.Value,InsertsTwo=extra.InsertsTwo},Enemy=enemy});}
            Log("牌序触发 [" + rule.Name + "] " + rule.Pattern + (enemy ? " · 敌人触发" : " · 玩家触发") + " · " + rule.Triggers + "/99");
            if (arena && ++ArenaTriggerCount >= 2) rotateArena = true;
        }

        private static SequenceEffect CopyEffect(SequenceEffect effect)=>new SequenceEffect {Effect=effect.Effect,Value=effect.Value,InsertsTwo=effect.InsertsTwo,Requirement=effect.Requirement};
        private void AddAvailableEffect(SequenceEffect effect){if(!Player.AvailableEffects.Any(e=>e.Effect==effect.Effect&&e.Value==effect.Value&&e.InsertsTwo==effect.InsertsTwo&&e.Requirement==effect.Requirement))Player.AvailableEffects.Add(CopyEffect(effect));}
        public void ChangeSequenceEffect(int index,bool secondary){
            if(Phase!=GamePhase.Map||index<0||index>=PlayerRules.Count||Player.AvailableEffects.Count==0)return;var rule=PlayerRules[index];var pool=Player.AvailableEffects;
            var current=secondary?rule.AdditionalEffect:new SequenceEffect {Effect=rule.Effect,Value=rule.Value,InsertsTwo=rule.InsertsTwo,Requirement=rule.Requirement};
            int at=current==null?-1:pool.FindIndex(e=>e.Effect==current.Effect&&e.Value==current.Value&&e.InsertsTwo==current.InsertsTwo&&e.Requirement==current.Requirement);
            if(secondary&&at==pool.Count-1){rule.AdditionalEffect=null;return;}
            var next=CopyEffect(pool[(at+1)%pool.Count]);if(secondary)rule.AdditionalEffect=next;else{rule.Effect=next.Effect;rule.Value=next.Value;rule.InsertsTwo=next.InsertsTwo;rule.Requirement=next.Requirement;}
        }

        public void ChangeNumber(int rule, int digit)
        {
            if (Phase != GamePhase.Map || rule < 0 || rule >= PlayerRules.Count || digit < 0 || digit > 2) return;
            var numbers = PlayerRules[rule].Numbers; numbers[digit] = (numbers[digit]+1) % 7;
        }
    }
}
