using UnityEngine;
using UnityEngine.UI;

namespace RainbowLi.UnityGame {
// Native UGUI geometry: editable colours and rounded corners, no texture dependency.
[AddComponentMenu("RainbowLi/UI/Poker Table Graphic")]
public sealed class PokerTableGraphic : MaskableGraphic {
 [Min(0)] public float cornerRadius=18,borderWidth=2;
 public Color borderColor=new Color(.7f,.62f,.4f,.7f);
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();var r=rectTransform.rect;if(r.width<=0||r.height<=0)return;
  float border=Mathf.Clamp(borderWidth,0,Mathf.Min(r.width,r.height)*.5f);
  if(border>0)Fill(mesh,r,cornerRadius,borderColor);
  var inner=new Rect(r.x+border,r.y+border,r.width-border*2,r.height-border*2);
  if(inner.width>0&&inner.height>0)Fill(mesh,inner,Mathf.Max(0,cornerRadius-border),color);
 }
 private static void Fill(VertexHelper mesh,Rect rect,float radius,Color tint){
  radius=Mathf.Clamp(radius,0,Mathf.Min(rect.width,rect.height)*.5f);
  int start=mesh.currentVertCount;mesh.AddVert(rect.center,tint,new Vector2(.5f,.5f));
  const int segments=8;int count=4*(segments+1);
  for(int corner=0;corner<4;corner++){
   var center=new Vector2(corner==0||corner==3?rect.xMax-radius:rect.xMin+radius,corner<2?rect.yMax-radius:rect.yMin+radius);
   for(int step=0;step<=segments;step++){
    float angle=(corner*90f+step*90f/segments)*Mathf.Deg2Rad;
    var p=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
    mesh.AddVert(p,tint,new Vector2(Mathf.InverseLerp(rect.xMin,rect.xMax,p.x),Mathf.InverseLerp(rect.yMin,rect.yMax,p.y)));
   }
  }
  for(int i=0;i<count;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%count);
 }
}
}
