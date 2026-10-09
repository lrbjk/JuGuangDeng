using System;
using System.Linq;
using RainbowLi;
public static class TableEffectChecks {
 static int checks;static int seed=17;
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);checks++;}
 static Card Card(CardEffect effect,int number=3,int x=2){bool direct=CombatStatuses.IsDirect(effect);bool negative=effect==CardEffect.Vulnerable||effect==CardEffect.Weaken||effect==CardEffect.Poison||effect==CardEffect.Bleed||effect==CardEffect.Burn||effect==CardEffect.Disorder;var temporary=new[]{CardEffect.Strength,CardEffect.Vulnerable,CardEffect.RandomNumbers,CardEffect.Chant,CardEffect.Reciprocity,CardEffect.Ignite,CardEffect.Envenom,CardEffect.Disorder,CardEffect.Mutation,CardEffect.AddNumber,CardEffect.SubtractNumber};return new Card {Number=number,Effect=effect,EffectAmount=x,UpgradeBonus=0,Spec=new CardEffectSpec {Code=effect.ToString(),Name=CombatStatuses.Name(effect),Text="X",Effect=effect,UsesNumber=direct,Min=x,Max=x,Lifetime=direct?EffectLifetime.Direct:temporary.Contains(effect)?EffectLifetime.Temporary:EffectLifetime.Persistent,Polarity=negative?EffectPolarity.Negative:EffectPolarity.Positive}};}
 static ContainmentGame Battle(params Card[] cards){var profile=new GameSetup {PlayerHealth=300,EnemyHealth=300,EnemyAttack=0,GuardInterval=0,EnemyNumbers=new[]{5,6}};var g=new ContainmentGame(seed,profile);g.StartBattle();g.PlayerRules.Clear();foreach(var r in g.ArenaRules)r.Numbers=new[]{0,0,0};g.EnemyRule.Numbers=new[]{0,0,0};g.Hand.Clear();g.Hand.AddRange(cards);for(int i=0;i<cards.Length;i++)g.InsertCard(cards[i],i);return g;}
 static void Start(ContainmentGame g){Check(g.Confirm(),"planning confirms with all enemy actions");}
 static void End(ContainmentGame g){int count=0;while(g.Phase==GamePhase.Resolving&&g.Round==1&&count++<5000)g.Step();Check(count<5000,"turn terminates");}
 public static void Main(){
  for(int n=1;n<=6;n++){
   Check(Card(CardEffect.Strike,n).Value==n,"damage X");Check(Card(CardEffect.Guard,n).Value==n,"shield X");Check(Card(CardEffect.Mend,n).Value==n,"heal X");Check(Card(CardEffect.IncreasedStrike,n).Value==1+n,"damage 1+X");Check(Card(CardEffect.InverseStrike,n).Value==7-n,"damage 7-X");
  }
  Check(Card(CardEffect.InverseStrike,9).Value==1,"inverse damage lower bound");
  var g=Battle(Card(CardEffect.Strike));Start(g);g.Step();Check(g.EnemyHealth==297,"table damage ignores legacy attack/bonus");
  g=Battle(Card(CardEffect.Guard));Start(g);g.Step();Check(g.Shield==3,"table guard");
  g=Battle(Card(CardEffect.Mend));g.Health=10;Start(g);g.Step();Check(g.Health==13,"table heal");
  g=Battle(Card(CardEffect.Strength,1,2),Card(CardEffect.Strike,3));Start(g);g.Step();g.Step();Check(g.EnemyHealth==295,"strength adds only to damage");End(g);Check(g.Player.Statuses[CardEffect.Strength]==0,"temporary strength expires");
  g=Battle(Card(CardEffect.Vulnerable,1,2),Card(CardEffect.Strike,3));Start(g);g.Step();g.Step();Check(g.EnemyHealth==295,"vulnerability applies to opposing incoming damage");
  g=Battle(Card(CardEffect.Weaken,1,2));g.Timeline.Where(a=>a.Enemy).ToList().ForEach(a=>a.Value=8);Start(g);End(g);Check(g.Health==288&&g.Enemy.Statuses[CardEffect.Weaken]==1,"weak 25% damage and end decay");
  g=Battle(Card(CardEffect.Regeneration,1,3));g.Health=10;Start(g);End(g);Check(g.Health==13&&g.Player.Statuses[CardEffect.Regeneration]==2,"regeneration start and decay");
  g=Battle(Card(CardEffect.Fortify,1,2));Start(g);g.Step();g.Player.ReceiveDamage(8);Check(g.Health==294,"fortify incoming reduction");End(g);Check(g.Player.Statuses[CardEffect.Fortify]==1,"fortify end decay");
  g=Battle(Card(CardEffect.Poison,1,6));var forecast=g.Forecast();Start(g);End(g);Check(g.EnemyHealth==294&&g.Enemy.Statuses[CardEffect.Poison]==3,"poison end damage halves");Check(forecast.EnemyHealth==g.EnemyHealth,"poison included in forecast");
  g=Battle(Card(CardEffect.Bleed,1,3));Start(g);End(g);Check(g.EnemyHealth==295&&g.Enemy.Statuses[CardEffect.Bleed]==1,"bleed every real enemy action");
  g=Battle(Card(CardEffect.IronWall,1,2),Card(CardEffect.Guard,4));Start(g);g.Step();g.Step();Check(g.Shield==5,"iron wall shield +25% floors");
  g=Battle(Card(CardEffect.Parry,1,1));Start(g);g.Step();g.Shield=5;g.Player.ReceiveDamage(10);Check(g.Health==300&&g.Shield==5&&g.Player.Statuses[CardEffect.Parry]==0,"parry consumes one without using shield");g.Player.ReceiveDamage(10);Check(g.Health==295&&g.Shield==0,"parry only blocks once");
  g=Battle(Card(CardEffect.Barrier,1,6));Start(g);End(g);Check(g.Shield==6&&g.Player.Statuses[CardEffect.Barrier]==3,"barrier next start gains shield and halves");
  g=Battle(Card(CardEffect.DamageCap,1,12));Start(g);g.Step();g.Player.ReceiveDamage(100);Check(g.Health==288,"damage capped per hit");End(g);Check(g.Player.Statuses[CardEffect.DamageCap]==12,"damage cap persists");
  g=Battle(Card(CardEffect.RandomNumbers,3,2));forecast=g.Forecast();var repeated=g.Forecast();Check(forecast.Numbers.SequenceEqual(repeated.Numbers)&&g.NumberStream.Count==0,"random forecast stable and pure");Start(g);g.Step();g.Step();g.Step();Check(g.NumberStream.Count==3,"random appends exactly X digits without recursive actions");g.Step();g.Step();Check(forecast.Numbers.SequenceEqual(g.NumberStream),"random forecast matches actual");
  g=Battle(Card(CardEffect.Chant,1,2),Card(CardEffect.Guard,2),Card(CardEffect.Guard,3));var rule=ContainmentGame.Rule("chant",new[]{1,2,3},RuleEffect.Damage,4);g.PlayerRules.Add(rule);forecast=g.Forecast();Start(g);End(g);Check(rule.Triggers==0&&g.EnemyHealth==288,"chant repeats matched effect two additional times");Check(forecast.RuleHits[rule.Name]==3,"chant forecast trigger count");
  g=Battle(Card(CardEffect.Reciprocity,1,1),Card(CardEffect.Strike,3));forecast=g.Forecast();Start(g);End(g);Check(g.EnemyHealth==294&&forecast.EnemyHealth==294,"reciprocity executes all behavior once extra, no recursion");
  g=Battle(Card(CardEffect.Burn,1,4));Start(g);End(g);Check(g.EnemyHealth==296&&g.Enemy.Statuses[CardEffect.Burn]==3,"burn next round damage and decrease");
  g=Battle(Card(CardEffect.Ignite,1,1),Card(CardEffect.Guard,2));Start(g);g.Step();g.Step();Check(g.Enemy.Statuses[CardEffect.Burn]==2,"ignite applies after each behavior");
  g=Battle(Card(CardEffect.Envenom,1,2),Card(CardEffect.Guard,2));Start(g);g.Step();g.Step();Check(g.Enemy.Statuses[CardEffect.Poison]==4,"envenom applies two poison each behavior");
  g=Battle(Card(CardEffect.Disorder,1,1));forecast=g.Forecast();Start(g);g.Step();g.Step();g.Step();Check(forecast.Numbers.SequenceEqual(g.NumberStream),"disorder randomizes opponent digits deterministically");
  g=Battle(Card(CardEffect.Mutation,3,2),Card(CardEffect.Guard,4),Card(CardEffect.Guard,5));Start(g);g.Step();Check(g.Player.Statuses[CardEffect.Mutation]==1,"mutation consumes one per filled digit");g.Step();g.Step();Check(g.Player.Statuses[CardEffect.Mutation]==0&&g.NumberStream[2]==5,"mutation affects exactly X digits");
  g=Battle(Card(CardEffect.AddNumber,3,2),Card(CardEffect.Guard,4));Start(g);g.Step();g.Step();Check(g.NumberStream.Take(2).SequenceEqual(new[]{5,6}),"add value alters stream, not card value");
  g=Battle(Card(CardEffect.SubtractNumber,3,2),Card(CardEffect.Guard,1));Start(g);g.Step();g.Step();Check(g.NumberStream.Take(2).SequenceEqual(new[]{1,-1}),"subtract value preserves arithmetic result");
  g=Battle(Card(CardEffect.Guard,3));g.Player.Statuses.Add(CardEffect.Bleed,3);g.Shield=100;Start(g);g.Step();Check(g.Health==297&&g.Player.Statuses[CardEffect.Bleed]==2,"bleed bypasses shield");
  g=Battle(Card(CardEffect.Poison,1,6));g.EnemyHealth=3;forecast=g.Forecast();Start(g);End(g);Check(g.Phase==GamePhase.Reward&&forecast.Victory,"poison can finish battle and is forecast");
  g=Battle(Card(CardEffect.Guard,1));g.Player.Statuses.Add(CardEffect.Burn,2);g.Health=1;Start(g);End(g);Check(g.Phase==GamePhase.Lost,"burn start can kill before hand draw");
  g=Battle(Card(CardEffect.Bleed,1,3));g.EnemyHealth=2;g.Timeline.First(a=>a.Enemy).Value=100;Start(g);End(g);Check(g.Phase==GamePhase.Reward&&g.Health==300,"fatal bleed cancels enemy behavior");
  for(int seed=1;seed<=100;seed++){
   var profile=new GameSetup {RandomStartingDeck=true};foreach(CardEffect e in Enum.GetValues(typeof(CardEffect)))profile.CardEffects.Add(Card(e).Spec);
   g=new ContainmentGame(seed,profile);Check(g.Deck.Count==18&&Enumerable.Range(1,6).All(n=>g.Deck.Count(c=>c.Number==n)==3),"all-effect random starter preserves three groups");Check(g.Deck.All(c=>c.Spec!=null&&c.EffectAmount>=c.Spec.Min&&c.EffectAmount<=c.Spec.Max),"generated amounts within effect range");Check(g.GenerateCard(6).Number==6,"directed generation uses all-effect pool");
  }
  for(int iteration=0;iteration<20;iteration++){seed=100+iteration;
   g=Battle(Card(CardEffect.Reciprocity,1,1),Card(CardEffect.RandomNumbers,2,2),Card(CardEffect.Chant,3,2),Card(CardEffect.Strike,4));
   g.PlayerRules.Add(new SequenceRule {Name="stacked chain",Numbers=new[]{2,2,2},Effect=RuleEffect.Power,Value=0,InsertsTwo=true});
   var expected=g.Forecast();var again=g.Forecast();Check(expected.Numbers.SequenceEqual(again.Numbers)&&expected.Health==again.Health,"stacked random/replay/chant preview stable");
   Start(g);int[] completedNumbers=new int[0];int steps=0;while(g.Phase==GamePhase.Resolving&&g.Round==1&&steps++<5000){g.Step();if(g.Round==1)completedNumbers=g.NumberStream.ToArray();}
   Check(steps<5000&&expected.Numbers.SequenceEqual(completedNumbers)&&expected.EnemyHealth==g.EnemyHealth,"combined effects terminate and match real forecast");
  }
  var state=new PlayerState(100);state.Statuses.Add(CardEffect.Strength,2);state.Statuses.Add(CardEffect.Weaken,1);Check(state.OutgoingDamage(5)==5,"outgoing strength then weak rounds down");state.Statuses.Add(CardEffect.Vulnerable,2);state.Statuses.Add(CardEffect.Fortify,1);state.Statuses.Add(CardEffect.DamageCap,3);state.Shield=2;state.ReceiveDamage(10);Check(state.Health==99&&state.Shield==0,"incoming vulnerability, fortify, cap then shield");
  Console.WriteLine("PASS: "+checks+" spreadsheet card effect assertions");
 }
}
