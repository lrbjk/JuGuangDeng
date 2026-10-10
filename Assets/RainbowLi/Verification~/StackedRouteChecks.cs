using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi;
using RainbowLi.UnityGame;
using RainbowLi.EditorTools;
public static class StackedRouteChecks {
 static int checks,stage;static double started;static GameView view;static RouteMapView map;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;}
 public static void Run(){try{
  for(int seed=0;seed<40;seed++){
   var graph=new RouteGraph(RouteGenerator.Generate(seed,4+seed%13,2+seed%4).Nodes,true);var run=new RouteRun(graph);Check(graph.Nodes.All(n=>n.next.Count==0),"stacks contain no path edges");
   while(!run.Finished){var allowed=graph.Nodes.Where(n=>run.CanEnter(n.id)).ToArray();Check(allowed.Length>0,"no deadlock");foreach(var n in graph.Nodes){bool front=n.kind==LevelKind.Boss?graph.Nodes.Where(v=>v.kind!=LevelKind.Boss&&!run.IsAbandoned(v.id)).All(v=>run.Completed.Contains(v.id)):!graph.Nodes.Any(v=>v.kind!=LevelKind.Boss&&v.lane==n.lane&&v.floor<n.floor&&!run.Completed.Contains(v.id));Check(run.CanEnter(n.id)==(!run.Completed.Contains(n.id)&&!run.IsAbandoned(n.id)&&front),"only exposed nodes enter");}
    var next=allowed[(seed+run.Completed.Count)%allowed.Length];Check(run.Enter(next.id),"enter exposed node");Check(!graph.Nodes.Any(n=>run.CanEnter(n.id)),"active room blocks second entry");run.Complete();
   }Check(run.Completed.Count==graph.Nodes.Count(n=>n.kind==LevelKind.Boss||n.lane==run.ChosenLane),"only chosen column cleared before boss");
  }
  var def=ScriptableObject.CreateInstance<LevelDefinition>();def.randomizeEachRun=false;def.authoredNodes=RouteGenerator.Generate(1).Nodes;foreach(var n in def.authoredNodes)n.next.Clear();Check(LevelRouteEditorWindow.ValidateDefinition(def)==null,"stack editor needs no edges");Check(def.CreateGraph(1).Stacked,"definition builds stacks");UnityEngine.Object.DestroyImmediate(def);
  var legacy=new RouteRun(RouteGenerator.Generate(1));legacy.Enter(0);legacy.Complete();Check(legacy.CanEnter(3)&&!legacy.CanEnter(2),"legacy paths preserved");
  RouteBannerSceneMigration.RunStacked();started=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;EditorApplication.EnterPlaymode();
 }catch(Exception e){Console.WriteLine("STACK FAILED: "+e);EditorApplication.Exit(1);}}
 static void Log(string text,string stack,LogType type){if(stack.Contains("UnityEditor.Search.SearchDatabase"))return;if(type==LogType.Exception||type==LogType.Error){Console.WriteLine("STACK FAILED: "+text+stack);EditorApplication.Exit(1);}}
 static void Tick(){try{
  if(EditorApplication.timeSinceStartup-started>80)throw new Exception("timeout");if(!EditorApplication.isPlaying)return;view=UnityEngine.Object.FindFirstObjectByType<GameView>();map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();if(view==null||!view.IsReady||map.levels.Run==null)return;
  var run=map.levels.Run;var g=view.session.Game;
  if(stage==0){Check(run.Graph.Stacked,"live stacked mode");Check(!map.connections.gameObject.activeSelf,"no connection lines");Check(map.nodes.Count(n=>n.button.interactable)==3,"one exposed node per column");Check(!map.nodes.First(n=>n.nodeId==3).banner.raycastTarget,"covered banner cannot intercept pointer");map.ShowTooltip(3);Check(!map.tooltipPanel.activeSelf,"covered tooltip hidden");map.nodes.First(n=>n.nodeId==3).button.onClick.Invoke();Check(g.Phase==GamePhase.Map&&run.ActiveId<0,"covered click rejected even direct invocation");Capture("01-stacks");
   var right=(RectTransform)map.nodes.First(n=>n.nodeId==2).transform;Vector2 rightBefore=right.anchoredPosition;map.nodes.First(n=>n.nodeId==0).button.onClick.Invoke();Check(g.Phase==GamePhase.Rest,"left front enters rest");view.progression.Leave();Check(run.Completed.Contains(0)&&run.CanEnter(3),"left next exposed");Check(!run.CanEnter(1)&&!run.CanEnter(2),"other columns cannot enter");Check(map.nodes.Where(n=>n.authoredLane!=0&&run.Graph.Find(n.nodeId).kind!=LevelKind.Boss).All(n=>!n.gameObject.activeSelf),"all other columns hidden");Check(map.nodes.Count(n=>n.button.interactable)==1,"only the immediate next room selectable");map.nodes.First(n=>n.nodeId==1).button.onClick.Invoke();Check(g.Phase==GamePhase.Map,"direct hidden click rejected");map.ShowTooltip(1);Check(!map.tooltipPanel.activeSelf,"hidden column tooltip rejected");map.ToggleOverview();Check(map.nodes.Where(n=>run.IsAbandoned(n.nodeId)).All(n=>!n.gameObject.activeSelf),"overview does not restore abandoned columns");map.ToggleOverview();Check(right.anchoredPosition==rightBefore,"other column does not move");Check(!map.nodes.First(n=>n.nodeId==0).gameObject.activeSelf,"finished front removed");Check(map.nodes.First(n=>n.nodeId==3).transform.localScale==Vector3.one,"next promoted to front size");Capture("02-left-cleared");stage=1;return;
  }
  if(stage==1){while(!run.Finished){int id=run.Graph.Nodes.First(n=>run.CanEnter(n.id)).id;Check(run.Enter(id)&&run.Complete(),"complete remaining stacked rooms");view.session.Notify();}Check(run.Completed.Count==run.Graph.Nodes.Count(n=>n.kind==LevelKind.Boss||n.lane==run.ChosenLane),"boss requires chosen column only");view.session.NewGame();Check(map.nodes.Count(n=>n.button.interactable)==3&&map.levels.Run.Completed.Count==0&&map.levels.Run.ChosenLane==-1,"new game restores initial choices");Console.WriteLine("PASS: "+checks+" chosen column, hidden entry, legacy, editor and live UI checks.");EditorApplication.update-=Tick;EditorApplication.Exit(0);}
 }catch(Exception e){Console.WriteLine("STACK FAILED: "+e);EditorApplication.Exit(1);}}
 static void Capture(string name){if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;var canvas=view.GetComponent<Canvas>();var mode=canvas.renderMode;var cam=UnityEngine.Object.FindFirstObjectByType<Camera>();var target=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);try{cam.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();foreach(var t in view.GetComponentsInChildren<Text>())t.SetAllDirty();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();Directory.CreateDirectory("Assets/RainbowLi/Verification~/StackPreview");File.WriteAllBytes("Assets/RainbowLi/Verification~/StackPreview/"+name+".png",image.EncodeToPNG());}finally{RenderTexture.active=active;cam.targetTexture=target;canvas.renderMode=mode;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
}

