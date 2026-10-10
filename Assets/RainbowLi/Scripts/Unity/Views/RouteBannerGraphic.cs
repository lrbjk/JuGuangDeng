using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
// A five-sided pennant rendered as a native, editable UGUI mesh.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RouteBannerGraphic : MaskableGraphic {
 [Range(0,.4f)] public float pointDepth=.18f;
 [Range(0,12)] public float borderWidth=2;
 public Color borderColor=new Color(.52f,.43f,.3f);
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();var r=rectTransform.rect;
  Vector2[] outer={new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin+r.height*pointDepth),new Vector2(r.center.x,r.yMin),new Vector2(r.xMin,r.yMin+r.height*pointDepth)};
  var center=r.center;vh.AddVert(center,color,Vector2.zero);
  for(int i=0;i<5;i++)vh.AddVert(outer[i],color,Vector2.zero);
  for(int i=0;i<5;i++)vh.AddTriangle(0,i+1,(i+1)%5+1);
  for(int i=0;i<5;i++){
   int next=(i+1)%5;var a=outer[i];var b=outer[next];var d=(center-(a+b)*.5f).normalized*borderWidth;int v=vh.currentVertCount;
   vh.AddVert(a,borderColor,Vector2.zero);vh.AddVert(b,borderColor,Vector2.zero);vh.AddVert(b+d,borderColor,Vector2.zero);vh.AddVert(a+d,borderColor,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);
  }
 }
}
}
