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
  [Header("旗帜路线表现")]
  public bool perspectiveLayout;
  [Range(.1f,.5f)] public float depthShrink=.23f;
  [Min(100)] public float foregroundSpacing=430;
  public Text resources,healthValue,progress,tooltipTitle,tooltipBody;
  public GameObject tooltipPanel;
  public Slider healthBar;
  public RectTransform playerMarker;
  [Header("三列卡片选关布局")]
  public bool diagramLayout;
  [Min(100)] public float diagramCardWidth=278,diagramCardHeight=400;
  [Min(20)] public float diagramStripHeight=64;
  [Range(1,6)] public int diagramBackCards=4;
  private bool overview;
  public int FocusFloor=>levels.Run!=null&&levels.Run.Graph.CardDungeon?levels.Run.PlayerFloor+1:levels.Run!=null&&levels.Run.Graph.Stacked?levels.Run.Graph.Nodes.Where(n=>!levels.Run.Completed.Contains(n.id)).Select(n=>n.floor).DefaultIfEmpty(0).Min():overview||levels.Run==null||levels.Run.CurrentId<0?0:Mathf.Min(levels.Run.Graph.Floors-1,levels.Run.Graph.Find(levels.Run.CurrentId).floor+1);
  public int StackDepth(LevelNodeData n,RouteGraph graph){if(graph.CardDungeon)return Mathf.Max(0,n.floor-FocusFloor);if(n.kind==LevelKind.Boss)return levels.Run==null||levels.Run.IsCovered(n.id)?7:0;return graph.Nodes.Count(v=>v.kind!=LevelKind.Boss&&v.lane==n.lane&&v.floor<n.floor&&(levels.Run==null||!levels.Run.Completed.Contains(v.id)));}
  public float NodeScale(LevelNodeData n,RouteGraph graph)=>diagramLayout?1:graph.Stacked?Mathf.Max(.58f,1f/(1+StackDepth(n,graph)*.12f)):DepthScale(n.floor);
  public float DepthScale(int floor)=>Mathf.Max(.38f,1f/(1f+Mathf.Max(0,floor-FocusFloor)*depthShrink));
  private float MapHeight(RouteGraph graph){if(diagramLayout)return diagramCardHeight+diagramStripHeight*diagramBackCards+24;if(graph.Stacked)return overview?Mathf.Max(800,260+graph.Nodes.Where(n=>n.kind!=LevelKind.Boss).GroupBy(n=>n.lane).Select(g=>g.Count()).DefaultIfEmpty(0).Max()*65):800;float height=260;for(int f=FocusFloor;f<graph.Floors;f++)height+=140*DepthScale(f);return Mathf.Max(800,height);}
  public void ToggleOverview(){overview=!overview;displayedCurrent=-2;Refresh();}
  public void ShowTooltip(int id){
   if(tooltipPanel==null||levels.Run==null)return;var n=levels.Run.Graph.Find(id);if(n==null)return;
   if(levels.Run.Graph.CardDungeon&&!levels.Run.CanEnter(id)){HideTooltip();return;}
   if(levels.Run.Graph.Stacked&&(levels.Run.IsCovered(id)||levels.Run.IsAbandoned(id))){HideTooltip();return;}
   var p=levels.definition!=null?levels.definition.Presentation(id):null;
   tooltipTitle.text=p!=null&&!string.IsNullOrWhiteSpace(p.title)?p.title:LevelNodeView.KindName(n.kind);
   string body=n.kind==LevelKind.Combat?"击败敌人，获得金币并选择一张奖励卡牌。":n.kind==LevelKind.Elite?"挑战更强的敌人，获得额外金币与卡牌奖励。":n.kind==LevelKind.Rest?"恢复生命，或调整卡组与牌序。":n.kind==LevelKind.Shop?"消耗金币购买卡牌、升级和铭刻。":n.kind==LevelKind.Event?"探索遗址，选择金币或生命奖励。":"击败最终首领，完成本次收容。";
   tooltipBody.text=(p!=null&&!string.IsNullOrWhiteSpace(p.description)?p.description:body)+"\n\n"+(levels.Run.CanEnter(id)?"点击进入关卡":levels.Run.Completed.Contains(id)?"已完成此关卡":"完成前方关卡后显露");tooltipPanel.SetActive(true);
  }
  public void HideTooltip(){if(tooltipPanel!=null)tooltipPanel.SetActive(false);}
  private int displayedCurrent=-2;
  private RouteGraph shown;
  private readonly List<Image> lines=new List<Image>();
  private void OnEnable(){levels.session.Changed+=Refresh;Refresh();}
  private void OnDisable(){levels.session.Changed-=Refresh;}
  public Vector2 Position(LevelNodeData n,RouteGraph graph){int lanes=graph.Nodes.Max(v=>v.lane)+1;
   if(diagramLayout)return new Vector2(contentWidth/2+(n.lane-(lanes-1)/2f)*(diagramCardWidth+76),-(8+diagramStripHeight*(diagramBackCards-Mathf.Min(diagramBackCards,StackDepth(n,graph)))));if(!perspectiveLayout)return new Vector2(contentWidth/2+(n.lane-(lanes-1)/2f)*horizontalSpacing,-60-(graph.Floors-1-n.floor)*verticalSpacing);
   if(graph.CardDungeon)return new Vector2(contentWidth/2+(n.lane-(lanes-1)/2f)*Mathf.Min(foregroundSpacing,contentWidth/(lanes+.8f)),-(MapHeight(graph)-190-StackDepth(n,graph)*60));
   if(graph.Stacked)return new Vector2(n.kind==LevelKind.Boss?contentWidth/2:contentWidth/2+(n.lane-(lanes-1)/2f)*Mathf.Min(foregroundSpacing,contentWidth/(lanes+.8f)),-(n.kind==LevelKind.Boss&&StackDepth(n,graph)>0?90:MapHeight(graph)-140-StackDepth(n,graph)*65));
   float y=MapHeight(graph)-140;for(int f=FocusFloor;f<n.floor;f++)y-=140*DepthScale(f);
   return new Vector2(contentWidth/2+(n.lane-(lanes-1)/2f)*Mathf.Min(foregroundSpacing,contentWidth/(lanes+.8f))*DepthScale(n.floor),-y);
  }
  public void Refresh(){
   var run=levels.Run;if(run==null)return;
   eventPanel.SetActive(levels.session.Game.Phase==GamePhase.Event);
   mapRoot.SetActive(levels.session.Game.Phase==GamePhase.Map);
   if(!mapRoot.activeSelf)HideTooltip();
   var graph=run.Graph;bool regenerate=shown!=graph;
   if(legend!=null)legend.gameObject.SetActive(!diagramLayout&&!graph.IndependentStacks);
   content.sizeDelta=new Vector2(contentWidth,perspectiveLayout?MapHeight(graph):graph.Floors*verticalSpacing+110);
   foreach(var v in nodes)v.gameObject.SetActive(graph.Find(v.nodeId)!=null);
   foreach(var n in graph.Nodes){
    var view=nodes.Find(v=>v.nodeId==n.id);
    if(view==null){view=Instantiate(nodePrefab,content);view.name="RouteNode_"+n.id;view.nodeId=n.id;view.authoredFloor=n.floor;view.authoredLane=n.lane;((RectTransform)view.transform).anchoredPosition=Position(n,graph);nodes.Add(view);}
    if(view.authoredFloor!=n.floor||view.authoredLane!=n.lane){((RectTransform)view.transform).anchoredPosition=Position(n,graph);view.authoredFloor=n.floor;view.authoredLane=n.lane;}
    view.gameObject.SetActive(true);int id=n.id;view.map=this;view.Bind(n,run,()=>levels.SelectNode(id));
    if(diagramLayout){view.gameObject.SetActive(n.kind!=LevelKind.Boss&&!run.IsAbandoned(n.id)&&!run.Completed.Contains(n.id)&&(graph.IndependentStacks||n.floor>=FocusFloor)&&StackDepth(n,graph)<=diagramBackCards);((RectTransform)view.transform).anchoredPosition=Position(n,graph);((RectTransform)view.transform).sizeDelta=new Vector2(diagramCardWidth,diagramCardHeight);view.SetDepth(1);view.SetStackDepth(StackDepth(n,graph));}
    else if(perspectiveLayout){view.gameObject.SetActive(graph.Stacked?!run.IsAbandoned(n.id)&&!run.Completed.Contains(n.id)&&(overview||n.kind==LevelKind.Boss||StackDepth(n,graph)<=6):n.floor>=FocusFloor);((RectTransform)view.transform).anchoredPosition=Position(n,graph);view.SetDepth(NodeScale(n,graph));if(graph.Stacked)view.SetStackDepth(StackDepth(n,graph));}
   }
   if(perspectiveLayout||diagramLayout)foreach(var node in nodes.OrderByDescending(n=>StackDepth(graph.Find(n.nodeId)??new LevelNodeData(),graph)))node.transform.SetAsLastSibling();
   if(regenerate){
    foreach(var line in lines)Destroy(line.gameObject);lines.Clear();
    foreach(Transform child in connections){child.gameObject.SetActive(false);Destroy(child.gameObject);}
    foreach(var n in graph.Nodes)foreach(int id in n.next){var go=new GameObject("Connection_"+n.id+"_"+id,typeof(RectTransform),typeof(Image));go.transform.SetParent(connections,false);var image=go.GetComponent<Image>();image.raycastTarget=false;lines.Add(image);}
    shown=graph;
   }
   connections.gameObject.SetActive(!graph.Stacked);
   int index=0;
   foreach(var n in graph.Nodes)foreach(int id in n.next){var image=lines[index++];var from=(RectTransform)nodes.Find(v=>v.nodeId==n.id).transform;var to=(RectTransform)nodes.Find(v=>v.nodeId==id).transform;var rect=(RectTransform)image.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);Vector2 a=from.anchoredPosition,b=to.anchoredPosition,d=b-a;rect.anchoredPosition=(a+b)/2;rect.sizeDelta=new Vector2(d.magnitude,3);rect.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);image.color=run.Completed.Contains(n.id)&&run.Completed.Contains(id)?new Color(.92f,.75f,.4f):run.CurrentId==n.id?new Color(.35f,.85f,.78f):new Color(.28f,.37f,.3f);}
   if(perspectiveLayout){foreach(var image in lines){image.color=new Color(.35f,.3f,.22f,.38f);image.rectTransform.sizeDelta=new Vector2(image.rectTransform.sizeDelta.x,2);}
    int edge=0;foreach(var n in graph.Nodes)foreach(int id in n.next){var image=lines[edge++];image.gameObject.SetActive(n.floor>=FocusFloor);if(run.CanEnter(n.id))image.color=new Color(.64f,.49f,.28f,.65f);}
    var g=levels.session.Game;if(resources!=null)resources.text="金币  "+g.Coins;
    if(healthValue!=null)healthValue.text=g.Health+" / "+g.HealthLimit;
    if(healthBar!=null){healthBar.maxValue=g.HealthLimit;healthBar.value=g.Health;}
    if(progress!=null)progress.text=string.Join("  /  ",Enumerable.Range(0,graph.Floors).Select(f=>"<color="+(run.Completed.Any(id=>graph.Find(id).floor==f)?"#B69750":f==FocusFloor?"#F3DFC0":"#60615D")+">"+(f==graph.Floors-1?"首领":(f+1).ToString("00"))+"</color>"));
   }
   title.text=graph.CardDungeon?"收容地牢":perspectiveLayout?"收容之路":"分叉路线 / 第 "+(run.CurrentId<0?1:graph.Find(run.CurrentId).floor+2)+" 层";
   if(graph.Stacked&&progress!=null)progress.text="已完成  "+run.Completed.Count+"  关    /    "+(run.Graph.Nodes.Any(n=>n.kind!=LevelKind.Boss&&!run.IsAbandoned(n.id)&&!run.Completed.Contains(n.id))?"首领尚未显露":"首领已显露");
   legend.text=perspectiveLayout?(overview?"全路线预览 · 金色边框为可选关卡 · 滚轮查看远处路线":"选择一条道路  /  第 "+(FocusFloor+1)+" 层 · 共 "+graph.Floors+" 层"):"从下往上前进 · 绿色可选 · 金色已完成 · 沿连线选择下一层";
   if(graph.Stacked)legend.text=run.ChosenLane<0?"选择一个起点  /  选定后沿该列前进":"完成当前关卡  /  只露出正后方的下一关";
   if(graph.CardDungeon){progress.text="当前位置  "+(run.PlayerFloor<0?"入口":"第 "+(run.PlayerFloor+1)+" 行")+" / 第 "+(run.PlayerLane+1)+" 列     ·     已前进 "+run.Completed.Count+" 步";legend.text="选择前方左 / 中 / 右卡片  ·  完成后移动位置";if(playerMarker!=null){playerMarker.gameObject.SetActive(true);int lanes=graph.Nodes.Max(n=>n.lane)+1;playerMarker.anchoredPosition=new Vector2(contentWidth/2+(run.PlayerLane-(lanes-1)/2f)*Mathf.Min(foregroundSpacing,contentWidth/(lanes+.8f)),-(MapHeight(graph)-25));playerMarker.SetAsLastSibling();}}
   if(diagramLayout){if(playerMarker!=null)playerMarker.gameObject.SetActive(false);progress.gameObject.SetActive(false);legend.text=graph.IndependentStacks?"三列独立推进 · 完成一张关卡，只补上该列的下一张": "当前位置：第 "+(run.PlayerLane+1)+" 列  ·  选择前方相邻卡片，完成关卡后向前移动";scroll.vertical=false;}
   if(graph.IndependentStacks){title.text="收容关卡";legend.text="三列独立推进 · 完成一张关卡，只补上该列的下一张";if(playerMarker!=null)playerMarker.gameObject.SetActive(false);}
   if(regenerate)scroll.verticalNormalizedPosition=0;
   if(followCurrentFloor&&displayedCurrent!=run.CurrentId){Canvas.ForceUpdateCanvases();int floor=run.CurrentId<0?0:graph.Find(run.CurrentId).floor;float overflow=Mathf.Max(1,content.rect.height-scroll.viewport.rect.height);scroll.verticalNormalizedPosition=perspectiveLayout?0:Mathf.Clamp01((floor*verticalSpacing-100)/overflow);displayedCurrent=run.CurrentId;}
  }
 }
}
