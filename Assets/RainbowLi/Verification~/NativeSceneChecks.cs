using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using RainbowLi;
using RainbowLi.UnityGame;
public static class NativeSceneChecks {
 static double started;
 static int stage,checks;
 static GameView view;
 static GameSession session;
 static void Check(bool condition,string label){if(!condition)throw new Exception(label);checks++;}
 public static void BuildAndRun(){RainbowLi.EditorTools.BuildEditableGame.Build();Run();}
 public static void MigrateAndRun(){RainbowLi.EditorTools.ActorSceneMigration.Run();Run();}
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");
  foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing scripts");
  view=UnityEngine.Object.FindFirstObjectByType<GameView>();session=UnityEngine.Object.FindFirstObjectByType<GameSession>();
  Check(view!=null&&session!=null,"native scene composition");
  Check(view.GetComponent<CanvasScaler>().screenMatchMode==CanvasScaler.ScreenMatchMode.Expand,"responsive table fitting");
  Check(view.hand.Length==6&&view.actions.Length==7&&view.rewards.Length==3,"persistent card slots");
  Check(view.hand.All(c=>c.suits.All(s=>s!=null)&&c.topRank.font!=null),"card sprites and font references");
  Check(view.hand.All(c=>PrefabUtility.IsPartOfPrefabInstance(c)),"card prefab instances persist in scene");
  Check(UnityEngine.Object.FindFirstObjectByType<ContainmentPrototype>()==null,"native scene has no IMGUI controller");
  Check(session.definition.player.startingDeck.Length==18,"authored starter deck");
  Check(session.player!=null&&session.enemy!=null,"separate scene actor components");
  Check(session.player.definition==session.definition.player&&session.enemy.definition==session.definition.enemy,"independent actor config assets");
  Check(UnityEngine.Object.FindFirstObjectByType<EventSystem>()!=null,"event system");
  started=EditorApplication.timeSinceStartup;
  Application.logMessageReceived+=HandleLog;
  EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
  EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
 }
 static void HandleLog(string message,string stack,LogType type){
  if(stack.Contains("UnityEditor.Search.SearchDatabase")&&stack.Contains("IndexationOnStartup"))return;
  if(type==LogType.Exception||type==LogType.Error){Console.WriteLine("NATIVE FAILED: "+message+"\n"+stack);EditorApplication.Exit(1);}
 }
 static void Tick(){
  try{
   if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Native test timeout");
   if(!EditorApplication.isPlaying)return;
   if(stage==0){session=UnityEngine.Object.FindFirstObjectByType<GameSession>();view=UnityEngine.Object.FindFirstObjectByType<GameView>();}
   if(session==null||session.Game==null||view==null||!view.IsReady)return;
   var g=session.Game;
   if(stage==0){
    if(!view.normalTask.isActiveAndEnabled)return;
    view.battle.autoPlay=false;
    Check(g.Phase==GamePhase.Map&&view.mapPanel.activeSelf,"map initialization");
    view.normalTask.onClick.Invoke();Check(g.Phase==GamePhase.Planning&&view.battlePanel.activeSelf,"map button opens battle");
    Check(view.hand.Count(c=>c.gameObject.activeSelf)==6,"six displayed hand cards");
    g.Hand[0].Effect=CardEffect.Strike;g.EnemyHealth=1;session.Notify();
    view.hand[0].button.onClick.Invoke();view.insertButtons[0].onClick.Invoke();
    Check(g.PlayerActionCount==1&&g.Timeline[0].Card==g.Hand[0],"card selection and slot insertion buttons");
    view.confirm.onClick.Invoke();Check(g.Phase==GamePhase.Resolving,"confirm button starts resolution");
    view.step.onClick.Invoke();Check(g.Phase==GamePhase.Reward&&view.rewardPanel.activeSelf,"lethal attack opens rewards");
    Check(view.rewards.All(c=>c.gameObject.activeSelf),"three reward cards shown");stage=1;return;
   }
   if(stage==1){
    view.rewardButtons[0].onClick.Invoke();Check(g.Phase==GamePhase.Map&&g.Deck.Count==19,"reward selection updates deck");
    g.Coins=100;view.progression.OpenShop();Check(view.servicePanel.activeSelf,"shop view");stage=2;return;
   }
   if(stage==2){
    var row=view.deckContent.GetComponentsInChildren<DeckRowView>().First();int upgrade=g.Deck[0].Upgrade;
    row.upgrade.onClick.Invoke();Check(g.Deck[0].Upgrade==upgrade+1,"upgrade button");
    row=view.deckContent.GetComponentsInChildren<DeckRowView>().First();row.engrave.onClick.Invoke();Check(g.Deck[0].Engraved,"engraving button");
    Check(!session.definition.player.startingDeck[0].CreateCard().Engraved,"asset runtime isolation");
    view.progression.Leave();g.Health=10;view.progression.OpenRest();view.heal.onClick.Invoke();Check(g.Health==34&&g.ServiceUsed,"rest button");
    view.progression.Leave();view.normalTask.onClick.Invoke();view.hand[0].button.onClick.Invoke();view.insertButtons[0].onClick.Invoke();view.confirm.onClick.Invoke();stage=3;return;
   }
   if(stage==3){
    if(g.Phase==GamePhase.Resolving)view.battle.Step();
    if(g.Round>=2){Check(g.Phase==GamePhase.Planning&&view.battlePanel.activeSelf,"next round native refresh");Check(view.hand.Count(c=>c.gameObject.activeSelf)==6,"redraw native hand");Console.WriteLine("PASS: "+checks+" native scene/reference/UI interaction checks; reward, upgrade, engraving, rest, redraw and attack effects run without exceptions.");EditorApplication.update-=Tick;EditorApplication.Exit(0);}
   }
  }catch(Exception e){Console.WriteLine("NATIVE FAILED: "+e);EditorApplication.Exit(1);}
 }
}
