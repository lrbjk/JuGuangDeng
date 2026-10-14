using System;
using System.Linq;
namespace RainbowLi.UnityGame {
 public static class EnemyEncounterAdapter {
  static EncounterEffect[] Effects(EnemyAuthoredEffect[] effects)=>effects.Select(e=>new EncounterEffect {Operation=(EncounterOperation)e.operation,Target=(EncounterTarget)e.target,UsesAttack=e.amountSource==EnemyContentAmount.基础攻击,Multiplier=e.multiplier,Amount=e.amount,Offset=e.offset,NumberMode=(EncounterNumber)e.numberMode,NumberMin=e.numberMin,NumberMax=e.numberMax}).ToArray();
  public static EncounterDefinition Normal(NormalEnemyRow row){
   var issues=EnemyConfiguredContent.Check(row);if(issues.Length>0)throw new InvalidOperationException(string.Join("\n",issues));
   if(!string.IsNullOrWhiteSpace(row.initialEffects)&&row.initialEffects!="无")throw new InvalidOperationException(row.title+"：普通敌人初始效果需提供数值被动配置。");
   return Convert(row,null);
  }
  public static EncounterDefinition Boss(BossContentRow row)=>Convert(row.content,row);
  static EncounterDefinition Convert(NormalEnemyRow row,BossContentRow boss){
   var actions=row.ActionRange;var attack=row.AttackRange;
   var result=new EncounterDefinition {Id=row.id,Title=row.title,Boss=boss!=null,Health=row.health,ActionMin=actions.Min,ActionMax=actions.Max,AttackMin=attack.Min,AttackMax=attack.Max};
   result.Skills=row.skills.Select(s=>{
    var numbers=EnemyNumberRange.Parse(s.numberRange,1);var rule=boss?.skillRules.Single(r=>r.slot==s.slot);
    return new EncounterSkill {Slot=s.slot,Name=s.name,Description=s.description,Weight=s.weight,Special=s.special,NumberMin=numbers.Min,NumberMax=numbers.Max,Effects=Effects(s.effects),
     Trigger=rule==null?(s.trigger==EnemyContentTrigger.血量首次达到阈值?EncounterTrigger.HealthThreshold:s.trigger==EnemyContentTrigger.使用过指定技能?EncounterTrigger.UsedSkill:EncounterTrigger.None):(EncounterTrigger)rule.trigger,
     ThresholdPercent=rule?.thresholdPercent??row.healthThresholdPercent,RequiredSkill=rule?.requiredSkill??s.requiredSkill,RequiredStage=rule?.requiredStage??0,RoundInterval=rule?.roundInterval??0,SequenceCountThreshold=rule?.sequenceCountThreshold??0,
     FixedTrigger=rule?.fixedTrigger??false,PriorityOnAppearance=rule?.priorityOnAppearance??false,LockInPhase=rule?.lockInPhase??false};
   }).ToArray();
   result.Sequences=row.sequences.Select(s=>{
    var rule=boss?.sequenceRules.Single(r=>r.slot==s.slot);var fixedNumbers=rule?.mode==BossSequenceMode.固定排列?(int[])rule.fixedNumbers.Clone():Array.Empty<int>();
    if(boss==null&&s.numbers!="随机"){
     var digits=(s.numbers??"").Where(char.IsDigit).Select(c=>c-'0').ToArray();if(digits.Length!=3||digits.Any(n=>n<1||n>6))throw new InvalidOperationException(row.title+"：牌序数字须为随机或三个1–6数字。");fixedNumbers=digits;
    }
    return new EncounterSequence {Slot=s.slot,Name=s.name,Description=s.description,Effects=Effects(s.effects),FixedNumbers=fixedNumbers,RequiredNumber=rule?.mode==BossSequenceMode.必须包含指定数字?rule.requiredNumber:0};
   }).ToArray();
   result.Rewards=row.rewards.Select(r=>{var amount=EnemyNumberRange.Parse(r.amount);return new EncounterReward {Kind=(int)r.kind,Min=amount.Min,Max=amount.Max,Probability=r.probability,ChoiceCount=r.choiceCount,CanSkip=r.canSkip};}).ToArray();
   if(boss!=null){var layers=EnemyNumberRange.Parse(boss.spawnLayers,1);result.LayerMin=layers.Min;result.LayerMax=layers.Max;result.Phases=boss.phases.Select(p=>new EncounterPhase {Index=p.index,ThresholdPercent=p.thresholdPercent,Label=p.label,Passives=p.passives.Select(a=>new EncounterPassive {Timing=(EncounterTiming)a.timing,Effects=Effects(a.effects)}).ToArray()}).ToArray();}
   return result;
  }
 }
}
