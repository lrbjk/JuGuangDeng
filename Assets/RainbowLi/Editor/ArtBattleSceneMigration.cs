using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class ArtBattleSceneMigration {
  const string Root="Assets/RainbowLi",Art=Root+"/Art/GJ 战斗ui分解/";
  static readonly Color Ink=new Color(.035f,.09f,.1f),Cream=new Color(.83f,.82f,.68f),Cyan=new Color(.45f,.77f,.78f);
  static Sprite Sprite(string name)=>AssetDatabase.LoadAllAssetsAtPath(Art+name+".png").OfType<Sprite>().First();
  static Sprite Noise=>AssetDatabase.LoadAllAssetsAtPath(Art+"背景2.png").OfType<Sprite>().Single(s=>s.name=="CardNoise");
  static void ImportArt(){
   foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Art.TrimEnd('/')})){
    string path=AssetDatabase.GUIDToAssetPath(guid);if(!path.EndsWith(".png",StringComparison.OrdinalIgnoreCase))continue;
    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Point;importer.maxTextureSize=2048;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
    if(path.EndsWith("/背景2.png",StringComparison.Ordinal)){
     importer.spriteImportMode=SpriteImportMode.Multiple;
     importer.spritesheet=new[]{new SpriteMetaData{name="BattleBackground",rect=new Rect(0,0,1920,1080),alignment=9,pivot=Vector2.one*.5f},new SpriteMetaData{name="CardNoise",rect=new Rect(1050,560,130,195),alignment=9,pivot=Vector2.one*.5f}};
    }
    importer.SaveAndReimport();
   }
  }
  static void Place(Component component,Transform parent,float x,float y,float w,float h){
   if(component==null)return;var r=(RectTransform)component.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;
  }
  static void Clear(Transform root){if(root==null)return;var image=root.GetComponent<Image>();if(image!=null)image.color=Color.clear;foreach(var outline in root.GetComponents<Outline>())outline.enabled=false;}
  static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var found=parent.Find(name);var go=found==null?new GameObject(name,typeof(RectTransform)):found.gameObject;Place(go.transform,parent,x,y,w,h);return (RectTransform)go.transform;}
  static Image Image(string name,Transform parent,float x,float y,float w,float h,Sprite sprite){var r=Rect(name,parent,x,y,w,h);var image=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;return image;}
  static void TextStyle(Text text,int size,Color color){if(text==null)return;text.font=SourceHanFontMigration.Body;text.fontStyle=FontStyle.Normal;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.Min(10,size);text.resizeTextMaxSize=size;text.raycastTarget=false;}
  static void HideHeading(Transform root){var heading=root.Find("Heading");if(heading!=null)heading.gameObject.SetActive(false);}
  static void Record(Component value){EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
  static Material CardMaterial(){string path=Art+"CardPaper.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("RainbowLi/UI/CardPaper"));AssetDatabase.CreateAsset(material,path);}return material;}
  static void Card(CardView card){
   card.illustrated=true;card.illustratedFace=Noise;card.attackIcon=Sprite("行为图标/普通攻击");card.guardIcon=Sprite("护盾");card.healIcon=Sprite("行为图标/治疗");card.otherIcon=Sprite("行为图标/强化");card.playerFace=new Color(.6f,.84f,.88f);card.enemyFace=new Color(.68f,.48f,.49f);card.illustratedInk=Ink;
   Place(card.face,card.transform,0,0,130,195);card.face.sprite=Noise;card.face.color=card.playerFace;card.face.material=CardMaterial();
   var border=card.face.GetComponent<Outline>()??card.face.gameObject.AddComponent<Outline>();border.effectColor=new Color(.01f,.025f,.03f);border.effectDistance=new Vector2(3,-3);
   Place(card.topRank,card.transform,6,4,32,34);TextStyle(card.topRank,25,Ink);Place(card.bottomRank,card.transform,91,118,32,34);TextStyle(card.bottomRank,25,Ink);card.bottomRank.rectTransform.pivot=Vector2.one*.5f;card.bottomRank.rectTransform.anchoredPosition=new Vector2(109,-136);card.bottomRank.rectTransform.localRotation=Quaternion.Euler(0,0,180);
   Place(card.topSuit,card.transform,99,8,23,23);card.topSuit.sprite=card.otherIcon;card.topSuit.color=Color.white;card.bottomSuit.gameObject.SetActive(false);foreach(var pip in card.pips)pip.gameObject.SetActive(false);
   var panel=Image("CaptionPlate",card.transform,6,156,118,33,Sprite("玩家牌序框"));panel.color=new Color(.58f,.82f,.86f);panel.transform.SetSiblingIndex(card.face.transform.GetSiblingIndex()+1);
   Place(card.caption,card.transform,10,158,110,29);TextStyle(card.caption,15,Ink);card.caption.transform.SetAsLastSibling();
   Place(card.engravingBadge,card.transform,111,-7,15,15);card.engravingBadge.color=new Color(.72f,.54f,.24f);var skin=AssetDatabase.LoadAssetAtPath<CardSkinCatalog>(Root+"/Data/DefaultCardSkins.asset");if(skin!=null)CardSkinSceneMigration.Configure(card,skin);Record(card);foreach(var r in card.GetComponentsInChildren<Component>(true))if(r!=null)Record(r);
  }
  static void Sequence(SequenceView row,Transform parent,int index,bool enemy){foreach(var arrow in row.GetComponentsInChildren<Text>(true))if(arrow.name.StartsWith("SequenceArrow_",StringComparison.Ordinal))arrow.gameObject.SetActive(false);
   Place(row,parent,0,index*(enemy?58:72),304,enemy?54:66);var image=row.GetComponent<Image>();if(image!=null){image.sprite=Sprite(enemy?"敌人牌序框":"玩家牌序框");image.color=Color.white;}foreach(var outline in row.GetComponents<Outline>())outline.enabled=false;
   Place(row.title,row.transform,6,0,290,12);TextStyle(row.title,9,Ink);
   Place(row.description,row.transform,141,6,156,enemy?43:53);TextStyle(row.description,17,Ink);
   for(int d=0;d<3;d++){Place(row.digits[d],row.transform,10+d*40,enemy?16:22,35,35);var digitImage=row.digits[d].GetComponent<Image>();digitImage.sprite=Sprite("牌序-小格子");digitImage.color=Color.white;Place(row.numbers[d],row.digits[d].transform,0,0,35,35);TextStyle(row.numbers[d],25,Ink);}
   if(row.primaryEffect!=null)row.primaryEffect.gameObject.SetActive(false);if(row.secondaryEffect!=null)row.secondaryEffect.gameObject.SetActive(false);Record(row);
  }
  static void HealthSpriteBorders(){foreach(string name in new[]{"敌人血","玩家血"}){var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name+".png");var border=new Vector4(1,0,name=="敌人血"?20:28,0);if(importer.spriteBorder!=border){importer.spriteBorder=border;importer.SaveAndReimport();}}}
  static void Health(Slider bar,Text label,Text shield,Transform parent,bool enemy){
   float x=enemy?957:644,y=enemy?466:724,w=enemy?526:734,h=enemy?29:37;
   Place(bar,parent,x,y,w,h);
   // Replace inherited slider visuals; old FillArea/handle geometry must not distort the art.
   foreach(var old in bar.GetComponentsInChildren<Image>(true))old.enabled=false;
   foreach(var outline in bar.GetComponentsInChildren<Outline>(true))outline.enabled=false;
   var track=Image("ArtTrack",bar.transform,0,0,w,h,Sprite(enemy?"敌人血槽":"玩家血槽"));track.enabled=true;track.transform.SetAsFirstSibling();
   float insetX=enemy?5:10,insetY=enemy?5:7;
   var fillArea=Rect("ArtFillArea",bar.transform,0,0,w,h);fillArea.anchorMin=Vector2.zero;fillArea.anchorMax=Vector2.one;fillArea.pivot=Vector2.one*.5f;fillArea.offsetMin=new Vector2(insetX,insetY);fillArea.offsetMax=new Vector2(-insetX,-insetY);
   var fill=Image("ArtFill",fillArea,0,0,w-insetX*2,h-insetY*2,Sprite(enemy?"敌人血":"玩家血"));fill.enabled=true;fill.type=UnityEngine.UI.Image.Type.Sliced;
   bar.transition=Selectable.Transition.None;bar.fillRect=fill.rectTransform;bar.handleRect=null;bar.targetGraphic=track;bar.interactable=false;fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.pivot=Vector2.one*.5f;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;bar.SetValueWithoutNotify(bar.value);track.canvasRenderer.SetColor(Color.white);
   Place(label,parent,x,y,w,h);TextStyle(label,16,Cream);
   var badge=shield.transform.parent;Place(badge,parent,enemy?925:612,enemy?455:715,enemy?43:57,enemy?47:62);var image=badge.GetComponent<Image>();if(image!=null){image.sprite=Sprite("护盾");image.color=Color.white;}foreach(var o in badge.GetComponents<Outline>())o.enabled=false;Place(shield,badge,0,10,enemy?43:57,enemy?30:37);TextStyle(shield,17,Cream);
  }
  static void Status(BattleStatusPanelView panel,Transform parent,bool enemy){
   Place(panel,parent,enemy?1518:1405,enemy?158:717,enemy?58:130,enemy?270:58);Clear(panel.transform);HideHeading(panel.transform);panel.iconMode=true;panel.singleRow=!enemy;panel.iconSize=46;panel.iconGap=6;panel.fallbackIcon=Sprite("行为图标/强化");
   panel.statusIcons=new[]{new BattleStatusPanelView.StatusIcon{effect=CardEffect.Strength,icon=Sprite("行为图标/伤害强化")},new BattleStatusPanelView.StatusIcon{effect=CardEffect.Poison,icon=Sprite("行为图标/中毒")},new BattleStatusPanelView.StatusIcon{effect=CardEffect.Weaken,icon=Sprite("行为图标/盲目")},new BattleStatusPanelView.StatusIcon{effect=CardEffect.Bleed,icon=Sprite("行为图标/撕扯")},new BattleStatusPanelView.StatusIcon{effect=CardEffect.Regeneration,icon=Sprite("行为图标/治疗")},new BattleStatusPanelView.StatusIcon{effect=CardEffect.Fortify,icon=Sprite("行为图标/强化")}};
   var scroll=panel.GetComponent<ScrollRect>();if(scroll!=null){scroll.horizontal=!enemy;scroll.vertical=enemy;Place(scroll.viewport,panel.transform,0,0,enemy?58:130,enemy?270:58);Clear(scroll.viewport);var hit=scroll.viewport.GetComponent<Graphic>();if(hit!=null)hit.raycastTarget=false;}
   var background=panel.GetComponent<Graphic>();if(background!=null)background.raycastTarget=false;
   Place(panel.content,panel.content.parent,0,0,enemy?58:130,enemy?270:58);Clear(panel.chipTemplate.transform);panel.chipTemplate.gameObject.SetActive(false);panel.emptyLabel.gameObject.SetActive(false);if(!enemy)PlayerBuffStripSceneMigration.Configure(panel);Record(panel);
  }
  [MenuItem("RainbowLi/UI/应用美术战斗界面")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");ImportArt();Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){
   ImportArt();var prefab=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/PokerCard.prefab");try{Card(prefab.GetComponent<CardView>());PrefabUtility.SaveAsPrefabAsset(prefab,Root+"/Prefabs/PokerCard.prefab");}finally{PrefabUtility.UnloadPrefabContents(prefab);}
   var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: Art battle scene migration saved.");EditorApplication.Exit(0);
  }
  public static void Refine(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("PASS: Refined health bars and battle card layout saved.");EditorApplication.Exit(0);}
  static Button EndButton(Transform parent,string name,string label,float x){
   var image=Image(name,parent,x,620,280,60,null);image.color=new Color(.07f,.2f,.22f);image.raycastTarget=true;
   var button=image.GetComponent<Button>()??image.gameObject.AddComponent<Button>();button.targetGraphic=image;
   var rect=Rect("Label",button.transform,0,0,280,60);var text=rect.GetComponent<Text>()??rect.gameObject.AddComponent<Text>();text.text=label;TextStyle(text,26,Cream);return button;
  }
  static void EndScreen(GameView view,Transform table){
   Place(view.endPanel.transform,table,0,0,1920,1080);var image=view.endPanel.GetComponent<Image>()??view.endPanel.AddComponent<Image>();image.color=new Color(.025f,.065f,.075f,1);image.raycastTarget=true;
   foreach(var outline in view.endPanel.GetComponents<Outline>())outline.enabled=false;
   Place(view.endTitle,view.endPanel.transform,460,330,1000,90);TextStyle(view.endTitle,44,Cream);
   Place(view.endSummary,view.endPanel.transform,560,450,800,120);TextStyle(view.endSummary,24,Cyan);
   view.endView.restart=EndButton(view.endPanel.transform,"RestartRun","再来一局",650);view.endView.mainMenu=EndButton(view.endPanel.transform,"ReturnToMenu","返回主菜单",990);EndScreenCopySceneMigration.Configure(view);Record(view.endView);
  }
  static Image RoomSurface(string name,Transform parent,float x,float y,float w,float h){
   var image=Image(name,parent,x,y,w,h,null);image.color=new Color(.04f,.105f,.115f);var outline=image.GetComponent<Outline>()??image.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.24f,.46f,.46f);outline.effectDistance=new Vector2(1,-1);return image;
  }
  static Text RoomLabel(string name,Transform parent,float x,float y,float w,float h,int size){var rect=Rect(name,parent,x,y,w,h);var text=rect.GetComponent<Text>()??rect.gameObject.AddComponent<Text>();TextStyle(text,size,Cream);return text;}
  static void RoomButton(Button button,Transform parent,float x,float y,float w,float h){Place(button,parent,x,y,w,h);var image=button.GetComponent<Image>();if(image!=null){image.sprite=null;image.color=new Color(.07f,.2f,.22f);image.raycastTarget=true;}foreach(var o in button.GetComponents<Outline>())o.enabled=false;var text=button.GetComponentInChildren<Text>(true);Place(text,button.transform,8,4,w-16,h-8);TextStyle(text,24,Cream);}
  static void NonCombatRooms(GameView view,Transform table){
   var room=view.GetComponent<NonCombatRoomView>()??view.gameObject.AddComponent<NonCombatRoomView>();room.view=view;
   var bg=Image("NonCombatBackdrop",table,0,0,1920,1080,null);bg.color=new Color(.02f,.055f,.065f);bg.transform.SetAsFirstSibling();room.backdrop=bg.gameObject;
   var sidebar=RoomSurface("RoomSidebar",bg.transform,30,40,390,1000).transform;
   room.roomTitle=RoomLabel("RoomTitle",sidebar,24,24,342,76,34);
   room.progress=RoomLabel("Progress",sidebar,24,124,342,100,24);
   room.elapsed=RoomLabel("Elapsed",sidebar,24,232,342,56,22);
   room.health=RoomLabel("Health",sidebar,24,310,342,56,26);
   room.resources=RoomLabel("Resources",sidebar,24,374,342,56,24);
   room.deck=EndButton(sidebar,"ViewDeck","查看当前牌组",40);Place(room.deck,sidebar,40,450,310,60);
   RoomLabel("SequenceHeading",sidebar,24,540,342,46,26).text="玩家牌序";room.sequenceSummaries=new Text[3];
   for(int i=0;i<3;i++)room.sequenceSummaries[i]=RoomLabel("Sequence_"+i,sidebar,24,598+i*100,342,90,20);
   room.settings=EndButton(sidebar,"RoomSettings","设置",40);Place(room.settings,sidebar,40,914,310,54);
   var sp=view.servicePanel.transform;Place(sp,table,460,80,1400,920);var surface=view.servicePanel.GetComponent<Image>();surface.color=new Color(.035f,.09f,.1f);surface.raycastTarget=false;
   Place(view.serviceTitle,sp,40,28,980,72);TextStyle(view.serviceTitle,34,Cream);RoomButton(sp.Find("BackToMap").GetComponent<Button>(),sp,1100,38,250,52);
   RoomButton(view.heal,sp,40,170,630,78);RoomButton(view.generate,sp,720,170,630,78);
   for(int i=0;i<view.buyNumberButtons.Length;i++)RoomButton(view.buyNumberButtons[i],sp,40+i%3*435,170+i/3*96,405,72);
   for(int i=0;i<view.rebuildButtons.Length;i++)RoomButton(view.rebuildButtons[i],sp,40+i*435,390,405,64);
   var entries=sp.Find("DeckOperationEntries");Place(entries,sp,40,510,1320,300);var entriesImage=entries.GetComponent<Image>();if(entriesImage!=null)entriesImage.color=new Color(.04f,.105f,.115f);
   RoomButton(view.serviceView.upgradeDeck,entries,30,45,600,80);RoomButton(view.serviceView.engraveDeck,entries,690,45,600,80);RoomButton(view.serviceView.removeDeck,entries,30,45,600,80);
   var hint=entries.Find("Hint");if(hint!=null){Place(hint,entries,30,165,1260,90);TextStyle(hint.GetComponent<Text>(),23,Cyan);}
   var eventPanel=view.GetComponent<RouteMapView>().eventPanel;Place(eventPanel.transform,table,460,80,1400,920);var eventImage=eventPanel.GetComponent<Image>();eventImage.color=new Color(.035f,.09f,.1f);eventImage.raycastTarget=false;
   var title=eventPanel.transform.Find("EventTitle").GetComponent<Text>();Place(title,eventPanel.transform,40,40,1320,90);TextStyle(title,34,Cream);
   var body=eventPanel.transform.Find("EventBody").GetComponent<Text>();Place(body,eventPanel.transform,130,200,1140,200);TextStyle(body,26,Cyan);
   RoomButton(eventPanel.transform.Find("EventGold").GetComponent<Button>(),eventPanel.transform,100,480,560,100);RoomButton(eventPanel.transform.Find("EventHeal").GetComponent<Button>(),eventPanel.transform,740,480,560,100);
   Record(room);
  }
  static void Apply(){HealthSpriteBorders();
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();if(view==null)throw new InvalidOperationException("缺少 GameView");var hud=view.battleHud;var table=view.transform.Find("Table");var bp=view.battlePanel.transform;var sidebar=table.Find("Sidebar");
   var art=view.GetComponent<BattleArtView>()??view.gameObject.AddComponent<BattleArtView>();art.view=view;art.backdrop=Image("ArtBattleBackdrop",table,0,0,1920,1080,AssetDatabase.LoadAllAssetsAtPath(Art+"背景2.png").OfType<Sprite>().Single(s=>s.name=="BattleBackground")).gameObject;art.backdrop.transform.SetAsFirstSibling();
   art.sidebar=sidebar.gameObject;EndScreen(view,table);NonCombatRooms(view,table);
   Place(bp,table,0,0,1920,1080);Clear(bp);Place(sidebar,table,0,0,440,1080);Clear(sidebar);hud.artLayout=true;
   foreach(var path in new[]{"EnemySkillGroup","EnemySequenceGroup","PlayerSequenceGroup","Resources"}){var group=sidebar.Find(path);Clear(group);if(group!=null)HideHeading(group);}
   var skills=hud.enemySkills[0].transform.parent;Place(skills,sidebar,101,143,304,210);Clear(skills);HideHeading(skills);
   var skillViewport=Rect("SkillViewport",skills,0,0,304,192);var clip=skillViewport.GetComponent<RectMask2D>()??skillViewport.gameObject.AddComponent<RectMask2D>();var skillContent=Rect("Content",skillViewport,0,0,304,204);var skillScroll=skillViewport.GetComponent<ScrollRect>()??skillViewport.gameObject.AddComponent<ScrollRect>();skillScroll.viewport=skillViewport;skillScroll.content=skillContent;skillScroll.horizontal=false;skillScroll.vertical=true;skillScroll.movementType=ScrollRect.MovementType.Clamped;skillScroll.scrollSensitivity=40;var touch=skillViewport.GetComponent<Image>()??skillViewport.gameObject.AddComponent<Image>();touch.color=Color.clear;
   hud.enemySkillContent=skillContent;hud.skillColumns=4;hud.skillOrigin=new Vector2(1,-1);hud.skillSpacing=new Vector2(74,103);hud.skillScale=.46f;
   for(int i=0;i<hud.enemySkills.Length;i++){Place(hud.enemySkills[i],skillContent,1+i%4*74,1+i/4*103,130,225);hud.enemySkills[i].transform.localScale=Vector3.one*.46f;}
   Image("SkillScrollArrow",sidebar,238,335,24,13,Sprite("敌人技能牌-箭头"));
   var enemyRules=sidebar.Find("EnemySequenceGroup");Place(enemyRules,sidebar,96,446,304,180);for(int i=0;i<hud.enemySequenceViews.Length;i++)Sequence(hud.enemySequenceViews[i],enemyRules,i,true);
   var playerRules=sidebar.Find("PlayerSequenceGroup");Place(playerRules,sidebar,96,824,304,220);for(int i=0;i<view.ruleViews.Length;i++)Sequence(view.ruleViews[i],playerRules,i,false);
   Place(hud.coins,sidebar,162,684,97,51);TextStyle(hud.coins,28,Cream);var deckButton=hud.deckCount.GetComponentInParent<Button>(true);if(deckButton!=null){Place(deckButton,sidebar,267,680,140,65);Clear(deckButton.transform);Place(hud.deckCount,deckButton.transform,68,4,66,56);}else Place(hud.deckCount,sidebar,337,684,64,51);TextStyle(hud.deckCount,28,Cream);
   var restart=sidebar.Find("NewGame");if(restart!=null)restart.gameObject.SetActive(false);view.roundStats.gameObject.SetActive(false);
   var arena=hud.arenaSequenceView.transform.parent;Place(arena,bp,584,80,335,145);var arenaImage=arena.GetComponent<Image>()??arena.gameObject.AddComponent<Image>();arenaImage.sprite=Sprite("场地牌序");arenaImage.color=Color.white;foreach(var outline in arena.GetComponents<Outline>())outline.enabled=false;HideHeading(arena);
   var row=hud.arenaSequenceView;Place(row,arena,20,47,295,72);Clear(row.transform);row.title.gameObject.SetActive(false);Place(row.description,row.transform,0,42,295,30);TextStyle(row.description,16,Cyan);
   for(int d=0;d<3;d++){Place(row.digits[d],row.transform,48+d*72,0,45,40);Clear(row.digits[d].transform);Place(row.numbers[d],row.digits[d].transform,0,0,45,40);TextStyle(row.numbers[d],32,Cyan);if(d<2){var go=Rect("Arrow_"+d,row.transform,93+d*72,0,27,40);var text=go.GetComponent<Text>()??go.gameObject.AddComponent<Text>();text.text="→";TextStyle(text,28,Cyan);}}
   var hint=arena.Find("SharedTriggerHint");if(hint!=null)hint.gameObject.SetActive(false);if(row.primaryEffect!=null)row.primaryEffect.gameObject.SetActive(false);if(row.secondaryEffect!=null)row.secondaryEffect.gameObject.SetActive(false);
   var portrait=hud.enemyPortrait.transform.parent;Place(portrait,bp,980,57,450,404);Clear(portrait);Place(hud.enemyPortrait,portrait,0,0,450,404);Place(hud.enemyName,portrait,0,0,450,25);TextStyle(hud.enemyName,17,Cyan);Place(hud.portraitPlaceholder,portrait,0,145,450,90);TextStyle(hud.portraitPlaceholder,24,Cyan);hud.enemyPortrait.color=Color.white;
   Health(view.enemyHealth,view.enemyStats,hud.enemyShield,bp,true);Health(view.playerHealth,view.playerStats,hud.playerShield,bp,false);Status(hud.enemyStatuses,bp,true);Status(hud.playerStatuses,bp,false);
   var actionScroll=hud.actionScroll;Place(actionScroll,bp,568,539,1255,160);Clear(actionScroll.transform);Place(actionScroll.viewport,actionScroll.transform,0,0,1255,160);Clear(actionScroll.viewport);Place(hud.actionArea,actionScroll.viewport,0,0,1255,160);
   var poker=hud.pokerLayout;poker.minimumSlots=12;poker.cardScale=.71f;poker.slotGap=11.7f;poker.cardTop=11;poker.horizontalPadding=8;poker.illustratedSlots=true;poker.slotArtwork=Sprite("单个卡槽");poker.surfaceArtwork=Sprite("行动条");
   if(poker.surface!=null){poker.surface.transform.SetParent(actionScroll.transform,false);var r=poker.surface.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;poker.surface.transform.SetAsFirstSibling();}
   Place(hud.handScroll,bp,581,855,1015,225);Clear(hud.handScroll.transform);Place(hud.handScroll.viewport,hud.handScroll.transform,0,0,1015,225);Clear(hud.handScroll.viewport);Place(hud.handArea,hud.handScroll.viewport,0,0,1015,225);hud.handScale=.94f;hud.cardGap=6;HideHeading(hud.handArea);Clear(hud.handArea);var handHeader=hud.handScroll.transform.parent.Find("Heading");if(handHeader!=null&&handHeader!=bp)handHeader.gameObject.SetActive(false);
   foreach(var text in bp.GetComponentsInChildren<Text>(true))if(new[]{"ActionHeading","HandHeading","ActionHelp","HandHelp","Tutorial"}.Contains(text.name))text.gameObject.SetActive(false);
   var draw=hud.drawCount.GetComponentInParent<Button>(true);Place(draw,bp,1620,875,149,96);draw.GetComponent<Image>().sprite=Sprite("抽牌堆");draw.GetComponent<Image>().color=Color.white;foreach(var o in draw.GetComponents<Outline>())o.enabled=false;Place(hud.drawCount,draw.transform,116,0,32,26);TextStyle(hud.drawCount,17,Cream);
   var discard=hud.discardCount.GetComponentInParent<Button>(true);Place(discard,bp,1625,994,139,60);discard.GetComponent<Image>().sprite=Sprite("弃牌堆");discard.GetComponent<Image>().color=Color.white;foreach(var o in discard.GetComponents<Outline>())o.enabled=false;Place(hud.discardCount,discard.transform,110,0,29,24);TextStyle(hud.discardCount,17,Cream);
   var settings=table.Find("Settings").GetComponent<Button>();art.battleSettings=settings.gameObject;Place(settings,table,1752,126,95,84);settings.GetComponent<Image>().sprite=Sprite("设置按钮");settings.GetComponent<Image>().color=Color.white;settings.GetComponentInChildren<Text>(true).text="";foreach(var o in settings.GetComponents<Outline>())o.enabled=false;
   // Keep all playback controls in the free right-hand area, clear of both health bars and card slots.
   Place(view.confirm,bp,1590,434,230,36);Place(view.pause,bp,1590,434,110,36);Place(view.step,bp,1710,434,110,36);
   var playback=view.battlePanel.GetComponentInChildren<BattlePlaybackView>(true);
   var controls=new System.Collections.Generic.List<Button>{view.confirm,view.pause,view.step};
   if(playback!=null){
    Place(playback.speed,bp,1590,390,110,36);Place(playback.skip,bp,1710,390,110,36);controls.Add(playback.speed);controls.Add(playback.skip);
    Place(playback.stateLabel,bp,1590,478,230,42);TextStyle(playback.stateLabel,16,Cyan);Record(playback);
   }
   foreach(var button in controls){button.GetComponent<Image>().color=new Color(.04f,.14f,.17f,.85f);foreach(var outline in button.GetComponents<Outline>())outline.enabled=false;var text=button.GetComponentInChildren<Text>(true);Place(text,button.transform,2,1,((RectTransform)button.transform).rect.width-4,34);TextStyle(text,18,Cyan);}view.confirm.GetComponentInChildren<Text>(true).text="开始执行";
   view.forecast.gameObject.SetActive(false);view.feedback.gameObject.SetActive(false);if(view.statusSummary!=null)view.statusSummary.gameObject.SetActive(false);
   foreach(var card in view.GetComponentsInChildren<CardView>(true))Card(card);
   hud.LayoutCardsPreview();art.Refresh();Record(view);Record(hud);Record(poker);Record(art);foreach(var r in view.GetComponentsInChildren<Component>(true))if(r!=null)Record(r);
  }
 }
}









