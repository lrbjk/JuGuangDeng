using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class SequenceModule : MonoBehaviour {
 public GameSession session;
 public void EditPrimary(int index){session.Game.ChangeSequenceEffect(index,false);session.Notify();}
 public void EditSecondary(int index){session.Game.ChangeSequenceEffect(index,true);session.Notify();}
 public void ChangeDigit(int rule,int digit){session.Game.ChangeNumber(rule,digit);session.Notify();}
 public void Rebuild(int rule){session.Game.RestRebuild(rule);session.Notify();}
 public void BuyEffect(int rule){session.Game.BuyRule(rule);session.Notify();}
}}
