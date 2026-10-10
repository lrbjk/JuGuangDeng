using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RainbowLi;
public static class AdvancedRuntimeChecks {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
 static void Invoke(ContainmentGame g,string name,object argument)=>typeof(ContainmentGame).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new[]{argument});
 static Card Card(int n,EngravingEffect? engraving=null,int x=2)=>new Card {Number=n,Effect=CardEffect.Guard,BaseBonus=0,Engraved=engraving.HasValue,Engraving=engraving.HasValue?new RuntimeEngraving {Code="engraving",Effect=engraving.Value,BaseX=x}:null};
 static ContainmentGame Battle(params Card[] cards){
  var setup=new GameSetup {PlayerHealth=1000,EnemyHealth=1000,EnemyAttack=0};setup.InitialDeck.AddRange(Enumerable.Range(0,6).Select(i=>i<cards.Length?cards[i]:Card(6)));
  var g=new ContainmentGame(77,setup);g.StartBattle();g.PlayerRules.Clear();foreach(var r in g.ArenaRules)r.Numbers=new[]{9,9,9};g.EnemyRule.Numbers=new[]{9,9,9};
  g.Hand.Clear();g.Hand.AddRange(cards);foreach(var c in cards)g.AddCard(c);for(int i=0;i<cards.Length;i++){int at=g.Timeline.FindIndex(a=>a.Card==cards[i]);while(at>i){g.MoveAction(at,-1);at--;}}
  foreach(var a in g.Timeline.Where(a=>a.Enemy)){a.Value=0;a.Number=6;}return g;
 }
 static void Resolve(ContainmentGame g){g.Confirm();int limit=0;while(g.Phase==GamePhase.Resolving&&g.CaptureSave().Pending.Length>0&&limit++<5000)g.Step();Check(limit<5000,"bounded resolution");}
 public static void Main(){
  foreach(bool enemy in new[]{false,true})foreach(var effect in new[]{RuleEffect.Heal,RuleEffect.Burn,RuleEffect.Poison,RuleEffect.Strength,RuleEffect.Bleed,RuleEffect.Vulnerable,RuleEffect.Parry,RuleEffect.Damage,RuleEffect.Shield,RuleEffect.Power}){
   var g=Battle();var own=enemy?(CombatantState)g.Enemy:g.Player;var target=enemy?(CombatantState)g.Player:g.Enemy;own.Health=997;
   Invoke(g,"ApplyRule",new BattleAction {Enemy=enemy,Rule=new SequenceRule {Effect=effect,Value=6,Code="test"}});
   if(effect==RuleEffect.Heal)Check(own.Health==1000,"healing capped both sides");
   else if(effect==RuleEffect.Damage)Check(target.Health==994,"sequence damage both sides");
   else if(effect==RuleEffect.Shield)Check(own.Shield==6,"sequence shield both sides");
   else if(effect==RuleEffect.Power)Check(own.Power==6,"boost both sides");
   else {var status=(CardEffect)Enum.Parse(typeof(CardEffect),effect.ToString());Check((effect==RuleEffect.Strength||effect==RuleEffect.Parry?own:target).Statuses[status]==6,"sequence status recipient "+effect);}
   Check(g.LastSettlementEvents.Any(e=>e.EffectCode=="test"),"sequence effect code event");
   bool opponent=effect==RuleEffect.Damage||effect==RuleEffect.Burn||effect==RuleEffect.Poison||effect==RuleEffect.Bleed||effect==RuleEffect.Vulnerable;Check(CardEffectTargeting.TargetsEnemy(new BattleAction {Enemy=enemy,Rule=new SequenceRule {Effect=effect}})==(opponent?!enemy:enemy),"sequence shared presentation recipient");
  }
  foreach(bool enemy in new[]{false,true}){
   var g=Battle();var own=enemy?(CombatantState)g.Enemy:g.Player;var target=enemy?(CombatantState)g.Player:g.Enemy;own.Health=800;own.Shield=50;own.Statuses.Add(CardEffect.Parry,2);
   Invoke(g,"ApplyRule",new BattleAction {Enemy=enemy,Rule=new SequenceRule {Effect=RuleEffect.LastStand,Value=50}});
   Check(own.Health==500&&own.Shield==50&&own.Statuses[CardEffect.Parry]==2&&target.Health==400,"last stand maximum health/direct loss");
   own.Health=300;int before=target.Health;Invoke(g,"ApplyRule",new BattleAction {Enemy=enemy,Rule=new SequenceRule {Effect=RuleEffect.LastStand,Value=50}});Check(own.Health==300&&target.Health==before,"last stand below threshold");
  }
  foreach(var effect in new[]{EngravingEffect.PoisonExplosion,EngravingEffect.BurnExplosion})foreach(bool enemy in new[]{false,true}){
   var g=Battle();var target=enemy?(CombatantState)g.Player:g.Enemy;var status=effect==EngravingEffect.PoisonExplosion?CardEffect.Poison:CardEffect.Burn;var card=Card(3,effect);target.Statuses.Add(status,3);
   typeof(ContainmentGame).GetMethod("ApplyEngraving",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new object[]{card,true,enemy});Check(target.Statuses[status]==0&&target.Health==(effect==EngravingEffect.PoisonExplosion?996:994),"explosion consumes, floors and targets");
   int hp=target.Health;typeof(ContainmentGame).GetMethod("ApplyEngraving",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(g,new object[]{card,true,enemy});Check(target.Health==hp,"zero explosion");
  }
  var stream=new List<NumberEntry>{new NumberEntry {Number=1,Action=new BattleAction {Card=Card(1,EngravingEffect.Bridge)}},new NumberEntry {Number=6},new NumberEntry {Number=2},new NumberEntry {Number=3}};
  Check(SequenceMatcher.Match(stream,new[]{1,2,3})!=null,"bridge skips position");stream[0].Blocked=true;Check(SequenceMatcher.Match(stream,new[]{1,2,3})==null,"blocked bridge excluded");
  stream=new List<NumberEntry>{new NumberEntry {Number=5,Candidates=new[]{1,5}},new NumberEntry {Number=6,Candidates=new[]{1,2,3,4,5,6}},new NumberEntry {Number=3}};Check(SequenceMatcher.Match(stream,new[]{1,2,3})!=null,"candidate and wildcard match");
  var growth=Card(3,EngravingEffect.Growth);var battle=Battle(Card(1),Card(2),growth);battle.PlayerRules.Add(ContainmentGame.Rule("growth",new[]{1,2,3},RuleEffect.Shield,4));uint state=battle.RandomState;
  var preview=battle.Forecast();Check(growth.PermanentGrowth==0&&battle.RandomState==state,"preview growth and RNG isolated");Resolve(battle);Check(growth.PermanentGrowth==2,"growth once per match");
  var charge=Card(1,EngravingEffect.Charge);battle=Battle(charge);battle.Player.Statuses.Add(CardEffect.Reciprocity,1);Resolve(battle);Check(charge.BattleCharge==1,"repeat doesn't charge twice");
  var head=Card(1,EngravingEffect.SequenceHeadBonus);battle=Battle(head,Card(2),Card(3));battle.PlayerRules.Add(ContainmentGame.Rule("head",new[]{1,2,3},RuleEffect.Shield,4));Resolve(battle);Check(battle.Shield==13,"head bonus adds only three");
  var inherited=Card(1,EngravingEffect.Inherit);battle=Battle(inherited,Card(2));Resolve(battle);Check(battle.Shield==4,"inherit once to next player card");
  var only=Card(1,EngravingEffect.OnlyPlayerCard);battle=Battle(only);var discarded=Card(4);battle.Hand.Add(discarded);Resolve(battle);Check(battle.Shield==7,"only card samples available discarded card with bonus");
  foreach(int seed in Enumerable.Range(1,30)){
   var roulette=Card(1,EngravingEffect.Roulette);battle=Battle(roulette);var save=battle.CaptureSave();save.RandomState=(uint)seed;battle=ContainmentGame.RestoreSave(save);Resolve(battle);Check(battle.Shield==0||battle.Shield==2,"roulette zero or double");
  }
  battle=Battle(Card(1),Card(2),Card(3));battle.PlayerRules.Add(ContainmentGame.Rule("repeat",new[]{1,2,3},RuleEffect.RepeatSequenceCards,2));Resolve(battle);Check(battle.Shield==18&&battle.NumberStream.Count==5,"repeat participants effects only");
  battle=Battle(Card(1),Card(2),Card(3));battle.PlayerRules.Add(ContainmentGame.Rule("random",new[]{1,2,3},RuleEffect.RandomNumbers,4));Resolve(battle);Check(battle.NumberStream.Count==9&&battle.NumberStream.All(n=>n>=1&&n<=6),"random insertion legality");
  battle=Battle(Card(1,EngravingEffect.RandomizeFollowing),Card(2),Card(3));Resolve(battle);Check(battle.NumberStream[0]==1&&battle.NumberStream.Skip(1).All(n=>n>=1&&n<=6),"randomization excludes current card");
  var mixed=new GameSetup {PlayerHealth=1000,EnemyHealth=1000,EnemyAttack=0};mixed.CardPool.Add(new Card {Number=2,Effect=CardEffect.Mend,BaseBonus=0});
  var randomGame=new ContainmentGame(19,mixed);randomGame.StartBattle();randomGame.Health=900;Invoke(randomGame,"ApplyRule",new BattleAction {Rule=new SequenceRule {Effect=RuleEffect.RandomCardEffects,Value=1}});randomGame.Confirm(); // No player selection: manually saved resolution phase for the queued test.
  var randomSave=randomGame.CaptureSave();randomSave.Phase=GamePhase.Resolving;randomGame=ContainmentGame.RestoreSave(randomSave,mixed);randomGame.Step();Check(randomGame.Health==902&&randomGame.EnemyHealth==1000,"random card uses generated behavior");
  var budget=new BattleActionQueue {Budget=4};budget.AddRange(Enumerable.Range(0,10).Select(i=>new BattleAction()));Check(budget.Count==4&&budget.Truncated,"shared insertion budget");budget.Budget=2;Check(budget.Count==2,"lowered budget truncates pending chain");
  var setupPool=new GameSetup();setupPool.SequencePool.Add(new EffectPoolEntry {Code="disabled",Enabled=false,Weight=1000,Min=1,Max=1});setupPool.SequencePool.Add(new EffectPoolEntry {Code="valid",Sequence=RuleEffect.Heal,Pools="heal",Weight=10,Min=2,Max=4,Multiplier=2});
  var game=new ContainmentGame(12,setupPool);for(int i=0;i<40;i++){var e=game.DrawSequenceEffect("heal");Check(e.Code=="valid"&&e.Value==e.BaseX*2&&e.BaseX>=2&&e.BaseX<=4,"weighted enabled pool and single multiplier");}
  bool rejected=false;try{game.DrawSequenceEffect("missing");}catch(InvalidOperationException){rejected=true;}Check(rejected,"empty pool explicit error");
  battle=Battle(Card(1,EngravingEffect.Charge),Card(2),Card(3));battle.ConfigureRoute(RouteGenerator.Generate(88));battle.Route.Enter(battle.Route.Graph.Nodes[0].id);battle.PlayerRules.Add(ContainmentGame.Rule("save",new[]{1,2,3},RuleEffect.RandomNumbers,3));battle.Confirm();battle.Step();
  var restored=ContainmentGame.RestoreSave(battle.CaptureSave());Check(restored.Deck[0].InstanceId==battle.Deck[0].InstanceId&&restored.Route.ActiveId==battle.Route.ActiveId,"save IDs and active route");
  while(battle.Phase==GamePhase.Resolving&&battle.CaptureSave().Pending.Length>0){battle.Step();restored.Step();Check(battle.RandomState==restored.RandomState&&battle.Health==restored.Health&&battle.EnemyHealth==restored.EnemyHealth&&battle.NumberStream.SequenceEqual(restored.NumberStream),"saved continuation equivalence");}
  var legacy=battle.CaptureSave();legacy.Version=1;legacy.Cards[0].InstanceId=null;restored=ContainmentGame.RestoreSave(legacy);Check(!string.IsNullOrEmpty(restored.Deck[0].InstanceId),"legacy ID migration");legacy.Version=99;rejected=false;try{ContainmentGame.RestoreSave(legacy);}catch(ArgumentException){rejected=true;}Check(rejected,"future saves rejected");
  var fresh=new ContainmentGame(52);var freshRestored=ContainmentGame.RestoreSave(fresh.CaptureSave());Check(freshRestored.Phase==GamePhase.Map&&freshRestored.Deck.Count==18,"fresh map save without enemy encounter");
  var rng=new RecoverableRandom(22);for(int i=0;i<20;i++)rng.Next();uint saved=rng.State;var clone=new RecoverableRandom(1){State=saved};for(int i=0;i<40;i++)Check(rng.Next(1,7)==clone.Next(1,7),"restored random stream");
  Console.WriteLine("PASS: "+checks+" advanced effect, random and save assertions");
 }
}
