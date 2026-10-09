using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class EngravingModule : MonoBehaviour {
 public GameSession session;
 public EngravingDefinition definition;
 public void Engrave(Card card){ if(session.Game.BuyUpgrade(card,true)){ if(definition!=null)card.EngravingDamage=definition.extraDamage; session.Notify(); } }
 public void Upgrade(Card card){if(session.Game.BuyUpgrade(card,false))session.Notify();}
}}
