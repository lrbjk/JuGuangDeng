using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace RainbowLi.UnityGame {
 public sealed class LevelNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {
  public int nodeId,authoredFloor,authoredLane;
  public Button button;
  public Text label;
  public Image face;
  public RouteBannerGraphic banner;
  public RouteIconGraphic icon;
  public Image customIcon;
  public Text stateLabel;
  public RouteMapView map;
  private Vector3 baseScale=Vector3.one;
  private bool hover;
  public static string KindName(LevelKind kind)=>kind==LevelKind.Combat?"战斗":kind==LevelKind.Elite?"精英":kind==LevelKind.Rest?"休息":kind==LevelKind.Shop?"商店":kind==LevelKind.Event?"事件":"首领";
  public void SetDepth(float scale){baseScale=Vector3.one*scale;transform.localScale=baseScale;}
  public void SetStackDepth(int depth){bool front=depth==0;label.gameObject.SetActive(front);stateLabel.gameObject.SetActive(front);banner.raycastTarget=front&&button.interactable;float width=((RectTransform)transform).rect.width;foreach(var g in new Graphic[]{icon,customIcon}){g.transform.localScale=Vector3.one*(front?1:.72f);((RectTransform)g.transform).anchoredPosition=new Vector2(width*.5f-(front?32:23),front?-(banner.pointDepth<=.01f?75:31):-10);}}
  private void Update(){if(banner==null)return;float pulse=button.interactable?Mathf.Sin(Time.unscaledTime*2.2f+nodeId)*.018f:0;transform.localScale=Vector3.Lerp(transform.localScale,baseScale*(1+(hover?.06f:pulse)),Time.unscaledDeltaTime*10);}
  public void OnPointerEnter(PointerEventData e){hover=true;map?.ShowTooltip(nodeId);}
  public void OnPointerExit(PointerEventData e){hover=false;map?.HideTooltip();}
  public void OnSelect(BaseEventData e){OnPointerEnter(null);}
  public void OnDeselect(BaseEventData e){OnPointerExit(null);}
  private UnityAction listener;
  public void Bind(LevelNodeData node,RouteRun run,Action click){
   if(listener!=null)button.onClick.RemoveListener(listener);listener=()=>click();button.onClick.AddListener(listener);
   bool visited=run.Completed.Contains(node.id),available=run.CanEnter(node.id);
   button.interactable=available;
   string name=KindName(node.kind);
   if(banner!=null){
    var presentation=map!=null&&map.levels.definition!=null?map.levels.definition.Presentation(node.id):null;
    label.text=presentation!=null&&!string.IsNullOrWhiteSpace(presentation.title)?presentation.title:name;
    stateLabel.text=visited?"已完成":available?(run.Graph.CardDungeon?"移动到这里":"进入关卡"):run.Graph.CardDungeon&&!run.IsCovered(node.id)?"无法到达":"";
    banner.color=visited?new Color(.23f,.21f,.16f):available?(node.kind==LevelKind.Elite||node.kind==LevelKind.Boss?new Color(.32f,.12f,.15f):new Color(.22f,.19f,.18f)):new Color(.09f,.10f,.11f);
    banner.borderColor=available?new Color(.78f,.65f,.42f):visited?new Color(.42f,.35f,.23f):new Color(.22f,.23f,.23f);banner.SetVerticesDirty();
    icon.kind=node.kind;icon.color=available?new Color(.97f,.9f,.73f):visited?new Color(.59f,.48f,.28f):new Color(.38f,.39f,.38f);icon.SetVerticesDirty();
    customIcon.sprite=presentation?.icon;customIcon.gameObject.SetActive(customIcon.sprite!=null);icon.gameObject.SetActive(customIcon.sprite==null);
    label.color=available?new Color(.97f,.9f,.73f):new Color(.51f,.51f,.47f);
    return;
   }
   label.text=name+"\n"+(visited?"已完成":available?"可选择":"未解锁");
   face.color=visited?new Color(.42f,.37f,.22f):available?new Color(.15f,.48f,.36f):new Color(.19f,.24f,.23f);
  }
 }
}
