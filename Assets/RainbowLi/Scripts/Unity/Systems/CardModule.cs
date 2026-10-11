using UnityEngine;
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
 public bool Reposition(Card card,int insertionSlot){
  var g=session.Game;if(g==null||g.Phase!=GamePhase.Planning||card==null||insertionSlot<0||insertionSlot>g.Timeline.Count)return false;
  int from=g.Timeline.FindIndex(a=>!a.Enemy&&a.Card==card);if(from<0)return false;
  int to=insertionSlot>from?insertionSlot-1:insertionSlot;if(to==from)return true;
  g.MoveAction(from,to-from);Selected=null;session.Notify();return true;
 }
 public bool Withdraw(Card card){
  var g=session.Game;if(g==null||g.Phase!=GamePhase.Planning||card==null)return false;
  int at=g.Timeline.FindIndex(a=>!a.Enemy&&a.Card==card);if(at<0)return false;
  g.RemoveAction(at);Selected=null;session.Notify();return true;
 }
 public void Move(int slot,int direction){session.Game.MoveAction(slot,direction);session.Notify();}
 public void Remove(int slot){session.Game.RemoveAction(slot);session.Notify();}
}}
