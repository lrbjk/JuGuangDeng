using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
 public static class DeckOperationSceneMigration {
  const string Root="Assets/RainbowLi";
  static readonly Color Gold=new Color(.92f,.76f,.42f),Cream=new Color(.95f,.92f,.8f),Surface=new Color(.04f,.12f,.095f),Tile=new Color(.08f,.22f,.18f);
  static Font font;
  [MenuItem("RainbowLi/UI/接入统一卡组操作与二次确认")]
  public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
  public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
  public static void Apply(){
   var view=UnityEngine.Object.FindFirstObjectByType<GameView>();font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Resources/RainbowLiChinese.ttf");
   var deck=view.GetComponent<DeckOperationView>();if(deck==null)deck=view.gameObject.AddComponent<DeckOperationView>();deck.view=view;view.engravings.deck=deck;view.progression.deck=deck;view.battleHud.deckOperations=deck;view.serviceView.deck=deck;view.rewardView.deck=deck;
   var old=view.transform.Find("DeckManagementPanel");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var panel=Panel("DeckManagementPanel",view.transform,0,0,1920,1080,Surface);panel.GetComponent<Image>().raycastTarget=true;deck.panel=panel.gameObject;
   var hud=view.GetComponent<AdventureMapHudView>();var original=view.GetComponent<RouteMapView>().mapRoot.transform.Find("AdventureSidebar");var sidebar=UnityEngine.Object.Instantiate(original,panel,false);sidebar.name="AdventureSidebar";
   foreach(var button in sidebar.GetComponentsInChildren<Button>(true)){button.onClick=new Button.ButtonClickedEvent();button.interactable=false;}
   foreach(string action in new[]{"NewRun","Save","Continue"}){var button=sidebar.Find("ProgressGroup/"+action);if(button!=null)button.gameObject.SetActive(false);}
   deck.completedRooms=FindCopy(hud.completedRooms,original,sidebar);deck.elapsedTime=FindCopy(hud.elapsedTime,original,sidebar);deck.growthCount=FindCopy(hud.growthCount,original,sidebar);deck.growthDescription=FindCopy(hud.growthDescription,original,sidebar);
   var map=view.GetComponent<RouteMapView>();deck.coins=FindCopy(map.resources,original,sidebar);deck.deckCount=FindCopy(view.battleHud.mapDeckCount,original,sidebar);
   deck.summaries=new SequenceView[3];for(int i=0;i<3;i++)deck.summaries[i]=sidebar.Find(AnimationUtility.CalculateTransformPath(hud.playerSequences[i].transform,original)).GetComponent<SequenceView>();
   deck.title=Label("Title",panel,480,10,1110,34,"当前卡组",24,Gold);
   var grid=Panel("CardGrid",panel,470,45,1136,984,Tile);var viewport=Panel("Viewport",grid,10,30,1092,920,Surface);viewport.GetComponent<Image>().raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
   var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport,false);Place(content.transform,0,0,1092,920);deck.content=(RectTransform)content.transform;
   deck.scroll=grid.gameObject.AddComponent<ScrollRect>();deck.scroll.viewport=viewport;deck.scroll.content=deck.content;deck.scroll.horizontal=false;deck.scroll.vertical=true;deck.scroll.scrollSensitivity=42;deck.scroll.movementType=ScrollRect.MovementType.Clamped;
   var track=Panel("VerticalScrollbar",grid,1110,30,14,920,new Color(.025f,.08f,.06f));var handle=Panel("Handle",track,0,0,14,920,Gold);handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.pivot=new Vector2(.5f,.5f);handle.sizeDelta=Vector2.zero;handle.anchoredPosition=Vector2.zero;
   var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;scrollbar.targetGraphic=handle.GetComponent<Image>();scrollbar.targetGraphic.raycastTarget=true;scrollbar.direction=Scrollbar.Direction.BottomToTop;deck.scroll.verticalScrollbar=scrollbar;
   deck.cardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PokerCard.prefab").GetComponent<CardView>();deck.emptyLabel=Label("Empty",viewport,20,12,1052,56,"暂无卡牌",25,Gold);
   deck.instruction=Label("Instruction",panel,1636,140,240,150,"选择卡牌后再次确认",22,Cream);deck.result=Label("Result",panel,1636,70,240,62,"",22,Gold);
   deck.operationButtons=new Button[7];for(int i=0;i<7;i++)deck.operationButtons[i]=Click("Operation_"+(DeckOperation)i,panel,1636,310+i*65,240,52,DeckOperationView.Name((DeckOperation)i),22);
   deck.generate=Click("GenerateCard",panel,1636,802,240,60,"生成一张随机卡牌",20);deck.generate.gameObject.SetActive(false);deck.exit=Click("Exit",panel,1636,898,240,98,"退出",30);
   var overlay=Panel("ConfirmationOverlay",panel,0,0,1920,1080,new Color(0,0,0,.72f));overlay.GetComponent<Outline>().enabled=false;overlay.GetComponent<Image>().raycastTarget=true;deck.confirmation=overlay.gameObject;
   var dialog=Panel("ConfirmationDialog",overlay,650,200,860,640,Surface);deck.confirmationTitle=Label("Title",dialog,26,18,808,50,"确认操作",30,Gold);
   deck.confirmationCard=((GameObject)PrefabUtility.InstantiatePrefab(deck.cardPrefab.gameObject,dialog)).GetComponent<CardView>();deck.confirmationCard.name="SelectedCard";Place(deck.confirmationCard.transform,26,126,130,225);deck.confirmationCard.transform.localScale=Vector3.one*1.5f;
   deck.confirmationBody=Label("Body",dialog,260,94,570,390,"目标、结果及费用",23,Cream);deck.confirmationBody.alignment=TextAnchor.MiddleLeft;
   deck.cancel=Click("Cancel",dialog,260,520,250,64,"取消",24);deck.confirm=Click("Confirm",dialog,540,520,250,64,"确认",24);overlay.gameObject.SetActive(false);
   var service=view.servicePanel.transform.Find("DeckOperationEntries");if(service!=null)UnityEngine.Object.DestroyImmediate(service.gameObject);var entries=Panel("DeckOperationEntries",view.servicePanel.transform,20,224,1018,320,Surface);
   view.serviceView.upgradeDeck=Click("Upgrade",entries,24,50,290,64,"卡组 / 升级卡牌",24);view.serviceView.engraveDeck=Click("Engrave",entries,352,50,290,64,"卡组 / 选择铭刻",24);view.serviceView.removeDeck=Click("Remove",entries,24,50,290,64,"卡组 / 删除卡牌",24);Label("Hint",entries,30,158,958,90,"所有卡牌操作在卡组界面选择，确认后执行。",23,Cream);
   var reward=view.rewardPanel.transform.Find("OpenRewardDeck");if(reward!=null)UnityEngine.Object.DestroyImmediate(reward.gameObject);view.rewardView.openDeck=Click("OpenRewardDeck",view.rewardPanel.transform,230,210,600,92,"打开卡组界面选择奖励",28);UnityEventTools.AddPersistentListener(view.rewardView.openDeck.onClick,deck.OpenRewards);
   var oldScroll=view.servicePanel.transform.Find("DeckScroll");if(oldScroll!=null)oldScroll.gameObject.SetActive(false);foreach(var card in view.rewards)card.gameObject.SetActive(false);foreach(var button in view.rewardButtons)button.gameObject.SetActive(false);
   panel.gameObject.SetActive(false);
   foreach(var component in view.GetComponentsInChildren<Component>(true)){if(component==null)continue;EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
   foreach(var component in new Component[]{deck,view.engravings,view.progression,view.battleHud,view.serviceView,view.rewardView})EditorUtility.SetDirty(component);
  }
  static Text FindCopy(Text source,Transform original,Transform copy)=>copy.Find(AnimationUtility.CalculateTransformPath(source.transform,original)).GetComponent<Text>();
  static RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Outline));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);go.GetComponent<Image>().color=color;go.GetComponent<Image>().raycastTarget=false;var outline=go.GetComponent<Outline>();outline.effectColor=new Color(Gold.r,Gold.g,Gold.b,.42f);outline.effectDistance=new Vector2(1,-1);return (RectTransform)go.transform;}
  static Text Label(string name,Transform parent,float x,float y,float w,float h,string caption,int size,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go.transform,x,y,w,h);var text=go.GetComponent<Text>();text.text=caption;text.font=font;text.fontSize=size;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=14;text.resizeTextMaxSize=size;return text;}
  static Button Click(string name,Transform parent,float x,float y,float w,float h,string caption,int size){var rect=Panel(name,parent,x,y,w,h,Tile);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.targetGraphic.raycastTarget=true;Label("Label",rect,4,4,w-8,h-8,caption,size,Cream);return button;}
  static void Place(Transform target,float x,float y,float w,float h){var rect=(RectTransform)target;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);rect.localScale=Vector3.one;}
 }
}
