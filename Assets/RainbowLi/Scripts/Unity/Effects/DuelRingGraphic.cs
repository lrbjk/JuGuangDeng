using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
// Native UGUI mesh: no texture or external tween package required.
public sealed class DuelRingGraphic : MaskableGraphic {
 [Range(1,16)] public float thickness=3;
 [Range(24,96)] public int segments=64;
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
  float inner=Mathf.Max(0,radius-thickness);var center=rectTransform.rect.center;
  for(int i=0;i<=segments;i++){
   float a=2*Mathf.PI*i/segments;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
   vh.AddVert(center+d*radius,color,Vector2.zero);vh.AddVert(center+d*inner,color,Vector2.one);
   if(i>0){int n=i*2;vh.AddTriangle(n-2,n-1,n);vh.AddTriangle(n,n-1,n+1);}
  }
 }
}
}
