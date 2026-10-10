using System;
using System.Linq;
using RainbowLi;
using RainbowLi.UnityGame;
public static class EffectTargetChecks {
 static int checks;
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);checks++;}
 static readonly CardEffect[] negative={CardEffect.Vulnerable,CardEffect.Weaken,CardEffect.Poison,CardEffect.Bleed,CardEffect.Burn,CardEffect.Disorder};
 static readonly CardEffect[] attacks={CardEffect.Strike,CardEffect.IncreasedStrike,CardEffect.InverseStrike};
 static ContainmentGame Battle(){var g=new ContainmentGame(314159,new GameSetup{PlayerHealth=300,EnemyHealth=300,EnemyActions=0});g.StartBattle();g.PlayerRules.Clear();g.EnemyRule.Numbers=new[]{0,0,0};foreach(var r in g.ArenaRules)r.Numbers=new[]{0,0,0};g.Hand.Clear();return g;}
 static BattleAction Action(CardEffect effect,bool enemy,EffectPolarity? polarity){
  var spec=polarity.HasValue?new CardEffectSpec{Code="target-check",Name=effect.ToString(),Text="X",Effect=effect,Polarity=polarity.Value,Lifetime=EffectLifetime.Persistent,Min=3,Max=3}:null;
  return enemy?new BattleAction{Enemy=true,Effect=effect,EffectSpec=spec,Number=3,Value=3}:new BattleAction{Effect=effect,Number=3,Card=new Card{Number=3,Effect=effect,Spec=spec,EffectAmount=3,AttackBase=0,BaseBonus=0,UpgradeBonus=0}};
 }
 public static void Run(){
  foreach(CardEffect effect in Enum.GetValues(typeof(CardEffect)))foreach(bool enemy in new[]{false,true})foreach(EffectPolarity? polarity in new EffectPolarity?[]{null,EffectPolarity.Positive,EffectPolarity.Negative,EffectPolarity.Special}){
   var g=Battle();var a=Action(effect,enemy,polarity);g.Timeline.Clear();g.Timeline.Add(a);g.Health=g.EnemyHealth=200;
   if(enemy){g.Hand.Add(new Card{Number=1,Effect=CardEffect.Guard});g.AddCard(g.Hand[0]);}else g.Hand.Add(a.Card);
   Check(g.Confirm(),"confirm target check");g.Step();bool opponent=attacks.Contains(effect)||negative.Contains(effect);bool toEnemy=opponent?!enemy:enemy;
   Check(CardMotionController.TargetsEnemy(a)==toEnemy,"animation target: "+effect+" enemy="+enemy+" polarity="+polarity);
   var owner=enemy?(CombatantState)g.Enemy:g.Player;var other=enemy?(CombatantState)g.Player:g.Enemy;
   if(attacks.Contains(effect)){Check(owner.Health==200&&other.Health<200,"attack hits opponent: "+effect);}
   else if(effect==CardEffect.Guard){Check(owner.Shield==3&&other.Shield==0,"shield goes to owner");}
   else if(effect==CardEffect.Mend){Check(owner.Health==203&&other.Health==200,"heal goes to owner");}
   else {Check((opponent?other:owner).Statuses[effect]>0&&(opponent?owner:other).Statuses[effect]==0,"status recipient: "+effect+" enemy="+enemy+" polarity="+polarity);}
  }
  foreach(bool enemy in new[]{false,true})foreach(RuleEffect effect in Enum.GetValues(typeof(RuleEffect)))Check(CardMotionController.TargetsEnemy(new BattleAction{Enemy=enemy,Rule=new SequenceRule{Effect=effect}})==(effect==RuleEffect.Damage?!enemy:enemy),"sequence target "+effect);
  foreach(bool enemy in new[]{false,true})Check(CardMotionController.TargetsEnemy(new BattleAction{Enemy=enemy,IsStatusTick=true})==enemy,"DOT acts on affected actor");
  Console.WriteLine("PASS: "+checks+" effect target/polarity/ownership checks.");
 }
}
