using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class StartMenuSceneMigration {
  const string Root="Assets/RainbowLi";
  static readonly Color Gold=new Color(.92f,.76f,.42f),Cream=new Color(.95f,.92f,.8f),Surface=new Color(.04f,.12f,.095f),Tile=new Color(.08f,.22f,.18f);
  static Font font;
  [MenuItem("RainbowLi/UI/接入游戏开始界面")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  public static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/SourceHanSansCN-Bold.otf");
   var menu=view.GetComponent<StartMenuView>();if(menu==null)menu=view.gameObject.AddComponent<StartMenuView>();menu.view=view;
   var existing=view.transform.Find("StartMenuPanel");if(existing!=null){SettingsSceneMigration.Apply();return;} // 保留已填写的策划文案和布局。
   var panel=Panel("StartMenuPanel",view.transform,0,0,1920,1080,Surface);menu.panel=panel.gameObject;panel.SetAsLastSibling();
   var sidebar=Panel("Sidebar",panel,18,24,406,1032,Surface);
   var credits=Panel("Credits",sidebar,32,46,342,470,Surface);menu.credits=Label("Text",credits,26,26,290,418,"制作人员名单\n（研发团队）\n\n待填写制作人员。",28);menu.credits.alignment=TextAnchor.UpperLeft;
   var flavor=Panel("Flavor",sidebar,32,568,342,196,Surface);menu.flavor=Label("Text",flavor,26,26,290,144,"风味文本\n（世界观体现）\n\n待填写世界观介绍。",26);menu.flavor.alignment=TextAnchor.UpperLeft;
   menu.manual=Click("Manual",sidebar,32,855,342,116,"教学手册");
   var main=Panel("IntroductionArea",panel,494,80,1322,682,Surface);
   var viewport=Panel("Viewport",main,30,30,1242,622,Surface);viewport.gameObject.AddComponent<RectMask2D>();viewport.GetComponent<Outline>().enabled=false;
   menu.introduction=Label("IntroductionText",viewport,20,16,1202,590,"此处为游戏内容介绍文本，后续可直接替换。\n\n通过卡牌效果与牌序组合完成战斗，在不同关卡中调整卡组、升级卡牌并选择铭刻。\n\n完成战斗后获得奖励；达到解锁条件时，可以在选关界面挑战 BOSS。",28);menu.introduction.alignment=TextAnchor.UpperLeft;menu.introduction.lineSpacing=1.35f;
   var textRect=(RectTransform)menu.introduction.transform;textRect.anchorMin=new Vector2(0,1);textRect.anchorMax=new Vector2(1,1);textRect.sizeDelta=new Vector2(-40,590);
   var fitter=textRect.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
   menu.introductionScroll=main.gameObject.AddComponent<ScrollRect>();menu.introductionScroll.viewport=viewport;menu.introductionScroll.content=textRect;menu.introductionScroll.horizontal=false;menu.introductionScroll.vertical=true;menu.introductionScroll.movementType=ScrollRect.MovementType.Clamped;
   var track=Panel("VerticalScrollbar",main,1288,30,16,622,Tile);var handle=Panel("Handle",track,0,0,16,622,Gold);handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.pivot=new Vector2(.5f,.5f);handle.sizeDelta=Vector2.zero;handle.anchoredPosition=Vector2.zero;
   var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;scrollbar.targetGraphic=handle.GetComponent<Image>();scrollbar.direction=Scrollbar.Direction.BottomToTop;menu.introductionScroll.verticalScrollbar=scrollbar;
   var bottom=Panel("Actions",panel,424,832,1496,248,Surface);menu.settings=Click("Settings",bottom,44,47,290,116,"设置");menu.start=Click("Start",bottom,574,47,290,116,"进行测试\n（开始游戏）");menu.quit=Click("Quit",bottom,1116,47,290,116,"退出测试");
   var settingsOverlay=Overlay("SettingsOverlay",panel);menu.settingsPanel=settingsOverlay.gameObject;var settingsDialog=Panel("Dialog",settingsOverlay,580,310,760,460,Surface);Label("Caption",settingsDialog,40,30,680,60,"设置",32);Label("VolumeLabel",settingsDialog,40,116,680,50,"音量",26);
   var sliderRoot=Panel("Volume",settingsDialog,70,190,620,28,Tile);var fill=Panel("Fill",sliderRoot,0,0,620,28,Gold);var thumb=Panel("Handle",sliderRoot,0,-8,24,44,Cream);menu.volume=sliderRoot.gameObject.AddComponent<Slider>();menu.volume.fillRect=fill;menu.volume.handleRect=thumb;menu.volume.targetGraphic=thumb.GetComponent<Image>();menu.volume.minValue=0;menu.volume.maxValue=1;menu.volume.value=1;
   menu.speed=Click("Speed",settingsDialog,40,272,310,64,"切换战斗播放速度");menu.closeSettings=Click("Close",settingsDialog,410,272,310,64,"返回");settingsOverlay.gameObject.SetActive(false);
   var manualOverlay=Overlay("ManualOverlay",panel);menu.tutorialPanel=manualOverlay.gameObject;var manualDialog=Panel("Dialog",manualOverlay,480,190,960,700,Surface);Label("Caption",manualDialog,40,20,880,60,"教学手册",32);
   menu.tutorial=Label("Text",manualDialog,50,110,860,430,"1. 在选关界面选择前方可进入的关卡。\n\n2. 战斗中，将手牌拖放到行动安排区域。行动从左向右执行，双方都能触发场地牌序，归属取决于最后一张参与牌。\n\n3. 点击抽牌堆、弃牌堆或卡组数量，查看对应卡牌与效果详情。\n\n4. 商店和休息中的升级、铭刻、删除等操作，先在卡组界面选牌，再确认。\n\n5. 每完成 10 次关卡，敌人卡牌效果全局 +1。每层完成 5 + 当前层数场战斗后，可挑战 BOSS。\n\n6. 选关界面的“保存”和“继续”用于手动保存和读取本局。",25);menu.tutorial.alignment=TextAnchor.UpperLeft;menu.closeManual=Click("Close",manualDialog,335,590,290,70,"返回");manualOverlay.gameObject.SetActive(false);
   foreach(var component in panel.GetComponentsInChildren<Component>(true)){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}EditorUtility.SetDirty(menu);SettingsSceneMigration.Apply();
  }
  static RectTransform Overlay(string name,Transform parent){var rect=Panel(name,parent,0,0,1920,1080,new Color(0,0,0,.75f));rect.GetComponent<Outline>().enabled=false;return rect;}
  static RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Outline));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);go.GetComponent<Image>().color=color;go.GetComponent<Image>().raycastTarget=true;go.GetComponent<Outline>().effectColor=new Color(Gold.r,Gold.g,Gold.b,.42f);go.GetComponent<Outline>().effectDistance=new Vector2(1,-1);return (RectTransform)go.transform;}
  static Text Label(string name,Transform parent,float x,float y,float w,float h,string caption,int size){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);var text=go.GetComponent<Text>();text.text=caption;text.font=font;text.fontSize=size;text.color=Cream;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;}
  static Button Click(string name,Transform parent,float x,float y,float w,float h,string caption){var rect=Panel(name,parent,x,y,w,h,Tile);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();Label("Text",rect,16,10,w-32,h-20,caption,28);return button;}
  static void Place(Transform target,float x,float y,float w,float h){var rect=(RectTransform)target;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);rect.localScale=Vector3.one;}
 }
}
