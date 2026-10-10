using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class TooltipView:MonoBehaviour {
 public GameObject panel;public Text title,body;
 Object owner;
 public void Show(Object source,string heading,string text,Vector2 screenPoint){owner=source;title.text=heading;body.text=text;panel.SetActive(true);panel.transform.SetAsLastSibling();var r=(RectTransform)panel.transform;var parent=(RectTransform)r.parent;r.sizeDelta=new Vector2(r.sizeDelta.x,Mathf.Clamp(body.preferredHeight+90,180,parent.rect.height-24));body.rectTransform.sizeDelta=new Vector2(body.rectTransform.sizeDelta.x,r.rect.height-88);var canvas=GetComponentInParent<Canvas>();RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screenPoint,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var point);point+=new Vector2(18,-18);point.x=Mathf.Clamp(point.x,parent.rect.xMin+12,parent.rect.xMax-r.rect.width-12);point.y=Mathf.Clamp(point.y,parent.rect.yMin+r.rect.height+12,parent.rect.yMax-12);r.localPosition=new Vector3(point.x,point.y,0);}
 public void Hide(Object source){if(owner==source)HideAll();}
 public void HideAll(){owner=null;if(panel!=null)panel.SetActive(false);}
}
}
