using UnityEngine;
namespace RainbowLi.UnityGame {
[CreateAssetMenu(menuName="RainbowLi/卡牌定义")]
public sealed class CardDefinition : ScriptableObject {
 [Range(1,6)] public int number = 1;
 public CardEffect effect;
 public CardEffectDefinition effectDefinition;
 [Min(0)] public int effectAmount;
 [Min(0)] public int baseBonus = 3, upgradeBonus = 2;
 public EngravingDefinition engraving;
 public Card CreateCard() => new Card { Spec=effectDefinition==null?null:effectDefinition.CreateSpec(),EffectAmount=effectDefinition==null?0:effectAmount>0?effectAmount:effectDefinition.minValue,Number=number, Effect=effectDefinition==null?effect:effectDefinition.effect, BaseBonus=baseBonus, UpgradeBonus=upgradeBonus, EngravingDamage=engraving == null ? 5 : engraving.ResolveExtraDamage() };
}}
