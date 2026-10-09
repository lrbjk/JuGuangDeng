using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RainbowLi;
using RainbowLi.UnityGame;
using RainbowLi.EditorTools;
public static class CardEditorChecks {
 static int checks;static double started;
 static void Check(bool yes,string message){if(!yes)throw new Exception(message);checks++;}
 static void Reject(Action action,string message){try{action();}catch(Exception){checks++;return;}throw new Exception(message);}
 public static void Run(){try{
  var player=AssetDatabase.LoadAssetAtPath<PlayerDefinition>("Assets/RainbowLi/Data/DefaultPlayer.asset");var enemy=AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/RainbowLi/Data/DefaultEnemy.asset");var p=player.cardTable;var e=enemy.cardTable;
  Check(p!=null&&e!=null&&p.rows.Length==26&&e.rows.Length==27,"saved tables and actor bindings");p.ValidateTable(false);e.ValidateTable(true);
  Check(e.rows.Count(r=>r.enabled)==3&&e.rows.Count(r=>r.enabled&&r.special)==1,"default enemy skill pool and special retained");
  var conditional=e.rows[0].Copy();conditional.condition=ActivationCondition.HealthPercentAtMost;conditional.threshold=50;var basic=CardTableWorkbook.Cells(conditional).Take(7).ToArray();Check(CardTableWorkbook.Parse(new[]{basic},new[]{conditional})[0].condition==ActivationCondition.HealthPercentAtMost,"seven-column imports preserve authored unlock conditions");
  var original=CardTableWorkbook.Read("C:/Users/31454/Downloads/卡牌效果.xlsx");var imported=CardTableWorkbook.Parse(original,p.rows);Check(imported.Length==26,"original workbook rows read despite incorrect worksheet dimensions");
  for(int i=0;i<26;i++){Check(imported[i].id==p.rows[i].id,"original id");Check(imported[i].description==p.rows[i].description,"original description");Check(imported[i].range==p.rows[i].range,"original X range");Check(imported[i].effect==p.rows[i].effect,"original effect dispatch");}
  string path="Assets/RainbowLi/Verification~/editor-roundtrip.xlsx";Directory.CreateDirectory(Path.GetDirectoryName(path));if(File.Exists(path))File.Delete(path);CardTableWorkbook.Write(path,e.rows);var roundtrip=CardTableWorkbook.Parse(CardTableWorkbook.Read(path),e.rows);
  Check(roundtrip.Length==27,"xlsx roundtrip row count");for(int i=0;i<27;i++)Check(CardTableWorkbook.Cells(roundtrip[i]).SequenceEqual(CardTableWorkbook.Cells(e.rows[i])),"all columns survive roundtrip");
  var quoted=CardTableWorkbook.ClipboardRows("a\t\"描述\t多列\n第二行\"\tb\r\nc\td\te");Check(quoted.Length==2&&quoted[0][1]=="描述\t多列\n第二行","Excel quoted clipboard parses tabs and line breaks");
  var copy=ScriptableObject.CreateInstance<PlayerCardTable>();copy.rows=p.rows.Select(r=>r.Copy()).ToArray();AssetDatabase.CreateAsset(copy,"Assets/RainbowLi/Data/EditorTestPlayer.asset");var window=ScriptableObject.CreateInstance<PlayerCardEditorWindow>();window.SetTable(copy);
  var edited=copy.rows[0].Copy();edited.name="测试伤害X";edited.range="9";edited.description="中文换行\n第二行";
  Undo.IncrementCurrentGroup();window.Import(new[]{CardTableWorkbook.Cells(edited)},false);Undo.FlushUndoRecordObjects();Check(copy.rows.Length==26&&copy.rows.Last().name=="测试伤害X","paste upserts by id without duplicating existing rows");Undo.PerformUndo();Check(copy.rows[0].name==p.rows[0].name&&copy.rows.Length==26,"import supports native Undo");
  var snapshot=copy.rows;var duplicate=new[]{CardTableWorkbook.Cells(edited),CardTableWorkbook.Cells(edited)};Reject(()=>window.Import(duplicate,true),"duplicate ids must fail");Check(ReferenceEquals(snapshot,copy.rows),"invalid import is atomic");
  var invalid=edited.Copy();invalid.range="0";Reject(()=>window.Import(new[]{CardTableWorkbook.Cells(invalid)},true),"zero range rejected");
  invalid=edited.Copy();invalid.range="1-2";invalid.weight=0;Reject(()=>window.Import(new[]{CardTableWorkbook.Cells(invalid)},true),"zero weight rejected");
  invalid=edited.Copy();invalid.id="未知效果";var unknown=CardTableWorkbook.Cells(invalid);unknown[15]="";Reject(()=>CardTableWorkbook.Parse(new[]{unknown},p.rows),"unknown dispatch never silently guesses");
  window.SaveTable();Check(!EditorUtility.IsDirty(copy),"save clears dirty state on asset");AssetDatabase.ImportAsset("Assets/RainbowLi/Data/EditorTestPlayer.asset",ImportAssetOptions.ForceUpdate);Check(AssetDatabase.LoadAssetAtPath<PlayerCardTable>("Assets/RainbowLi/Data/EditorTestPlayer.asset").rows.Length==26,"saved table asset reloads");Destroy(window);AssetDatabase.DeleteAsset("Assets/RainbowLi/Data/EditorTestPlayer.asset");
  var manual=ScriptableObject.CreateInstance<PlayerCardTable>();manual.rows=p.rows.Take(6).Select((r,i)=>{var row=r.Copy();row.number=i+1;return row;}).ToArray();var setup=new GameSetup {RandomStartingDeck=false};manual.Apply(setup);var game=new ContainmentGame(33,setup);Check(game.Deck.Select(c=>c.Number).SequenceEqual(Enumerable.Range(1,6)),"manual deck reads edited card digits");Check(game.Deck.All(c=>c.Spec!=null),"manual deck uses table effects");Destroy(manual);
  // An enemy status effect must respect the sheet's polarity/lifetime, including forecast copies.
  var statusTable=ScriptableObject.CreateInstance<EnemyCardTable>();var row2=e.rows.First(r=>r.effect==CardEffect.Poison).Copy();row2.enabled=true;row2.min=row2.max=6;row2.range="6";row2.number=5;statusTable.rows=new[]{row2};setup=new GameSetup {PlayerHealth=500,EnemyHealth=500,EnemyActions=1};statusTable.Apply(setup);game=new ContainmentGame(10,setup);game.StartBattle();game.PlayerRules.Clear();foreach(var rule in game.ArenaRules)rule.Numbers=new[]{0,0,0};game.EnemyRule.Numbers=new[]{0,0,0};game.Hand.Clear();var card=new Card {Number=2,Effect=CardEffect.Mend,BaseBonus=0};game.Hand.Add(card);game.InsertCard(card,game.Timeline.Count);Check(game.Timeline[0].EffectSpec.Polarity==EffectPolarity.Negative&&game.Timeline[0].Value==6,"enemy action contains table spec and fixed rolled X");var prediction=game.Forecast();Check(game.Confirm(),"status battle confirms");game.Step();Check(game.Player.Statuses[CardEffect.Poison]==6,"enemy table poison targets player");int guard=0;while(game.Phase==GamePhase.Resolving&&game.Round==1&&guard++<100)game.Step();Check(game.Health==prediction.Health&&game.EnemyHealth==prediction.EnemyHealth,"table enemy forecast agrees with actual status resolution");Destroy(statusTable);
  // Custom positive polarity on poison must be retained rather than inferred from the enum.
  row2.polarity=EffectPolarity.Positive;setup.EnemySkills[0].Spec=row2.Spec();game=new ContainmentGame(20,setup);game.StartBattle();game.PlayerRules.Clear();foreach(var rule in game.ArenaRules)rule.Numbers=new[]{0,0,0};game.EnemyRule.Numbers=new[]{0,0,0};game.Hand.Clear();game.Hand.Add(card);game.InsertCard(card,game.Timeline.Count);prediction=game.Forecast();game.Confirm();game.Step();Check(game.Enemy.Statuses[CardEffect.Poison]==6&&game.Player.Statuses[CardEffect.Poison]==0,"enemy table custom polarity is authoritative");guard=0;while(game.Phase==GamePhase.Resolving&&game.Round==1&&guard++<100)game.Step();Check(game.EnemyHealth==prediction.EnemyHealth,"custom polarity survives forecast cloning");
  EditorSceneManager.OpenScene("Assets/RainbowLi/Scenes/RainbowLiGame.unity");started=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();
 }catch(Exception ex){Debug.LogError("EDITOR CHECK FAILED: "+ex);EditorApplication.Exit(1);}}
 static void Destroy(UnityEngine.Object o){UnityEngine.Object.DestroyImmediate(o);}
 static void Log(string message,string stack,LogType type){if(stack.Contains("UnityEditor.Search.SearchDatabase")&&stack.Contains("IndexationOnStartup"))return;if(type==LogType.Error||type==LogType.Exception){Console.WriteLine("EDITOR CHECK FAILED: "+message);EditorApplication.Exit(1);}}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-started>60)throw new Exception("play mode timeout");if(!EditorApplication.isPlaying)return;var view=UnityEngine.Object.FindFirstObjectByType<GameView>();var map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();if(view==null||!view.IsReady||map==null||map.levels.Run==null)return;
  Check(view.session.player.definition.cardTable!=null&&view.session.enemy.definition.cardTable!=null,"native scene actor definitions use independent sheets");var game=view.session.Game;Check(game.Deck.Count==18&&game.Deck.All(c=>c.Spec!=null),"native random starter from table");Check(Enumerable.Range(1,6).All(n=>game.Deck.Count(c=>c.Number==n)==3),"three digit groups retained");map.nodes.First(n=>n.nodeId==0).button.onClick.Invoke();Check(game.Timeline.All(a=>a.Enemy&&a.EffectSpec!=null),"native enemy intents generated from table");
  Console.WriteLine("PASS: "+checks+" card editor / workbook / native integration checks");EditorApplication.update-=Tick;EditorApplication.Exit(0);
 }catch(Exception ex){Console.WriteLine("EDITOR CHECK FAILED: "+ex);EditorApplication.Exit(1);}}
}
