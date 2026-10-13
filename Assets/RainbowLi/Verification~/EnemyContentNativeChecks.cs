using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RainbowLi;
using RainbowLi.UnityGame;
using UnityEditor;
using UnityEngine;
public static class EnemyContentNativeChecks {
 static int checks;
 static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
 static object Call(ContainmentGame game,string name,params object[] args)=>typeof(ContainmentGame).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,args);
 static void GuardDeck(GameSetup s){s.RandomStartingDeck=false;s.PlayerHealth=10000;s.InitialDeck.Clear();for(int i=0;i<6;i++)s.InitialDeck.Add(new Card {Number=i+1,Effect=CardEffect.Guard,Spec=new CardEffectSpec {Name="Guard fixture",Text="Guard",Effect=CardEffect.Guard,Min=0,Max=0},EffectAmount=0});}
 static void ResolveRound(ContainmentGame g){g.AddCard(g.Hand[0]);Check(g.Confirm(),"native confirm");int round=g.Round,budget=0;while(g.Phase==GamePhase.Resolving&&g.Round==round&&budget++<5000)g.Step();Check(budget<5000,"native chains terminate");}
 public static void Run(){try{
  var definition=AssetDatabase.LoadAssetAtPath<GameDefinition>("Assets/RainbowLi/Data/DefaultGame.asset");Check(definition.enemy.contentTable!=null&&definition.enemy.bossTable!=null,"default enemy table bindings");var s=definition.CreateSetup();
  Check(s.NormalEncounters.Count==8&&s.BossEncounters.Count==3,"all 8 normals and 3 bosses adapted");
  Check(s.BossEncounters.Count(b=>b.LayerMin<=1&&b.LayerMax>=1)==2&&s.BossEncounters.Count(b=>b.LayerMin<=2&&b.LayerMax>=2)==1,"boss spawn layers");
  string normalBefore=JsonUtility.ToJson(definition.enemy.contentTable),bossBefore=JsonUtility.ToJson(definition.enemy.bossTable);
  foreach(var encounter in s.NormalEncounters.Concat(s.BossEncounters)){
   var one=definition.CreateSetup();one.NormalEncounters.Clear();one.BossEncounters.Clear();(encounter.Boss?one.BossEncounters:one.NormalEncounters).Add(encounter.Copy());GuardDeck(one);
   var g=new ContainmentGame(991,one);g.Victories=6;var graph=new RouteRun(RouteGenerator.Generate(9));graph.ConfigureBossUnlock(encounter.LayerMin);
   if(encounter.LayerMin>1){var save=g.CaptureSave();save.AdventureLayer=encounter.LayerMin;g=ContainmentGame.RestoreSave(save,one);}
   g.StartBattle(encounter.Boss);Check(g.Enemy.Encounter.Definition.Id==encounter.Id&&g.EnemyMaxHealth==encounter.Health,"native selection "+encounter.Title);
   Check(g.Enemy.ActiveRules.Count()==encounter.Sequences.Length,"all native sequences "+encounter.Title);
   foreach(var rule in g.Enemy.ActiveRules){Check(rule.EncounterEffects.Length>0,"all compound sequence effects preserved");Check(!string.IsNullOrEmpty(rule.Description),"authored description");}
   for(int round=0;round<8&&g.Phase==GamePhase.Planning;round++){
    if(round==1)g.EnemyHealth=Math.Max(1,g.EnemyMaxHealth*3/10);
    var saved=JsonUtility.FromJson<RunSave>(JsonUtility.ToJson(g.CaptureSave()));var restored=ContainmentGame.RestoreSave(saved,one);
    Check(restored.Enemy.Encounter.Definition.Id==g.Enemy.Encounter.Definition.Id&&restored.Enemy.ActiveRules.Count()==g.Enemy.ActiveRules.Count(),"native JSON roster and sequence restoration");
    var state=g.RandomState;g.Forecast();Check(g.RandomState==state,"native encounter preview preserves RNG");ResolveRound(g);
   }
   // Resolve each authored skill and sequence through the shared settlement API.
   foreach(var skill in encounter.Skills){
    var isolated=new ContainmentGame(100,one);isolated.Victories=6;if(encounter.LayerMin>1){var save=isolated.CaptureSave();save.AdventureLayer=2;isolated=ContainmentGame.RestoreSave(save,one);}isolated.StartBattle(encounter.Boss);
    isolated.Player.MaxHealth=isolated.Health=10000;isolated.EnemyMaxHealth=isolated.EnemyHealth=10000;
    var action=(BattleAction)Call(isolated,"ContentAction",skill,true,true);Call(isolated,"ApplyEncounterEffects",action,action.EncounterEffects,RainbowLi.BattleEventSource.EnemySkill);
    Check(isolated.Health>=0&&isolated.EnemyHealth>=0,"native skill settlement "+skill.Name);
    if(isolated.CaptureSave().Pending.Length>0){
     var pendingSave=JsonUtility.FromJson<RunSave>(JsonUtility.ToJson(isolated.CaptureSave()));var resumed=ContainmentGame.RestoreSave(pendingSave,one);isolated.AddCard(isolated.Hand[0]);resumed.AddCard(resumed.Hand[0]);isolated.Confirm();resumed.Confirm();
     for(int step=0;step<20&&isolated.Phase==GamePhase.Resolving&&isolated.CaptureSave().Pending.Length>0;step++){isolated.Step();resumed.Step();Check(isolated.Health==resumed.Health&&isolated.EnemyHealth==resumed.EnemyHealth&&isolated.EnemyShield==resumed.EnemyShield&&isolated.Enemy.Statuses.Summary==resumed.Enemy.Statuses.Summary&&isolated.RandomState==resumed.RandomState,"native JSON ordered effect continuation "+skill.Name);}
    }
   }
  }
  var levels=AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/RainbowLi/Data/DefaultLevels.asset");var runGame=new ContainmentGame(55,s);runGame.ConfigureRoute(levels.CreateGraph(55));runGame.Route.ConfigureBossUnlock(1);runGame.EnsureBossRouteBattles();
  var run=runGame.Route;
  while(!run.BossUnlocked){var next=run.Graph.Nodes.Where(n=>n.kind!=LevelKind.Boss&&run.CanEnter(n.id)).OrderBy(n=>n.kind==LevelKind.Combat||n.kind==LevelKind.Elite?1:0).FirstOrDefault();Check(next!=null,"even service-heavy path can unlock boss");Check(run.Enter(next.id)&&run.Complete(),"native route step");}
  var boss=run.Graph.Nodes.Single(n=>n.kind==LevelKind.Boss);Check(run.Enter(boss.id),"native first boss unlocked");runGame.StartBattle(true);Check(runGame.Enemy.Encounter.Definition.LayerMin==1,"first-layer native boss");
  runGame.EnemyHealth=1;var c=runGame.Hand[0];c.Spec=null;c.Effects.Clear();c.Effect=CardEffect.Strike;c.BaseBonus=10000;runGame.InsertCard(c,0);runGame.Confirm();while(runGame.Phase==GamePhase.Resolving)runGame.Step();while(runGame.Phase==GamePhase.Reward)Check(runGame.SkipReward(),"native boss reward skip");
  Check(runGame.Phase==GamePhase.Map&&runGame.Route.AdventureLayer==2&&runGame.CompletedRoomCount>0,"native second layer route and cumulative progress");
  while(!runGame.Route.BossUnlocked){var next=runGame.Route.Graph.Nodes.FirstOrDefault(n=>n.kind!=LevelKind.Boss&&runGame.Route.CanEnter(n.id));Check(next!=null,"second-layer boss reachable");runGame.Route.Enter(next.id);runGame.Route.Complete();}
  boss=runGame.Route.Graph.Nodes.Single(n=>n.kind==LevelKind.Boss);runGame.Route.Enter(boss.id);runGame.StartBattle(true);Check(runGame.Enemy.Encounter.Definition.Id.Contains("抽象概念"),"MO-166 reachable in layer two");Check(runGame.Enemy.ActiveRules.Any(r=>r.Numbers.SequenceEqual(new[]{1,2,3}))&&runGame.Enemy.ActiveRules.Any(r=>r.Numbers.SequenceEqual(new[]{4,5,6})),"native fixed 123 and 456 boss patterns");
  Check(normalBefore==JsonUtility.ToJson(definition.enemy.contentTable)&&bossBefore==JsonUtility.ToJson(definition.enemy.bossTable),"no authoring asset mutation during combat");
  EnemyEncounterChecks.Main();File.WriteAllText("enemy-content-result.txt","PASS: "+checks+" Unity actual-table assertions, 8 normal enemies, 3 bosses, every skill, composite sequences, 8-round smoke, native JSON, forecast and complete layer progression.");EditorApplication.Exit(0);
 }catch(Exception error){Debug.LogException(error);File.WriteAllText("enemy-content-result.txt",error.ToString());EditorApplication.Exit(1);}}
}
