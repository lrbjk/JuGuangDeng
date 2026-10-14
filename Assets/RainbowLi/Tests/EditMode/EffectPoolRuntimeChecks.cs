using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RainbowLi;

public static class EffectPoolRuntimeChecks {
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static GameSetup Setup(){var s=new GameSetup();for(int i=0;i<6;i++)s.InitialDeck.Add(new Card {Number=i+1});return s;}
 static void Distribution(Dictionary<string,int> counts,int total,params double[] expected){
  string[] ids={"a","b","c"};for(int i=0;i<expected.Length;i++)Check(Math.Abs(counts[ids[i]]/(double)total-expected[i])<.01,"two-stage probability for "+ids[i]);
 }
 static Dictionary<string,int> Counts()=>new Dictionary<string,int>{{"a",0},{"b",0},{"c",0}};
 public static void Main(){
  var s=Setup();s.CardEffects.Add(new CardEffectSpec {Code="a",Pools="默认池,伤害池",Weight=1,Min=3,Max=4});s.CardEffects.Add(new CardEffectSpec {Code="b",Pools="伤害池",Weight=3});s.CardEffects.Add(new CardEffectSpec {Code="c",Pools="默认池",Weight=1});
  s.CardEffectPools.Add(new EffectPoolWeight {Name="默认卡池",Weight=1});s.CardEffectPools.Add(new EffectPoolWeight {Name="伤害卡池",Weight=3});s.CardEffectPools.Add(new EffectPoolWeight {Name="empty",Weight=0});
  s.SequencePool.Add(new EffectPoolEntry {Code="a",Pools="默认池,伤害池",Weight=1,Min=3,Max=4,Multiplier=2});s.SequencePool.Add(new EffectPoolEntry {Code="b",Pools="伤害池",Weight=3,Min=1,Max=1});s.SequencePool.Add(new EffectPoolEntry {Code="c",Pools="默认池",Weight=1,Min=1,Max=1});s.SequencePool.Add(new EffectPoolEntry {Code="disabled",Pools="默认池,伤害池",Enabled=false,Weight=1000000});
  s.SequenceEffectPools.AddRange(s.CardEffectPools.Select(p=>p.Copy()));
  s.EngravingPool.Add(new EffectPoolEntry {Code="a",Weight=1,Min=2,Max=5});s.EngravingPool.Add(new EffectPoolEntry {Code="b",Weight=3,Min=1,Max=1});s.EngravingPool.Add(new EffectPoolEntry {Code="disabled",Enabled=false,Weight=1000000});
  var game=new ContainmentGame(987,s);var cards=Counts();var rules=Counts();var marks=Counts();const int n=30000;
  for(int i=0;i<n;i++){
   var c=game.GenerateCard(4);cards[c.Spec.Code]++;Check(c.Number==4,"directed card number retained");if(c.Spec.Code=="a")Check(c.EffectAmount>=3&&c.EffectAmount<=4,"card X range");
   var r=game.DrawSequenceEffect();rules[r.Code]++;if(r.Code=="a")Check(r.BaseX>=3&&r.BaseX<=4&&r.Value==r.BaseX*2,"sequence X and multiplier");
   var e=game.DrawEngraving();marks[e.Code]++;Check(e.Code!="a"||e.BaseX>=2&&e.BaseX<=5,"engraving X range");
  }
  // a belongs to both pools: P(a)=1/4*1/2+3/4*1/4.
  Distribution(cards,n,.3125,.5625,.125);Distribution(rules,n,.3125,.5625,.125);Distribution(marks,n,.25,.75);
  for(int i=0;i<200;i++)Check(game.DrawSequenceEffect("默认卡池").Code!="b","explicit pool restriction");
  var restored=ContainmentGame.RestoreSave(game.CaptureSave(),s);
  for(int i=0;i<100;i++){
   var a=game.GenerateCard();var b=restored.GenerateCard();Check(a.Spec.Code==b.Spec.Code&&a.Number==b.Number&&a.EffectAmount==b.EffectAmount,"saved card lottery continuation");
   var r=game.DrawSequenceEffect();var t=restored.DrawSequenceEffect();Check(r.Code==t.Code&&r.Value==t.Value,"saved sequence lottery continuation");
   Check(game.DrawEngraving().Code==restored.DrawEngraving().Code,"saved engraving lottery continuation");
  }
  s.RandomStartingDeck=true;s.RepeatWeightMultiplier=.01f;var starter=new ContainmentGame(52,s);s.RepeatWeightMultiplier=1;var sameStarter=new ContainmentGame(52,s);
  Check(starter.Deck.Count==18&&starter.Deck.Select(c=>c.Spec.Code).SequenceEqual(sameStarter.Deck.Select(c=>c.Spec.Code)),"new table lottery unaffected by legacy repeat penalty");
  for(int number=1;number<=6;number++)Check(starter.Deck.Count(c=>c.Number==number)==3,"starter keeps three copies of each number");
  Check(EffectPoolNames.Contains("持续状态效果池,临时状态效果池","持续效果池")&&EffectPoolNames.Contains("临时状态效果池","临时效果池"),"pool aliases");
  var limited=Setup();limited.CardEffects.Add(new CardEffectSpec {Code="ordinary",Pools="普通池",Weight=1});
  foreach(var modifier in new[]{CardEffect.AddNumber,CardEffect.SubtractNumber})limited.CardEffects.Add(new CardEffectSpec {Code=modifier.ToString(),Effect=modifier,Pools="数字池",Min=1,Max=2,Weight=1});
  limited.CardEffectPools.Add(new EffectPoolWeight {Name="普通池",Weight=1});limited.CardEffectPools.Add(new EffectPoolWeight {Name="数字池",Weight=10});
  var limitedGame=new ContainmentGame(71,limited);int one=0,two=0;
  for(int i=0;i<3000;i++){
   var c=limitedGame.GenerateCard();if(c.Effect==CardEffect.AddNumber||c.Effect==CardEffect.SubtractNumber){
    Check(c.EffectAmount==1?c.Number>=2&&c.Number<=4:c.EffectAmount==2&&c.Number==3,"modifier amount restricts original card number");
    if(c.EffectAmount==1)one++;else two++;
   }
   for(int number=1;number<=6;number++){
    c=limitedGame.GenerateCard(number);Check(c.Number==number,"restricted lottery retains requested number");
    if(c.Effect==CardEffect.AddNumber||c.Effect==CardEffect.SubtractNumber)Check(c.EffectAmount==1?number>=2&&number<=4:c.EffectAmount==2&&number==3,"directed modifier excludes incompatible amount");
   }
  }
  Check(one>0&&two>0,"both modifier amounts remain available");limited.RandomStartingDeck=true;
  foreach(var c in new ContainmentGame(19,limited).Deck)if(c.Effect==CardEffect.AddNumber||c.Effect==CardEffect.SubtractNumber)Check(c.EffectAmount==1?c.Number>=2&&c.Number<=4:c.EffectAmount==2&&c.Number==3,"starter modifier number limits");
  var battle=new ContainmentGame(5,Setup());battle.StartBattle();var method=typeof(ContainmentGame).GetMethod("ApplyAdvancedRule",BindingFlags.NonPublic|BindingFlags.Instance);
  foreach(bool enemy in new[]{false,true})foreach(var effect in new[]{RuleEffect.IronWall,RuleEffect.Regeneration}){
   method.Invoke(battle,new object[]{new BattleAction {Enemy=enemy,Rule=ContainmentGame.Rule("status",new[]{1,2,3},effect,3)}});
   var actor=enemy?(CombatantState)battle.Enemy:battle.Player;Check(actor.Statuses[effect==RuleEffect.IronWall?CardEffect.IronWall:CardEffect.Regeneration]==3,"new sequence status recipient");
  }
  Console.WriteLine("PASS: two-stage card/sequence probability, mixed engraving weights, aliases, starter, X, new statuses and save continuation (90000 draws)");
 }
}
