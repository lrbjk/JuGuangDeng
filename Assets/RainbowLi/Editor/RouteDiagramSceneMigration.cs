using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class RouteDiagramSceneMigration {
  const string Root="Assets/RainbowLi";
  static readonly Color Gold=new Color(.93f,.78f,.47f),Cream=new Color(.95f,.92f,.8f),Surface=new Color(.055f,.14f,.12f),Tile=new Color(.085f,.20f,.17f);
  static Font font;
  [MenuItem("RainbowLi/UI/接入三列卡片选关界面")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  public static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();var map=view.GetComponent<RouteMapView>();var root=map.mapRoot.transform;
   font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Resources/RainbowLiChinese.ttf");
   var growth=view.session.GetComponent<EnemyGrowthModule>();if(growth==null)growth=view.session.gameObject.AddComponent<EnemyGrowthModule>();view.session.enemyGrowth=growth;
   var hud=view.GetComponent<AdventureMapHudView>();if(hud==null)hud=view.gameObject.AddComponent<AdventureMapHudView>();hud.session=view.session;hud.levels=map.levels;hud.sequences=view.sequences;hud.growth=growth;
   if(root.Find("AdventureSidebar")!=null){EditorUtility.SetDirty(hud);EditorUtility.SetDirty(view.session);return;}
   foreach(Transform child in root)if(child.name.StartsWith("Horizon_")||child.name.StartsWith("Stone_")||new[]{"Chapter","RouteTitle","MapHealth","HealthValue","FloorProgress","Overview","NewRun"}.Contains(child.name))child.gameObject.SetActive(false);
   root.GetComponent<Image>().color=new Color(.035f,.11f,.085f);
   var sidebar=Panel("AdventureSidebar",root,20,20,406,1032,Surface);
   var stats=Panel("ProgressGroup",sidebar,18,16,370,288,Surface);
   hud.completedRooms=Label("CompletedRooms",Panel("CompletedTile",stats,20,20,330,64,Tile),8,0,314,64,"已完成关卡  0",25,Gold);
   hud.elapsedTime=Label("ElapsedTime",Panel("TimerTile",stats,20,118,330,62,Tile),8,0,314,62,"已用时间  00:00:00",23,Cream);
   var newRun=Click("NewRun",stats,20,230,160,36,"新的一局");UnityEventTools.AddPersistentListener(newRun.onClick,view.session.NewGame);
   var save=Click("Save",stats,194,230,68,36,"保存");UnityEventTools.AddPersistentListener(save.onClick,map.levels.Save);
   var load=Click("Continue",stats,270,230,80,36,"继续");UnityEventTools.AddPersistentListener(load.onClick,map.levels.Continue);
   var enemy=Panel("EnemyGrowthGroup",sidebar,18,330,370,244,Surface);
   hud.growthCount=Label("GrowthCount",Panel("CountTile",enemy,20,20,330,58,Tile),8,0,314,58,"敌人增益  0 次",25,Gold);
   hud.growthDescription=Label("GrowthDescription",enemy,22,96,326,128,"后续敌人：力量 +0",19,Cream);hud.growthDescription.alignment=TextAnchor.MiddleLeft;
   var resources=Panel("Resources",sidebar,38,612,330,70,Tile);
   map.resources.transform.SetParent(resources,false);Place(map.resources.transform,8,10,146,50);Style(map.resources,21,Cream);
   var deck=view.battleHud.mapDeckCount.transform.parent;deck.SetParent(resources,false);Place(deck,170,10,152,50);Place(view.battleHud.mapDeckCount.transform,4,0,144,50);Style(view.battleHud.mapDeckCount,20,Cream);
   var rules=Panel("PlayerSequenceGroup",sidebar,18,746,370,286,Surface);Label("Heading",rules,8,2,354,36,"玩家牌序",26,Gold);
   hud.playerSequences=new SequenceView[3];
   for(int i=0;i<3;i++){
    var row=UnityEngine.Object.Instantiate(view.ruleViews[i],rules);row.name="MapPlayerSequence_"+i;Place(row.transform,14,42+i*80,340,74);
    for(int d=0;d<3;d++)row.digits[d].onClick=new Button.ButtonClickedEvent();
    int index=i;row.primaryEffect.onClick=new Button.ButtonClickedEvent();row.secondaryEffect.onClick=new Button.ButtonClickedEvent();
    if(i==0){UnityEventTools.AddIntPersistentListener(row.primaryEffect.onClick,view.sequences.EditPrimary,0);UnityEventTools.AddIntPersistentListener(row.secondaryEffect.onClick,view.sequences.EditSecondary,0);}
    if(i==1){UnityEventTools.AddIntPersistentListener(row.primaryEffect.onClick,view.sequences.EditPrimary,1);UnityEventTools.AddIntPersistentListener(row.secondaryEffect.onClick,view.sequences.EditSecondary,1);}
    if(i==2){UnityEventTools.AddIntPersistentListener(row.primaryEffect.onClick,view.sequences.EditPrimary,2);UnityEventTools.AddIntPersistentListener(row.secondaryEffect.onClick,view.sequences.EditSecondary,2);}
    hud.playerSequences[i]=row;
   }
   int adventureLayer=map.levels.definition==null?1:Mathf.Max(1,map.levels.definition.adventureLayer);int requirement=CombatResolver.Clamp(5L+adventureLayer);var boss=Panel("BossRoom",root,916,42,348,104,Surface);Label("Title",boss,8,4,332,36,"BOSS 房间",27,Gold);hud.battleCount=Label("BattleCount",boss,8,44,332,30,"战斗进度  0 / "+requirement+"  ·  第 "+adventureLayer+" 层",21,Cream);hud.bossState=Label("BossState",boss,8,74,332,26,"再完成 "+requirement+" 场战斗解锁",16,Cream);
   hud.bossButton=boss.gameObject.AddComponent<Button>();hud.bossButton.targetGraphic=boss.GetComponent<Image>();boss.GetComponent<Image>().raycastTarget=true;UnityEventTools.AddPersistentListener(hud.bossButton.onClick,hud.EnterBoss);
   var strip=Panel("BattleProgress",root,744,157,708,86,Surface);hud.battleMarkers=new Image[5];for(int i=0;i<5;i++){var marker=Panel("BattleMarker_"+i,strip,78+i*118,11,64,64,Tile);hud.battleMarkers[i]=marker.GetComponent<Image>();Label("Number",marker,0,0,64,64,Mathf.CeilToInt((i+1)*requirement/5f).ToString(),22,Cream);}
   Place(map.scroll.transform,510,270,1180,680);Place(map.scroll.viewport,0,0,1180,680);Place(map.content,0,0,1180,680);Place(map.connections,0,0,1180,680);
   map.diagramLayout=true;map.perspectiveLayout=true;map.contentWidth=1180;map.foregroundSpacing=354;map.scroll.vertical=false;map.scroll.horizontal=false;
   map.nodePrefab=CreateCard();
   foreach(var node in map.nodes)UnityEngine.Object.DestroyImmediate(node.gameObject);map.nodes.Clear();
   var graph=map.levels.definition.CreateGraph(104729);var run=new RouteRun(graph);
   foreach(var n in graph.Nodes){var node=((GameObject)PrefabUtility.InstantiatePrefab(map.nodePrefab.gameObject,map.content)).GetComponent<LevelNodeView>();node.name="RouteNode_"+n.id;node.nodeId=n.id;node.authoredFloor=n.floor;node.authoredLane=n.lane;node.map=map;node.Bind(n,run,()=>{});node.SetStackDepth(Mathf.Max(0,n.floor));Place(node.transform,map.contentWidth/2+(n.lane-1)*354,8+64*(4-Mathf.Min(4,n.floor)),278,400);((RectTransform)node.transform).pivot=new Vector2(.5f,1);node.gameObject.SetActive(n.kind!=LevelKind.Boss&&n.floor<=4);}
   foreach(var node in map.content.GetComponentsInChildren<LevelNodeView>(true).OrderByDescending(n=>n.authoredFloor)){node.transform.SetAsLastSibling();map.nodes.Add(node);}
   map.connections.gameObject.SetActive(false);if(map.playerMarker!=null)map.playerMarker.gameObject.SetActive(false);
   Place(map.legend.transform,510,966,1180,50);Style(map.legend,22,Cream);
   Place(map.tooltipPanel.transform,1640,300,250,260);Place(map.tooltipTitle.transform,16,12,218,40);Place(map.tooltipBody.transform,16,58,218,186);map.tooltipPanel.transform.SetAsLastSibling();
   var settings=root.Find("Settings");if(settings!=null){Place(settings,1766,45,90,90);var text=settings.GetComponentInChildren<Text>();Place(text.transform,3,3,84,84);Style(text,25,Gold);}
   foreach(var component in root.GetComponentsInChildren<Component>(true))if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
   EditorUtility.SetDirty(map);EditorUtility.SetDirty(hud);EditorUtility.SetDirty(view.session);
  }
  static LevelNodeView CreateCard(){
   var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/RouteSelectionCard.prefab");if(existing!=null)return existing.GetComponent<LevelNodeView>();
   var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/DungeonCard.prefab"));go.name="RouteSelectionCard";var node=go.GetComponent<LevelNodeView>();Place(go.transform,0,0,278,400);((RectTransform)go.transform).pivot=new Vector2(.5f,1);
   Place(node.label.transform,18,204,242,54);Style(node.label,30,Cream);Place(node.stateLabel.transform,18,322,242,42);Style(node.stateLabel,23,Gold);node.banner.pointDepth=0;node.banner.borderWidth=2;
   var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/RouteSelectionCard.prefab");UnityEngine.Object.DestroyImmediate(go);return prefab.GetComponent<LevelNodeView>();
  }
  static RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Outline));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;var outline=go.GetComponent<Outline>();outline.effectColor=new Color(Gold.r,Gold.g,Gold.b,.4f);outline.effectDistance=new Vector2(1,-1);return (RectTransform)go.transform;}
  static Text Label(string name,Transform parent,float x,float y,float w,float h,string caption,int size,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);var text=go.GetComponent<Text>();text.text=caption;Style(text,size,color);return text;}
  static Button Click(string name,Transform parent,float x,float y,float w,float h,string caption){var panel=Panel(name,parent,x,y,w,h,Tile);var button=panel.gameObject.AddComponent<Button>();button.targetGraphic=panel.GetComponent<Image>();button.targetGraphic.raycastTarget=true;Label("Label",panel,2,0,w-4,h,caption,18,Cream);return button;}
  static void Style(Text text,int size,Color color){text.font=font;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=size;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
  static void Place(Transform target,float x,float y,float w,float h){var rect=(RectTransform)target;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;}
 }
}
