using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 [DisallowMultipleComponent]
 public sealed class LowHealthOverlay : MonoBehaviour {
  [Tooltip("低于此生命百分比时显示红色滤镜。")][Range(0,100)] public int thresholdPercent=40;
  [Tooltip("红色滤镜透明度。")][Range(0,1)] public float opacity=.18f;
  [Min(.01f)] public float fadeSeconds=.25f;
  public Image overlay;
  private float targetAlpha;
  private StartMenuView menu;
  public static LowHealthOverlay Ensure(GameView view){
   var effect=view.GetComponent<LowHealthOverlay>();
   if(effect==null)effect=view.gameObject.AddComponent<LowHealthOverlay>();
   effect.menu=view.GetComponent<StartMenuView>();effect.EnsureImage();return effect;
  }
  public void EnsureImage(){
   if(overlay!=null)return;
   var parent=GetComponentInParent<Canvas>();if(parent==null)return;
   var go=new GameObject("LowHealthFilter",typeof(RectTransform),typeof(Canvas),typeof(Image),typeof(LayoutElement));
   go.transform.SetParent(parent.rootCanvas.transform,false);
   var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
   // A dedicated sorting layer keeps the full-screen tint above panels opened later.
   var canvas=go.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=parent.rootCanvas.sortingOrder+100;
   go.GetComponent<LayoutElement>().ignoreLayout=true;
   overlay=go.GetComponent<Image>();overlay.raycastTarget=false;overlay.maskable=false;overlay.color=new Color(1,0,0,0);overlay.enabled=false;
  }
  public bool ShouldShow(int health,int maximum,bool inBattle){
   return inBattle&&maximum>0&&(long)health*100<(long)maximum*thresholdPercent;
  }
  public void Refresh(int health,int maximum,bool inBattle){
   EnsureImage();targetAlpha=ShouldShow(health,maximum,inBattle)?opacity:0;
  }
  private void LateUpdate(){Advance(Time.unscaledDeltaTime);}
  private void Advance(float deltaTime){
   if(overlay==null)return;
   float visibleTarget=menu!=null&&menu.IsOpen?0:targetAlpha;
   float alpha=Mathf.MoveTowards(overlay.color.a,visibleTarget,deltaTime*Mathf.Max(.01f,opacity)/Mathf.Max(.01f,fadeSeconds));
   overlay.color=new Color(1,0,0,alpha);overlay.enabled=alpha>0;
  }
  private void OnDisable(){targetAlpha=0;if(overlay!=null){overlay.color=new Color(1,0,0,0);overlay.enabled=false;}}
 }
}
