using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
namespace RainbowLi.UnityGame {
[CreateAssetMenu(menuName="RainbowLi/卡牌定义")]
public sealed class CardDefinition : ScriptableObject {
 [Header("配表绑定（优先于手工效果）")]
 [Tooltip("选择玩家或敌人卡牌表。绑定后使用行 ID 对应的全部启用效果。")]
 public CardTableDefinition cardTable;
 [Tooltip("卡牌表中的唯一行 ID。数值采用表格范围下限，创建后预览和执行不会重新抽取。")]
 public string rowId;
 [Range(1,6)] public int number = 1;
 [Header("未绑定配表时的手工效果")]
 public CardEffect effect;
 public CardEffectDefinition effectDefinition;
 [Min(0)] public int effectAmount;
 [Min(0)] public int baseBonus = 3, upgradeBonus = 2;
 public EngravingDefinition engraving;
 [Tooltip("未绑定配表时使用。非空时优先于单效果配置；每次创建会复制效果。")]
 public CardEffectInstance[] orderedEffects;
 public CardEffectSpec[] GetEffectSpecs(){
  if(cardTable!=null)return cardTable.GetEffects(rowId);
  if(orderedEffects!=null&&orderedEffects.Length>0)return orderedEffects.Select(e=>{
   if(e==null||e.Spec==null)throw new InvalidOperationException("卡牌手工复合效果缺少配置。");
   return e.Spec.Copy();
  }).ToArray();
  return effectDefinition!=null?new[]{effectDefinition.CreateSpec()}:new[]{new CardEffectSpec{Effect=effect,UsesNumber=true}};
 }
 public Card CreateCard(){
  var card=new Card {Effects=cardTable!=null||orderedEffects==null?new List<CardEffectInstance>():orderedEffects.Select(e=>e==null?throw new InvalidOperationException("卡牌手工复合效果缺少配置。"):e.Copy()).ToList(),Spec=effectDefinition==null?null:effectDefinition.CreateSpec(),EffectAmount=effectDefinition==null?0:effectAmount>0?effectAmount:effectDefinition.minValue,Number=number,Effect=effectDefinition==null?effect:effectDefinition.effect,BaseBonus=baseBonus,UpgradeBonus=upgradeBonus,Engraved=engraving!=null,Engraving=engraving?.CreateEffect(),EngravingCode=engraving==null?null:engraving.effectId,EngravingDamage=engraving==null?5:engraving.ResolveExtraDamage()};
  if(cardTable!=null){
   var specs=GetEffectSpecs();
   card.Effects=specs.Select(s=>new CardEffectInstance {Spec=s.Copy(),BaseX=s.Min,Multiplier=1,UpgradeBonus=upgradeBonus}).ToList();
   card.Spec=specs[0].Copy();card.Effect=specs[0].Effect;card.EffectAmount=specs[0].Min;
  }
  return card;
 }
}}
