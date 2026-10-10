using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class EngravingModule : MonoBehaviour {
 public GameSession session;
 public EngravingDefinition definition;
 public void Engrave(Card card){
  bool fixedDefinition=definition!=null&&(definition.effectTable!=null&&!string.IsNullOrWhiteSpace(definition.effectId)||!session.Game.HasConfiguredEngravings);
  var template=fixedDefinition?definition.CreateEffect():null;
  if(session.Game.BuyUpgrade(card,true)){
   if(template!=null){card.Engraving=session.Game.GenerateEngraving(template);card.EngravingCode=card.Engraving.Code;}
   if(card.Engraving?.Effect==EngravingEffect.FollowUpDamage)card.EngravingDamage=card.Engraving.Value;
   session.Notify();
  }
 }
 public void Upgrade(Card card){if(session.Game.BuyUpgrade(card,false))session.Notify();}
}}
