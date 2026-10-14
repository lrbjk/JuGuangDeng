using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RainbowLi;
using RainbowLi.UnityGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class EnemyContentSceneChecks {
 static double started;static int stage,checks;static GameView view;static GameSession session;
 static void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
 public static void Run(){EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();}
 static void Fail(Exception error){Debug.LogException(error);File.WriteAllText("enemy-scene-result.txt",error.ToString());EditorApplication.update-=Tick;EditorApplication.Exit(1);}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Scene verification timeout at stage "+stage+" view="+(view!=null)+" session="+(session!=null)+" phase="+session?.Game?.Phase+" presentation="+view?.battle?.HasPresentation);if(!EditorApplication.isPlaying)return;
  if(stage==0){
   view=UnityEngine.Object.FindFirstObjectByType<GameView>();session=view?.session;if(session?.Game==null||view.progression.levels?.Run==null)return;
   var menu=view.GetComponent<StartMenuView>();if(menu!=null&&menu.IsOpen)menu.StartGame();
   foreach(var root in new[]{view.gameObject,session.gameObject})foreach(var t in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"game UI and session script bindings");
   var levels=view.progression.levels;var node=levels.Run.Graph.Nodes.First(n=>n.kind==LevelKind.Combat&&levels.Run.CanEnter(n.id));levels.SelectNode(node.id);Check(session.Game.Enemy.Encounter!=null&&!session.Game.Boss,"scene normal node uses content table");
   view.battle.autoPlay=false;
   // Select a layer-two fixture in the actual scene, preserving native bindings.
   var s=session.definition.CreateSetup();s.PlayerHealth=10000;var g=new ContainmentGame(13,s);g.ConfigureRoute(session.Game.Route.Graph);g.Route.ConfigureBossUnlock(2);
   while(!g.Route.BossUnlocked){var next=g.Route.Graph.Nodes.First(n=>n.kind!=LevelKind.Boss&&g.Route.CanEnter(n.id));g.Route.Enter(next.id);g.Route.Complete();}
   typeof(GameSession).GetProperty("Game").SetValue(session,g);session.Notify();levels.SelectBoss();Check(g.Enemy.Encounter.Definition.LayerMin==2,"scene MO-166 selected by layer");
   Check(view.battleHud.enemySequenceViews.Length>=6&&view.battleHud.enemySkills.Length>=9,"scene expands all boss sequences and skills");
   Check(view.battleHud.enemySequenceViews.Take(6).All(r=>!string.IsNullOrEmpty(r.description.text)),"all six boss sequence descriptions shown");
   Check(view.battleHud.enemySkills.Take(9).All(r=>r.gameObject.activeSelf),"all nine boss skills shown");
   var scroll=view.battleHud.enemySequenceViews[0].GetComponentInParent<ScrollRect>();Check(scroll!=null&&scroll.vertical&&scroll.content.rect.height>scroll.viewport.rect.height,"boss sequence list scrolls within sidebar");
   Debug.Log("Enemy scene verification: boss UI bound; stage 1");stage=1;return;
  }
  if(stage==1){
   if(view.battle.HasPresentation)return;var g=session.Game;g.EnemyHealth=0;typeof(ContainmentGame).GetMethod("FinishContentBattle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,null);session.Notify();view.battle.Skip();stage=2;return;
  }
  if(stage==2){
   if(!view.rewardPanel.activeInHierarchy||!view.engravings.deck.IsOpen)return;
   var deck=view.engravings.deck;var skip=deck.panel.transform.Find("SkipReward");Check(skip!=null&&skip.gameObject.activeInHierarchy,"skip reward button shown for configured optional card reward");
   skip.GetComponent<Button>().onClick.Invoke();Check(session.Game.Phase==GamePhase.Won,"final boss reward skip completes game");
   File.WriteAllText("enemy-scene-result.txt","PASS: "+checks+" actual scene bindings, ordinary enemy selection, layer-two BOSS, all 6 sequences / 9 skills, scroll layout and optional reward flow.");EditorApplication.update-=Tick;EditorApplication.Exit(0);
  }
 }catch(Exception error){Fail(error);}}
}
