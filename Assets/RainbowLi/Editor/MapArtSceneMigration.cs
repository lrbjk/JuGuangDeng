using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class MapArtSceneMigration {
  const string Root="Assets/RainbowLi",Art=Root+"/Art/选关/";
  static readonly Color Cyan=new Color(.48f,.79f,.8f),Rose=new Color(.78f,.72f,.72f);
  static Sprite Sprite(string name)=>AssetDatabase.LoadAllAssetsAtPath(Art+name+".png").OfType<Sprite>().First();
  static void Place(Component c,Transform parent,float x,float y,float w,float h){var r=(RectTransform)c.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;}
  static void Clear(Transform root){var image=root.GetComponent<Image>();if(image!=null){image.color=Color.clear;image.raycastTarget=false;}foreach(var o in root.GetComponents<Outline>())o.enabled=false;}
  static Image Image(string name,Transform parent,float x,float y,float w,float h,Sprite sprite){var t=parent.Find(name);var go=t==null?new GameObject(name,typeof(RectTransform),typeof(Image)):t.gameObject;var image=go.GetComponent<Image>()??go.AddComponent<Image>();Place(image,parent,x,y,w,h);image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;return image;}
  static void Style(Text t,int size,Color color){t.font=SourceHanFontMigration.Body;t.fontSize=size;t.color=color;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=10;t.resizeTextMaxSize=size;t.alignment=TextAnchor.MiddleCenter;}
  static void Import(){foreach(string name in new[]{"背景2","boss","boss未解锁","进度条","进度条格子","打勾","选关ui 1"}){var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name+".png");importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
   if(name=="选关ui 1"){importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritesheet=new[]{new SpriteMetaData{name="RouteCard",rect=new Rect(769,120,212,286),alignment=9,pivot=Vector2.one*.5f}};}
   importer.SaveAndReimport();}}
  static void Card(LevelNodeView node){var art=Image("RouteCardArtwork",node.transform,0,0,212,286,Sprite("选关ui 1"));art.transform.SetAsFirstSibling();node.cardArtwork=art;node.banner.color=Color.clear;node.banner.borderColor=Color.clear;node.banner.pointDepth=0;node.button.transition=Selectable.Transition.None;node.button.targetGraphic=node.banner;
   if(node.face!=null&&(Graphic)node.face!=(Graphic)node.banner)node.face.color=Color.clear;
   Place(node.label,node.transform,12,232,188,40);Style(node.label,22,new Color(.06f,.08f,.09f));node.stateLabel.gameObject.SetActive(false);
   foreach(var g in new Graphic[]{node.icon,node.customIcon}){var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(162,-24);r.sizeDelta=Vector2.one*32;r.localScale=Vector3.one;g.raycastTarget=false;}
   foreach(var o in node.GetComponentsInChildren<Outline>(true))o.enabled=false;
  }
  static void Sequences(AdventureMapHudView hud,Transform root){var group=(root.Find("AdventureSidebar/PlayerSequenceGroup")??root.Find("PlayerSequenceGroup"));Place(group,root,98,822,304,210);Clear(group);var heading=group.Find("Heading");if(heading!=null)heading.gameObject.SetActive(false);
   for(int i=0;i<hud.playerSequences.Length;i++){var row=hud.playerSequences[i];Place(row,group,0,i*70,304,66);var img=row.GetComponent<Image>();img.sprite=AssetDatabase.LoadAllAssetsAtPath(Root+"/Art/GJ 战斗ui分解/玩家牌序框.png").OfType<Sprite>().First();img.color=Color.white;foreach(var o in row.GetComponents<Outline>())o.enabled=false;
    Place(row.title,row.transform,6,0,290,12);Style(row.title,9,new Color(.05f,.1f,.11f));Place(row.description,row.transform,141,6,156,53);Style(row.description,17,new Color(.05f,.1f,.11f));
    for(int d=0;d<3;d++){Place(row.digits[d],row.transform,10+d*40,22,35,35);row.digits[d].GetComponent<Image>().sprite=AssetDatabase.LoadAllAssetsAtPath(Root+"/Art/GJ 战斗ui分解/牌序-小格子.png").OfType<Sprite>().First();row.digits[d].GetComponent<Image>().color=Color.white;Place(row.numbers[d],row.digits[d].transform,0,0,35,35);Style(row.numbers[d],25,new Color(.05f,.1f,.11f));}
    foreach(var t in row.GetComponentsInChildren<Text>(true))if(t.name.StartsWith("SequenceArrow_"))t.gameObject.SetActive(false);row.primaryEffect.gameObject.SetActive(false);row.secondaryEffect.gameObject.SetActive(false);
   }
  }
  static void Apply(){var view=UnityEngine.Object.FindFirstObjectByType<GameView>();var map=view.GetComponent<RouteMapView>();var hud=view.GetComponent<AdventureMapHudView>();var root=map.mapRoot.transform;
   Clear(root);var background=Image("MapArtBackdrop",root,0,0,1920,1080,Sprite("背景2"));background.transform.SetAsFirstSibling();
   var sidebar=root.Find("AdventureSidebar");Clear(sidebar);Place(sidebar,root,0,0,440,1080);
   var stats=sidebar.Find("ProgressGroup");Clear(stats);Place(stats,sidebar,0,0,440,365);Clear(stats.Find("CompletedTile"));Clear(stats.Find("TimerTile"));
   // Background contains static headings; only values are drawn over their blank spaces.
   Place(hud.completedRooms,stats,292,120,100,56);Style(hud.completedRooms,32,Cyan);
   Place(hud.elapsedTime,stats,246,195,146,64);Style(hud.elapsedTime,23,Cyan);
   
   int j=0;foreach(string name in new[]{"NewRun","Save","Continue"}){var b=stats.Find(name);Place(b,stats,104+j*98,290,92,36);var image=b.GetComponent<Image>();image.color=new Color(.04f,.13f,.15f,.85f);foreach(var o in b.GetComponents<Outline>())o.enabled=false;Style(b.GetComponentInChildren<Text>(),17,Cyan);j++;}
   var growth=sidebar.Find("EnemyGrowthGroup");Clear(growth);Place(growth,sidebar,0,0,440,650);Clear(growth.Find("CountTile"));Place(hud.growthCount,growth,270,411,50,53);Style(hud.growthCount,27,Rose);Place(hud.growthDescription,growth,120,474,270,146);Style(hud.growthDescription,18,Rose);hud.growthDescription.alignment=TextAnchor.UpperLeft;
   var resources=sidebar.Find("Resources");Clear(resources);Place(resources,sidebar,0,0,440,750);Place(map.resources,resources,150,686,98,48);Style(map.resources,26,new Color(.76f,.67f,.52f));
   var deck=view.battleHud.mapDeckCount.transform.parent;Place(deck,resources,325,686,78,48);Clear(deck);deck.GetComponent<Image>().raycastTarget=true;Place(view.battleHud.mapDeckCount,deck,0,0,78,48);Style(view.battleHud.mapDeckCount,26,new Color(.76f,.67f,.52f));hud.illustratedCoins=map.resources;hud.illustratedDeck=view.battleHud.mapDeckCount;Sequences(hud,root);
   var boss=hud.bossButton.transform;Place(boss,root,908,104,588,237);Clear(boss);foreach(Transform c in boss)c.gameObject.SetActive(false);hud.bossArtwork=boss.GetComponent<Image>();hud.bossArtwork.raycastTarget=true;hud.bossLockedSprite=Sprite("boss未解锁");hud.bossUnlockedSprite=Sprite("boss");hud.bossArtwork.sprite=hud.bossLockedSprite;hud.bossArtwork.color=Color.white;hud.bossButton.transition=Selectable.Transition.None;
   var strip=root.Find("BattleProgress");Place(strip,root,785,186,823,94);Clear(strip);strip.GetComponent<Image>().sprite=Sprite("进度条");strip.GetComponent<Image>().color=Color.white;hud.markerStrip=(RectTransform)strip;hud.markerSprite=Sprite("进度条格子");hud.checkSprite=Sprite("打勾");hud.illustrated=true;
   foreach(var image in hud.battleMarkers)foreach(var o in image.GetComponents<Outline>())o.enabled=false;
   Place(map.scroll,root,650,358,1100,626);Place(map.scroll.viewport,map.scroll.transform,0,0,1100,626);Place(map.content,map.scroll.viewport,0,0,1100,626);map.contentWidth=1100;map.diagramCardWidth=212;map.diagramCardHeight=286;map.diagramStripHeight=61;map.diagramColumnGap=110;map.diagramBackCards=5;map.scroll.horizontal=map.scroll.vertical=false;
   Clear(map.scroll.transform);Clear(map.scroll.viewport);map.legend.gameObject.SetActive(false);
   var prefab=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/RouteSelectionCard.prefab");try{Card(prefab.GetComponent<LevelNodeView>());PrefabUtility.SaveAsPrefabAsset(prefab,Root+"/Prefabs/RouteSelectionCard.prefab");}finally{PrefabUtility.UnloadPrefabContents(prefab);}map.nodePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/RouteSelectionCard.prefab").GetComponent<LevelNodeView>();
   foreach(var node in map.nodes){Card(node);var graph=map.levels.definition.CreateGraph(104729);var n=graph.Find(node.nodeId);if(n==null)continue;node.map=map;node.Bind(n,new RouteRun(graph),()=>{});Place(node,map.content,550+(n.lane-1)*322,8+61*(5-Mathf.Min(5,n.floor)),212,286);((RectTransform)node.transform).pivot=new Vector2(.5f,1);node.SetStackDepth(n.floor);node.gameObject.SetActive(n.kind!=LevelKind.Boss&&n.floor<=5);}
   Place(map.tooltipPanel.transform,root,1640,380,240,250);map.tooltipPanel.SetActive(false);var settings=root.Find("Settings");if(settings!=null){Place(settings,root,1755,74,100,50);var label=settings.GetComponentInChildren<Text>();Place(label,settings,0,0,100,50);Style(label,22,Cyan);}
   foreach(var c in root.GetComponentsInChildren<Component>(true))if(c!=null){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}EditorUtility.SetDirty(hud);EditorUtility.SetDirty(map);
  }
  [MenuItem("RainbowLi/UI/应用美术选关界面")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/"))throw new InvalidOperationException("请打开 RainbowLi 场景");Import();Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){Import();var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: Map art migration saved.");EditorApplication.Exit(0);}
 }
}

