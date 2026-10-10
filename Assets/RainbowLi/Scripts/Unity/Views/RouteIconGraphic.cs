using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RouteIconGraphic : MaskableGraphic {
 public LevelKind kind;
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();var r=rectTransform.rect;float s=Mathf.Min(r.width,r.height)/60;Vector2 c=r.center;
  System.Action<Vector2,Vector2,float> line=(a,b,w)=>{a=c+a*s;b=c+b*s;var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*w*s*.5f;int v=vh.currentVertCount;vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddVert(a-n,color,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);};
  if(kind==LevelKind.Combat){line(new Vector2(-20,-20),new Vector2(20,23),5);line(new Vector2(20,-20),new Vector2(-20,23),5);line(new Vector2(-22,-7),new Vector2(-6,-23),4);line(new Vector2(22,-7),new Vector2(6,-23),4);}
  else if(kind==LevelKind.Rest){line(new Vector2(-23,-20),new Vector2(23,-20),5);line(new Vector2(-15,-20),new Vector2(0,8),5);line(new Vector2(15,-20),new Vector2(0,8),5);line(new Vector2(-5,3),new Vector2(-10,16),4);line(new Vector2(-10,16),new Vector2(-5,27),3);line(new Vector2(8,2),new Vector2(13,15),4);}
  else if(kind==LevelKind.Shop){line(new Vector2(-19,9),new Vector2(19,9),4);line(new Vector2(-19,9),new Vector2(-16,-22),4);line(new Vector2(-16,-22),new Vector2(16,-22),4);line(new Vector2(16,-22),new Vector2(19,9),4);line(new Vector2(-10,9),new Vector2(-10,23),4);line(new Vector2(-10,23),new Vector2(10,23),4);line(new Vector2(10,23),new Vector2(10,9),4);line(new Vector2(0,0),new Vector2(0,-12),5);}
  else if(kind==LevelKind.Event){line(new Vector2(0,26),new Vector2(23,0),3);line(new Vector2(23,0),new Vector2(0,-26),3);line(new Vector2(0,-26),new Vector2(-23,0),3);line(new Vector2(-23,0),new Vector2(0,26),3);line(new Vector2(0,13),new Vector2(0,-3),5);line(new Vector2(0,-12),new Vector2(0,-15),5);}
  else {line(new Vector2(-18,16),new Vector2(18,16),5);line(new Vector2(-18,16),new Vector2(-18,-5),5);line(new Vector2(18,16),new Vector2(18,-5),5);line(new Vector2(-18,-5),new Vector2(-8,-18),5);line(new Vector2(18,-5),new Vector2(8,-18),5);line(new Vector2(-8,-18),new Vector2(8,-18),5);line(new Vector2(-9,5),new Vector2(-5,5),7);line(new Vector2(5,5),new Vector2(9,5),7);line(new Vector2(-18,16),new Vector2(-27,26),4);line(new Vector2(18,16),new Vector2(27,26),4);if(kind==LevelKind.Boss){line(new Vector2(-13,24),new Vector2(0,32),4);line(new Vector2(0,32),new Vector2(13,24),4);}}
 }
}
}
