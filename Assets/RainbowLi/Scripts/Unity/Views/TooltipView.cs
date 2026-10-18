using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
public sealed class TooltipView:MonoBehaviour {
 public GameObject panel;public Text title,body;
 [Header("效果说明二级美术")]
 public bool illustrated;
 [Min(360),Tooltip("包含外发光的完整宽度，文字放在素材内部。")] public float artWidth=720;
 Object owner;Vector2 lastScreenPoint,lastParentSize;
 public void Show(Object source,string heading,string text,Vector2 screenPoint){
  owner=source;title.text=heading;body.text=text;panel.SetActive(true);panel.transform.SetAsLastSibling();
  lastScreenPoint=screenPoint;RefreshLayout();
 }
 void LateUpdate(){if(panel!=null&&panel.activeInHierarchy&&((RectTransform)panel.transform.parent).rect.size!=lastParentSize)RefreshLayout();}
 void RefreshLayout(){
  var r=(RectTransform)panel.transform;var parent=(RectTransform)r.parent;lastParentSize=parent.rect.size;
  if(illustrated){
   float width=Mathf.Min(artWidth,parent.rect.width-24),scale=width/874f;
   r.sizeDelta=new Vector2(width,r.sizeDelta.y);
   var image=panel.GetComponent<Image>();if(image!=null)image.pixelsPerUnitMultiplier=874f/width;
   Place(title,148*scale,124*scale,320*scale,52*scale);
   Place(body,180*scale,195*scale,500*scale,Mathf.Max(1,r.rect.height-372*scale));
   body.rectTransform.ForceUpdateRectTransforms();
   float height=Mathf.Clamp(Mathf.Max(width*505f/874f,body.preferredHeight+372*scale+8),180,parent.rect.height-24);
   r.sizeDelta=new Vector2(width,height);
   Place(body,180*scale,195*scale,500*scale,height-372*scale);
  }else{
   r.sizeDelta=new Vector2(r.sizeDelta.x,Mathf.Clamp(body.preferredHeight+90,180,parent.rect.height-24));
   body.rectTransform.sizeDelta=new Vector2(body.rectTransform.sizeDelta.x,r.rect.height-88);
  }
  var canvas=GetComponentInParent<Canvas>();RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,lastScreenPoint,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var point);
  point+=new Vector2(18,-18);point.x=Mathf.Clamp(point.x,parent.rect.xMin+12,parent.rect.xMax-r.rect.width-12);point.y=Mathf.Clamp(point.y,parent.rect.yMin+r.rect.height+12,parent.rect.yMax-12);r.localPosition=new Vector3(point.x,point.y,0);
 }
 static void Place(Text text,float x,float y,float width,float height){var r=text.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
 public void Hide(Object source){if(owner==source)HideAll();}
 public void HideAll(){owner=null;if(panel!=null)panel.SetActive(false);}
}
}
