using System;
using System.Collections.Generic;
using System.Linq;
namespace RainbowLi {
 public sealed partial class ContainmentGame {
  private const int ChainLimit=4096;
  private SequenceRule RollRule(SequenceRule rule){if(rule.RollX){rule.BaseX=random.Next(rule.MinX,rule.MaxX+1);rule.Value=CombatResolver.Clamp((long)rule.BaseX*rule.Multiplier);rule.RollX=false;}if(rule.AdditionalEffect?.RollX==true){var e=rule.AdditionalEffect;e.BaseX=random.Next(e.MinX,e.MaxX+1);e.Value=CombatResolver.Clamp((long)e.BaseX*e.Multiplier);e.RollX=false;}return rule;}
  private Card RollCard(Card card){var e=card.Engraving;if(e?.RollX==true){e.BaseX=random.Next(e.MinX,e.MaxX+1);e.RollX=false;}return card;}
  private void QueueLimited(IEnumerable<BattleAction> actions){pending.InsertRange(0,actions.Take(Math.Max(0,ChainLimit-processedActions-pending.Count)));}
  private BattleAction EffectReplay(BattleAction source,int bonus=0)=>new BattleAction {Card=source.Card,OrderedEffects=source.OrderedEffects.Select(e=>e.Copy()).ToList(),Enemy=source.Enemy,Effect=source.Effect,EffectSpec=source.EffectSpec,Value=source.Value,Number=source.Number,SkillName=source.SkillName,EffectOnly=true,IsRepeat=true,SourceAction=source.SourceAction??source,EffectBonus=CombatResolver.Clamp((long)source.EffectBonus+bonus),SequenceTrigger=settlingAction?.SequenceTrigger??source.SequenceTrigger};
  private void PrepareEngraving(BattleAction action){
   var card=action.Card;var engraving=card?.Engraving;if(card==null||action.EffectOnly||action.IsRepeat)return;
   action.EffectBonus=CombatResolver.Clamp((long)card.PermanentGrowth+card.BattleCharge);
   if(engraving==null)return;
   int index=Timeline.IndexOf(action.SourceAction??action);
   if(engraving.Effect==EngravingEffect.FirstActionBonus&&index==0||engraving.Effect==EngravingEffect.LastActionBonus&&index==Timeline.Count-1)action.EffectBonus+=engraving.FixedValue>0?engraving.FixedValue:2;
   if(engraving.Effect==EngravingEffect.Roulette)action.EffectMultiplier=random.Next(2)==0?0:2;
   if(engraving.Effect==EngravingEffect.Charge)card.BattleCharge++;
  }
  private void FinishCardEngraving(BattleAction action){
   if(action.EffectOnly||action.IsRepeat||action.Card?.Engraving==null)return;
   var e=action.Card.Engraving;
   if(e.Effect==EngravingEffect.Inherit)inheritedEffects.Add(EffectReplay(new BattleAction {Card=action.Card,Enemy=action.Enemy,Effect=action.Effect,Number=action.Number,SourceAction=action.SourceAction??action},action.EffectBonus));
   
   if(e.Effect==EngravingEffect.OnlyPlayerCard&&!action.Enemy&&Timeline.Count(a=>a.Card!=null&&!a.Enemy)==1){
    var pool=roundDiscard.ToList();Shuffle(pool);QueueLimited(pool.Take(3).Select(c=>EffectReplay(new BattleAction {Card=c,Number=c.Number,Effect=c.Effect},2)));
   }
  }
  private void AddNumberEntry(BattleAction action,int number){
   var card=action.Card;var e=card?.Engraving;
   var entry=new NumberEntry {Number=number,Enemy=action.Enemy,Action=action,Candidates=action.NumberCandidates==null?null:(int[])action.NumberCandidates.Clone()};
   if(e?.Effect==EngravingEffect.Wildcard)entry.Candidates=new[]{1,2,3,4,5,6};
   else if(e?.Effect==EngravingEffect.ExtraNumber)entry.Candidates=new[]{number,e.Value};
   int index=Timeline.IndexOf(action.SourceAction??action);
   if(e?.Effect==EngravingEffect.BlockSequence||index>=0&&Timeline.Skip(Math.Max(0,index-1)).Take(index==0?2:3).Any(a=>a.Card?.Engraving?.Effect==EngravingEffect.BlockSequence))entry.Blocked=true;
   NumberEntries.Add(entry);
  }
  private void ApplyAdvancedRule(BattleAction action){
   var rule=action.Rule;int value=Math.Max(0,rule.Value);string code=rule.Code??rule.Name;bool enemy=action.Enemy;
   switch(rule.Effect){
    case RuleEffect.Heal:SettleHealing(enemy,enemy,value,code,BattleEventSource.Sequence);break;
    case RuleEffect.Burn:ApplyStatus(enemy,!enemy,CardEffect.Burn,value,code,BattleEventSource.Sequence);break;
    case RuleEffect.Poison:ApplyStatus(enemy,!enemy,CardEffect.Poison,value,code,BattleEventSource.Sequence);break;
    case RuleEffect.Strength:ApplyStatus(enemy,enemy,CardEffect.Strength,value,code,BattleEventSource.Sequence);break;
    case RuleEffect.IronWall:ApplyStatus(enemy,enemy,CardEffect.IronWall,value,code,BattleEventSource.Sequence,shieldBonusPercent:rule.ShieldBonusPercent);break;
    case RuleEffect.Regeneration:ApplyStatus(enemy,enemy,CardEffect.Regeneration,value,code,BattleEventSource.Sequence);break;
    case RuleEffect.Bleed:ApplyStatus(enemy,!enemy,CardEffect.Bleed,value,code,BattleEventSource.Sequence);break;
    case RuleEffect.Vulnerable:ApplyStatus(enemy,!enemy,CardEffect.Vulnerable,value,code,BattleEventSource.Sequence);break;
    case RuleEffect.Parry:ApplyStatus(enemy,enemy,CardEffect.Parry,value,code,BattleEventSource.Sequence);break;
    case RuleEffect.RandomNumbers:
     var numbers=new List<BattleAction>();for(int i=0;i<Math.Min(value,Math.Max(0,ChainLimit-processedActions-pending.Count));i++)numbers.Add(new BattleAction {IsNumberOnly=true,Enemy=enemy,Number=NextBattleNumber(),SourceAction=action.SourceAction});QueueLimited(numbers);break;
    case RuleEffect.LastStand:
     int health=Actor(enemy).Health;int target=(int)((long)Actor(enemy).MaxHealth*Math.Min(100,value)/100);int loss=Math.Max(0,health-target);
     SettleHealthLoss(enemy,loss,code,BattleEventSource.Sequence);if(Health>0&&EnemyHealth>0)SettleDamage(enemy,!enemy,CombatResolver.Clamp(2L*loss),code,BattleEventSource.Sequence);break;
    case RuleEffect.RandomCardEffects:
     var effects=new List<BattleAction>();for(int i=0;i<Math.Min(value,Math.Max(0,ChainLimit-processedActions-pending.Count));i++){var card=GenerateCard();effects.Add(new BattleAction {Card=card,Number=card.Number,Effect=card.Effect,Enemy=enemy,EffectOnly=true,IsRepeat=true,SourceAction=action.SourceAction});}QueueLimited(effects);break;
    case RuleEffect.RepeatSequenceCards:
     var repeats=new List<BattleAction>();for(int i=0;i<Math.Min(value,99);i++)foreach(var participant in action.Participants)if(participant.Card!=null||!participant.IsNumberOnly)repeats.Add(EffectReplay(participant));QueueLimited(repeats);break;
   }
  }
  private EffectPoolEntry DrawEffect(List<EffectPoolEntry> pool,string name){
   var candidates=pool.Where(e=>e.Enabled&&e.Weight>0&&!float.IsNaN(e.Weight)&&!float.IsInfinity(e.Weight)&&e.InPool(name)).ToArray();
   if(candidates.Length==0)throw new InvalidOperationException("No enabled runtime effects in pool: "+name);
   return candidates[WeightedChoice(candidates.Select(e=>(double)e.Weight).ToArray())];
  }
  public SequenceEffect DrawSequenceEffect(string pool=null){
   if(setup==null||setup.SequencePool.Count==0)return CopyEffect(Player.AvailableEffects[random.Next(Player.AvailableEffects.Count)]);
   // An explicit pool is an intentional restriction; otherwise draw the pool first.
   var e=DrawEffect(setup.SequencePool,string.IsNullOrWhiteSpace(pool)?DrawPool(setup.SequenceEffectPools):pool);int x=random.Next(e.Min,e.Max+1);return new SequenceEffect {Code=e.Code,ShieldBonusPercent=e.ShieldBonusPercent,Effect=e.Sequence,BaseX=x,Multiplier=e.Multiplier,FixedValue=e.FixedValue,Value=CombatResolver.Clamp((long)x*e.Multiplier)};
  }
  public RuntimeEngraving DrawEngraving(string pool=null){
   if(setup==null||setup.EngravingPool.Count==0)return new RuntimeEngraving {Code="LegacyEngraving",Effect=EngravingEffect.FollowUpDamage,BaseX=5};
   var e=DrawEffect(setup.EngravingPool,pool);return new RuntimeEngraving {Code=e.Code,Effect=e.Engraving,BaseX=random.Next(e.Min,e.Max+1),Multiplier=e.Multiplier,FixedValue=e.FixedValue};
  }
  public bool HasConfiguredEngravings=>setup!=null&&setup.EngravingPool.Count>0;
  public RuntimeEngraving GenerateEngraving(RuntimeEngraving template){var e=template.Copy();if(e.RollX){e.BaseX=random.Next(e.MinX,e.MaxX+1);e.RollX=false;}return e;}
 }
}
