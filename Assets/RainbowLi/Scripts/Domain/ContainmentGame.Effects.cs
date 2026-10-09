using System;
using System.Collections.Generic;
namespace RainbowLi {
 public sealed partial class ContainmentGame {
  private CombatantState Actor(bool enemy)=>enemy?(CombatantState)Enemy:Player;
  private int NextBattleNumber(){battleRandomState^=battleRandomState<<13;battleRandomState^=battleRandomState>>17;battleRandomState^=battleRandomState<<5;return (int)(battleRandomState%6)+1;}
  private void ApplyCardEffect(BattleAction action){
   var owner=Actor(action.Enemy);var target=Actor(!action.Enemy);var card=action.Card;
   int value=(card==null?action.Value:card.Value)+owner.Power;
   if(action.Effect==CardEffect.Strike||action.Effect==CardEffect.IncreasedStrike||action.Effect==CardEffect.InverseStrike)target.ReceiveDamage(owner.OutgoingDamage(value));
   else if(action.Effect==CardEffect.Guard)owner.GainShield(value);
   else if(action.Effect==CardEffect.Mend)owner.Heal(value);
   else{
    var spec=card?.Spec??action.EffectSpec;
    bool negative=spec!=null?spec.Polarity==EffectPolarity.Negative:
     action.Effect==CardEffect.Vulnerable||action.Effect==CardEffect.Weaken||action.Effect==CardEffect.Poison||action.Effect==CardEffect.Bleed||action.Effect==CardEffect.Burn||action.Effect==CardEffect.Disorder;
    (negative?target:owner).Statuses.Add(action.Effect,value,spec==null?(EffectLifetime?)null:spec.Lifetime);
   }
  }
  private bool BleedBefore(BattleAction action){var owner=Actor(action.Enemy);int amount=owner.Statuses[CardEffect.Bleed];if(amount>0){owner.LoseHealth(amount);owner.Statuses[CardEffect.Bleed]--;Log((action.Enemy?"敌方":"玩家")+"流血，失去 "+amount+" 生命。");}return owner.Health>0;}
  private void AfterBehavior(BattleAction action){
   var owner=Actor(action.Enemy);var target=Actor(!action.Enemy);if(owner.Health<=0||target.Health<=0)return;
   target.Statuses.Add(CardEffect.Burn,owner.Statuses[CardEffect.Ignite]);target.Statuses.Add(CardEffect.Poison,owner.Statuses[CardEffect.Envenom]);
   var added=new List<BattleAction>();
   if(owner.Statuses[CardEffect.Reciprocity]>0&&!action.IsRepeat)added.Add(new BattleAction {Card=action.Card,Enemy=action.Enemy,Number=action.Number,Effect=action.Effect,EffectSpec=action.EffectSpec,Value=action.Value,Rule=action.Rule,SourceAction=action.SourceAction??action,IsRepeat=true,SkillName=action.SkillName,Special=action.Special});
   for(int i=0;i<Math.Min(4096,owner.Statuses[CardEffect.RandomNumbers]);i++)added.Add(new BattleAction {IsNumberOnly=true,Enemy=action.Enemy,Number=NextBattleNumber()});
   pending.InsertRange(0,added);
  }
  private void FillNumber(BattleAction action){
   var status=Actor(action.Enemy).Statuses;int number=action.Number;
   if(status[CardEffect.Disorder]>0||status[CardEffect.Mutation]>0)number=NextBattleNumber();
   if(status[CardEffect.Mutation]>0)status[CardEffect.Mutation]--;
   number+=status[CardEffect.AddNumber]-status[CardEffect.SubtractNumber];NumberStream.Add(number);LastAction=action.Label+" · 填入 "+number;Log("填入数字 "+number+(action.Enemy?" / 敌方":" / 玩家"));
   bool matched=MatchSequences(action.Enemy);ApplyEngraving(action.Card,matched);
  }
  private void StartStatusEffects(CombatantState actor,bool enemy){
   var status=actor.Statuses;string name=enemy?"敌方":"玩家";
   int burn=status[CardEffect.Burn];if(burn>0){actor.ReceiveDamage(burn);status[CardEffect.Burn]--;Log(name+"烧伤结算 "+burn);}
   if(actor.Health<=0)return;
   int regen=status[CardEffect.Regeneration];if(regen>0){actor.Heal(regen);status[CardEffect.Regeneration]--;Log(name+"再生恢复 "+regen);}
   int barrier=status[CardEffect.Barrier];if(barrier>0){actor.GainShield(barrier);status[CardEffect.Barrier]/=2;Log(name+"壁垒护盾 "+barrier);}
  }
  private bool QueueEndTicks(){
   if(endTicksQueued)return false;endTicksQueued=true;
   if(Player.Statuses[CardEffect.Poison]>0)pending.Add(new BattleAction {IsStatusTick=true,Enemy=false,EndTick=true});
   if(Enemy.Statuses[CardEffect.Poison]>0)pending.Add(new BattleAction {IsStatusTick=true,Enemy=true,EndTick=true});
   return pending.Count>0;
  }
  private void ApplyStatusTick(BattleAction action){var owner=Actor(action.Enemy);int amount=owner.Statuses[CardEffect.Poison];action.Value=amount;owner.ReceiveDamage(amount);owner.Statuses[CardEffect.Poison]/=2;}
  private bool CheckBattleEnd(){if(Health<=0){Phase=GamePhase.Lost;Message="收容失败。重新开始可生成一副新卡组。";pending.Clear();return true;}if(EnemyHealth<=0){FinishBattle();return true;}return false;}
 }
}
