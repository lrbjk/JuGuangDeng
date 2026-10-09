using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class RouteSceneMigration {
  const string Root="Assets/RainbowLi";
  static Font font;
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("ROUTE MIGRATION PASS: branch graph and level module saved.");}
  public static void Apply(){
   var session=Object.FindFirstObjectByType<GameSession>();var gameView=Object.FindFirstObjectByType<GameView>();font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Resources/RainbowLiChinese.ttf");
   var def=AssetDatabase.LoadAssetAtPath<LevelDefinition>(Root+"/Data/DefaultLevels.asset");if(def==null){def=ScriptableObject.CreateInstance<LevelDefinition>();def.authoredNodes=RouteGenerator.Generate(104729,def.floors,def.lanes).Nodes;AssetDatabase.CreateAsset(def,Root+"/Data/DefaultLevels.asset");}
   var levels=session.GetComponentInChildren<LevelModule>(true);if(levels==null){var go=new GameObject("LevelSystem");go.transform.SetParent(session.transform,false);levels=go.AddComponent<LevelModule>();}levels.session=session;levels.definition=def;gameView.progression.levels=levels;
   var controller=gameView.GetComponent<RouteMapView>();if(controller==null)controller=gameView.gameObject.AddComponent<RouteMapView>();controller.levels=levels;
   if(controller.mapRoot!=null){EditorUtility.SetDirty(controller);return;}
   foreach(Transform child in gameView.mapPanel.transform)child.gameObject.SetActive(false);
   var root=Rect("BranchMap",gameView.mapPanel.transform,0,0,1090,600);controller.mapRoot=root.gameObject;
   controller.title=Label("RouteTitle",root,20,2,1050,46,"分叉路线 / 选择起点",27);controller.legend=Label("RouteLegend",root,20,555,1050,40,"从下往上前进 · 沿连线选择下一层",16);
   var scroll=Rect("RouteScroll",root,20,62,1050,480);var sr=scroll.gameObject.AddComponent<ScrollRect>();sr.horizontal=false;sr.movementType=ScrollRect.MovementType.Clamped;controller.scroll=sr;
   var vp=Rect("Viewport",scroll,0,0,1050,480);vp.gameObject.AddComponent<RectMask2D>();var viewportImage=vp.gameObject.AddComponent<Image>();viewportImage.color=new Color(.045f,.15f,.105f);viewportImage.raycastTarget=true;
   var content=Rect("RouteContent",vp,0,0,1020,1110);sr.viewport=vp;sr.content=content;controller.content=content;
   var connections=Rect("Connections",content,0,0,1020,1110);controller.connections=connections;
   var prefab=CreateNodePrefab();controller.nodePrefab=prefab;var graph=new RouteGraph(def.authoredNodes);
   foreach(var n in graph.Nodes){var view=((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,content)).GetComponent<LevelNodeView>();view.name="RouteNode_"+n.id;view.nodeId=n.id;view.authoredFloor=n.floor;view.authoredLane=n.lane;var rect=(RectTransform)view.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=controller.Position(n,graph);view.Bind(n,new RouteRun(graph),()=>{});controller.nodes.Add(view);}
   foreach(var n in graph.Nodes)foreach(int id in n.next){var a=controller.Position(n,graph);var b=controller.Position(graph.Find(id),graph);var line=Rect("Connection_"+n.id+"_"+id,connections,0,0,1,3);line.pivot=new Vector2(.5f,.5f);line.anchoredPosition=(a+b)/2;line.sizeDelta=new Vector2((b-a).magnitude,3);line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);var im=line.gameObject.AddComponent<Image>();im.color=new Color(.28f,.37f,.3f);im.raycastTarget=false;}
   var ep=Rect("RouteEventPanel",gameView.mapPanel.transform.parent,340,290,1060,565);controller.eventPanel=ep.gameObject;var bg=ep.gameObject.AddComponent<Image>();bg.color=new Color(.1f,.18f,.14f);bg.raycastTarget=false;
   Label("EventTitle",ep,20,60,1020,70,"档案遗址 / 选择一次奖励",30);Label("EventBody",ep,60,170,940,95,"探索途中找到遗留档案。\n收集金币，或维护体能后继续前进。",22);
   var gold=Button("EventGold",ep,75,345,420,78,"收集档案 / 获得金币");UnityEventTools.AddPersistentListener(gold.onClick,levels.EventGold);
   var heal=Button("EventHeal",ep,565,345,420,78,"维护体能 / 恢复生命");UnityEventTools.AddPersistentListener(heal.onClick,levels.EventHeal);ep.gameObject.SetActive(false);
   EditorUtility.SetDirty(levels);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(gameView.progression);
  }
  static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  static Text Label(string name,Transform parent,float x,float y,float w,float h,string value,int size){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.92f,.84f,.6f);t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=11;t.resizeTextMaxSize=size;return t;}
  static Button Button(string name,Transform parent,float x,float y,float w,float h,string label){var r=Rect(name,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=new Color(.15f,.48f,.36f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;Label("Label",r,2,2,w-4,h-4,label,22);return b;}
  static LevelNodeView CreateNodePrefab(){
   string path=Root+"/Prefabs/LevelNode.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing.GetComponent<LevelNodeView>();
   var r=Rect("LevelNode",null,0,0,140,76);var view=r.gameObject.AddComponent<LevelNodeView>();view.face=r.gameObject.AddComponent<Image>();view.face.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/CardFace.png");view.face.type=Image.Type.Sliced;view.face.color=new Color(.15f,.48f,.36f);view.button=r.gameObject.AddComponent<Button>();view.button.targetGraphic=view.face;view.label=Label("NodeLabel",r,4,4,132,68,"战斗\n可选择",20);var shadow=r.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(4,-5);shadow.effectColor=new Color(0,0,0,.5f);var prefab=PrefabUtility.SaveAsPrefabAsset(r.gameObject,path);Object.DestroyImmediate(r.gameObject);return prefab.GetComponent<LevelNodeView>();
  }
 }
}
