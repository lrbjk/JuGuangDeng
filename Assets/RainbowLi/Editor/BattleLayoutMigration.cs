using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class BattleLayoutMigration {
 const string Root="Assets/RainbowLi";
 static Font font;
 static readonly Color panel=new Color(.075f,.15f,.14f),edge=new Color(.42f,.49f,.39f),gold=new Color(.93f,.78f,.47f);
 [MenuItem("RainbowLi/UI/应用牌面与UI布局到当前场景")]
 public static void ApplyCurrent(){if(Application.isPlaying)return;if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;Apply();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());}
 public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("BATTLE LAYOUT PASS: editable diagram layout saved.");}
 public static void Apply(){
  var v=UnityEngine.Object.FindFirstObjectByType<GameView>();if(v==null)throw new InvalidOperationException("请先打开 RainbowLiGame 场景");
  if(v.battleHud!=null){Debug.Log("布局已应用，直接编辑场景中的 BattleHudView。");return;}
  font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Resources/RainbowLiChinese.ttf");
  var canvas=v.GetComponent<CanvasScaler>();canvas.referenceResolution=new Vector2(1920,1080);canvas.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
  var table=(RectTransform)v.transform.Find("Table");table.sizeDelta=new Vector2(1920,1080);
  var oldHud=table.Find("HUD");oldHud.gameObject.SetActive(false);
  var oldShared=table.Find("SharedRules");oldShared.gameObject.SetActive(false);
  var sidebar=Box("Sidebar",table,12,12,392,1056);
  var skills=Box("EnemySkillGroup",sidebar,12,12,368,306);Label("Heading",skills,12,8,344,30,"敌人技能组",26);
  var enemyRules=Box("EnemySequenceGroup",sidebar,12,332,368,246);Label("Heading",enemyRules,12,6,344,32,"敌人牌序",26);
  var resources=Box("Resources",sidebar,12,592,368,64);
  var playerRules=Box("PlayerSequenceGroup",sidebar,12,736,368,306);Label("Heading",playerRules,12,8,344,32,"玩家牌序",26);
  var arenaPanel=Box("ArenaSequence",table,430,24,395,174);Move(v.arena,arenaPanel,16,8,363,158);
  var bp=(RectTransform)v.battlePanel.transform;Set(bp,table,430,12,1478,1056);
  var portrait=Box("EnemyPortraitArea",bp,465,56,540,348);
  var portraitImage=Rect("Portrait",portrait,8,8,524,332).gameObject.AddComponent<Image>();portraitImage.color=Color.white;portraitImage.preserveAspect=true;portraitImage.raycastTarget=false;portraitImage.enabled=false;
  var placeholder=Label("PortraitPlaceholder",portrait,15,70,510,210,"敌人立绘",28);
  var name=Label("EnemyName",portrait,12,8,516,42,"逸散实体",26);
  var hud=v.gameObject.AddComponent<BattleHudView>();v.battleHud=hud;hud.view=v;hud.enemyPortrait=portraitImage;hud.portraitPlaceholder=placeholder;hud.enemyName=name;
  Move(v.enemyHealth,bp,505,420,500,30);FitHealth(v.enemyHealth);
  Move(v.enemyStats,bp,505,416,500,38);
  hud.enemyShield=Label("EnemyShield",Box("EnemyShieldBadge",bp,465,408,40,54),0,0,40,54,"护盾\n0",16);
  var actions=Box("ActionArrangement",bp,8,516,1410,196);Label("ActionHeading",bp,8,492,650,22,"行动安排  /  从左到右执行",18);
  hud.actionArea=Rect("ActionCards",actions,8,0,1394,190);
  for(int i=0;i<v.actions.Length;i++){Reparent(v.actions[i],hud.actionArea);Reparent(v.actionLabels[i],hud.actionArea);Reparent(v.leftButtons[i],hud.actionArea);Reparent(v.rightButtons[i],hud.actionArea);Reparent(v.removeButtons[i],hud.actionArea);}
  foreach(var b in v.insertButtons)Reparent(b,hud.actionArea);
  Move(v.playerHealth,bp,82,742,1136,36);FitHealth(v.playerHealth);
  Move(v.playerStats,bp,82,740,1136,40);
  hud.playerShield=Label("PlayerShield",Box("PlayerShieldBadge",bp,8,726,64,64),0,0,64,64,"护盾\n0",20);
  var hand=Box("CurrentHand",bp,8,806,1240,232);Label("Heading",hand,12,2,600,30,"当前手牌  /  选择后点击 + 放置",20);hud.handArea=hand;
  foreach(var card in v.hand)Reparent(card,hand);
  var draw=Button("DrawPile",bp,1326,806,106,98,"抽牌堆\n0");hud.drawCount=draw.GetComponentInChildren<Text>();UnityEventTools.AddPersistentListener(draw.onClick,hud.ShowDrawPile);
  var discard=Button("DiscardPile",bp,1326,924,106,98,"弃牌堆\n0");hud.discardCount=discard.GetComponentInChildren<Text>();UnityEventTools.AddPersistentListener(discard.onClick,hud.ShowDiscardPile);
  var settings=Button("Settings",table,1776,34,108,68,"设置");UnityEventTools.AddPersistentListener(settings.onClick,hud.ToggleSettings);
  Move(v.confirm,bp,1030,460,182,42);Move(v.pause,bp,1030,460,82,42);Move(v.step,bp,1120,460,92,42);
  Move(v.feedback,bp,8,450,880,38);Move(v.roundStats,sidebar,12,668,190,54);
  hud.turnLabel=v.roundStats;hud.coins=Label("Coins",resources,10,6,160,50,"金币 0",22);hud.deckCount=Label("DeckCount",resources,190,6,166,50,"牌组 18",22);
  Move(v.forecast,bp,1040,118,388,108);Move(v.statusSummary,bp,1040,242,388,104);
  if(bp.Find("BattleHeading")!=null)bp.Find("BattleHeading").gameObject.SetActive(false);
  var restart=oldHud.Find("NewGame");Move(restart.GetComponent<Button>(),sidebar,208,676,148,40);
  hud.enemyRules=new Text[3];for(int i=0;i<3;i++)hud.enemyRules[i]=Label("EnemyRule_"+i,Box("RuleRow_"+i,enemyRules,14,48+i*63,340,58),8,3,324,52,"—",20);
  for(int i=0;i<v.ruleViews.Length;i++)CompactRule(v.ruleViews[i],playerRules,i);
  hud.enemySkills=new CardView[8];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PokerCard.prefab");
  for(int i=0;i<8;i++){var card=((GameObject)PrefabUtility.InstantiatePrefab(prefab,skills)).GetComponent<CardView>();card.name="EnemySkill_"+i;Set((RectTransform)card.transform,skills,24+i%4*86,46+i/4*126,130,225);card.transform.localScale=Vector3.one*.52f;card.SetPreview(i%6+1,CardEffect.Strike,"技能 "+(i+1));hud.enemySkills[i]=card;}
  // Other room panels keep their existing contents and bindings, centered in the larger table.
  foreach(var p in new[]{v.mapPanel,v.rewardPanel,v.servicePanel,v.endPanel}){var r=(RectTransform)p.transform;r.anchoredPosition+=new Vector2(240,-150);}
  var map=v.GetComponent<RouteMapView>();if(map!=null&&map.eventPanel!=null)((RectTransform)map.eventPanel.transform).anchoredPosition+=new Vector2(240,-150);
  var fx=v.session.GetComponentInChildren<BattleEffects>();fx.gameView=v;fx.ruleOrigin=arenaPanel;fx.playerTarget=(RectTransform)v.playerHealth.transform;fx.enemyTarget=(RectTransform)v.enemyHealth.transform;
  Set(fx.comboLabel.rectTransform,fx.overlay,860,480,700,54);
  CreateSettings(hud,table);CreatePile(hud,table);
  hud.LayoutCardsPreview();
  v.enemyIntent.gameObject.SetActive(false);
  EditorUtility.SetDirty(v);EditorUtility.SetDirty(hud);EditorUtility.SetDirty(fx);
 }
 static void CompactRule(SequenceView r,Transform parent,int index){Set((RectTransform)r.transform,parent,14,48+index*82,340,74);Move(r.title,r.transform,5,2,145,24);Move(r.description,r.transform,164,26,170,44);for(int d=0;d<3;d++)Move(r.digits[d],r.transform,6+d*50,30,44,38);if(r.primaryEffect!=null)Move(r.primaryEffect,r.transform,154,2,86,22);if(r.secondaryEffect!=null)Move(r.secondaryEffect,r.transform,244,2,90,22);}
 static void CreateSettings(BattleHudView hud,Transform table){var r=Box("SettingsPanel",table,660,300,700,380);hud.settingsPanel=r.gameObject;Label("Title",r,20,12,660,52,"设置",28);var close=Button("Close",r,570,20,105,38,"关闭");UnityEventTools.AddPersistentListener(close.onClick,hud.CloseSettings);var speed=Button("Speed",r,40,110,620,54,"播放速度  1×");hud.speedLabel=speed.GetComponentInChildren<Text>();UnityEventTools.AddPersistentListener(speed.onClick,hud.CycleSpeed);Label("VolumeLabel",r,40,192,200,40,"战斗音量",22);var sr=Box("Volume",r,240,200,420,24);var fill=Rect("Fill",sr,0,0,420,24).gameObject.AddComponent<Image>();fill.color=gold;var handle=Rect("Handle",sr,0,0,18,36).gameObject.AddComponent<Image>();handle.color=Color.white;var slider=sr.gameObject.AddComponent<Slider>();slider.fillRect=(RectTransform)fill.transform;slider.handleRect=(RectTransform)handle.transform;slider.targetGraphic=handle;slider.value=.3f;hud.volume=slider;UnityEventTools.AddPersistentListener(slider.onValueChanged,hud.SetVolume);Label("Help",r,40,276,620,60,"设置即时生效。关闭后继续安排行动。",18);r.gameObject.SetActive(false);}
 static void CreatePile(BattleHudView hud,Transform table){var r=Box("PilePanel",table,650,140,760,800);hud.pilePanel=r.gameObject;hud.pileTitle=Label("Title",r,20,10,580,54,"牌堆",28);var close=Button("Close",r,630,20,100,40,"关闭");UnityEventTools.AddPersistentListener(close.onClick,hud.ClosePile);var scroll=Rect("Scroll",r,24,86,712,682);var sr=scroll.gameObject.AddComponent<ScrollRect>();sr.horizontal=false;sr.movementType=ScrollRect.MovementType.Clamped;var vp=Box("Viewport",scroll,0,0,712,682);vp.gameObject.AddComponent<RectMask2D>();vp.GetComponent<Image>().raycastTarget=true;hud.pileBody=Label("Content",vp,16,0,680,680,"暂无卡牌",20);hud.pileBody.alignment=TextAnchor.UpperLeft;hud.pileBody.resizeTextForBestFit=false;var fitter=hud.pileBody.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;sr.viewport=vp;sr.content=hud.pileBody.rectTransform;r.gameObject.SetActive(false);}
 static void FitHealth(Slider s){s.fillRect.anchorMin=Vector2.zero;s.fillRect.anchorMax=Vector2.one;s.fillRect.pivot=new Vector2(.5f,.5f);s.fillRect.offsetMin=new Vector2(3,3);s.fillRect.offsetMax=new Vector2(-3,-3);}
 static void Reparent(Component c,Transform parent){c.transform.SetParent(parent,false);var r=(RectTransform)c.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);}
 static void Move(Component c,Transform parent,float x,float y,float w,float h){if(c==null)return;Set((RectTransform)c.transform,parent,x,y,w,h);if(c is Button){var label=c.GetComponentInChildren<Text>(true);if(label!=null)Set(label.rectTransform,c.transform,4,2,w-8,h-4);}}
 static void Set(RectTransform r,Transform parent,float x,float y,float w,float h){r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;}
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));Set((RectTransform)go.transform,parent,x,y,w,h);return (RectTransform)go.transform;}
 static RectTransform Box(string name,Transform parent,float x,float y,float w,float h){var r=Rect(name,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=panel;im.raycastTarget=false;var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=edge;outline.effectDistance=new Vector2(1,-1);return r;}
 static Text Label(string name,Transform parent,float x,float y,float w,float h,string text,int size){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=gold;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=12;t.resizeTextMaxSize=size;return t;}
 static Button Button(string name,Transform parent,float x,float y,float w,float h,string caption){var r=Box(name,parent,x,y,w,h);var im=r.GetComponent<Image>();im.color=new Color(.14f,.30f,.27f);im.raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;Label("Label",r,4,2,w-8,h-4,caption,22);return b;}
}
}
