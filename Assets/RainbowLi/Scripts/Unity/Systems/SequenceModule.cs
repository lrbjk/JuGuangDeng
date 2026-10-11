using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class SequenceModule : MonoBehaviour {
 public GameSession session;
 public SequenceEditorView editor;
 public bool CanAdjust=>editor!=null&&editor.IsOpen&&session.Game!=null&&session.Game.Phase==GamePhase.Map;
 public void EditPrimary(int index){if(!CanAdjust)return;session.Game.ChangeSequenceEffect(index,false);session.Notify();}
 public void EditSecondary(int index){if(!CanAdjust)return;session.Game.ChangeSequenceEffect(index,true);session.Notify();}
 public void ChangeDigit(int rule,int digit){if(!CanAdjust)return;session.Game.ChangeNumber(rule,digit);session.Notify();}
 public void EquipEffect(int rule,bool secondary,int availableIndex){if(!CanAdjust)return;if(session.Game.EquipSequenceEffect(rule,secondary,availableIndex))session.Notify();}
 public void OpenAdjustment(int rule){if(editor!=null)editor.Open(rule);}
 public void Rebuild(int rule){if(editor==null||!editor.IsOpen)return;session.Game.RestRebuild(rule);session.Notify();}
 public void BuyEffect(int rule){if(editor==null||!editor.IsOpen)return;session.Game.BuyRule(rule);session.Notify();}
}}
