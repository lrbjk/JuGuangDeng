using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace RainbowLi.UnityGame {
 public sealed class RouteMapView : MonoBehaviour {
  public LevelModule levels;
  public ScrollRect scroll;
  public RectTransform content,connections;
  public LevelNodeView nodePrefab;
  public List<LevelNodeView> nodes=new List<LevelNodeView>();
  public Text title,legend;
  public GameObject eventPanel,mapRoot;
  [Min(50)] public float verticalSpacing=125,horizontalSpacing=210;
  [Min(50)] public float contentWidth=1020;
  public bool followCurrentFloor=true;
  private int displayedCurrent=-2;
  private RouteGraph shown;
  private readonly List<Image> lines=new List<Image>();
  private void OnEnable(){levels.session.Changed+=Refresh;Refresh();}
  private void OnDisable(){levels.session.Changed-=Refresh;}
  public Vector2 Position(LevelNodeData n,RouteGraph graph){int lanes=graph.Nodes.Max(v=>v.lane)+1;return new Vector2(contentWidth/2+(n.lane-(lanes-1)/2f)*horizontalSpacing,-60-(graph.Floors-1-n.floor)*verticalSpacing);}
  public void Refresh(){
   var run=levels.Run;if(run==null)return;
   eventPanel.SetActive(levels.session.Game.Phase==GamePhase.Event);
   mapRoot.SetActive(levels.session.Game.Phase==GamePhase.Map);
   var graph=run.Graph;bool regenerate=shown!=graph;
   content.sizeDelta=new Vector2(contentWidth,graph.Floors*verticalSpacing+110);
   foreach(var v in nodes)v.gameObject.SetActive(graph.Find(v.nodeId)!=null);
   foreach(var n in graph.Nodes){
    var view=nodes.Find(v=>v.nodeId==n.id);
    if(view==null){view=Instantiate(nodePrefab,content);view.name="RouteNode_"+n.id;view.nodeId=n.id;view.authoredFloor=n.floor;view.authoredLane=n.lane;((RectTransform)view.transform).anchoredPosition=Position(n,graph);nodes.Add(view);}
    if(view.authoredFloor!=n.floor||view.authoredLane!=n.lane){((RectTransform)view.transform).anchoredPosition=Position(n,graph);view.authoredFloor=n.floor;view.authoredLane=n.lane;}
    view.gameObject.SetActive(true);int id=n.id;view.Bind(n,run,()=>levels.SelectNode(id));
   }
   if(regenerate){
    foreach(var line in lines)Destroy(line.gameObject);lines.Clear();
    foreach(Transform child in connections){child.gameObject.SetActive(false);Destroy(child.gameObject);}
    foreach(var n in graph.Nodes)foreach(int id in n.next){var go=new GameObject("Connection_"+n.id+"_"+id,typeof(RectTransform),typeof(Image));go.transform.SetParent(connections,false);var image=go.GetComponent<Image>();image.raycastTarget=false;lines.Add(image);}
    shown=graph;
   }
   int index=0;
   foreach(var n in graph.Nodes)foreach(int id in n.next){var image=lines[index++];var from=(RectTransform)nodes.Find(v=>v.nodeId==n.id).transform;var to=(RectTransform)nodes.Find(v=>v.nodeId==id).transform;var rect=(RectTransform)image.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);Vector2 a=from.anchoredPosition,b=to.anchoredPosition,d=b-a;rect.anchoredPosition=(a+b)/2;rect.sizeDelta=new Vector2(d.magnitude,3);rect.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);image.color=run.Completed.Contains(n.id)&&run.Completed.Contains(id)?new Color(.92f,.75f,.4f):run.CurrentId==n.id?new Color(.35f,.85f,.78f):new Color(.28f,.37f,.3f);}
   title.text="分叉路线 / 第 "+(run.CurrentId<0?1:graph.Find(run.CurrentId).floor+2)+" 层";
   legend.text="从下往上前进 · 绿色可选 · 金色已完成 · 沿连线选择下一层";
   if(regenerate)scroll.verticalNormalizedPosition=0;
   if(followCurrentFloor&&displayedCurrent!=run.CurrentId){Canvas.ForceUpdateCanvases();int floor=run.CurrentId<0?0:graph.Find(run.CurrentId).floor;float overflow=Mathf.Max(1,content.rect.height-scroll.viewport.rect.height);scroll.verticalNormalizedPosition=Mathf.Clamp01((floor*verticalSpacing-100)/overflow);displayedCurrent=run.CurrentId;}
  }
 }
}
