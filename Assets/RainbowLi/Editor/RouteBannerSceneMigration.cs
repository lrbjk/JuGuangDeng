using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class RouteBannerSceneMigration {
 const string Root="Assets/RainbowLi";static Font font;
 [MenuItem("RainbowLi/关卡/接入旗帜选关界面")]
 public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
 public static void Run(){var s=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Debug.Log("BANNER ROUTE SCENE PASS");}
 public static void RunStacked(){var s=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");ApplyStacked();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();}
 [MenuItem("RainbowLi/关卡/切换为独立关卡堆叠")]
 public static void ApplyStacked(){
  if(Application.isPlaying)return;if(!EditorSceneManager.GetActiveScene().path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 文件夹内的场景。");
  var map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();if(map==null)throw new InvalidOperationException("请打开 RainbowLiGame 场景。");
  Undo.RecordObject(map.levels.definition,"独立关卡堆叠");map.levels.definition.stackedLayout=true;map.connections.gameObject.SetActive(false);
  var graph=map.levels.definition.CreateGraph(104729);var run=new RouteRun(graph);foreach(var node in map.nodes){var n=graph.Find(node.nodeId);if(n==null)continue;Undo.RecordObject(node.transform,"堆叠位置");((RectTransform)node.transform).anchoredPosition=map.Position(n,graph);node.SetDepth(map.NodeScale(n,graph));node.Bind(n,run,()=>{});node.SetStackDepth(map.StackDepth(n,graph));}
  var overview=map.mapRoot.transform.Find("Overview");if(overview!=null)overview.GetComponentInChildren<Text>().text="查看 / 返回完整堆叠";
  map.legend.text="选择一个起点  /  选定后沿该列前进";map.progress.text="已完成  0  关  /  首领尚未显露";
  EditorUtility.SetDirty(map.levels.definition);EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
 }
 public static void Apply(){
  var map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(map==null||view==null)throw new InvalidOperationException("场景缺少 RouteMapView / GameView。");
  if(map.perspectiveLayout&&map.mapRoot!=null&&map.mapRoot.name=="BannerRoute")return;
  font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/SourceHanSansCN-Bold.otf");Undo.RecordObject(map,"旗帜选关");
  if(map.mapRoot!=null)Undo.DestroyObjectImmediate(map.mapRoot);map.nodes.Clear();
  var panel=(RectTransform)view.mapPanel.transform;Undo.RecordObject(panel,"全屏选关");panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(0,1);panel.anchoredPosition=Vector2.zero;panel.sizeDelta=new Vector2(1920,1080);panel.SetAsLastSibling();
  var root=Rect("BannerRoute",panel,0,0,1920,1080);map.mapRoot=root.gameObject;Fill(root,new Color(.025f,.03f,.036f));
  // Layered stone pillars and a subdued horizon are editable native Images.
  for(int i=0;i<9;i++){var band=Rect("Horizon_"+i,root,0,220+i*80,1920,80);Fill(band,new Color(.065f,.068f,.071f,.12f+i*.012f));}
  for(int side=0;side<2;side++)for(int i=0;i<8;i++){var stone=Rect("Stone_"+side+"_"+i,root,side==0?35+(i%2)*18:1810-(i%2)*18,120+i*105,65,94);Fill(stone,new Color(.09f,.09f,.095f));stone.localRotation=Quaternion.Euler(0,0,side==0?7:-7);}
  Label("Chapter",root,90,52,330,32,"第一章 / 档案深处",20,new Color(.6f,.55f,.44f));
  map.title=Label("RouteTitle",root,690,43,540,52,"收容之路",36,new Color(.93f,.84f,.66f));
  map.resources=Label("Resources",root,1240,58,550,32,"生命 / 金币 / 牌组",19,new Color(.78f,.75f,.66f));
  map.healthBar=Health(root);map.healthValue=Label("HealthValue",root,325,85,105,32,"70 / 70",18,new Color(.85f,.71f,.65f));map.progress=Label("FloorProgress",root,490,116,940,40,"路线进度",18,new Color(.55f,.5f,.4f));
  var settings=Button("Settings",root,1807,44,72,48,"设置");UnityEventTools.AddPersistentListener(settings.onClick,view.battleHud.ToggleSettings);
  // Settings must stay above the full-screen map while retaining existing battle bindings.
  view.battleHud.settingsPanel.transform.SetAsLastSibling();
  var reset=Button("NewRun",root,83,970,170,48,"新的一局");UnityEventTools.AddPersistentListener(reset.onClick,view.session.NewGame);
  var overview=Button("Overview",root,1605,970,225,48,"查看 / 返回全路线");UnityEventTools.AddPersistentListener(overview.onClick,map.ToggleOverview);
  map.legend=Label("RouteHint",root,510,970,900,44,"选择一条道路",23,new Color(.75f,.69f,.55f));
  var scroll=Rect("RouteScroll",root,120,167,1680,800);map.scroll=scroll.gameObject.AddComponent<ScrollRect>();map.scroll.horizontal=false;map.scroll.movementType=ScrollRect.MovementType.Clamped;map.scroll.scrollSensitivity=28;
  var vp=Rect("Viewport",scroll,0,0,1680,800);vp.gameObject.AddComponent<RectMask2D>();var hit=vp.gameObject.AddComponent<Image>();hit.color=Color.clear;
  map.content=Rect("RouteContent",vp,0,0,1680,800);map.scroll.viewport=vp;map.scroll.content=map.content;map.connections=Rect("Connections",map.content,0,0,1680,800);
  map.perspectiveLayout=true;map.contentWidth=1680;map.foregroundSpacing=430;map.depthShrink=.23f;
  map.nodePrefab=CreatePrefab();
  var def=map.levels.definition;Undo.RecordObject(def,"示例固定路线");def.randomizeEachRun=false;if(def.authoredNodes.Count==0)def.authoredNodes=RouteGenerator.Generate(104729,def.floors,def.lanes).Nodes;
  var first=def.authoredNodes.Find(n=>n.floor==0&&n.lane==0);if(first!=null)first.kind=LevelKind.Rest;var third=def.authoredNodes.Find(n=>n.floor==0&&n.lane==2);if(third!=null)third.kind=LevelKind.Elite;
  var graph=new RouteGraph(def.authoredNodes);
  foreach(var n in graph.Nodes){var node=((GameObject)PrefabUtility.InstantiatePrefab(map.nodePrefab.gameObject,map.content)).GetComponent<LevelNodeView>();node.name="RouteNode_"+n.id;node.nodeId=n.id;node.authoredFloor=n.floor;node.authoredLane=n.lane;node.map=map;node.SetDepth(map.DepthScale(n.floor));((RectTransform)node.transform).anchoredPosition=map.Position(n,graph);node.Bind(n,new RouteRun(graph),()=>{});map.nodes.Add(node);}
  foreach(var node in System.Linq.Enumerable.OrderByDescending(map.nodes,n=>n.authoredFloor))node.transform.SetAsLastSibling();
  foreach(var n in graph.Nodes)foreach(int id in n.next){Vector2 a=map.Position(n,graph),b=map.Position(graph.Find(id),graph);var line=Rect("Connection_"+n.id+"_"+id,map.connections,0,0,1,2);line.pivot=new Vector2(.5f,.5f);line.anchoredPosition=(a+b)/2;line.sizeDelta=new Vector2((b-a).magnitude,2);line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);Fill(line,new Color(.32f,.28f,.2f,.4f));}
  var tip=Rect("RoomTooltip",root,1485,290,335,238);Fill(tip,new Color(.09f,.085f,.075f,.98f));map.tooltipPanel=tip.gameObject;map.tooltipTitle=Label("Title",tip,22,12,291,43,"战斗",25,new Color(.93f,.79f,.49f));map.tooltipBody=Label("Description",tip,22,60,291,157,"关卡说明",18,new Color(.8f,.77f,.68f));map.tooltipBody.alignment=TextAnchor.UpperLeft;tip.gameObject.SetActive(false);
  Undo.RegisterCreatedObjectUndo(root.gameObject,"创建旗帜选关界面");EditorUtility.SetDirty(def);EditorUtility.SetDirty(map);EditorUtility.SetDirty(panel);
 }
 static LevelNodeView CreatePrefab(){
  var r=Rect("RouteBannerNode",null,0,0,230,205);r.pivot=new Vector2(.5f,.5f);var n=r.gameObject.AddComponent<LevelNodeView>();n.banner=r.gameObject.AddComponent<RouteBannerGraphic>();n.button=r.gameObject.AddComponent<Button>();n.button.targetGraphic=n.banner;n.button.transition=Selectable.Transition.None;
  var bar=Rect("TopRail",r, -8,0,246,9);Fill(bar,new Color(.42f,.36f,.28f));
  var icon=Rect("RoomIcon",r,83,31,64,64);n.icon=icon.gameObject.AddComponent<RouteIconGraphic>();n.icon.raycastTarget=false;
  var custom=Rect("CustomIcon",r,83,31,64,64);n.customIcon=custom.gameObject.AddComponent<Image>();n.customIcon.preserveAspect=true;n.customIcon.raycastTarget=false;custom.gameObject.SetActive(false);
  n.label=Label("Name",r,14,103,202,46,"战斗",27,new Color(.93f,.86f,.71f));n.stateLabel=Label("State",r,14,145,202,30,"选择此路",15,new Color(.68f,.59f,.42f));
  var shadow=r.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(0,-8);shadow.effectColor=new Color(0,0,0,.7f);
  var prefab=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Root+"/Prefabs/RouteBannerNode.prefab");UnityEngine.Object.DestroyImmediate(r.gameObject);return prefab.GetComponent<LevelNodeView>();
 }
 static Slider Health(Transform parent){var r=Rect("MapHealth",parent,85,98,230,10);Fill(r,new Color(.16f,.12f,.13f));var fill=Rect("Fill",r,0,0,230,10);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;Fill(fill,new Color(.65f,.18f,.2f));var s=r.gameObject.AddComponent<Slider>();s.fillRect=fill;s.interactable=false;return s;}
 static RectTransform Rect(string name,Transform p,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(p,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 static void Fill(RectTransform r,Color c){var im=r.gameObject.AddComponent<Image>();im.color=c;im.raycastTarget=false;}
 static Text Label(string name,Transform p,float x,float y,float w,float h,string text,int size,Color color){var t=Rect(name,p,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=12;t.resizeTextMaxSize=size;return t;}
 static Button Button(string name,Transform p,float x,float y,float w,float h,string text){var r=Rect(name,p,x,y,w,h);Fill(r,new Color(.14f,.13f,.11f));r.GetComponent<Image>().raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();Label("Label",r,3,3,w-6,h-6,text,18,new Color(.78f,.72f,.58f));return b;}
}
}

