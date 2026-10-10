using UnityEngine;
namespace RainbowLi.UnityGame {
[CreateAssetMenu(menuName="RainbowLi/卡牌定义")]
public sealed class CardDefinition : ScriptableObject {
 public CardTableDefinition cardTable;
 public string rowId;
 [Range(1,6)] public int number = 1;
 public CardEffect effect;
 public CardEffectDefinition effectDefinition;
 [Min(0)] public int effectAmount;
 [Min(0)] public int baseBonus = 3, upgradeBonus = 2;
 public EngravingDefinition engraving;
 public CardEffectSpec[] GetEffectSpecs()=>cardTable!=null?cardTable.GetEffects(rowId):effectDefinition!=null?new[]{effectDefinition.CreateSpec()}:new[]{new CardEffectSpec{Effect=effect,UsesNumber=true}};
 public Card CreateCard(){if(cardTable!=null){var specs=GetEffectSpecs();if(specs.Length>1)throw new System.NotSupportedException(rowId+"：复合效果需要玩法程序接入。");var spec=specs[0];return new Card{Spec=spec,Number=number,Effect=spec.Effect,EffectAmount=spec.Min,BaseBonus=baseBonus,UpgradeBonus=upgradeBonus,EngravingDamage=engraving==null?5:engraving.ResolveExtraDamage()};}return new Card { Spec=effectDefinition==null?null:effectDefinition.CreateSpec(),EffectAmount=effectDefinition==null?0:effectAmount>0?effectAmount:effectDefinition.minValue,Number=number, Effect=effectDefinition==null?effect:effectDefinition.effect, BaseBonus=baseBonus, UpgradeBonus=upgradeBonus, EngravingDamage=engraving == null ? 5 : engraving.ResolveExtraDamage() };}
}}
