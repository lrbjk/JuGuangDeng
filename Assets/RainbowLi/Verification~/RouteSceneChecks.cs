using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RainbowLi;
using RainbowLi.UnityGame;
public static class RouteSceneChecks {
 static int checks,stage;static double started;static GameView view;static RouteMapView map;static LevelModule level;
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
 public static void Run(){
  RainbowLi.EditorTools.RouteSceneMigration.Run();
  foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"no missing scripts");
  map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();Check(map.nodes.Count==22&&map.nodePrefab!=null,"persistent route prefab nodes");Check(map.nodes.All(n=>PrefabUtility.IsPartOfPrefabInstance(n)),"route prefab instances");
  started=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
  EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();
 }
 static void Log(string message,string stack,LogType type){if(stack.Contains("UnityEditor.Search.SearchDatabase")&&stack.Contains("IndexationOnStartup"))return;if(type==LogType.Error||type==LogType.Exception){Console.WriteLine("ROUTE FAILED: "+message+"\n"+stack);EditorApplication.Exit(1);}}
 static void Select(int id){map.nodes.First(n=>n.nodeId==id).button.onClick.Invoke();}
 static void Kill(){var g=level.session.Game;g.Hand[0].Effect=CardEffect.Strike;g.EnemyHealth=1;level.session.Notify();view.hand[0].button.onClick.Invoke();view.insertButtons[0].onClick.Invoke();view.confirm.onClick.Invoke();view.step.onClick.Invoke();}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-started>90)throw new Exception("timeout");if(!EditorApplication.isPlaying)return;
  view=UnityEngine.Object.FindFirstObjectByType<GameView>();map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();level=UnityEngine.Object.FindFirstObjectByType<LevelModule>();
  if(view==null||!view.IsReady||level.Run==null)return;var g=level.session.Game;
  if(stage==0){
   view.battle.autoPlay=false;Check(level.Run.Graph.Nodes.Count(n=>level.Run.CanEnter(n.id))==3,"three starting branches");
   // Change runtime node copies to deterministically cover every room type, never authoring assets.
   foreach(var n in level.Run.Graph.Nodes){if(n.floor==2)n.kind=LevelKind.Event;if(n.floor==3)n.kind=LevelKind.Elite;if(n.floor==4)n.kind=LevelKind.Rest;}
   level.session.Notify();Select(1);Check(g.Phase==GamePhase.Planning&&level.Run.ActiveId==1,"map button enters combat");
   level.SelectNode(0);Check(level.Run.ActiveId==1,"no second room while active");Kill();Check(g.Phase==GamePhase.Reward&&level.Run.CurrentId==-1,"reward pending blocks route progression");stage=1;return;
  }
  if(stage==1){view.rewardButtons[0].onClick.Invoke();Check(level.Run.CurrentId==1&&g.Phase==GamePhase.Map,"reward completes room");Check(!level.Run.CanEnter(1)&&!level.Run.CanEnter(10),"no replay or floor skipping");g.Coins=100;Select(4);Check(g.Phase==GamePhase.Shop,"connected shop");stage=2;return;}
  if(stage==2){var row=view.deckContent.GetComponentsInChildren<DeckRowView>().First();row.upgrade.onClick.Invoke();row=view.deckContent.GetComponentsInChildren<DeckRowView>().First();row.engrave.onClick.Invoke();Check(g.Deck[0].Engraved,"route shop engraving");view.progression.Leave();Check(level.Run.CurrentId==4,"leaving shop advances");stage=3;return;}
  if(stage==3){
   if(g.Phase==GamePhase.Map){var next=level.Run.Graph.Nodes.First(n=>level.Run.CanEnter(n.id));Select(next.id);return;}
   if(g.Phase==GamePhase.Event){int coins=g.Coins;Check(map.eventPanel.activeSelf,"event panel visible");level.EventGold();level.EventGold();Check(g.Coins==coins+level.definition.eventGold&&g.Phase==GamePhase.Map,"event reward once");return;}
   if(g.Phase==GamePhase.Rest){g.Health=10;view.heal.onClick.Invoke();Check(g.Health==34,"route rest healing");view.progression.Leave();return;}
   if(g.Phase==GamePhase.Shop){view.progression.Leave();return;}
   if(g.Phase==GamePhase.Planning){if(g.Elite)Check(g.EnemyMaxHealth>level.session.enemy.definition.maxHealth&&g.EliteAttackBonus==3,"elite room tuning");Kill();return;}
   if(g.Phase==GamePhase.Reward){view.rewardButtons[0].onClick.Invoke();return;}
   if(g.Phase==GamePhase.Won){Check(level.Run.Finished,"boss finishes run");level.session.NewGame();Check(level.Run.CurrentId==-1&&level.Run.Completed.Count==0,"new run resets map");view.battle.autoPlay=false;Select(0);g=level.session.Game;g.Health=1;view.hand[0].button.onClick.Invoke();view.insertButtons[view.progression.session.Game.Timeline.Count].onClick.Invoke();view.confirm.onClick.Invoke();view.step.onClick.Invoke();Check(g.Phase==GamePhase.Lost&&level.Run.Failed,"death ends route");level.session.NewGame();Check(!level.Run.Failed,"new run clears defeat");Console.WriteLine("PASS: "+checks+" route scene checks; all room types, branching, rewards, elite, boss, defeat and reset.");EditorApplication.update-=Tick;EditorApplication.Exit(0);}
  }
 }catch(Exception e){Console.WriteLine("ROUTE FAILED: "+e);EditorApplication.Exit(1);}}
}
