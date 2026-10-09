using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RainbowLi;
using RainbowLi.UnityGame;
public static class TableSceneChecks {
 static int checks;static double started;static GameView view;
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);checks++;}
 public static void Run(){
  RainbowLi.EditorTools.CardTableSceneMigration.Run();
  view=UnityEngine.Object.FindFirstObjectByType<GameView>();var catalog=view.cards.catalog;Check(catalog.effects.Length==26,"all 26 effects connected to card module");Check(view.session.cardSystem==view.cards&&view.session.definition.cardEffects==catalog,"session and definition use catalog");
  for(int i=0;i<26;i++){
   var d=catalog.effects[i];Check(d.effectId=="卡效_"+(i+1).ToString("00")&&d.sourceRow==i+2,"stable row identities");Check(!string.IsNullOrWhiteSpace(d.description),"source description preserved");
   var template=AssetDatabase.LoadAssetAtPath<CardDefinition>("Assets/RainbowLi/Data/Cards/TableCard_"+(i+1).ToString("00")+".asset");Check(template!=null&&template.effectDefinition==d&&template.CreateCard().Spec.Code==d.effectId,"editable card template references effect asset");
  }
  Check(catalog.effects[5].minValue==1&&catalog.effects[5].maxValue==2,"strength 1-2 range");Check(catalog.effects[15].minValue==9&&catalog.effects[15].maxValue==15,"damage limit 9-15 range");
  Check(view.hand.Concat(view.actions).All(c=>PrefabUtility.IsPartOfPrefabInstance(c)),"persistent card prefab instances retained");Check(view.statusSummary!=null,"native status label saved");
  foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"no missing scripts");
  started=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();
 }
 static void Log(string message,string stack,LogType type){if(stack.Contains("UnityEditor.Search.SearchDatabase")&&stack.Contains("IndexationOnStartup"))return;if(type==LogType.Error||type==LogType.Exception){Console.WriteLine("TABLE FAILED: "+message+"\n"+stack);EditorApplication.Exit(1);}}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-started>60)throw new Exception("table scene timeout");if(!EditorApplication.isPlaying)return;view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null||!view.IsReady)return;
  var map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();if(map==null||map.levels.Run==null)return;view.battle.autoPlay=false;
  var g=view.session.Game;Check(g.Deck.Count==18&&g.Deck.All(c=>c.Spec!=null),"starter uses spreadsheet effects");Check(Enumerable.Range(1,6).All(n=>g.Deck.Count(c=>c.Number==n)==3),"starter three digit groups");
  Check(g.GenerateCard(6).Number==6&&g.GenerateCard(6).Spec!=null,"shop directed generator uses catalog");
  map.nodes.First(n=>n.nodeId==0).button.onClick.Invoke();var effect=view.cards.catalog.effects[10];var card=new Card {Number=3,Effect=CardEffect.Poison,Spec=effect.CreateSpec(),EffectAmount=6,UpgradeBonus=0};g.Hand.Clear();g.Hand.Add(card);view.session.Notify();
  Check(view.hand[0].caption.text.Contains("中毒"),"card displays table effect name");view.hand[0].button.onClick.Invoke();Check(g.Message.Contains("回合结束"),"selecting card shows source rule");view.insertButtons[0].onClick.Invoke();Check(view.confirm.interactable,"all intents arranged");var forecast=g.Forecast();
  view.confirm.onClick.Invoke();view.session.ResolveOne();Check(g.Enemy.Statuses[CardEffect.Poison]==6&&view.statusSummary.text.Contains("中毒6"),"status applied and visible");
  int guard=0;while(g.Phase==GamePhase.Resolving&&g.Round==1&&guard++<500)view.session.ResolveOne();Check(guard<500&&g.Enemy.Statuses[CardEffect.Poison]==3,"end-turn poison and halving through session");Check(g.EnemyHealth==forecast.EnemyHealth,"native forecast includes poison");
  view.session.NewGame();g=view.session.Game;map.nodes.First(n=>n.nodeId==0).button.onClick.Invoke();g.Hand.Clear();g.Hand.Add(new Card {Number=3,Effect=CardEffect.RandomNumbers,Spec=view.cards.catalog.effects[16].CreateSpec(),EffectAmount=2});view.session.Notify();view.hand[0].button.onClick.Invoke();view.insertButtons[0].onClick.Invoke();var first=g.Forecast();var second=g.Forecast();Check(first.Numbers.SequenceEqual(second.Numbers),"random preview stable through native scene");view.confirm.onClick.Invoke();for(int i=0;i<3;i++)view.session.ResolveOne();Check(g.NumberStream.Count==3,"native random action fills two extra digits");
  view.session.NewGame();Check(view.session.Game.Player.Statuses.Any==false&&view.session.Game.Enemy.Statuses.Any==false,"new game clears all statuses");
  g=view.session.Game;map.nodes.First(n=>n.nodeId==0).button.onClick.Invoke();g.Hand.Clear();g.Hand.Add(new Card {Number=3,Effect=CardEffect.Strike,Spec=view.cards.catalog.effects[0].CreateSpec()});g.Player.Statuses.Add(CardEffect.Bleed,2);g.Player.Statuses.Add(CardEffect.Reciprocity,1);view.session.Notify();view.hand[0].button.onClick.Invoke();view.insertButtons[0].onClick.Invoke();ResolutionEvent last=new ResolutionEvent();view.session.Resolved+=e=>last=e;view.confirm.onClick.Invoke();view.session.ResolveOne();Check(last.PlayerDamage==2&&last.EnemyDamage==3,"bleed and attack reported separately in one behavior");var fx=UnityEngine.Object.FindFirstObjectByType<BattleEffects>();var field=typeof(BattleEffects).GetField("bursts",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);Check(((System.Collections.ICollection)field.GetValue(fx)).Count==2,"both damaged targets receive a burst");view.session.ResolveOne();Check(last.Action.IsRepeat&&last.Slot==0,"repeated card uses original card visual origin");
  view.session.NewGame();g=view.session.Game;map.nodes.First(n=>n.nodeId==0).button.onClick.Invoke();g.Player.Statuses.Add(CardEffect.Parry,1);g.Timeline[0].Effect=CardEffect.Strike;g.Timeline[0].Value=10;g.Hand.Clear();g.Hand.Add(new Card {Number=3,Effect=CardEffect.Strike,Spec=view.cards.catalog.effects[0].CreateSpec()});view.session.Notify();view.hand[0].button.onClick.Invoke();view.insertButtons[g.Timeline.Count].onClick.Invoke();view.confirm.onClick.Invoke();int hp=g.Health;view.session.ResolveOne();Check(last.PlayerParried&&g.Health==hp,"parry reports immunity without health loss");Check(((System.Collections.ICollection)field.GetValue(fx)).Count==1,"parry produces block feedback");
  Console.WriteLine("PASS: "+checks+" spreadsheet/native scene checks");EditorApplication.update-=Tick;EditorApplication.Exit(0);
 }catch(Exception e){Console.WriteLine("TABLE FAILED: "+e);EditorApplication.Exit(1);}}
}
