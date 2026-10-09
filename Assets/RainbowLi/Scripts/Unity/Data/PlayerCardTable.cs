using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/配表/玩家卡牌表")]
 public sealed class PlayerCardTable : CardTableDefinition {
  public void Apply(GameSetup setup){
   ValidateTable(false);setup.CardEffects.Clear();setup.CardEffects.AddRange(rows.Where(r=>r.enabled).Select(r=>r.Spec()));
   if(!setup.RandomStartingDeck){
    var starter=rows.Where(r=>r.enabled&&r.starting).ToArray();if(starter.Length<6)throw new System.InvalidOperationException("手工起始牌组至少需要 6 行启用的起始牌。");
    setup.InitialDeck.Clear();setup.InitialDeck.AddRange(starter.Select(r=>new Card {Number=r.number,Effect=r.effect,Spec=r.Spec(),EffectAmount=r.min,BaseBonus=0,UpgradeBonus=r.effect==CardEffect.DamageCap||r.effect==CardEffect.Reciprocity||r.effect==CardEffect.Disorder?0:2}));
   }
  }
 }
}
