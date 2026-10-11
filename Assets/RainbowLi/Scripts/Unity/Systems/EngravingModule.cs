using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class EngravingModule : MonoBehaviour {
 public GameSession session;
 public EngravingDefinition definition;
 public DeckOperationView deck;
 public void Engrave(Card card){if(deck!=null)deck.OpenCard(DeckOperation.Engrave,card);}
 internal bool ExecuteEngraving(Card card){
  bool fixedDefinition=definition!=null&&(definition.effectTable!=null&&!string.IsNullOrWhiteSpace(definition.effectId)||!session.Game.HasConfiguredEngravings);
  var template=fixedDefinition?definition.CreateEffect():null;
  if(session.Game.BuyUpgrade(card,true)){
   if(template!=null){card.Engraving=session.Game.GenerateEngraving(template);card.EngravingCode=card.Engraving.Code;}
   if(card.Engraving?.Effect==EngravingEffect.FollowUpDamage)card.EngravingDamage=card.Engraving.Value;
   return true;
  }
  return false;
 }
 public void Upgrade(Card card){if(deck!=null)deck.OpenCard(DeckOperation.Upgrade,card);}
}}
