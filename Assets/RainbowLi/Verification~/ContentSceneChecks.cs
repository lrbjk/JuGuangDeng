using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi;
using RainbowLi.UnityGame;
public static class ContentSceneChecks {
 static double started;static int checks;static GameView view;
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);checks++;}
 public static void Run(){RainbowLi.EditorTools.ContentSceneMigration.Run();var saved=UnityEngine.Object.FindFirstObjectByType<GameView>();Check(saved.hand.Concat(saved.actions).All(c=>PrefabUtility.IsPartOfPrefabInstance(c)),"all saved cards remain prefab instances");started=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();}
 static void Log(string message,string stack,LogType type){if(stack.Contains("UnityEditor.Search.SearchDatabase")&&stack.Contains("IndexationOnStartup"))return;if(type==LogType.Error||type==LogType.Exception){Console.WriteLine("CONTENT FAILED: "+message+"\n"+stack);EditorApplication.Exit(1);}}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-started>60)throw new Exception("content timeout");if(!EditorApplication.isPlaying)return;
  view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null||!view.IsReady)return;
  var g=view.session.Game;var map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();if(map==null||map.levels.Run==null)return;
  view.battle.autoPlay=false;
  Check(g.BaseAttack==5&&g.HandLimit==9,"player config applied");Check(g.Deck.Count==18&&Enumerable.Range(1,6).All(n=>g.Deck.Count(c=>c.Number==n)==3),"random starter groups");Check(view.hand.Length==9&&view.actions.Length==10&&view.insertButtons.Length==11,"saved native card slots");
  Check(view.session.enemy.definition.skills.Length==3,"authored enemy pool");Check(view.session.enemy.definition.conditionalSequences.Length==1,"authored conditional rule");
  view.ruleViews[0].secondaryEffect.onClick.Invoke();Check(g.PlayerRules[0].AdditionalEffect!=null,"secondary effect native button");view.ruleViews[0].primaryEffect.onClick.Invoke();Check(g.PlayerRules[0].Effect==RuleEffect.Shield,"primary effect native button");
  map.nodes.First(n=>n.nodeId==0).button.onClick.Invoke();Check(g.Phase==GamePhase.Planning,"route battle started");
  Check(!view.ruleViews[0].digits[0].interactable&&!view.ruleViews[0].secondaryEffect.gameObject.activeSelf,"sequence editing locked in battle");
  int enemyCount=g.Timeline.Count;view.cards.Remove(0);Check(g.Timeline.Count==enemyCount&&view.removeButtons[0].interactable==false,"cannot remove enemy action");
  g.Hand.AddRange(g.Deck.Where(c=>!g.Hand.Contains(c)).Take(3));view.session.Notify();Check(view.hand.All(h=>h.gameObject.activeSelf),"all nine cards visible");
  for(int i=0;i<10;i++)g.Timeline.Add(new BattleAction {Enemy=true,Number=1,Effect=CardEffect.Guard,Value=0});view.session.Notify();Check(view.actions.Length>=12&&view.actions.Take(g.Timeline.Count).All(a=>a.gameObject.activeSelf),"overflow intents visible");
  // Begin a fresh real battle for conditional skills, retaining the route controller.
  view.session.NewGame();g=view.session.Game;map.nodes.First(n=>n.nodeId==0).button.onClick.Invoke();g.Health=9999;g.EnemyHealth=9999;g.Round=2;
  foreach(var rule in g.PlayerRules.Concat(g.ArenaRules))rule.Numbers=new[]{0,0,0};g.EnemyRule.Numbers=new[]{0,0,0};view.session.Notify();
  view.hand[0].button.onClick.Invoke();view.insertButtons[0].onClick.Invoke();Check(g.CanConfirm,"all enemy intents arranged");view.confirm.onClick.Invoke();int steps=0;while(g.Phase==GamePhase.Resolving&&steps++<100)view.session.ResolveOne();
  Check(g.Round==3&&g.Timeline.Count(a=>a.Special)==1&&g.Timeline.Count==3,"round condition adds mandatory skill above action limit");Check(view.actions.Take(3).All(a=>a.gameObject.activeSelf),"special card visible");
  g.EnemyHealth=g.EnemyMaxHealth/2;view.hand[0].button.onClick.Invoke();view.insertButtons[0].onClick.Invoke();var predicted=g.Forecast();view.confirm.onClick.Invoke();steps=0;while(g.Phase==GamePhase.Resolving&&steps++<100)view.session.ResolveOne();
  Check(g.Enemy.ActiveRules.Count()==2,"health conditional sequence activated and persists");Check(predicted!=null,"forecast accepts conditional battle");
  Console.WriteLine("PASS: "+checks+" native document content checks");EditorApplication.update-=Tick;EditorApplication.Exit(0);
 }catch(Exception e){Console.WriteLine("CONTENT FAILED: "+e);EditorApplication.Exit(1);}}
}
