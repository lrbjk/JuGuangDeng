using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class CardDungeonSceneMigration {
 const string Root="Assets/RainbowLi";
 [MenuItem("RainbowLi/关卡/接入玩家移动的卡片地牢")]
 public static void ApplyCurrent(){if(Application.isPlaying)return;var s=EditorSceneManager.GetActiveScene();if(!s.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(s);}
 public static void Run(){var s=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();}
 public static void Apply(){
  var map=UnityEngine.Object.FindFirstObjectByType<RouteMapView>();if(map==null)throw new InvalidOperationException("缺少 RouteMapView。");
  var def=map.levels.definition;Undo.RecordObject(def,"卡片地牢配置");def.cardDungeon=true;def.stackedLayout=true;def.startingLane=-1;
  if(map.playerMarker!=null&&map.nodePrefab.name=="DungeonCard"){EditorUtility.SetDirty(def);return;}
  Undo.RecordObject(map,"地牢视图");map.foregroundSpacing=285;map.connections.gameObject.SetActive(false);
  var prefab=CreatePrefab();map.nodePrefab=prefab;
  foreach(var node in map.nodes)if(node!=null)Undo.DestroyObjectImmediate(node.gameObject);map.nodes.Clear();
  var graph=def.CreateGraph(104729);var run=new RouteRun(graph);foreach(var n in graph.Nodes){var node=((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,map.content)).GetComponent<LevelNodeView>();node.name="DungeonCell_"+n.id;node.nodeId=n.id;node.authoredFloor=n.floor;node.authoredLane=n.lane;node.map=map;((RectTransform)node.transform).anchoredPosition=map.Position(n,graph);node.SetDepth(map.NodeScale(n,graph));node.Bind(n,run,()=>{});node.SetStackDepth(map.StackDepth(n,graph));node.gameObject.SetActive(map.StackDepth(n,graph)<=6);map.nodes.Add(node);}
  foreach(var node in map.nodes.OrderByDescending(n=>n.authoredFloor))node.transform.SetAsLastSibling();
  var marker=new GameObject("PlayerPosition",typeof(RectTransform),typeof(Image));Undo.RegisterCreatedObjectUndo(marker,"玩家位置标记");marker.transform.SetParent(map.content,false);map.playerMarker=(RectTransform)marker.transform;map.playerMarker.anchorMin=map.playerMarker.anchorMax=new Vector2(0,1);map.playerMarker.pivot=new Vector2(.5f,.5f);map.playerMarker.sizeDelta=new Vector2(130,40);map.playerMarker.anchoredPosition=new Vector2(map.contentWidth/2,-775);marker.GetComponent<Image>().color=new Color(.28f,.24f,.15f);marker.GetComponent<Image>().raycastTarget=false;
  var label=new GameObject("Label",typeof(RectTransform),typeof(Text));label.transform.SetParent(marker.transform,false);var rect=(RectTransform)label.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;var text=label.GetComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/SourceHanSansCN-Bold.otf");text.fontSize=20;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.94f,.82f,.55f);text.text="玩家位置";text.raycastTarget=false;
  map.title.text="收容地牢";map.progress.text="当前位置  入口 / 第 "+(graph.StartLane+1)+" 列";map.legend.text="选择前方左 / 中 / 右卡片  ·  完成后移动位置";map.mapRoot.transform.Find("Overview").GetComponentInChildren<Text>().text="查看 / 返回地牢";
  EditorUtility.SetDirty(def);EditorUtility.SetDirty(map);
 }
 static LevelNodeView CreatePrefab(){
  string path=Root+"/Prefabs/DungeonCard.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing.GetComponent<LevelNodeView>();
  var original=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/RouteBannerNode.prefab");var go=UnityEngine.Object.Instantiate(original);go.name="DungeonCard";var node=go.GetComponent<LevelNodeView>();((RectTransform)go.transform).sizeDelta=new Vector2(190,280);node.banner.pointDepth=0;node.banner.borderWidth=3;go.transform.Find("TopRail").gameObject.SetActive(false);
  var name=(RectTransform)node.label.transform;name.anchoredPosition=new Vector2(14,-172);name.sizeDelta=new Vector2(162,46);node.label.resizeTextMaxSize=25;
  var state=(RectTransform)node.stateLabel.transform;state.anchoredPosition=new Vector2(14,-220);state.sizeDelta=new Vector2(162,34);node.SetStackDepth(0);
  var prefab=PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);return prefab.GetComponent<LevelNodeView>();
 }
}
}
