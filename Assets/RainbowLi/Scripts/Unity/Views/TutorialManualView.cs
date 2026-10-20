using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class TutorialManualView : MonoBehaviour {
  public StartMenuView menu;
  [Tooltip("教学手册正文，来自用户提供的 Word 文档。")]
  public TextAsset content;
  public ScrollRect scroll;
  public static TutorialManualView Ensure(StartMenuView menu){
   var manual=menu.GetComponent<TutorialManualView>();if(manual==null)manual=menu.gameObject.AddComponent<TutorialManualView>();
   manual.menu=menu;manual.Configure();return manual;
  }
  public void Configure(){
   if(menu==null||menu.tutorial==null)return;
   if(content==null)content=Resources.Load<TextAsset>("TutorialManual");
   var text=menu.tutorial;if(content!=null)text.text=content.text.Trim();
   if(scroll==null){
    var parent=text.transform.parent;
    var go=new GameObject("ManualScroll",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));go.transform.SetParent(parent,false);
    var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(48,122);rect.offsetMax=new Vector2(-48,-106);
    var image=go.GetComponent<Image>();image.color=Color.clear;image.raycastTarget=true;
    scroll=go.GetComponent<ScrollRect>();scroll.viewport=rect;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;
   }
   text.transform.SetParent(scroll.viewport,false);var body=text.rectTransform;
   body.anchorMin=new Vector2(0,1);body.anchorMax=new Vector2(1,1);body.pivot=new Vector2(.5f,1);body.anchoredPosition=Vector2.zero;body.sizeDelta=new Vector2(0,body.sizeDelta.y);
   text.fontSize=24;text.resizeTextForBestFit=false;text.alignment=TextAnchor.UpperLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.lineSpacing=1.2f;text.raycastTarget=false;text.supportRichText=false;
   var fitter=text.GetComponent<ContentSizeFitter>();if(fitter==null)fitter=text.gameObject.AddComponent<ContentSizeFitter>();fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=body;
   if(scroll.verticalScrollbar==null){
    var go=new GameObject("ManualScrollbar",typeof(RectTransform),typeof(Image),typeof(Scrollbar));go.transform.SetParent(scroll.transform.parent,false);
    var rect=(RectTransform)go.transform;rect.anchorMin=new Vector2(1,0);rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(-34,122);rect.offsetMax=new Vector2(-26,-106);go.GetComponent<Image>().color=new Color(.08f,.2f,.18f,.7f);
    var handle=new GameObject("Handle",typeof(RectTransform),typeof(Image));handle.transform.SetParent(go.transform,false);var handleRect=(RectTransform)handle.transform;handleRect.anchorMin=Vector2.zero;handleRect.anchorMax=Vector2.one;handleRect.offsetMin=handleRect.offsetMax=Vector2.zero;
    var image=handle.GetComponent<Image>();image.color=new Color(.5f,.85f,.8f,.75f);var bar=go.GetComponent<Scrollbar>();bar.handleRect=handleRect;bar.targetGraphic=image;bar.direction=Scrollbar.Direction.BottomToTop;
    scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
   }
   menu.closeManual.transform.SetAsLastSibling();
  }
  public void Open(){
   menu.tutorialPanel.SetActive(true);menu.tutorialPanel.transform.SetAsLastSibling();
   Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);scroll.StopMovement();scroll.verticalNormalizedPosition=1;
  }
 }
}
