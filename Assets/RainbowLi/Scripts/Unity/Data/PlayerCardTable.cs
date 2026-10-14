using System.Linq;
using UnityEngine;
namespace RainbowLi.UnityGame {
 [CreateAssetMenu(menuName="RainbowLi/配表/玩家卡牌表")]
 public sealed class PlayerCardTable : CardTableDefinition {
  public void Apply(GameSetup setup){
   ValidateTable(false);var groups=rows.Where(r=>r.enabled).Select(r=>new CardEffectGroup{Specs=r.EffectSpecs()}).ToArray();
   setup.CardEffects.Clear();setup.CardEffects.AddRange(groups.Select(g=>g.Primary.Copy()));setup.CardEffectGroups.Clear();setup.CardEffectGroups.AddRange(groups);
   if(!setup.RandomStartingDeck){
    var starter=rows.Where(r=>r.enabled&&r.starting).ToArray();if(starter.Length<6)throw new System.InvalidOperationException("手工起始牌组至少需要 6 行启用的起始牌。");
    setup.InitialDeck.Clear();setup.InitialDeck.AddRange(starter.Select(r=>new CardEffectGroup{Specs=r.EffectSpecs()}.CreateCard(r.number)));
   }
  }
 }
}
