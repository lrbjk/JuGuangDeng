using System;
using System.Linq;
using RainbowLi;
public static class ContentChecks {
 static int checks;
 static void Check(bool yes,string text){if(!yes)throw new Exception(text);checks++;}
 public static void Main(){
  for(int seed=1;seed<=100;seed++){
   var setup=new GameSetup {RandomStartingDeck=true,PlayerAttack=5};var game=new ContainmentGame(seed,setup);
   Check(game.Deck.Count==18&&Enumerable.Range(1,6).All(n=>game.Deck.Count(c=>c.Number==n)==3),"random starter has three complete groups");
   Check(game.Deck.All(c=>c.Value==5+c.Number+3),"all card values use player attack");
   game.StartBattle();Check(game.Hand.Count==6,"six draws per round");
   game.AddCard(game.Hand[0]);var required=game.Timeline.First(a=>a.Enemy);game.Timeline.Remove(required);Check(!game.Confirm(),"missing enemy intent blocks execution");game.Timeline.Add(required);Check(game.Confirm(),"all intents permit execution");
  }
  var profile=new GameSetup {HandSize=12,HandLimit=9};var g=new ContainmentGame(7,profile);g.StartBattle();Check(g.Hand.Count==9,"overflow cards go to discard");
  var skills=new GameSetup();skills.EnemySkills.Add(new EnemySkill {Name="basic",Number=1,Weight=10});skills.EnemySkills.Add(new EnemySkill {Name="special",Number=2,Weight=5,Special=true,Condition=ActivationCondition.RoundAtLeast,Threshold=3});
  var enemy=new EnemyState();enemy.StartEncounter(100,false);var rng=new Random(42);
  Check(enemy.GenerateActions(rng,2,skills).All(a=>!a.Special),"locked specials excluded");
  var actions=enemy.GenerateActions(rng,3,skills).ToArray();Check(actions.Length==3&&actions.Count(a=>a.Special)==1,"unlock adds one extra mandatory special");
  for(int round=4;round<200;round++){actions=enemy.GenerateActions(rng,round,skills).ToArray();Check(actions.Length==2&&actions.Count(a=>a.Special)<=1,"later random draws respect special cap");}
  enemy.StartEncounter(100,false);skills.EnemySkills[1].Condition=ActivationCondition.HealthPercentAtMost;skills.EnemySkills[1].Threshold=50;enemy.Health=51;Check(enemy.ActivateSpecials(1,skills).Count==0,"health condition not early");enemy.Health=50;Check(enemy.ActivateSpecials(1,skills).Count==1&&enemy.ActivateSpecials(1,skills).Count==0,"health special only forced once");
  enemy.ConditionalRules.Add(new ConditionalSequence {Condition=ActivationCondition.HealthPercentAtMost,Threshold=50,Rule=ContainmentGame.Rule("conditional",new[]{1,2,3},RuleEffect.Shield,5)});enemy.ActivateSpecials(1,skills);Check(enemy.ActiveRules.Count()==1,"conditional sequence activated");enemy.Heal(40);enemy.ActivateSpecials(2,skills);Check(enemy.ActiveRules.Count()==1,"activation persists after healing");
  var rule=ContainmentGame.Rule("dual",new[]{1,2,3},RuleEffect.Damage,2);rule.Requirement=PatternRequirement.Straight;rule.AdditionalEffect=new SequenceEffect {Effect=RuleEffect.Shield,Value=8};Check(rule.Accepts(rule.Requirement),"straight requirement");rule.Numbers=new[]{1,1,1};Check(!rule.Accepts(rule.Requirement)&&rule.Accepts(PatternRequirement.Triple),"pattern requirements distinguish triples");rule.Numbers[2]=0;Check(!rule.Accepts(PatternRequirement.Any),"empty slot invalidates rule");
  g=new ContainmentGame(5);g.OpenService(false);while(g.Deck.Count>6)g.Deck.RemoveAt(0);g.RestRemove(g.Deck[0]);Check(g.Deck.Count==6&&!g.ServiceUsed&&g.Message.Contains("6"),"minimum deck blocked with feedback");
  skills.EnemySkills[1].Threshold=80;g=new ContainmentGame(4,skills);g.StartBattle();foreach(var r in g.PlayerRules)r.Numbers=new[]{0,0,0};foreach(var r in g.ArenaRules)r.Numbers=new[]{0,0,0};g.EnemyRule.Numbers=new[]{0,0,0};g.Hand[0].Effect=CardEffect.Strike;g.Hand[0].BaseBonus=10;g.InsertCard(g.Hand[0],0);var prediction=g.Forecast();g.Confirm();int guard=0;while(g.Phase==GamePhase.Resolving&&g.Round==1&&guard++<50)g.Step();Check(g.Health==prediction.Health&&g.EnemyHealth==prediction.EnemyHealth,"forecast includes mid-round forced special");
  var dualSetup=new GameSetup {EnemyNumbers=new[]{1,2},GuardInterval=0};var dual=ContainmentGame.Rule("two effects",new[]{1,2,3},RuleEffect.Damage,2);dual.Requirement=PatternRequirement.Straight;dual.AdditionalEffect=new SequenceEffect {Effect=RuleEffect.Shield,Value=8};dualSetup.PlayerRules.Add(dual);
  g=new ContainmentGame(3,dualSetup);g.StartBattle();foreach(var arenaRule in g.ArenaRules)arenaRule.Numbers=new[]{0,0,0};g.EnemyRule.Numbers=new[]{0,0,0};g.Hand[0].Number=3;g.Hand[0].Effect=CardEffect.Guard;g.AddCard(g.Hand[0]);prediction=g.Forecast();g.Confirm();for(int i=0;i<5;i++)g.Step();Check(g.EnemyHealth==g.EnemyMaxHealth-2&&g.Shield==14,"both sequence effects execute");Check(g.Shield==prediction.Shield&&g.EnemyHealth==prediction.EnemyHealth,"two-effect forecast matches");
  dual.AdditionalEffect.Requirement=PatternRequirement.Triple;g=new ContainmentGame(3,dualSetup);g.StartBattle();foreach(var arenaRule in g.ArenaRules)arenaRule.Numbers=new[]{0,0,0};g.EnemyRule.Numbers=new[]{0,0,0};g.Hand[0].Number=3;g.Hand[0].Effect=CardEffect.Guard;g.AddCard(g.Hand[0]);g.Confirm();for(int i=0;i<4;i++)g.Step();Check(g.Shield==6,"secondary requirement checked independently");
  var restricted=new GameSetup {RandomStartingDeck=true};restricted.CardPool.Add(new Card {Number=1,Effect=CardEffect.Mend});g=new ContainmentGame(1,restricted);Check(g.Deck.All(c=>c.Effect==CardEffect.Mend)&&g.Deck.Count==18,"starter respects fixed effect pool");
  var tooSmall=new GameSetup();tooSmall.InitialDeck.Add(new Card {Number=1});bool rejected=false;try{new ContainmentGame(1,tooSmall);}catch(ArgumentException){rejected=true;}Check(rejected,"authored starting deck cannot violate minimum");
  g=new ContainmentGame(3);for(int i=0;i<3;i++)g.ChangeSequenceEffect(0,false);Check(g.PlayerRules[0].Effect==RuleEffect.Damage,"only owned primary effects cycle");g.ChangeSequenceEffect(0,true);Check(g.PlayerRules[0].AdditionalEffect!=null,"owned secondary can be assigned");g.StartBattle();var before=g.PlayerRules[0].AdditionalEffect.Effect;g.ChangeSequenceEffect(0,true);Check(g.PlayerRules[0].AdditionalEffect.Effect==before,"effect editing blocked in battle");
  Console.WriteLine("PASS: "+checks+" document content assertions");
 }
}
