using System;
using System.Collections.Generic;
namespace RainbowLi {
 public sealed partial class ContainmentGame {
  private CombatantState Actor(bool enemy)=>enemy?(CombatantState)Enemy:Player;
  private int NextBattleNumber(){battleRandomState^=battleRandomState<<13;battleRandomState^=battleRandomState>>17;battleRandomState^=battleRandomState<<5;return (int)(battleRandomState%6)+1;}
  private void ApplyCardEffect(BattleAction action){
   var card=action.Card;
   if(card!=null&&card.HasOrderedEffects){
    for(int i=0;i<card.Effects.Count;i++){
     settlingEffectIndex=i;var effect=card.Effects[i];
     ApplyEffect(action.Enemy,effect.Spec.Effect,effect.ResolveValue(card.Number,card.Upgrade),effect.Spec);
     if(Health<=0||EnemyHealth<=0)return;
    }
   }else {settlingEffectIndex=0;ApplyEffect(action.Enemy,action.Effect,card==null?action.Value:card.Value,card?.Spec??action.EffectSpec);}
  }
  private void ApplyEffect(bool enemy,CardEffect effect,int amount,CardEffectSpec spec){
   var owner=Actor(enemy);int value=CombatResolver.Clamp((long)amount+owner.Power);
   string code=string.IsNullOrEmpty(spec?.Code)?effect.ToString():spec.Code;
   var source=settlingAction?.Card!=null?BattleEventSource.Card:BattleEventSource.EnemySkill;
   if(effect==CardEffect.Strike||effect==CardEffect.IncreasedStrike||effect==CardEffect.InverseStrike)SettleDamage(enemy,!enemy,value,code,source);
   else if(effect==CardEffect.Guard)SettleShield(enemy,enemy,value,code,source);
   else if(effect==CardEffect.Mend)SettleHealing(enemy,enemy,value,code,source);
   else{
    var spec=card?.Spec??action.EffectSpec;
    (CardEffectTargeting.TargetsOpponent(action.Effect)?target:owner).Statuses.Add(action.Effect,value,spec==null?(EffectLifetime?)null:spec.Lifetime);
   }
  }
  private bool BleedBefore(BattleAction action){var owner=Actor(action.Enemy);int amount=owner.Statuses[CardEffect.Bleed];if(amount>0){settlingTiming=BattleEventTiming.BeforeAction;SettleHealthLoss(action.Enemy,amount,"Bleed",BattleEventSource.Status);ChangeStatus(action.Enemy,CardEffect.Bleed,amount-1,"Bleed");Log((action.Enemy?"敌方":"玩家")+"流血，失去 "+amount+" 生命。");}settlingTiming=BattleEventTiming.Action;return owner.Health>0;}
  private void AfterBehavior(BattleAction action){
   var owner=Actor(action.Enemy);var target=Actor(!action.Enemy);if(owner.Health<=0||target.Health<=0)return;
   settlingTiming=BattleEventTiming.AfterAction;settlingEffectIndex=-1;
   ApplyStatus(action.Enemy,!action.Enemy,CardEffect.Burn,owner.Statuses[CardEffect.Ignite],"Ignite",BattleEventSource.Status);
   ApplyStatus(action.Enemy,!action.Enemy,CardEffect.Poison,owner.Statuses[CardEffect.Envenom],"Envenom",BattleEventSource.Status);
   var added=new List<BattleAction>();
   if(owner.Statuses[CardEffect.Reciprocity]>0&&!action.IsRepeat)added.Add(new BattleAction {Card=action.Card,Enemy=action.Enemy,Number=action.Number,Effect=action.Effect,EffectSpec=action.EffectSpec,Value=action.Value,Rule=action.Rule,SequenceTrigger=action.SequenceTrigger,SourceAction=action.SourceAction??action,IsRepeat=true,SkillName=action.SkillName,Special=action.Special});
   for(int i=0;i<Math.Min(4096,owner.Statuses[CardEffect.RandomNumbers]);i++)added.Add(new BattleAction {IsNumberOnly=true,Enemy=action.Enemy,Number=NextBattleNumber()});
   pending.InsertRange(0,added);
  }
  private void FillNumber(BattleAction action){
   settlingTiming=BattleEventTiming.Action;settlingEffectIndex=-1;
   var status=Actor(action.Enemy).Statuses;int number=action.Number;
   if(status[CardEffect.Disorder]>0||status[CardEffect.Mutation]>0)number=NextBattleNumber();
   if(status[CardEffect.Mutation]>0)ChangeStatus(action.Enemy,CardEffect.Mutation,status[CardEffect.Mutation]-1,"Mutation");
   number+=status[CardEffect.AddNumber]-status[CardEffect.SubtractNumber];NumberStream.Add(number);LastAction=action.Label+" · 填入 "+number;Log("填入数字 "+number+(action.Enemy?" / 敌方":" / 玩家"));
   bool matched=MatchSequences(action.Enemy);ApplyEngraving(action.Card,matched,action.Enemy);
  }
  private void StartStatusEffects(CombatantState actor,bool enemy){
   var status=actor.Statuses;string name=enemy?"敌方":"玩家";
   int burn=status[CardEffect.Burn];if(burn>0){SettleDamage(enemy,enemy,burn,"Burn",BattleEventSource.Status,false);ChangeStatus(enemy,CardEffect.Burn,burn-1,"Burn");Log(name+"烧伤结算 "+burn);}
   if(actor.Health<=0)return;
   int regen=status[CardEffect.Regeneration];if(regen>0){SettleHealing(enemy,enemy,regen,"Regeneration",BattleEventSource.Status);ChangeStatus(enemy,CardEffect.Regeneration,regen-1,"Regeneration");Log(name+"再生恢复 "+regen);}
   int barrier=status[CardEffect.Barrier];if(barrier>0){SettleShield(enemy,enemy,barrier,"Barrier",BattleEventSource.Status);ChangeStatus(enemy,CardEffect.Barrier,barrier/2,"Barrier");Log(name+"壁垒护盾 "+barrier);}
  }
  private bool QueueEndTicks(){
   if(endTicksQueued)return false;endTicksQueued=true;
   if(Player.Statuses[CardEffect.Poison]>0)pending.Add(new BattleAction {IsStatusTick=true,Enemy=false,EndTick=true});
   if(Enemy.Statuses[CardEffect.Poison]>0)pending.Add(new BattleAction {IsStatusTick=true,Enemy=true,EndTick=true});
   return pending.Count>0;
  }
  private void ApplyStatusTick(BattleAction action){settlingTiming=BattleEventTiming.RoundEnd;int amount=Actor(action.Enemy).Statuses[CardEffect.Poison];action.Value=amount;SettleDamage(action.Enemy,action.Enemy,amount,"Poison",BattleEventSource.Status,false);ChangeStatus(action.Enemy,CardEffect.Poison,amount/2,"Poison");}
  private bool CheckBattleEnd(){if(Phase!=GamePhase.Planning&&Phase!=GamePhase.Resolving)return Phase==GamePhase.Lost||Phase==GamePhase.Won||Phase==GamePhase.Reward;if(Health<=0){Phase=GamePhase.Lost;Message="收容失败。重新开始可生成一副新卡组。";pending.Clear();return true;}if(EnemyHealth<=0){FinishBattle();return true;}return false;}
 }
}
