using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RainbowLi.UnityGame;
namespace RainbowLi.EditorTools {
public static class UiModulesSceneMigration {
 const string Root="Assets/RainbowLi";
 public static void Run(){var s=EditorSceneManager.OpenScene(Root+"/Scenes/RainbowLiGame.unity");Apply();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();}
 [MenuItem("RainbowLi/UI/接入独立面板与卡牌说明")]
 public static void ApplyCurrent(){if(Application.isPlaying)return;var s=EditorSceneManager.GetActiveScene();if(!s.path.StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("请打开 RainbowLi 场景。");Apply();EditorSceneManager.MarkSceneDirty(s);}
 static void Apply(){var v=UnityEngine.Object.FindFirstObjectByType<GameView>();if(v==null)throw new InvalidOperationException("缺少 GameView");
  v.battleView=Get<BattlePanelView>(v.battlePanel);v.battleView.view=v;v.rewardView=Get<RewardPanelView>(v.rewardPanel);v.rewardView.view=v;v.serviceView=Get<ServicePanelView>(v.servicePanel);v.serviceView.view=v;v.endView=Get<EndPanelView>(v.endPanel);v.endView.view=v;
  if(v.tooltip==null){var root=Rect("CardTooltip",v.transform,520,280);var image=root.gameObject.AddComponent<Image>();image.color=new Color(.05f,.09f,.10f,.98f);image.raycastTarget=false;var outline=root.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.85f,.70f,.4f);outline.effectDistance=new Vector2(2,-2);var tooltip=root.gameObject.AddComponent<TooltipView>();tooltip.panel=root.gameObject;tooltip.title=Label("Title",root,18,16,484,44,23);tooltip.body=Label("Body",root,18,70,484,192,19);tooltip.body.resizeTextForBestFit=true;tooltip.body.resizeTextMinSize=14;tooltip.body.resizeTextMaxSize=19;v.tooltip=tooltip;root.gameObject.SetActive(false);}
  var hud=v.battleHud;if(hud!=null&&hud.handScroll==null){
   var outer=hud.handArea;var viewport=Rect("HandViewport",outer,outer.rect.width-16,outer.rect.height-30);viewport.anchoredPosition=new Vector2(8,-30);viewport.gameObject.AddComponent<RectMask2D>();var content=Rect("HandContent",viewport,viewport.rect.width,viewport.rect.height);foreach(var card in v.hand)card.transform.SetParent(content,false);hud.handArea=content;hud.handScroll=Scroll(outer.gameObject,viewport,content);
   var action=hud.actionArea;var parent=(RectTransform)action.parent;var actionViewport=Rect("ActionViewport",parent,action.rect.width,action.rect.height);actionViewport.anchoredPosition=action.anchoredPosition;actionViewport.gameObject.AddComponent<RectMask2D>();action.SetParent(actionViewport,false);action.anchoredPosition=Vector2.zero;hud.actionScroll=Scroll(parent.gameObject,actionViewport,action);
   var heading=outer.Find("Heading");if(heading!=null)heading.GetComponent<Text>().text="当前手牌 / 选择后点击 + 放置 / 滚轮查看更多";
   hud.LayoutCardsPreview();EditorUtility.SetDirty(hud);
  }
  foreach(var text in new[]{v.feedback,v.serviceTitle}){text.resizeTextForBestFit=true;text.resizeTextMinSize=14;text.resizeTextMaxSize=text.fontSize;EditorUtility.SetDirty(text);}
  foreach(var c in new Component[]{v,v.battleView,v.rewardView,v.serviceView,v.endView})EditorUtility.SetDirty(c);
 }
 static T Get<T>(GameObject root)where T:Component{var c=root.GetComponent<T>();return c==null?root.AddComponent<T>():c;}
 static ScrollRect Scroll(GameObject root,RectTransform viewport,RectTransform content){var sr=Get<ScrollRect>(root);sr.viewport=viewport;sr.content=content;sr.horizontal=true;sr.vertical=false;sr.scrollSensitivity=50;sr.movementType=ScrollRect.MovementType.Clamped;return sr;}
 static RectTransform Rect(string name,Transform parent,float w,float h){var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(w,h);return r;}
 static Text Label(string name,Transform parent,float x,float y,float w,float h,int size){var r=Rect(name,parent,w,h);r.anchoredPosition=new Vector2(x,-y);var text=r.gameObject.AddComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Resources/RainbowLiChinese.ttf");text.fontSize=size;text.color=new Color(.95f,.88f,.7f);text.alignment=TextAnchor.UpperLeft;text.raycastTarget=false;return text;}
}
}
