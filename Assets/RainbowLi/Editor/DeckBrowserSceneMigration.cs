using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class DeckBrowserSceneMigration {
 const string Root="Assets/RainbowLi";
 [MenuItem("RainbowLi/UI/接入当前牌组查看")]
 public static void ApplyCurrent(){if(Application.isPlaying)return;var scene=EditorSceneManager.GetActiveScene();if(!scene.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(scene);}
 public static void Run(){var scene=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 public static void Apply(){
  var hud=UnityEngine.Object.FindFirstObjectByType<BattleHudView>();if(hud==null)throw new InvalidOperationException("缺少 BattleHudView。");
  var resources=hud.coins.transform.parent;var existing=resources.Find("DeckButton");
  var root=existing==null?Rect("DeckButton",resources,182,0,180,44):(RectTransform)existing;
  var image=root.GetComponent<Image>();if(image==null)image=root.gameObject.AddComponent<Image>();image.color=new Color(.12f,.27f,.24f);image.raycastTarget=true;
  var button=root.GetComponent<Button>();if(button==null)button=root.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,hud.ShowDeck);
  hud.deckCount.transform.SetParent(root,false);Place(hud.deckCount.rectTransform,8,2,164,40);hud.deckCount.raycastTarget=false;
  var map=hud.view.GetComponent<RouteMapView>();if(map!=null&&map.resources!=null){
   var resource=map.resources.rectTransform;var parent=resource.parent;var mapButton=parent.Find("MapDeckButton");
   if(mapButton==null){
    float x=resource.anchoredPosition.x,y=-resource.anchoredPosition.y;var mapRoot=Rect("MapDeckButton",parent,x+140,y,190,resource.sizeDelta.y);resource.sizeDelta=new Vector2(130,resource.sizeDelta.y);
    var mapImage=mapRoot.gameObject.AddComponent<Image>();mapImage.color=new Color(.12f,.27f,.24f);var mapClick=mapRoot.gameObject.AddComponent<Button>();mapClick.targetGraphic=mapImage;UnityEventTools.AddPersistentListener(mapClick.onClick,hud.ShowDeck);
    var label=Rect("Label",mapRoot,4,0,182,resource.sizeDelta.y).gameObject.AddComponent<Text>();label.font=map.resources.font;label.fontSize=20;label.alignment=TextAnchor.MiddleCenter;label.color=map.resources.color;label.raycastTarget=false;label.text="牌组 · 查看";hud.mapDeckCount=label;
   }else hud.mapDeckCount=mapButton.GetComponentInChildren<Text>();
  }
  var existingBrowser=hud.pilePanel.transform.Find("DeckBrowser");
  if(existingBrowser==null){
   var panel=Rect("DeckBrowser",hud.pilePanel.transform,24,86,712,682);var browser=panel.gameObject.AddComponent<DeckBrowserView>();
   var scroll=panel.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
   var viewport=Rect("Viewport",panel,0,0,712,682);var background=viewport.gameObject.AddComponent<Image>();background.color=new Color(.055f,.12f,.11f);viewport.gameObject.AddComponent<RectMask2D>();
   var content=Rect("CardGrid",viewport,0,0,712,682);scroll.viewport=viewport;scroll.content=content;
   var empty=Rect("Empty",viewport,20,20,672,80).gameObject.AddComponent<Text>();empty.font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/SourceHanSansCN-Bold.otf");empty.fontSize=22;empty.color=new Color(.93f,.78f,.47f);empty.alignment=TextAnchor.MiddleCenter;empty.text="当前牌组暂无卡牌";empty.raycastTarget=false;
   browser.content=content;browser.scroll=scroll;browser.emptyLabel=empty;browser.tooltip=hud.view.tooltip;browser.textPileScroll=hud.pilePanel.transform.Find("Scroll").gameObject;browser.cardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PokerCard.prefab").GetComponent<CardView>();hud.deckBrowser=browser;panel.gameObject.SetActive(false);
  }else hud.deckBrowser=existingBrowser.GetComponent<DeckBrowserView>();
  EditorUtility.SetDirty(hud);EditorUtility.SetDirty(button);EditorUtility.SetDirty(hud.deckCount);
 }
 static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;Place(rect,x,y,width,height);return rect;}
 static void Place(RectTransform rect,float x,float y,float width,float height){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;}
}
}
