using UnityEngine;
namespace RainbowLi.UnityGame {
public sealed class EngravingModule : MonoBehaviour {
 public GameSession session;
 public EngravingDefinition definition;
 public void Engrave(Card card){int damage=definition==null?(card==null?5:card.EngravingDamage):definition.ResolveExtraDamage();if(session.Game.BuyUpgrade(card,true)){card.EngravingDamage=damage;session.Notify();}}
 public void Upgrade(Card card){if(session.Game.BuyUpgrade(card,false))session.Notify();}
}}
