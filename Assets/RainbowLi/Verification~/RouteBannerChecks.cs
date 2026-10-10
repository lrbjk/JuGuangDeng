using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using RainbowLi;
using RainbowLi.UnityGame;
using RainbowLi.EditorTools;
public static class RouteBannerChecks {
 static int checks,stage;static double start;static GameView view;static RouteMapView map;
 static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);checks++;}
 public static void Run(){try{
  var d=ScriptableObject.CreateInstance<LevelDefinition>();LevelRouteEditorWindow.Generate(d,104729);d.randomizeEachRun=false;Check(LevelRouteEditorWindow.ValidateDefinition(d)==null,"generated graph valid");
  var a=d.authoredNodes[0];int next=a.next[1];LevelRouteEditorWindow.ToggleConnection(d,a.id,next);Check(!a.next.Contains(next),"toggle removes edge");LevelRouteEditorWindow.ToggleConnection(d,a.id,next);Check(a.next.Contains(next),"toggle adds edge");
  bool rejected=false;try{LevelRouteEditorWindow.ToggleConnection(d,a.id,d.authoredNodes.Last().id);}catch(ArgumentException){rejected=true;}Check(rejected,"skip floor edge rejected");
  a.next.Clear();Check(LevelRouteEditorWindow.ValidateDefinition(d)!=null,"dead end rejected");LevelRouteEditorWindow.Generate(d,104729);d.authoredNodes[1].lane=d.authoredNodes[0].lane;Check(LevelRouteEditorWindow.ValidateDefinition(d)!=null,"duplicate position rejected");LevelRouteEditorWindow.Generate(d,104729);int removed=d.authoredNodes[3].id;LevelRouteEditorWindow.RemoveNode(d,removed);Check(!d.authoredNodes.Any(n=>n.next.Contains(removed)),"delete removes incoming edges");
  LevelRouteEditorWindow.Generate(d,104729);d.presentations.Add(new LevelDefinition.NodePresentation{nodeId=1,title="测试关卡",description="测试说明"});Check(d.Presentation(1).title=="测试关卡","fixed presentation");d.randomizeEachRun=true;Check(d.Presentation(1)==null,"random ignores ID presentation");UnityEngine.Object.DestroyImmediate(d);
  var saved=ScriptableObject.CreateInstance<LevelDefinition>();saved.randomizeEachRun=false;LevelRouteEditorWindow.Generate(saved,998);const string temp="Assets/RainbowLi/Data/RouteRoundTripCheck.asset";AssetDatabase.DeleteAsset(temp);saved.name="RouteRoundTripCheck";AssetDatabase.CreateAsset(saved,temp);saved.presentations.Add(new LevelDefinition.NodePresentation{nodeId=0,title="存档节点"});EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceUpdate);saved=AssetDatabase.LoadAssetAtPath<LevelDefinition>(temp);Check(saved.CreateGraph(123).Nodes.Count==22&&saved.Presentation(0).title=="存档节点","asset save/load round trip");Undo.ClearAll();int edge=saved.authoredNodes[0].next[0];LevelRouteEditorWindow.ToggleConnection(saved,0,edge);Undo.FlushUndoRecordObjects();Undo.PerformUndo();Check(saved.authoredNodes[0].next.Contains(edge),"connection undo restores graph");AssetDatabase.DeleteAsset(temp);
  RouteBannerSceneMigration.Run();view=UnityEngine.Object.FindFirstObjectByType<GameView>();map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();Check(map.perspectiveLayout&&map.nodes.Count==22,"saved banner map");Check(map.nodes.All(n=>PrefabUtility.IsPartOfPrefabInstance(n)&&n.banner!=null),"native banner prefab instances");Check(map.levels.definition.randomizeEachRun==false,"authored map enabled");
  foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"no missing scripts "+t.name);
  RouteBannerSceneMigration.Apply();Check(map.nodes.Count==22,"migration idempotent");
  start=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;LevelRouteEditorWindow.Open(map.levels.definition);EditorApplication.update+=Tick;EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();
 }catch(Exception ex){Console.WriteLine("BANNER FAILED: "+ex);EditorApplication.Exit(1);}}
 static void Log(string message,string stack,LogType type){if(stack.Contains("UnityEditor.Search.SearchDatabase"))return;if(type==LogType.Error||type==LogType.Exception){Console.WriteLine("BANNER FAILED: "+message+stack);EditorApplication.Exit(1);}}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-start>100)throw new Exception("timeout "+stage);if(!EditorApplication.isPlaying)return;
  view=UnityEngine.Object.FindFirstObjectByType<GameView>();map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();if(view==null||!view.IsReady||map.levels.Run==null)return;var g=view.session.Game;var run=map.levels.Run;
  if(stage==0){
   view.battle.autoPlay=false;Check(g.Phase==GamePhase.Map&&map.mapRoot.activeInHierarchy,"initial map visible");Check(map.nodes.Count(n=>n.button.interactable)==3,"three available starting flags");Check(map.nodes.First(n=>n.nodeId==0).label.text=="休息","rest label");Check(map.nodes.First(n=>n.nodeId==2).label.text=="精英","elite label");Check(map.DepthScale(0)>map.DepthScale(3),"perspective scaling");Check(map.healthBar.value==g.Health,"map health bound");
   map.ShowTooltip(2);Check(map.tooltipPanel.activeSelf&&map.tooltipBody.text.Contains("额外金币"),"elite tooltip");Capture("01-route");map.HideTooltip();
   map.nodes.First(n=>n.nodeId==0).button.onClick.Invoke();Check(g.Phase==GamePhase.Rest&&!map.mapRoot.activeSelf,"rest enters service");view.progression.Leave();Check(g.Phase==GamePhase.Map&&run.CurrentId==0,"rest completes");Check(map.FocusFloor==1,"next layer foreground");Check(map.nodes.Where(n=>n.authoredFloor==0).All(n=>!n.gameObject.activeSelf),"past layer hidden");Check(map.nodes.Count(n=>n.button.interactable)==run.Graph.Find(0).next.Count,"only connected choices available");map.ToggleOverview();Check(map.FocusFloor==0&&map.nodes.All(n=>n.gameObject.activeSelf),"overview exposes full graph");Capture("02-overview");map.ToggleOverview();
   view.session.NewGame();g=view.session.Game;map.ShowTooltip(1);map.nodes.First(n=>n.nodeId==1).button.onClick.Invoke();Check(g.Phase==GamePhase.Planning&&!map.tooltipPanel.activeSelf,"battle hides tooltip");Check(view.battlePanel.activeSelf&&!view.mapPanel.activeSelf,"battle panel retained");stage=1;return;
  }
  if(stage==1){if(view.cardMotion.BlocksResolution)return;view.session.NewGame();map.nodes.First(n=>n.nodeId==2).button.onClick.Invoke();Check(view.session.Game.Elite,"elite node starts elite battle");Check(view.session.Game.EnemyMaxHealth>view.session.enemy.definition.maxHealth,"elite health applied");view.session.NewGame();g=view.session.Game;
   var custom=map.levels.definition.presentations;custom.Add(new LevelDefinition.NodePresentation{nodeId=1,title="档案守卫",description="自定义关卡说明"});view.session.Notify();Check(map.nodes.First(n=>n.nodeId==1).label.text=="档案守卫","authored title bound");map.ShowTooltip(1);Check(map.tooltipBody.text.Contains("自定义关卡说明"),"authored tooltip bound");custom.Clear();map.HideTooltip();view.session.Notify();
   Check(map.nodes.Count(n=>n.button.interactable)==3&&runReset(),"new game resets focus and choices");
   view.cardMotion.enableMotion=false;view.battle.autoPlay=false;
   foreach(var node in map.levels.Run.Graph.Nodes){if(node.floor==2)node.kind=LevelKind.Event;if(node.floor==3)node.kind=LevelKind.Elite;if(node.floor==4)node.kind=LevelKind.Rest;if(node.floor==5)node.kind=LevelKind.Shop;}
   stage=2;return;
  }
  if(stage==2){
   if(g.Phase==GamePhase.Map){var next=run.Graph.Nodes.First(n=>run.CanEnter(n.id));map.nodes.First(n=>n.nodeId==next.id).button.onClick.Invoke();return;}
   if(g.Phase==GamePhase.Rest||g.Phase==GamePhase.Shop){view.progression.Leave();return;}
   if(g.Phase==GamePhase.Event){int coins=g.Coins;map.levels.EventGold();map.levels.EventGold();Check(g.Coins==coins+map.levels.definition.eventGold,"event payout exactly once");return;}
   if(g.Phase==GamePhase.Planning){g.EnemyHealth=1;g.EnemyShield=0;g.Hand[0].Effect=CardEffect.Strike;view.session.Notify();view.cards.Select(g.Hand[0]);view.cards.Insert(0);view.battle.Confirm();view.battle.Step();return;}
   if(g.Phase==GamePhase.Reward){view.progression.Reward(0);return;}
   if(g.Phase==GamePhase.Won){Check(run.Finished&&run.Completed.Count==run.Graph.Floors,"all layers and boss complete");view.session.NewGame();Check(map.FocusFloor==0,"new run after boss resets");Console.WriteLine("PASS: "+checks+" route editor, banner scene, asset roundtrip, Undo and full route checks.");EditorApplication.update-=Tick;EditorApplication.Exit(0);}
  }
 }catch(Exception ex){Console.WriteLine("BANNER FAILED: "+ex);EditorApplication.Exit(1);}}
 static bool runReset()=>map.FocusFloor==0&&map.levels.Run.Completed.Count==0;
 static void Capture(string name){if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;var canvas=view.GetComponent<Canvas>();var mode=canvas.renderMode;var cam=UnityEngine.Object.FindFirstObjectByType<Camera>();var target=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);try{cam.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();foreach(var text in view.GetComponentsInChildren<Text>())text.SetAllDirty();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();Directory.CreateDirectory("Assets/RainbowLi/Verification~/RoutePreview");File.WriteAllBytes("Assets/RainbowLi/Verification~/RoutePreview/"+name+".png",image.EncodeToPNG());}finally{RenderTexture.active=active;cam.targetTexture=target;canvas.renderMode=mode;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}

