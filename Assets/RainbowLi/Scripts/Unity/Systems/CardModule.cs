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
 public void Move(int slot,int direction){session.Game.MoveAction(slot,direction);session.Notify();}
 public void Remove(int slot){session.Game.RemoveAction(slot);session.Notify();}
}}
