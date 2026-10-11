using UnityEngine;
using UnityEngine.EventSystems;

namespace RainbowLi.UnityGame {
[DisallowMultipleComponent]
public sealed class HandCardDrag : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,ICancelHandler {
 private BattlePanelView panel;
 private Card card;
 private BattleAction action;
 private ContainmentGame game;
 private int round,pointer;
 private RectTransform ghost,canvasRect;
 private CanvasGroup sourceGroup;
 private float originalAlpha;
 public bool IsDragging {get;private set;}
 public void Bind(BattlePanelView owner,Card value,BattleAction placedAction=null){if(IsDragging&&(panel!=owner||card!=value||action!=placedAction||!Valid()))CancelDrag();panel=owner;card=value;action=placedAction;}
 private bool CanDrag()=>panel!=null&&(action==null?panel.CanDrag(card):panel.CanDragAction(action));
 private bool Valid()=>panel!=null&&panel.view.session.Game==game&&game.Round==round&&CanDrag();
 public void OnBeginDrag(PointerEventData e){
  if(!isActiveAndEnabled||IsDragging||e.button!=PointerEventData.InputButton.Left||!CanDrag())return;
  var canvas=GetComponentInParent<Canvas>();if(canvas==null)return;canvas=canvas.rootCanvas;canvasRect=canvas.transform as RectTransform;if(canvasRect==null)return;
  game=panel.view.session.Game;round=game.Round;pointer=e.pointerId;
  if(panel.view.tooltip!=null)panel.view.tooltip.HideAll();
  var source=GetComponent<CardView>();var preview=Instantiate(source,canvasRect);preview.name=action==null?"DraggedHandCard":"DraggedActionCard";
  var drag=preview.GetComponent<HandCardDrag>();if(drag!=null)drag.enabled=false;
  preview.SetPresentationOnly();ghost=(RectTransform)preview.transform;
  ghost.localScale=new Vector3(transform.lossyScale.x/canvasRect.lossyScale.x,transform.lossyScale.y/canvasRect.lossyScale.y,1);ghost.localRotation=Quaternion.identity;
  var previewGroup=preview.GetComponent<CanvasGroup>();if(previewGroup!=null){previewGroup.alpha=1;previewGroup.blocksRaycasts=false;}
  sourceGroup=GetComponent<CanvasGroup>();if(sourceGroup==null)sourceGroup=gameObject.AddComponent<CanvasGroup>();originalAlpha=sourceGroup.alpha;sourceGroup.alpha=panel.draggedHandOpacity;
  IsDragging=true;e.eligibleForClick=false;OnDrag(e);
 }
 public void OnDrag(PointerEventData e){
  if(!IsDragging||e.pointerId!=pointer)return;if(!Valid()){CancelDrag();return;}
  if(RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect,e.position,e.pressEventCamera,out var world))ghost.position=world-ghost.TransformVector(ghost.rect.center);
  ghost.SetAsLastSibling();
  if(panel.TryGetDropSlot(e.position,e.pressEventCamera,out int slot))panel.ShowInsertion(slot);else panel.HideInsertion();
 }
 public void OnEndDrag(PointerEventData e){
  if(!IsDragging||e.pointerId!=pointer)return;
  bool withdraw=Valid()&&action!=null&&panel.IsHandDrop(e.position,e.pressEventCamera);
  bool valid=!withdraw&&Valid()&&panel.TryGetDropSlot(e.position,e.pressEventCamera,out _);
  int slot=-1;if(valid)panel.TryGetDropSlot(e.position,e.pressEventCamera,out slot);
  var owner=panel;var dropped=card;bool reposition=action!=null;CancelDrag();e.eligibleForClick=false;
  if(withdraw)owner.view.cards.Withdraw(dropped);
  else if(valid){if(reposition)owner.view.cards.Reposition(dropped,slot);else owner.view.cards.Place(dropped,slot);}
 }
 public void OnCancel(BaseEventData e){CancelDrag();}
 private void Update(){if(IsDragging&&!Valid())CancelDrag();}
 private void LateUpdate(){if(IsDragging&&sourceGroup!=null)sourceGroup.alpha=panel.draggedHandOpacity;}
 public void CancelDrag(){
  if(!IsDragging)return;IsDragging=false;
  if(sourceGroup!=null)sourceGroup.alpha=originalAlpha;
  if(ghost!=null){ghost.gameObject.SetActive(false);Destroy(ghost.gameObject);}ghost=null;
  if(panel!=null)panel.HideInsertion();
 }
 private void OnDisable(){CancelDrag();}
}
}
