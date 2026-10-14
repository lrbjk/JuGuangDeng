using System;
using System.Collections.Generic;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/卡牌效果池")]
 public sealed class CardEffectCatalog : ScriptableObject {
  public CardEffectDefinition[] effects;
  public void Apply(GameSetup setup){
   setup.CardEffects.Clear();setup.CardEffectGroups.Clear();var ids=new HashSet<string>();
   if(effects==null)return;
   foreach(var definition in effects){if(definition==null)continue;if(!ids.Add(definition.effectId))throw new InvalidOperationException("卡牌效果编号重复："+definition.effectId);setup.CardEffects.Add(definition.CreateSpec());}
  }
 }
}
