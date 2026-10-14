using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class BuildEditableGame {
 const string Root="Assets/RainbowLi";
 static Font font;static Sprite[] suits;static Sprite faceSprite;
 static Color cream=new Color(.98f,.95f,.85f),green=new Color(.055f,.24f,.16f),dark=new Color(.12f,.18f,.15f),blue=new Color(.13f,.43f,.68f),gold=new Color(.92f,.75f,.4f);
 [MenuItem("RainbowLi/创建可编辑游戏场景（新场景）")]
 public static void Build(){
  if(!Application.isBatchMode){
   if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   if(File.Exists(Root+"/Scenes/RainbowLiGame.unity")&&!EditorUtility.DisplayDialog("重新生成场景", "这会覆盖 RainbowLiGame 的场景布局和默认配置。日常编辑请直接修改场景、Prefab 与 Data 资产。", "重新生成", "取消"))return;
  }
  foreach(string dir in new[]{"Data/Cards","Data/Sequences","Prefabs","Art","Scenes"})Directory.CreateDirectory(Root+"/"+dir);
  AssetDatabase.Refresh();font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/SourceHanSansCN-Bold.otf");MakeSprites();
  var engraving=Asset<EngravingDefinition>("Data/DefaultEngraving.asset");
  var definitions=new System.Collections.Generic.List<CardDefinition>();
  foreach(CardEffect effect in Enum.GetValues(typeof(CardEffect)))for(int n=1;n<=6;n++){
   var def=Asset<CardDefinition>("Data/Cards/"+effect+n+".asset");def.number=n;def.effect=effect;def.engraving=engraving;EditorUtility.SetDirty(def);definitions.Add(def);
  }
  var rules=new[]{RuleAsset("PlayerStrike","基础 / 冲击",new[]{1,2,3},RuleEffect.Damage,10),RuleAsset("PlayerShield","基础 / 屏障",new[]{3,4,5},RuleEffect.Shield,10),RuleAsset("PlayerChain","基础 / 连锁",new[]{2,2,2},RuleEffect.Power,2,true)};
  var arenas=new[]{RuleAsset("ArenaStrike","场地 / 冲击",new[]{1,3,5},RuleEffect.Damage,4),RuleAsset("ArenaShield","场地 / 屏障",new[]{2,4,6},RuleEffect.Shield,6),RuleAsset("ArenaPower","场地 / 强化",new[]{3,3,3},RuleEffect.Power,8)};
  var gameDef=Asset<GameDefinition>("Data/DefaultGame.asset");gameDef.initialDeck=gameDef.cardPool=definitions.ToArray();gameDef.playerRules=rules;gameDef.arenaRules=arenas;gameDef.enemyRule=RuleAsset("Enemy","实体 / 共振",new[]{6,5,4},RuleEffect.Damage,6);EditorUtility.SetDirty(gameDef);
  var cardPrefab=MakeCardPrefab();var sequencePrefab=MakeSequencePrefab();var rowPrefab=MakeRowPrefab();var particlePrefab=MakeParticlePrefab();var numberPrefab=MakeNumberPrefab();
  AssetDatabase.SaveAssets();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  // NewScene unloads assets referenced only from the call stack; reload authored assets afterwards.
  cardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PokerCard.prefab").GetComponent<CardView>();
  sequencePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/SequenceCard.prefab").GetComponent<SequenceView>();
  rowPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/DeckRow.prefab").GetComponent<DeckRowView>();
  particlePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ImpactSpark.prefab").GetComponent<Image>();
  numberPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/DamageNumber.prefab").GetComponent<Text>();
  gameDef=AssetDatabase.LoadAssetAtPath<GameDefinition>(Root+"/Data/DefaultGame.asset");
  engraving=AssetDatabase.LoadAssetAtPath<EngravingDefinition>(Root+"/Data/DefaultEngraving.asset");
  rules=gameDef.playerRules;
  var systems=new GameObject("GameSystems");var session=systems.AddComponent<GameSession>();session.definition=gameDef;
  var cards=ChildSystem<CardModule>(systems,"CardSystem");cards.session=session;
  var sequences=ChildSystem<SequenceModule>(systems,"SequenceSystem");sequences.session=session;
  var engravings=ChildSystem<EngravingModule>(systems,"EngravingSystem");engravings.session=session;engravings.definition=engraving;
  var battle=ChildSystem<BattleModule>(systems,"BattleSystem");battle.session=session;
  var progression=ChildSystem<ProgressionModule>(systems,"ProgressionSystem");progression.session=session;
  var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=green;
  var events=new GameObject("EventSystem",typeof(EventSystem));
  var inputType=Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
  if(inputType!=null)events.AddComponent(inputType);else events.AddComponent<StandaloneInputModule>();
  var canvasGO=new GameObject("GameCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
  canvasGO.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
  var scaler=canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
  var table=Rect("Table",canvasGO.transform,0,0,1440,900);table.anchorMin=table.anchorMax=table.pivot=new Vector2(.5f,.5f);table.anchoredPosition=Vector2.zero;Image(table,green);
  var view=canvasGO.AddComponent<GameView>();view.session=session;view.cards=cards;view.battle=battle;view.progression=progression;view.sequences=sequences;view.engravings=engravings;view.deckRowPrefab=rowPrefab;
  var hud=Rect("HUD",table,0,0,302,900);Image(hud,dark);
  Label("Title",hud,20,20,260,60,"魔法封存局",30,gold);
  view.enemyStats=Label("EnemyStats",hud,20,110,260,88,"敌人生命 / 护盾",20,Color.white);
  view.enemyHealth=Health("EnemyHealth",hud,24,210,254,28,new Color(.8f,.22f,.2f));
  view.playerStats=Label("PlayerStats",hud,20,315,260,75,"玩家 70 / 70   护盾 0",20,Color.white);
  view.playerHealth=Health("PlayerHealth",hud,24,402,254,28,new Color(.35f,.85f,.78f));
  view.roundStats=Label("RoundStats",hud,20,455,260,85,"回合 0 / 行动 0\n金币 0 / 牌组 18",20,Color.white);
  view.forecast=Label("Forecast",hud,20,562,260,100,"本回合预计结果",18,Color.white);
  var restart=Button("NewGame",hud,24,735,254,54,"新的一局");UnityEventTools.AddPersistentListener(restart.onClick,session.NewGame);
  Label("Footer",hud,20,815,260,48,"RAINBOWLI\n数字牌序 · 收容战斗",16,Color.white);
  var shared=Rect("SharedRules",table,325,24,1090,235);view.ruleViews=new SequenceView[3];
  for(int i=0;i<3;i++){var r=((GameObject)PrefabUtility.InstantiatePrefab(sequencePrefab.gameObject,shared)).GetComponent<SequenceView>();Place((RectTransform)r.transform,22+i*166,10,152,190);r.name="PlayerRule_"+(i+1);r.Bind(rules[i].CreateRule(),true);view.ruleViews[i]=r;}
  view.arena=Label("ArenaRule",shared,535,16,235,175,"场地牌序 / 双方共享\n末张所属方触发",18,gold);
  view.enemyIntent=Label("EnemySequence",shared,790,16,270,175,"敌方牌序",18,Color.white);
  view.mapPanel=Rect("MapPanel",table,325,270,1090,600).gameObject;
  Label("MapHeading",view.mapPanel.transform,20,15,1050,50,"选择下一个收容任务",28,gold);
  view.normalTask=Button("NormalTask",view.mapPanel.transform,45,130,460,110,"普通收容 / 25 金币 + 新卡牌");UnityEventTools.AddPersistentListener(view.normalTask.onClick,progression.StartNormal);
  view.restTask=Button("RestTask",view.mapPanel.transform,550,130,460,110,"休息 / 恢复、生成、删牌、重构");UnityEventTools.AddPersistentListener(view.restTask.onClick,progression.OpenRest);
  var shop=Button("ShopTask",view.mapPanel.transform,45,300,460,110,"商店 / 升级、铭刻、卡牌");UnityEventTools.AddPersistentListener(shop.onClick,progression.OpenShop);
  view.bossTask=Button("BossTask",view.mapPanel.transform,550,300,460,110,"核心收容 / 需完成两次普通战斗");UnityEventTools.AddPersistentListener(view.bossTask.onClick,progression.StartBoss);
  Label("Tutorial",view.mapPanel.transform,30,465,1040,90,"选择手牌 → 点击 + 插入敌我行动之间 → 从左到右执行\n最近三个数字触发牌序，场地由最后一个数字所属方触发",20,Color.white);
  view.battlePanel=Rect("BattlePanel",table,325,270,1090,610).gameObject;
  var bp=view.battlePanel.transform;Label("BattleHeading",bp,20,0,1050,40,"安排行动 / 从左向右执行",28,gold);
  view.actions=new CardView[7];view.actionLabels=new Text[7];view.leftButtons=new Button[7];view.rightButtons=new Button[7];view.removeButtons=new Button[7];view.insertButtons=new Button[8];
  for(int i=0;i<7;i++){
   float x=36+i*145;view.actionLabels[i]=Label("Actor_"+i,bp,x,50,108,25,(i+1)+" / 敌人",16,Color.white);
   var card=((GameObject)PrefabUtility.InstantiatePrefab(cardPrefab.gameObject,bp)).GetComponent<CardView>();card.name="ActionCard_"+i;Place((RectTransform)card.transform,x,85,130,225);card.transform.localScale=Vector3.one*.82f;card.SetPreview(2,CardEffect.Strike,"伤害 5");view.actions[i]=card;
   view.leftButtons[i]=Button("MoveLeft_"+i,bp,x,288,32,26,"<");view.rightButtons[i]=Button("MoveRight_"+i,bp,x+36,288,32,26,">");view.removeButtons[i]=Button("Remove_"+i,bp,x+72,288,32,26,"×");
  }
  for(int i=0;i<8;i++)view.insertButtons[i]=Button("InsertSlot_"+i,bp,8+i*145,145,24,42,"+");
  view.hand=new CardView[6];
  for(int i=0;i<6;i++){var c=((GameObject)PrefabUtility.InstantiatePrefab(cardPrefab.gameObject,bp)).GetComponent<CardView>();c.name="HandCard_"+i;Place((RectTransform)c.transform,55+i*164,344,130,225);c.SetPreview(i+1,(CardEffect)(i%3),"手牌 / 点击选择");view.hand[i]=c;}
  view.confirm=Button("Confirm",bp,55,575,230,44,"开始执行");UnityEventTools.AddPersistentListener(view.confirm.onClick,battle.Confirm);
  view.pause=Button("Pause",bp,55,575,160,44,"暂停");view.pauseLabel=view.pause.GetComponentInChildren<Text>();UnityEventTools.AddPersistentListener(view.pause.onClick,battle.ToggleAutoPlay);
  view.step=Button("Step",bp,230,575,160,44,"下一步");UnityEventTools.AddPersistentListener(view.step.onClick,battle.Step);
  view.feedback=Label("Feedback",bp,420,565,640,58,"选择手牌 → 点击 + 放置",16,Color.white);
  view.rewardPanel=Rect("RewardPanel",table,340,290,1060,570).gameObject;var rp=view.rewardPanel.transform;Image((RectTransform)rp,dark);
  Label("RewardHeading",rp,20,20,1020,65,"收容成功 / 选择一张生成卡牌",28,gold);view.rewards=new CardView[3];view.rewardButtons=new Button[3];
  for(int i=0;i<3;i++){var c=((GameObject)PrefabUtility.InstantiatePrefab(cardPrefab.gameObject,rp)).GetComponent<CardView>();c.name="RewardCard_"+i;Place((RectTransform)c.transform,106+i*340,130,130,225);c.transform.localScale=Vector3.one*1.2f;c.SetPreview(i+1,CardEffect.Mend,"奖励卡牌");view.rewards[i]=c;view.rewardButtons[i]=Button("TakeReward_"+i,rp,34+i*340,445,300,54,"加入牌组");}
  view.servicePanel=Rect("ServicePanel",table,340,280,1060,595).gameObject;var sp=view.servicePanel.transform;Image((RectTransform)sp,dark);
  view.serviceTitle=Label("ServiceTitle",sp,20,12,770,50,"档案交易 / 构筑牌组",26,gold);
  var back=Button("BackToMap",sp,810,15,228,42,"返回任务选择");UnityEventTools.AddPersistentListener(back.onClick,progression.Leave);
  view.heal=Button("Heal",sp,20,82,490,44,"恢复 24 生命");UnityEventTools.AddPersistentListener(view.heal.onClick,progression.Heal);
  view.generate=Button("Generate",sp,540,82,490,44,"激发 / 生成随机卡牌");UnityEventTools.AddPersistentListener(view.generate.onClick,progression.Generate);
  view.buyNumberButtons=new Button[6];for(int i=0;i<6;i++)view.buyNumberButtons[i]=Button("BuyNumber_"+(i+1),sp,20+i*170,82,155,44,"数字 "+(i+1)+" / 15 金币");
  view.rebuildButtons=new Button[3];for(int i=0;i<3;i++)view.rebuildButtons[i]=Button("RebuildRule_"+i,sp,20+i*340,145,320,42,"重构牌序 "+(i+1));
  var scroll=Rect("DeckScroll",sp,20,210,1018,364);Image(scroll,new Color(.08f,.12f,.1f));var sr=scroll.gameObject.AddComponent<ScrollRect>();sr.horizontal=false;sr.movementType=ScrollRect.MovementType.Clamped;
  var viewport=Rect("Viewport",scroll,0,0,1018,364);viewport.gameObject.AddComponent<RectMask2D>();
  var content=Rect("Content",viewport,0,0,1018,60);var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlHeight=false;layout.childControlWidth=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;layout.spacing=6;
  var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;sr.viewport=viewport;sr.content=content;view.deckContent=content;
  var preview=((GameObject)PrefabUtility.InstantiatePrefab(rowPrefab.gameObject,content)).GetComponent<DeckRowView>();preview.name="EditorPreviewRow";
  view.endPanel=Rect("EndPanel",table,340,300,1060,500).gameObject;var ep=view.endPanel.transform;Image((RectTransform)ep,dark);view.endTitle=Label("EndTitle",ep,30,80,1000,80,"收容完成",32,gold);view.endSummary=Label("EndSummary",ep,30,220,1000,160,"完成收容 / 最终牌组",22,Color.white);
  var overlay=Rect("EffectsOverlay",canvasGO.transform,0,0,1440,900);overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.pivot=new Vector2(.5f,.5f);overlay.sizeDelta=Vector2.zero;overlay.anchoredPosition=Vector2.zero;
  var fx=ChildSystem<BattleEffects>(systems,"BattleEffects");fx.session=session;fx.overlay=overlay;fx.shakeRoot=table;fx.playerTarget=(RectTransform)view.playerHealth.transform;fx.enemyTarget=(RectTransform)view.enemyHealth.transform;fx.playerFill=view.playerHealth.fillRect.GetComponent<Image>();fx.enemyFill=view.enemyHealth.fillRect.GetComponent<Image>();fx.ruleOrigin=shared;fx.actionViews=view.actions;fx.particlePrefab=particlePrefab;fx.numberPrefab=numberPrefab;fx.comboLabel=Label("ComboLabel",overlay,700,258,620,70,"",40,gold);fx.audioSource=fx.gameObject.AddComponent<AudioSource>();fx.audioSource.playOnAwake=false;fx.audioSource.volume=.3f;
  view.battlePanel.SetActive(false);view.rewardPanel.SetActive(false);view.servicePanel.SetActive(false);view.endPanel.SetActive(false);
  ActorSceneMigration.ApplyToSession(session);
  RouteSceneMigration.Apply();ContentSceneMigration.Apply();if(AssetDatabase.LoadAssetAtPath<CardEffectCatalog>(Root+"/Data/DefaultCardEffects.asset")!=null)CardTableSceneMigration.Apply();
  AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,Root+"/Scenes/RainbowLiGame.unity");
  Debug.Log("RAINBOWLI BUILD PASS: editable Canvas, 6 modules, persistent slots, config assets and prefabs saved.");
 }
 static T ChildSystem<T>(GameObject root,string name)where T:Component{var go=new GameObject(name);go.transform.SetParent(root.transform);return go.AddComponent<T>();}
 static T Asset<T>(string relative)where T:ScriptableObject{string path=Root+"/"+relative;var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}
 static SequenceDefinition RuleAsset(string id,string name,int[] nums,RuleEffect effect,int value,bool insert=false){var r=Asset<SequenceDefinition>("Data/Sequences/"+id+".asset");r.displayName=name;r.numbers=nums;r.effect=effect;r.value=value;r.insertTwo=insert;EditorUtility.SetDirty(r);return r;}
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;Place(r,x,y,w,h);return r;}
 static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
 static Image Image(RectTransform r,Color color){var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
 static Text Label(string name,Transform parent,float x,float y,float w,float h,string text,int size,Color color){var r=Rect(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=11;t.resizeTextMaxSize=size;return t;}
 static Button Button(string name,Transform parent,float x,float y,float w,float h,string text){var r=Rect(name,parent,x,y,w,h);var image=Image(r,blue);image.raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.disabledColor=new Color(.5f,.5f,.5f,.6f);b.colors=colors;Label("Label",r,4,2,w-8,h-4,text,18,Color.white);var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.03f,.05f,.04f);outline.effectDistance=new Vector2(3,-3);return b;}
 static Slider Health(string name,Transform parent,float x,float y,float w,float h,Color color){var r=Rect(name,parent,x,y,w,h);Image(r,new Color(.03f,.05f,.04f));var fill=Rect("Fill",r,3,3,w-6,h-6);Image(fill,color);var slider=r.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.minValue=0;slider.maxValue=70;slider.value=70;slider.interactable=false;slider.transition=Selectable.Transition.None;return slider;}
 static void Invert(RectTransform r){r.pivot=new Vector2(.5f,.5f);r.anchoredPosition+=new Vector2(r.sizeDelta.x/2,-r.sizeDelta.y/2);r.localRotation=Quaternion.Euler(0,0,180);}
 static CardView MakeCardPrefab(){
  var r=Rect("PokerCard",null,0,0,130,225);var v=r.gameObject.AddComponent<CardView>();var face=Rect("Face",r,0,0,130,190);v.face=Image(face,cream);v.face.sprite=faceSprite;v.face.type=UnityEngine.UI.Image.Type.Sliced;v.face.raycastTarget=true;v.button=face.gameObject.AddComponent<Button>();v.button.targetGraphic=v.face;v.button.transition=Selectable.Transition.None;
  var shadow=face.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.4f);shadow.effectDistance=new Vector2(5,-7);
  v.topRank=Label("TopRank",face,7,5,27,25,"1",20,new Color(.1f,.22f,.28f));v.bottomRank=Label("BottomRank",face,96,159,27,25,"1",20,new Color(.1f,.22f,.28f));Invert(v.bottomRank.rectTransform);
  v.topSuit=Image(Rect("TopSuit",face,11,29,12,17),Color.white);v.bottomSuit=Image(Rect("BottomSuit",face,107,144,12,17),Color.white);Invert((RectTransform)v.bottomSuit.transform);v.suits=suits;v.topSuit.sprite=v.bottomSuit.sprite=suits[1];
  v.pips=new Image[6];for(int i=0;i<6;i++){v.pips[i]=Image(Rect("Pip_"+i,face,35+(i%2)*40,54+(i/2)*37,20,28),new Color(.1f,.22f,.28f));v.pips[i].sprite=suits[1];}
  v.engravingBadge=Image(Rect("EngravingBadge",face,46,8,38,20),gold);Label("Mark",v.engravingBadge.transform,0,0,38,20,"铭",14,dark);
  v.caption=Label("EffectLabel",r,-10,198,150,27,"护盾 +4",16,Color.white);
  v.SetPreview(1,CardEffect.Guard,"护盾 +4");var prefab=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Root+"/Prefabs/PokerCard.prefab");UnityEngine.Object.DestroyImmediate(r.gameObject);return prefab.GetComponent<CardView>();
 }
 static SequenceView MakeSequencePrefab(){var r=Rect("SequenceCard",null,0,0,152,190);Image(r,new Color(.8f,.84f,.68f));var v=r.gameObject.AddComponent<SequenceView>();v.title=Label("Title",r,4,6,144,32,"基础牌序",18,dark);v.digits=new Button[3];v.numbers=new Text[3];for(int i=0;i<3;i++){v.digits[i]=Button("Digit_"+i,r,8+i*46,53,42,46,(i+1).ToString());v.numbers[i]=v.digits[i].GetComponentInChildren<Text>();}v.description=Label("Description",r,6,117,140,61,"伤害 10",16,dark);var go=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Root+"/Prefabs/SequenceCard.prefab");UnityEngine.Object.DestroyImmediate(r.gameObject);return go.GetComponent<SequenceView>();}
 static DeckRowView MakeRowPrefab(){var r=Rect("DeckRow",null,0,0,1018,58);Image(r,new Color(.16f,.24f,.2f));var layout=r.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=58;var v=r.gameObject.AddComponent<DeckRowView>();v.label=Label("CardDescription",r,8,4,470,48,"01 / 数字 1 · 冲击 4",17,Color.white);v.upgrade=Button("Upgrade",r,485,8,220,42,"升级 +2 / 12 金币");v.engrave=Button("Engrave",r,728,8,270,42,"铭刻 / 20 金币");v.remove=Button("Remove",r,728,8,270,42,"删除这张牌");v.remove.gameObject.SetActive(false);var go=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Root+"/Prefabs/DeckRow.prefab");UnityEngine.Object.DestroyImmediate(r.gameObject);return go.GetComponent<DeckRowView>();}
 static Image MakeParticlePrefab(){var r=Rect("ImpactSpark",null,0,0,6,6);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);var im=Image(r,Color.white);var go=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Root+"/Prefabs/ImpactSpark.prefab");UnityEngine.Object.DestroyImmediate(r.gameObject);return go.GetComponent<Image>();}
 static Text MakeNumberPrefab(){var text=Label("DamageNumber",null,0,0,290,90,"伤害 10",42,gold);text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=new Vector2(.5f,.5f);var go=PrefabUtility.SaveAsPrefabAsset(text.gameObject,Root+"/Prefabs/DamageNumber.prefab");UnityEngine.Object.DestroyImmediate(text.gameObject);return go.GetComponent<Text>();}
 static void MakeSprites(){
  suits=new Sprite[4];for(int suit=0;suit<4;suit++){
   var texture=new Texture2D(24,32,TextureFormat.RGBA32,false);
   for(int y=0;y<32;y++)for(int x=0;x<24;x++){float px=(x-11.5f)/11.5f,py=(y-15.5f)/15.5f;bool inside;if(suit==0)inside=Mathf.Abs(px)+Mathf.Abs(py)*.75f<.74f;else if(suit==1||suit==2){float sy=suit==1?-py:py;float a=px*px+sy*sy-.75f;inside=a*a*a-px*px*sy*sy*sy<0;if(suit==1)inside|=py<-.35f&&Mathf.Abs(px)<.12f;}else inside=px*px+(py-.42f)*(py-.42f)<.16f||(px-.4f)*(px-.4f)+py*py<.16f||(px+.4f)*(px+.4f)+py*py<.16f||py<-.1f&&Mathf.Abs(px)<.12f;texture.SetPixel(x,y,inside?Color.white:Color.clear);}
   texture.Apply();string path=Root+"/Art/Suit"+suit+".png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);suits[suit]=ImportSprite(path,Vector4.zero);
  }
  var face=new Texture2D(32,32,TextureFormat.RGBA32,false);for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=Mathf.Max(4-x,x-27,0),dy=Mathf.Max(4-y,y-27,0);face.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(4.5f-Mathf.Sqrt(dx*dx+dy*dy))));}face.Apply();string facePath=Root+"/Art/CardFace.png";File.WriteAllBytes(facePath,face.EncodeToPNG());UnityEngine.Object.DestroyImmediate(face);faceSprite=ImportSprite(facePath,new Vector4(6,6,6,6));
 }
 static Sprite ImportSprite(string path,Vector4 border){AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.spriteBorder=border;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
}}
