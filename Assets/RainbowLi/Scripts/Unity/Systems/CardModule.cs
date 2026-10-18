using UnityEngine;
using System.Linq;
namespace RainbowLi.UnityGame {
public sealed class CardModule : MonoBehaviour {
 public GameSession session;
 public CardEffectCatalog catalog;
 public void Apply(GameSetup setup){catalog?.Apply(setup); }
 public Card Selected {get; private set;}
 private int observedRound;
 private void OnEnable(){ session.Changed+=TrackRound; }
 private void OnDisable(){ session.Changed-=TrackRound; }
 private void TrackRound(){ if(session.Game!=null && (session.Game.Round!=observedRound || session.Game.Phase!=GamePhase.Planning)){ Selected=null; observedRound=session.Game.Round; } }
 public void Select(Card card){ if(session.Game.Phase!=GamePhase.Planning)return; Selected=Selected==card?null:card;if(Selected!=null)session.Game.Message=Selected.RulesText;session.Notify(); }
 public void Insert(int slot){ if(Selected!=null && session.Game.InsertCard(Selected,slot)){Selected=null;session.Notify();} }
 public bool Place(Card card,int slot){if(card==null||session.Game==null||!session.Game.InsertCard(card,slot))return false;Selected=null;session.Notify();return true;}
 public bool PlaceInBoardSlot(Card card,int slot,PokerActionLayout layout){
  var g=session.Game;if(g==null||g.Phase!=GamePhase.Planning||card==null||layout==null)return false;
  layout.EnsureBoardSlots();var moving=g.Timeline.Find(a=>!a.Enemy&&a.Card==card);if(!layout.IsSlotAvailable(slot,moving))return false;
  int target=g.Timeline.Count(a=>a!=moving&&a.BoardSlot<slot+1);
  if(moving==null){if(!g.InsertCard(card,target))return false;moving=g.Timeline.Find(a=>!a.Enemy&&a.Card==card);}
  else return MoveActionInBoardSlot(moving,slot,layout);
  moving.BoardSlot=slot+1;Selected=null;session.Notify();return true;
 }
 public bool CanMoveAction(BattleAction action){
  var g=session.Game;return g!=null&&g.Phase==GamePhase.Planning&&action!=null&&g.Timeline.Contains(action)
   &&!action.LockedPosition&&!action.PassiveAction&&!action.ContentCompletion&&!action.IsStatusTick&&!action.IsNumberOnly
   &&(action.Enemy||action.Card!=null);
 }
 public bool MoveActionInBoardSlot(BattleAction action,int slot,PokerActionLayout layout){
  var g=session.Game;if(!CanMoveAction(action)||layout==null||!layout.IsSlotAvailable(slot,action))return false;
  int from=g.Timeline.IndexOf(action),target=g.Timeline.Count(a=>a!=action&&a.BoardSlot<slot+1);
  if(from!=target){g.MoveAction(from,target-from);if(g.Timeline.IndexOf(action)!=target)return false;}
  action.BoardSlot=slot+1;Selected=null;session.Notify();return true;
 }
 public bool RepositionAction(BattleAction action,int insertionSlot){
  var g=session.Game;if(!CanMoveAction(action)||insertionSlot<0||insertionSlot>g.Timeline.Count)return false;
  int from=g.Timeline.IndexOf(action),to=insertionSlot>from?insertionSlot-1:insertionSlot;
  if(to==from)return true;
  g.MoveAction(from,to-from);Selected=null;session.Notify();return g.Timeline.IndexOf(action)==to;
 }
 public bool Reposition(Card card,int insertionSlot){
  var g=session.Game;if(g==null||card==null)return false;
  return RepositionAction(g.Timeline.Find(a=>!a.Enemy&&a.Card==card),insertionSlot);
 }
 public bool Withdraw(Card card){
  var g=session.Game;if(g==null||g.Phase!=GamePhase.Planning||card==null)return false;
  int at=g.Timeline.FindIndex(a=>!a.Enemy&&a.Card==card);if(at<0)return false;
  g.RemoveAction(at);Selected=null;session.Notify();return !g.Timeline.Any(a=>!a.Enemy&&a.Card==card);
 }
 public void Move(int slot,int direction){session.Game.MoveAction(slot,direction);session.Notify();}
 public void Remove(int slot){session.Game.RemoveAction(slot);session.Notify();}
}}
